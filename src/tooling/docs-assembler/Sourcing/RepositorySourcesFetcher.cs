// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Concurrent;
using System.IO.Abstractions;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.LinkIndex;
using Elastic.Documentation.Links;
using Microsoft.Extensions.Logging;
using ProcNet.Std;

namespace Documentation.Assembler.Sourcing;

public class AssemblerRepositorySourcer(ILoggerFactory logFactory, AssembleContext context)
{
	private readonly ILogger<AssemblerRepositorySourcer> _logger = logFactory.CreateLogger<AssemblerRepositorySourcer>();

	private AssemblyConfiguration Configuration => context.Configuration;
	private PublishEnvironment PublishEnvironment => context.Environment;

	private RepositorySourcer RepositorySourcer => new(logFactory, context.CheckoutDirectory, context.ReadFileSystem, context.Collector);

	public CheckoutResult GetAll()
	{
		var fs = context.ReadFileSystem;
		var repositories = Configuration.ReferenceRepositories.Values.Concat<Repository>([Configuration.Narrative]);
		var checkouts = new List<Checkout>();
		var linkRegistrySnapshotPath = Path.Combine(context.CheckoutDirectory.FullName, CheckoutResult.LinkRegistrySnapshotFileName);
		if (!fs.File.Exists(linkRegistrySnapshotPath))
			throw new FileNotFoundException("Link-index snapshot not found. Run the clone-all command first.", linkRegistrySnapshotPath);
		var linkRegistrySnapshotStr = File.ReadAllText(linkRegistrySnapshotPath);
		var linkRegistry = LinkRegistry.Deserialize(linkRegistrySnapshotStr);
		foreach (var repo in repositories)
		{
			var checkoutFolder = fs.DirectoryInfo.New(Path.Combine(context.CheckoutDirectory.FullName, repo.Name));
			IGitRepository gitFacade = new SingleCommitOptimizedGitRepository(context.Collector, checkoutFolder);
			if (!checkoutFolder.Exists)
			{
				context.Collector.EmitError(checkoutFolder.FullName, $"'{repo.Name}' does not exist in link index checkout directory");
				continue;
			}
			var head = gitFacade.GetCurrentCommit();
			var checkout = new Checkout
			{
				Repository = repo,
				Directory = checkoutFolder,
				HeadReference = head
			};
			checkouts.Add(checkout);
		}
		return new CheckoutResult
		{
			Checkouts = checkouts,
			LinkRegistrySnapshot = linkRegistry
		};
	}

	public async Task<CheckoutResult> CloneAll(bool fetchLatest, Cancel ctx = default)
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
		await context.WriteFileSystem.File.WriteAllTextAsync(
			Path.Combine(context.CheckoutDirectory.FullName, CheckoutResult.LinkRegistrySnapshotFileName),
			LinkRegistry.Serialize(linkRegistry),
			ctx
		);
		return new CheckoutResult
		{
			Checkouts = checkouts,
			LinkRegistrySnapshot = linkRegistry
		};
	}

	public async Task WriteLinkRegistrySnapshot(LinkRegistry linkRegistrySnapshot, Cancel ctx = default) => await context.WriteFileSystem.File.WriteAllTextAsync(
			Path.Combine(context.OutputDirectory.FullName, "docs", CheckoutResult.LinkRegistrySnapshotFileName),
			LinkRegistry.Serialize(linkRegistrySnapshot),
			ctx
		);
}


public class RepositorySourcer(ILoggerFactory logFactory, IDirectoryInfo checkoutDirectory, IFileSystem readFileSystem, DiagnosticsCollector collector)
{
	private readonly ILogger<RepositorySourcer> _logger = logFactory.CreateLogger<RepositorySourcer>();

	// <summary>
	// Clones the repository to the checkout directory and checks out the specified git reference.
	// </summary>
	// <param name="repository">The repository to clone.</param>
	// <param name="gitRef">The git reference to check out. Branch, commit or tag</param>
	public Checkout CloneRef(Repository repository, string gitRef, bool pull = false, int attempt = 1)
	{
		var checkoutFolder = readFileSystem.DirectoryInfo.New(Path.Combine(checkoutDirectory.FullName, repository.Name));
		IGitRepository git = new SingleCommitOptimizedGitRepository(collector, checkoutFolder);
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
		var isGitInitialized = GitInit(git, repository);
		string? head = null;
		if (isGitInitialized)
		{
			try
			{
				head = git.GetCurrentCommit();
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
			FetchAndCheckout(git, repository, gitRef);
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
				git.Pull(gitRef);
			}
			catch (Exception e)
			{
				_logger.LogError(e, "{RepositoryName}: Failed to update {GitRef} from {Path}, falling back to recreating from scratch",
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
	private static bool GitInit(IGitRepository git, Repository repository)
	{
		var isGitAlreadyInitialized = git.IsInitialized();
		if (isGitAlreadyInitialized)
			return true;
		git.Init();
		git.GitAddOrigin(repository.Origin);
		return false;
	}

	private static void FetchAndCheckout(IGitRepository git, Repository repository, string gitRef)
	{
		git.Fetch(gitRef);
		switch (repository.CheckoutStrategy)
		{
			case CheckoutStrategy.Full:
				git.DisableSparseCheckout();
				break;
			case CheckoutStrategy.Partial:
				git.EnableSparseCheckout("docs");
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(repository), repository.CheckoutStrategy, null);
		}
		git.Checkout(gitRef);
	}
}

public record CheckoutResult
{
	public static string LinkRegistrySnapshotFileName => "link-index.snapshot.json";
	public required LinkRegistry LinkRegistrySnapshot { get; init; }
	public required IReadOnlyCollection<Checkout> Checkouts { get; init; }
}
