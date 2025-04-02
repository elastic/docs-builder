// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using Documentation.Assembler.Configuration;
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

	public async Task<IReadOnlyCollection<Checkout>> AcquireAllLatest(Cancel ctx = default)
	{
		_logger.LogInformation(
			"Cloning all repositories for environment {EnvironmentName} using '{ContentSourceStrategy}' content sourcing strategy",
			PublishEnvironment.Name,
			PublishEnvironment.ContentSource.ToStringFast(true)
		);

		var dict = new ConcurrentDictionary<string, Stopwatch>();
		var checkouts = new ConcurrentBag<Checkout>();

		_logger.LogInformation("Cloning narrative content: {Repository}", NarrativeRepository.RepositoryName);
		var checkout = CloneOrUpdateRepository(Configuration.Narrative, NarrativeRepository.RepositoryName, dict);
		checkouts.Add(checkout);

		_logger.LogInformation("Cloning {ReferenceRepositoryCount} repositories", Configuration.ReferenceRepositories.Count);
		await Parallel.ForEachAsync(Configuration.ReferenceRepositories,
			new ParallelOptions
			{
				CancellationToken = ctx,
				MaxDegreeOfParallelism = Environment.ProcessorCount
			}, async (kv, c) =>
			{
				await Task.Run(() =>
				{
					var name = kv.Key.Trim();
					var clone = CloneOrUpdateRepository(kv.Value, name, dict);
					checkouts.Add(clone);
				}, c);
			}).ConfigureAwait(false);

		foreach (var kv in dict.OrderBy(kv => kv.Value.Elapsed))
			_logger.LogInformation("-> took: {Elapsed}\t{RepositoryBranch}", kv.Key, kv.Value.Elapsed);

		return checkouts.ToList().AsReadOnly();
	}

	private Checkout CloneOrUpdateRepository(Repository repository, string name, ConcurrentDictionary<string, Stopwatch> dict)
	{
		var fs = context.ReadFileSystem;
		var checkoutFolder = fs.DirectoryInfo.New(Path.Combine(context.CheckoutDirectory.FullName, name));
		var relativePath = Path.GetRelativePath(Paths.WorkingDirectoryRoot.FullName, checkoutFolder.FullName);
		var sw = Stopwatch.StartNew();
		var branch = PublishEnvironment.ContentSource == ContentSource.Next
			? repository.GitReferenceNext
			: repository.GitReferenceCurrent;

		_ = dict.AddOrUpdate($"{name} ({branch})", sw, (_, _) => sw);

		string? head;
		if (checkoutFolder.Exists)
		{
			if (!TryUpdateSource(name, branch, relativePath, checkoutFolder, out head))
				head = CheckoutFromScratch(repository, name, branch, relativePath, checkoutFolder);
		}
		else
			head = CheckoutFromScratch(repository, name, branch, relativePath, checkoutFolder);

		sw.Stop();

		return new Checkout
		{
			Repository = repository,
			Directory = checkoutFolder,
			HeadReference = head
		};
	}

	private bool TryUpdateSource(string name, string branch, string relativePath, IDirectoryInfo checkoutFolder, [NotNullWhen(true)] out string? head)
	{
		head = null;
		try
		{
			_logger.LogInformation("Pull: {Name}\t{Branch}\t{RelativePath}", name, branch, relativePath);
			// --allow-unrelated-histories due to shallow clones not finding a common ancestor
			ExecIn(checkoutFolder, "git", "pull", "--depth", "1", "--allow-unrelated-histories", "--no-ff");
			head = Capture(checkoutFolder, "git", "rev-parse", "HEAD");
			return true;
		}
		catch (Exception e)
		{
			_logger.LogError(e, "Failed to update {Name} from {RelativePath}, falling back to recreating from scratch", name, relativePath);
			if (checkoutFolder.Exists)
			{
				checkoutFolder.Delete(true);
				checkoutFolder.Refresh();
			}
		}

		return false;
	}

	private string CheckoutFromScratch(Repository repository, string name, string branch, string relativePath,
		IDirectoryInfo checkoutFolder)
	{
		_logger.LogInformation("Checkout: {Name}\t{Branch}\t{RelativePath}", name, branch, relativePath);
		if (repository.CheckoutStrategy == "full")
		{
			Exec("git", "clone", repository.Origin, checkoutFolder.FullName,
				"--depth", "1", "--single-branch",
				"--branch", branch
			);
		}
		else if (repository.CheckoutStrategy == "partial")
		{
			Exec(
				"git", "clone", "--filter=blob:none", "--no-checkout", repository.Origin, checkoutFolder.FullName
			);

			ExecIn(checkoutFolder, "git", "sparse-checkout", "set", "--cone");
			ExecIn(checkoutFolder, "git", "checkout", branch);
			ExecIn(checkoutFolder, "git", "sparse-checkout", "set", "docs");
		}

		return Capture(checkoutFolder, "git", "rev-parse", "HEAD");
	}

	private void Exec(string binary, params string[] args) => ExecIn(null, binary, args);

	private void ExecIn(IDirectoryInfo? workingDirectory, string binary, params string[] args)
	{
		var arguments = new ExecArguments(binary, args)
		{
			WorkingDirectory = workingDirectory?.FullName
		};
		var result = Proc.Exec(arguments);
		if (result != 0)
			context.Collector.EmitError("", $"Exit code: {result} while executing {binary} {string.Join(" ", args)} in {workingDirectory}");
	}

	// ReSharper disable once UnusedMember.Local
	private string Capture(IDirectoryInfo? workingDirectory, string binary, params string[] args)
	{
		// Try 10 times to capture the output of the command, if it fails we'll throw an exception on the last try
		for (var i = 0; i < 9; i++)
		{
			try
			{
				return CaptureOutput();
			}
			catch
			{
				// ignored
			}
		}
		return CaptureOutput();

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
			if (result.ExitCode != 0)
				context.Collector.EmitError("", $"Exit code: {result.ExitCode} while executing {binary} {string.Join(" ", args)} in {workingDirectory}");
			var line = result.ConsoleOut.FirstOrDefault()?.Line ?? throw new Exception($"No output captured for {binary}: {workingDirectory}");
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
