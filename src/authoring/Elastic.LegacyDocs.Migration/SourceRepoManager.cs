// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.LegacyDocs.Migration;

public record SourceRepoOptions
{
	public string? ReposDirectory { get; init; }
	public Dictionary<string, LegacyRepo> Repos { get; init; } = [];
}

public record ResolvedSource
{
	public string RepoName { get; init; } = "";
	public string Branch { get; init; } = "";
	public string DocsPath { get; init; } = "";
	public string? LocalPath { get; init; }
}

public class SourceRepoManager(SourceRepoOptions options)
{
	public IReadOnlyList<ResolvedSource> ResolveSources(LegacyBook book, string version)
	{
		var results = new List<ResolvedSource>();

		foreach (var source in book.Sources)
		{
			if (IsExcluded(source, version))
				continue;

			var branch = ResolveBranch(source, version);
			var localPath = ResolveLocalPath(source.Repo);

			results.Add(new ResolvedSource
			{
				RepoName = source.Repo,
				Branch = branch,
				DocsPath = source.Path,
				LocalPath = localPath
			});
		}

		return results;
	}

	private static bool IsExcluded(LegacySource source, string version) =>
		source.ExcludeBranches.Any(b => b.Name == version);

	private static string ResolveBranch(LegacySource source, string version) =>
		source.MapBranches.TryGetValue(version, out var mapped) ? mapped : version;

	private string? ResolveLocalPath(string repoName) =>
		options.ReposDirectory is not null
			? Path.Combine(options.ReposDirectory, repoName)
			: null;
}
