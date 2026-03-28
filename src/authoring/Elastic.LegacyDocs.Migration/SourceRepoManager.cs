// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using ProcNet;

namespace Elastic.LegacyDocs.Migration;

public record SourceRepoOptions
{
	public required string ReposDirectory { get; init; }
	public Dictionary<string, string> RepoUrls { get; init; } = [];
	public Dictionary<string, HashSet<string>> SparsePaths { get; init; } = [];
}

public record ResolvedSource
{
	public string RepoName { get; init; } = "";
	public string GitBranch { get; init; } = "";
	public string DocsPath { get; init; } = "";
	public string? Prefix { get; init; }
	public required string LocalPath { get; init; }
}

public class SourceRepoManager(SourceRepoOptions options, ILogger logger)
{
	private readonly ConcurrentDictionary<string, string> _resolvedPaths = [];
	private readonly ConcurrentDictionary<string, bool> _bareClones = [];

	/// <summary>Collects all declared source paths per repo from the conf.yaml book definitions.</summary>
	public static Dictionary<string, HashSet<string>> CollectSparsePaths(LegacyConf conf)
	{
		var result = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

		foreach (var book in conf.Contents.SelectMany(c => c.Sections))
		{
			foreach (var source in book.Sources)
			{
				if (!result.TryGetValue(source.Repo, out var paths))
				{
					paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
					result[source.Repo] = paths;
				}

				var normalized = NormalizeSparsePath(source.Path);
				if (normalized is not null)
					_ = paths.Add(normalized);
			}
		}

		return result;
	}

	public async Task<IReadOnlyList<ResolvedSource>> ResolveSourcesAsync(
		LegacyBook book, BranchRef version, CancellationToken ct = default)
	{
		var results = new List<ResolvedSource>();
		var versionLabel = version.VersionLabel;
		var defaultGitBranch = version.GitBranch;

		foreach (var source in book.Sources)
		{
			if (IsExcluded(source, versionLabel))
				continue;

			var gitBranch = ResolveBranch(source, versionLabel, defaultGitBranch);

			try
			{
				var localPath = await EnsureWorktreeAsync(source.Repo, gitBranch, ct);
				results.Add(new ResolvedSource
				{
					RepoName = source.Repo,
					GitBranch = gitBranch,
					DocsPath = source.Path,
					Prefix = source.Prefix,
					LocalPath = localPath
				});
			}
			catch (Exception ex) when (ex is not OperationCanceledException)
			{
				logger.LogWarning("Skipping {Repo}@{Branch}: {Message}", source.Repo, gitBranch, ex.Message);
			}
		}

		return results;
	}

	public void CleanAll()
	{
		if (!Directory.Exists(options.ReposDirectory))
			return;

		logger.LogInformation("Cleaning repos directory: {ReposDir}", options.ReposDirectory);
		Directory.Delete(options.ReposDirectory, recursive: true);
		_resolvedPaths.Clear();
		_bareClones.Clear();
	}

	private static bool IsExcluded(LegacySource source, string versionLabel) =>
		source.ExcludeBranches.Any(b => b.VersionLabel == versionLabel);

	private static string ResolveBranch(LegacySource source, string versionLabel, string defaultGitBranch) =>
		source.MapBranches.TryGetValue(versionLabel, out var mapped) ? mapped : defaultGitBranch;

	private string BareClonePath(string repoName) =>
		Path.Combine(options.ReposDirectory, $"{repoName}.git");

	private string WorktreePath(string repoName, string gitBranch) =>
		Path.Combine(options.ReposDirectory, repoName, gitBranch);

	private async Task EnsureBareCloneAsync(string repoName, CancellationToken ct)
	{
		if (_bareClones.ContainsKey(repoName))
			return;

		var barePath = BareClonePath(repoName);
		if (Directory.Exists(barePath))
		{
			_bareClones[repoName] = true;
			return;
		}

		if (!options.RepoUrls.TryGetValue(repoName, out var url))
			throw new InvalidOperationException($"Unknown repo: {repoName}");

		logger.LogInformation("Cloning bare {Repo}...", repoName);

		_ = Directory.CreateDirectory(options.ReposDirectory);
		await ExecGitAsync(ct, "clone", "--bare", "--filter=blob:none", url, barePath);

		_bareClones[repoName] = true;
		logger.LogInformation("Cloned bare {Repo}", repoName);
	}

