// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Elastic.LegacyDocs.Migration;

public record SourceRepoOptions
{
	public required string ReposDirectory { get; init; }
	public Dictionary<string, string> RepoUrls { get; init; } = [];
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
	private readonly ConcurrentDictionary<string, string> _clonedPaths = [];

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
				var localPath = await EnsureClonedAsync(source.Repo, gitBranch, ct);
				results.Add(new ResolvedSource
				{
					RepoName = source.Repo,
					GitBranch = gitBranch,
					DocsPath = source.Path,
					Prefix = source.Prefix,
					LocalPath = localPath
				});
			}
			catch (InvalidOperationException ex)
			{
				logger.LogWarning("Skipping {Repo}@{Branch}: {Message}", source.Repo, gitBranch, ex.Message);
			}
		}

		return results;
	}

	private static bool IsExcluded(LegacySource source, string versionLabel) =>
		source.ExcludeBranches.Any(b => b.VersionLabel == versionLabel);

	private static string ResolveBranch(LegacySource source, string versionLabel, string defaultGitBranch) =>
		source.MapBranches.TryGetValue(versionLabel, out var mapped) ? mapped : defaultGitBranch;

	private async Task<string> EnsureClonedAsync(string repoName, string gitBranch, CancellationToken ct)
	{
		var key = $"{repoName}/{gitBranch}";

		if (_clonedPaths.TryGetValue(key, out var existing))
			return existing;

		var clonePath = Path.Combine(options.ReposDirectory, repoName, gitBranch);

		if (Directory.Exists(Path.Combine(clonePath, ".git")))
		{
			_clonedPaths[key] = clonePath;
			return clonePath;
		}

		if (!options.RepoUrls.TryGetValue(repoName, out var url))
			throw new InvalidOperationException($"Unknown repo: {repoName}");

		logger.LogInformation("Cloning {Repo}@{Branch}...", repoName, gitBranch);

		var args = $"clone --depth 1 --branch {gitBranch} {url} \"{clonePath}\"";
		await RunGitAsync(args, repoName, gitBranch, ct);

		_clonedPaths[key] = clonePath;
		return clonePath;
	}

	private async Task RunGitAsync(string args, string repoName, string branch, CancellationToken ct)
	{
		var psi = new ProcessStartInfo("git", args)
		{
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = true
		};

		using var process = Process.Start(psi)
			?? throw new InvalidOperationException("Failed to start git");

		var stderr = await process.StandardError.ReadToEndAsync(ct);
		await process.WaitForExitAsync(ct);

		if (process.ExitCode != 0)
			throw new InvalidOperationException($"git clone {repoName}@{branch} failed (exit {process.ExitCode}): {stderr.Trim()}");

		logger.LogInformation("Cloned {Repo}@{Branch}", repoName, branch);
	}
}
