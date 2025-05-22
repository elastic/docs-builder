// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.LinkIndex;
using Elastic.Markdown.IO;
using Microsoft.Extensions.Logging;
using ProcNet;
using ProcNet.Std;

namespace Documentation.Assembler.Sourcing;

public class AssemblerRepositorySourcer(ILoggerFactory logger, AssembleContext context)
{
	private readonly ILogger<AssemblerRepositorySourcer> _logger = logger.CreateLogger<AssemblerRepositorySourcer>();

	private AssemblyConfiguration Configuration => context.Configuration;
	private PublishEnvironment PublishEnvironment => context.Environment;

	private RepositorySourcer RepositorySourcer => new(logger, context.CheckoutDirectory, context.ReadFileSystem, context.Collector);

	public IReadOnlyCollection<Checkout> GetAll()
	{
		var fs = context.ReadFileSystem;
		var repositories = Configuration.ReferenceRepositories.Values.Concat<Repository>([Configuration.Narrative]);
		var checkouts = new List<Checkout>();
		foreach (var repo in repositories)
		{
			var checkoutFolder = fs.DirectoryInfo.New(Path.Combine(context.CheckoutDirectory.FullName, repo.Name));
			var checkout = new Checkout
			{
				Repository = repo,
				Directory = checkoutFolder,
				//TODO read from links.json and ensure we check out exactly that git reference
				//+ validate that git reference belongs to the appropriate branch
				HeadReference = Guid.NewGuid().ToString("N")
			};
			checkouts.Add(checkout);
		}

		return checkouts;
	}

	public async Task<IReadOnlyCollection<Checkout>> CloneAll(bool fetchLatest, Cancel ctx = default)
	{
		_logger.LogInformation("Cloning all repositories for environment {EnvironmentName} using '{ContentSourceStrategy}' content sourcing strategy",
			PublishEnvironment.Name,
			PublishEnvironment.ContentSource.ToStringFast(true)
		);
		var checkouts = new ConcurrentBag<Checkout>();

		ILinkIndexReader linkIndexReader = Aws3LinkIndexReader.CreateAnonymous();
		var linkRegistry = await linkIndexReader.GetRegistry(ctx);

		var repositories = new Dictionary<string, Repository>(Configuration.ReferenceRepositories)
		{
			{ NarrativeRepository.RepositoryName, Configuration.Narrative }
		};

		await Parallel.ForEachAsync(repositories,
			new ParallelOptions
			{
				CancellationToken = ctx,
				MaxDegreeOfParallelism = Environment.ProcessorCount
			}, async (repo, c) =>
			{
				await Task.Run(() =>
				{
					if (!linkRegistry.Repositories.TryGetValue(repo.Key, out var entry))
					{
						context.Collector.EmitError("", $"'{repo.Key}' does not exist in link index");
						return;
					}
					var branch = repo.Value.GetBranch(PublishEnvironment.ContentSource);
					var gitRef = branch;
					if (!fetchLatest)
					{
						if (!entry.TryGetValue(branch, out var entryInfo))
						{
							context.Collector.EmitError("", $"'{repo.Key}' does not have a '{branch}' entry in link index");
							return;
						}
						gitRef = entryInfo.GitReference;
					}
					checkouts.Add(RepositorySourcer.CloneRef(repo.Value, gitRef, fetchLatest));
				}, c);
			}).ConfigureAwait(false);
		return checkouts;
	}
}


public class RepositorySourcer(ILoggerFactory logger, IDirectoryInfo checkoutDirectory, IFileSystem readFileSystem, DiagnosticsCollector collector)
{
	private readonly ILogger<RepositorySourcer> _logger = logger.CreateLogger<RepositorySourcer>();

