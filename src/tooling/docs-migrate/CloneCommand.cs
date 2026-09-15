// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.LegacyDocs.Migration;
using Microsoft.Extensions.Logging;

namespace Documentation.Migrate;

internal sealed class CloneCommand(ILoggerFactory logFactory)
{
	private readonly ILogger _logger = logFactory.CreateLogger<CloneCommand>();

	/// <summary>Clone source repos needed for the selected books and versions.</summary>
	/// <param name="workDir">Working directory for migration artifacts</param>
	/// <param name="majors">Number of top major versions to include (default 1)</param>
	/// <param name="minors">Max minor versions per major to include (default all)</param>
	/// <param name="all">Process all versions</param>
	/// <param name="minVersion">Minimum major version to process</param>
	/// <param name="book">Filter to books whose prefix starts with this value</param>
	/// <param name="clean">Delete all cloned repos and start fresh</param>
	/// <param name="ct">Cancellation token</param>
	public async Task<int> Clone(
		string? workDir = null,
		int majors = 1,
		int? minors = null,
		bool all = false,
		int? minVersion = null,
		string? book = null,
		bool clean = false,
		CancellationToken ct = default
	)
	{
		var dir = SharedOptions.ResolveWorkDir(workDir);
		var conf = await SharedOptions.LoadConfAsync(dir, ct);

		var opts = new FilterOptions(majors, all, minVersion, book, minors);
		// Save without Book — book filter is a per-run override, not a persistent setting
		SharedOptions.SaveFilterOptions(dir, opts with { Book = null });

		var books = SharedOptions.FilterBooks(conf, opts.Book);

		var reposDir = Path.Combine(dir, "repos");
		var sparsePaths = SourceRepoManager.CollectSparsePaths(conf);
		var repoOptions = new SourceRepoOptions { ReposDirectory = reposDir, RepoUrls = conf.Repos, SparsePaths = sparsePaths };
		var repoManager = new SourceRepoManager(repoOptions, _logger);

		if (clean)
		{
			repoManager.CleanAll();
			_logger.LogInformation("Cleaned repos directory");
			return 0;
		}

		var clonedBranches = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var clonedRepos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		foreach (var b in books)
		{
			var versions = SharedOptions.FilterVersions(b, opts.Majors, opts.All, opts.MinVersion, opts.Minors);
			foreach (var version in versions)
			{
				ct.ThrowIfCancellationRequested();

				try
				{
					var sources = await repoManager.ResolveSourcesAsync(b, version, ct);
					foreach (var source in sources)
					{
						_ = clonedRepos.Add(source.RepoName);
						_ = clonedBranches.Add($"{source.RepoName}/{source.GitBranch}");
					}
				}
				catch (Exception ex) when (ex is not OperationCanceledException)
				{
					_logger.LogWarning("Failed to clone for {Prefix} {Version}: {Message}", b.Prefix, version.VersionLabel, ex.Message);
				}
			}
		}

		_logger.LogInformation("Cloned {RepoCount} repos, {BranchCount} worktrees", clonedRepos.Count, clonedBranches.Count);
		return 0;
	}
}
