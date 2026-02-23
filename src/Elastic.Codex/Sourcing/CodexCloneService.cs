// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Configuration.Codex;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.LinkIndex;
using Elastic.Documentation.Links;
using Elastic.Documentation.Services;
using Microsoft.Extensions.Logging;

namespace Elastic.Codex.Sourcing;

/// <summary>
/// Service for cloning repositories defined in the link index for a codex environment.
/// </summary>
public class CodexCloneService(ILoggerFactory logFactory, ILinkIndexReader linkIndexReader) : IService
{
	private const string LinkRegistrySnapshotFileName = "link-index.snapshot.json";
	private static readonly string[] DocsetSearchPaths = ["docs/docset.yml", "docs/_docset.yml", "docset.yml", "_docset.yml"];
	private readonly ILogger _logger = logFactory.CreateLogger<CodexCloneService>();

	/// <summary>
	/// Clones all repositories defined in the link index for the codex environment.
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
		var repoEntries = GetRepositoryEntries(linkRegistry);

		_logger.LogInformation("Cloning {Count} documentation sets to {Directory}",
			repoEntries.Count, checkoutDir.FullName);

		await Parallel.ForEachAsync(
			repoEntries,
			new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount, CancellationToken = ctx },
			async (entry, c) =>
			{
				var checkout = CloneRepository(context, entry, fetchLatest, assumeCloned);
				if (checkout != null)
				{
					lock (checkouts)
						checkouts.Add(checkout);
				}
				await Task.CompletedTask;
			});

		if (Path.IsPathRooted(LinkRegistrySnapshotFileName))
			throw new InvalidOperationException($"Snapshot file name '{LinkRegistrySnapshotFileName}' must be a relative path.");

		var snapshotFilePath = Path.Combine(context.CheckoutDirectory.FullName, LinkRegistrySnapshotFileName);

		await context.WriteFileSystem.File.WriteAllTextAsync(
			snapshotFilePath,
			LinkRegistry.Serialize(linkRegistry),
			ctx);

		return new CodexCloneResult(checkouts, linkRegistry);
	}

	private static IReadOnlyList<(string RepoName, LinkRegistryEntry Entry)> GetRepositoryEntries(LinkRegistry linkRegistry)
	{
		var result = new List<(string RepoName, LinkRegistryEntry Entry)>();

		foreach (var (repoName, branches) in linkRegistry.Repositories)
		{
			if (branches.Count == 0)
				continue;

			// Pick the most recently updated branch when multiple exist
			var entry = branches.Values.MaxBy(e => e.UpdatedAt);
			if (entry != null)
				result.Add((repoName, entry));
		}

		return result;
	}

	private CodexCheckout? CloneRepository(CodexContext context, (string RepoName, LinkRegistryEntry Entry) repoEntry, bool fetchLatest, bool assumeCloned)
	{
		var (repoName, entry) = repoEntry;

		if (Path.IsPathRooted(repoName))
		{
			context.Collector.EmitError(
				context.ConfigurationPath,
				$"Repository name '{repoName}' must be a relative path");
			return null;
		}

		var repoDir = context.ReadFileSystem.DirectoryInfo.New(
			Path.Combine(context.CheckoutDirectory.FullName, repoName));

		var gitUrl = GetGitUrl($"elastic/{repoName}");
		var gitRef = fetchLatest ? entry.Branch : entry.GitReference;

		_logger.LogInformation("Cloning {Name} from {Origin} at {GitRef}",
			repoName, $"elastic/{repoName}", gitRef);

		try
		{
			var git = new CodexGitRepository(logFactory, context.Collector, repoDir);

			if (assumeCloned && git.IsInitialized())
				_logger.LogInformation("Assuming {Name} is already cloned", repoName);
			else if (git.IsInitialized() && !fetchLatest)
				_logger.LogInformation("{Name} already cloned, skipping (use --fetch-latest to update)", repoName);
			else
			{
				if (!repoDir.Exists)
					repoDir.Create();

				if (!git.IsInitialized())
				{
					git.Init();
					git.GitAddOrigin(gitUrl);
				}

				// Full clone without sparse checkout to discover docset.yml location
				git.Fetch(gitRef);
				git.Checkout("FETCH_HEAD");
			}

			var currentCommit = git.GetCurrentCommit();

			// Find docset.yml and read codex metadata
			var docsetFile = FindDocsetFile(context.ReadFileSystem, repoDir);
			if (docsetFile == null)
			{
				context.Collector.EmitWarning(context.ConfigurationPath,
					$"docset.yml or _docset.yml not found in repository '{repoName}'; skipping");
				return null;
			}

			var docSet = DocumentationSetFile.LoadMetadata(docsetFile);
			var docsDirectory = docsetFile.Directory!;
			var docsPath = Path.GetRelativePath(repoDir.FullName, docsDirectory.FullName);

			var docsPathForRef = string.IsNullOrEmpty(docsPath) || docsPath == "."
				? "."
				: docsPath.Replace('\\', '/');
			var docSetRef = CreateDocumentationSetReference(repoName, entry, docsPathForRef, docSet);

			return new CodexCheckout(docSetRef, repoDir, docsDirectory, currentCommit);
		}
		catch (Exception ex)
		{
			// Emit warning instead of error: repos may be in the link index before the clone
			// workflow has permission to access them. Continue with repos we can clone.
			context.Collector.EmitWarning(context.ConfigurationPath,
				$"Could not clone repository '{repoName}': {ex.Message}");
			_logger.LogWarning(ex, "Could not clone repository {Name}; skipping", repoName);
			return null;
		}
	}

	private static IFileInfo? FindDocsetFile(IFileSystem fileSystem, IDirectoryInfo repoDir)
	{
		foreach (var candidate in DocsetSearchPaths)
		{
			var path = Path.Combine(repoDir.FullName, candidate);
			var file = fileSystem.FileInfo.New(path);
			if (file.Exists)
				return file;
		}

		// Recursive search
		return SearchForDocsetRecursive(fileSystem, repoDir);
	}

	private static IFileInfo? SearchForDocsetRecursive(IFileSystem fileSystem, IDirectoryInfo directory)
	{
		try
		{
			foreach (var file in directory.GetFiles())
			{
				if (file.Name is "docset.yml" or "_docset.yml")
					return file;
			}

			foreach (var subDir in directory.GetDirectories())
			{
				if (subDir.Name is ".git" or "node_modules")
					continue;

				var found = SearchForDocsetRecursive(fileSystem, subDir);
				if (found != null)
					return found;
			}
		}
		catch (UnauthorizedAccessException)
		{
			// Skip directories we can't access
		}

		return null;
	}

	private static CodexDocumentationSetReference CreateDocumentationSetReference(
		string repoName,
		LinkRegistryEntry entry,
		string docsPath,
		DocumentationSetFile docSet) => new()
		{
			Name = repoName,
			Origin = $"elastic/{repoName}",
			Branch = entry.Branch,
			Path = docsPath,
			Group = docSet.Codex?.Group,
			Icon = docSet.Icon
		};

	private static string GetGitUrl(string origin)
	{
		if (origin.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
			origin.StartsWith("git@", StringComparison.OrdinalIgnoreCase))
			return origin;

		if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_ACTIONS")))
		{
			var token = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
			return !string.IsNullOrEmpty(token)
				? $"https://oauth2:{token}@github.com/{origin}.git"
				: $"https://github.com/{origin}.git";
		}

		return $"git@github.com:{origin}.git";
	}
}

/// <summary>
/// Result of cloning codex repositories.
/// </summary>
public record CodexCloneResult(IReadOnlyList<CodexCheckout> Checkouts, LinkRegistry LinkRegistrySnapshot)
{
	/// <summary>
	/// Gets the documentation set references for the cloned checkouts.
	/// </summary>
	public IReadOnlyList<CodexDocumentationSetReference> DocumentationSetReferences =>
		Checkouts.Select(c => c.Reference).ToList();
}

/// <summary>
/// Represents a cloned repository checkout for the codex.
/// </summary>
public record CodexCheckout(
	CodexDocumentationSetReference Reference,
	IDirectoryInfo RepositoryDirectory,
	IDirectoryInfo DocsDirectory,
	string CommitHash);