	// <summary>
	// Clones the repository to the checkout directory and checks out the specified git reference.
	// </summary>
	// <param name="repository">The repository to clone.</param>
	// <param name="gitRef">The git reference to check out. Branch, commit or tag</param>
	public Checkout CloneRef(Repository repository, string gitRef, bool pull = false, int attempt = 1)
	{
		var checkoutFolder = readFileSystem.DirectoryInfo.New(Path.Combine(checkoutDirectory.FullName, repository.Name));
		if (attempt > 3)
		{
			collector.EmitError("", $"Failed to clone repository {repository.Name}@{gitRef} after 3 attempts");
			return new Checkout
			{
				Directory = checkoutFolder,
				HeadReference = "",
				Repository = repository,
			};
		}
		_logger.LogInformation("{RepositoryName}: Cloning repository {RepositoryName}@{Commit} to {CheckoutFolder}", repository.Name, repository.Name, gitRef,
			checkoutFolder.FullName);
		if (!checkoutFolder.Exists)
		{
			checkoutFolder.Create();
			checkoutFolder.Refresh();
		}
		var isGitInitialized = GitInit(repository, checkoutFolder);
		string? head = null;
		if (isGitInitialized)
		{
			try
			{
				head = Capture(checkoutFolder, "git", "rev-parse", "HEAD");
			}
			catch (Exception e)
			{
				_logger.LogError(e, "{RepositoryName}: Failed to acquire current commit, falling back to recreating from scratch", repository.Name);
				checkoutFolder.Delete(true);
				checkoutFolder.Refresh();
				return CloneRef(repository, gitRef, pull, attempt + 1);
			}
		}
		// Repository already checked out the same commit
		if (head != null && head == gitRef)
			// nothing to do, already at the right commit
			_logger.LogInformation("{RepositoryName}: HEAD already at {GitRef}", repository.Name, gitRef);
		else
		{
			FetchAndCheckout(repository, gitRef, checkoutFolder);
			if (!pull)
			{
				return new Checkout
				{
					Directory = checkoutFolder,
					HeadReference = gitRef,
					Repository = repository,
				};
			}
			try
			{
				ExecIn(checkoutFolder, "git", "pull", "--depth", "1", "--allow-unrelated-histories", "--no-ff", "origin", gitRef);
			}
			catch (Exception e)
			{
				_logger.LogError(e, "{RepositoryName}: Failed to update {GitRef} from {RelativePath}, falling back to recreating from scratch",
					repository.Name, gitRef, checkoutFolder.FullName);
				checkoutFolder.Delete(true);
				checkoutFolder.Refresh();
				return CloneRef(repository, gitRef, pull, attempt + 1);
			}
		}

		return new Checkout
		{
			Directory = checkoutFolder,
			HeadReference = gitRef,
			Repository = repository,
		};
	}

	/// <summary>
	/// Initializes the git repository if it is not already initialized.
	/// Returns true if the repository was already initialized.
	/// </summary>
	private bool GitInit(Repository repository, IDirectoryInfo checkoutFolder)
	{
		var isGitAlreadyInitialized = Directory.Exists(Path.Combine(checkoutFolder.FullName, ".git"));
		if (isGitAlreadyInitialized)
			return true;
		ExecIn(checkoutFolder, "git", "init");
		ExecIn(checkoutFolder, "git", "remote", "add", "origin", repository.Origin);
		return false;
	}

	private void FetchAndCheckout(Repository repository, string gitRef, IDirectoryInfo checkoutFolder)
	{
		ExecIn(checkoutFolder, "git", "fetch", "--no-tags", "--prune", "--no-recurse-submodules", "--depth", "1", "origin", gitRef);
		if (repository.CheckoutStrategy == CheckoutStrategy.Partial)
			ExecIn(checkoutFolder, "git", "sparse-checkout", "set", "docs");
		ExecIn(checkoutFolder, "git", "checkout", "--force", gitRef);
	}

	private void ExecIn(IDirectoryInfo? workingDirectory, string binary, params string[] args)
	{
		var arguments = new ExecArguments(binary, args)
		{
			WorkingDirectory = workingDirectory?.FullName
		};
		var result = Proc.Exec(arguments);
		if (result != 0)
			collector.EmitError("", $"Exit code: {result} while executing {binary} {string.Join(" ", args)} in {workingDirectory}");
	}

	// ReSharper disable once UnusedMember.Local
	private string Capture(IDirectoryInfo? workingDirectory, string binary, params string[] args)
	{
		// Try 10 times to capture the output of the command, if it fails, we'll throw an exception on the last try
		Exception? e = null;
		for (var i = 0; i <= 9; i++)
		{
			try
			{
				return CaptureOutput();
			}
			catch (Exception ex)
			{
				if (ex is not null)
					e = ex;
			}
		}

		if (e is not null)
			collector.EmitError("", "failure capturing stdout", e);


		return string.Empty;

		string CaptureOutput()
		{
			var arguments = new StartArguments(binary, args)
			{
				WorkingDirectory = workingDirectory?.FullName,
				//WaitForStreamReadersTimeout = TimeSpan.FromSeconds(3),
				Timeout = TimeSpan.FromSeconds(3),
				WaitForExit = TimeSpan.FromSeconds(3),
				ConsoleOutWriter = NoopConsoleWriter.Instance
			};
			var result = Proc.Start(arguments);
			var line = result.ExitCode != 0
				? throw new Exception($"Exit code is not 0. Received {result.ExitCode} from {binary}: {workingDirectory}")
				: result.ConsoleOut.FirstOrDefault()?.Line ?? throw new Exception($"No output captured for {binary}: {workingDirectory}");
			return line;
		}
	}
}

public class NoopConsoleWriter : IConsoleOutWriter
{
	public static readonly NoopConsoleWriter Instance = new();

	public void Write(Exception e) { }

	public void Write(ConsoleOut consoleOut) { }
}
