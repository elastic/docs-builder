// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Configuration.Codex;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.LinkIndex;
using Elastic.Documentation.Links;
using Elastic.Documentation.Services;
using Microsoft.Extensions.Logging;

namespace Elastic.Codex.Sourcing;

/// <summary>
/// Service for cloning repositories defined in a codex configuration.
/// </summary>
public class CodexCloneService(ILoggerFactory logFactory, ILinkIndexReader linkIndexReader) : IService
{
	private const string LinkRegistrySnapshotFileName = "link-index.snapshot.json";
	private readonly ILogger _logger = logFactory.CreateLogger<CodexCloneService>();

	/// <summary>
	/// Clones all repositories defined in the codex configuration.
	/// </summary>
	public async Task<CodexCloneResult> CloneAll(
		CodexContext context,
		bool fetchLatest,
		bool assumeCloned,
		Cancel ctx)
	{
		var checkouts = new List<CodexCheckout>();
		var checkoutDir = context.CheckoutDirectory;

		if (!checkoutDir.Exists)
			checkoutDir.Create();

		var linkRegistry = await linkIndexReader.GetRegistry(ctx);

		_logger.LogInformation("Cloning {Count} documentation sets to {Directory}",
			context.Configuration.DocumentationSets.Count, checkoutDir.FullName);

		await Parallel.ForEachAsync(
			context.Configuration.DocumentationSets,
			new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount, CancellationToken = ctx },
			async (docSetRef, c) =>
			{
				var checkout = await CloneRepository(context, docSetRef, linkRegistry, fetchLatest, assumeCloned, c);
				if (checkout != null)
				{
					lock (checkouts)
						checkouts.Add(checkout);
				}
			});

		await context.WriteFileSystem.File.WriteAllTextAsync(
			Path.Combine(context.CheckoutDirectory.FullName, LinkRegistrySnapshotFileName),
			LinkRegistry.Serialize(linkRegistry),
			ctx);

		return new CodexCloneResult(checkouts, linkRegistry);
	}

	private async Task<CodexCheckout?> CloneRepository(
		CodexContext context,
		CodexDocumentationSetReference docSetRef,
		LinkRegistry linkRegistry,
		bool fetchLatest,
		bool assumeCloned,
		Cancel _)
	{
		if (Path.IsPathRooted(docSetRef.ResolvedRepoName))
		{
			context.Collector.EmitError(
				context.ConfigurationPath,
				$"Repository name '{docSetRef.ResolvedRepoName}' must be a relative path");
			return null;
		}

		var repoDir = context.ReadFileSystem.DirectoryInfo.New(
			Path.Combine(context.CheckoutDirectory.FullName, docSetRef.ResolvedRepoName));

		var gitUrl = docSetRef.GetGitUrl();
		var branch = docSetRef.Branch;
		var docsPath = docSetRef.Path;

		if (Path.IsPathRooted(docsPath))
		{
			context.Collector.EmitError(
				context.ConfigurationPath,
				$"Documentation path '{docsPath}' for repository '{docSetRef.Name}' must be a relative path");
			return null;
		}

		var gitRef = branch;
		if (!fetchLatest && linkRegistry.Repositories.TryGetValue(docSetRef.ResolvedRepoName, out var entry) &&
			entry.TryGetValue(branch, out var entryInfo))
			gitRef = entryInfo.GitReference;

		_logger.LogInformation("Cloning {Name} from {Origin} at {GitRef}",
			docSetRef.Name, docSetRef.ResolvedOrigin, gitRef);

		try
		{
			var git = new CodexGitRepository(logFactory, context.Collector, repoDir);

			if (assumeCloned && git.IsInitialized())
			{
				_logger.LogInformation("Assuming {Name} is already cloned", docSetRef.Name);
			}
			else if (git.IsInitialized() && !fetchLatest)
			{
				_logger.LogInformation("{Name} already cloned, skipping (use --fetch-latest to update)", docSetRef.Name);
			}
			else
			{
				if (!repoDir.Exists)
					repoDir.Create();

				if (!git.IsInitialized())
				{
					git.Init();
					git.GitAddOrigin(gitUrl);
				}

				// Enable sparse checkout for just the docs folder
				git.EnableSparseCheckout([docsPath]);
				git.Fetch(gitRef);
				git.Checkout("FETCH_HEAD");
			}

			var currentCommit = git.GetCurrentCommit();
			var docsDirectory = context.ReadFileSystem.DirectoryInfo.New(
				Path.Combine(repoDir.FullName, docsPath));

			if (!docsDirectory.Exists)
			{
				context.Collector.EmitError(context.ConfigurationPath,
					$"Documentation directory '{docsPath}' not found in repository '{docSetRef.Name}'");
				return null;
			}

			return new CodexCheckout(docSetRef, repoDir, docsDirectory, currentCommit);
		}
		catch (Exception ex)
		{
			context.Collector.EmitError(context.ConfigurationPath,
				$"Failed to clone repository '{docSetRef.Name}': {ex.Message}");
			_logger.LogError(ex, "Failed to clone repository {Name}", docSetRef.Name);
			return null;
		}
	}
}

/// <summary>
/// Result of cloning codex repositories.
/// </summary>
public record CodexCloneResult(IReadOnlyList<CodexCheckout> Checkouts, LinkRegistry LinkRegistrySnapshot);

/// <summary>
/// Represents a cloned repository checkout for the codex.
/// </summary>
public record CodexCheckout(
	CodexDocumentationSetReference Reference,
	IDirectoryInfo RepositoryDirectory,
	IDirectoryInfo DocsDirectory,
	string CommitHash);