	private async Task<string> EnsureWorktreeAsync(string repoName, string gitBranch, CancellationToken ct)
	{
		var key = $"{repoName}/{gitBranch}";

		if (_resolvedPaths.TryGetValue(key, out var existing))
			return existing;

		var worktreePath = WorktreePath(repoName, gitBranch);

		if (Directory.Exists(worktreePath))
		{
			_resolvedPaths[key] = worktreePath;
			return worktreePath;
		}

		await EnsureBareCloneAsync(repoName, ct);

		var barePath = BareClonePath(repoName);

		var resolvedBranch = await FetchBranchAsync(barePath, gitBranch, ct);

		logger.LogInformation("Creating worktree {Repo}@{Branch}...", repoName, resolvedBranch);
		_ = Directory.CreateDirectory(Path.GetDirectoryName(worktreePath)!);
		await ExecGitInAsync(barePath, allowFailure: false, ct, "worktree", "add", worktreePath, resolvedBranch, "--no-checkout");

		await ApplySparseCheckoutAsync(repoName, worktreePath, ct);

		await ExecGitInAsync(worktreePath, allowFailure: true, ct, "checkout");

		_resolvedPaths[key] = worktreePath;
		logger.LogInformation("Ready {Repo}@{Branch}", repoName, gitBranch);
		return worktreePath;
	}

	private async Task ApplySparseCheckoutAsync(string repoName, string worktreePath, CancellationToken ct)
	{
		if (!options.SparsePaths.TryGetValue(repoName, out var paths) || paths.Count == 0)
			return;

		logger.LogInformation("Sparse checkout {Repo}: {Paths}", repoName, string.Join(", ", paths));

		await ExecGitInAsync(worktreePath, allowFailure: false, ct, "sparse-checkout", "init", "--cone");
		await ExecGitInAsync(worktreePath, allowFailure: false, ct, ["sparse-checkout", "set", .. paths]);
	}

	private async Task<string> FetchBranchAsync(string barePath, string gitBranch, CancellationToken ct)
	{
		string[] candidates = [gitBranch, "master", "main"];
		foreach (var branch in candidates)
		{
			logger.LogInformation("Fetching {Branch}...", branch);
			var ok = await TryExecGitInAsync(barePath, ct, "fetch", "origin", $"{branch}:{branch}", "--depth", "1");
			if (ok)
				return branch;
		}

		throw new InvalidOperationException($"No branch found for {gitBranch} (also tried master, main) in {barePath}");
	}

	private static async Task<bool> TryExecGitInAsync(string workingDirectory, CancellationToken ct, params string[] args)
	{
		var arguments = new ExecArguments("git", args)
		{
			WorkingDirectory = workingDirectory,
			ValidExitCodeClassifier = _ => true
		};
		var exitCode = await Proc.ExecAsync(arguments, ct);
		return exitCode == 0;
	}

	private static async Task ExecGitAsync(CancellationToken ct, params string[] args)
	{
		var arguments = new ExecArguments("git", args)
		{
			ValidExitCodeClassifier = _ => true
		};
		var exitCode = await Proc.ExecAsync(arguments, ct);
		if (exitCode != 0)
			throw new InvalidOperationException($"git {args[0]} failed with exit code {exitCode}");
	}

	private static async Task ExecGitInAsync(string workingDirectory, bool allowFailure, CancellationToken ct, params string[] args)
	{
		var arguments = new ExecArguments("git", args)
		{
			WorkingDirectory = workingDirectory,
			ValidExitCodeClassifier = _ => true
		};
		var exitCode = await Proc.ExecAsync(arguments, ct);
		if (exitCode != 0 && !allowFailure)
			throw new InvalidOperationException(
				$"git {args[0]} failed with exit code {exitCode} in {workingDirectory}");
	}

	/// <summary>Extracts the top-level directory from a source path for sparse checkout.</summary>
	internal static string? NormalizeSparsePath(string sourcePath)
	{
		var trimmed = sourcePath.Trim().TrimStart('/').TrimEnd('/');
		if (string.IsNullOrEmpty(trimmed))
			return null;

		if (trimmed.Contains('*') || trimmed.StartsWith(":(glob)", StringComparison.Ordinal))
			return null;

		if (!trimmed.Contains('/'))
			return trimmed;

		var firstSlash = trimmed.IndexOf('/');
		return trimmed[..firstSlash];
	}
}
