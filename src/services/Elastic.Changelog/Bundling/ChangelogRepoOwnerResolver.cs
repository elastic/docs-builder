// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Changelog.Bundling;

/// <summary>
/// Resolves the owner/repo segments used to key the changelog entry pool (<c>changelog/{owner}/{repo}/{branch}/...</c>).
/// Shared by CDN entry sourcing (<see cref="ChangelogBundlingService"/>) and upload
/// (<c>ChangelogCommands.ResolveUploadRepoOwnerBranch</c>) so a <c>bundle.repo: "owner/repo"</c> value without
/// an explicit <c>bundle.owner</c> resolves to the same owner on both sides.
/// </summary>
public static class ChangelogRepoOwnerResolver
{
	/// <summary>
	/// Resolves the owner: <paramref name="owner"/> when set, otherwise the <c>owner/</c> prefix of a combined
	/// <paramref name="repo"/> value (e.g. <c>acme/widget</c> -&gt; <c>acme</c>) so it is not lost, otherwise
	/// <paramref name="fallback"/>.
	/// </summary>
	public static string? ResolveOwner(string? owner, string? repo, string? fallback)
	{
		if (!string.IsNullOrWhiteSpace(owner))
			return owner;

		if (!string.IsNullOrWhiteSpace(repo))
		{
			var slash = repo.IndexOf('/', StringComparison.Ordinal);
			if (slash > 0)
				return repo[..slash];
		}

		return fallback;
	}

	/// <summary>Reduces a configured repo value to the single CDN-key path segment (<c>owner/repo</c> -&gt; <c>repo</c>); null/empty unchanged.</summary>
	public static string? NormalizeRepo(string? repo)
	{
		if (string.IsNullOrWhiteSpace(repo))
			return repo;
		var slash = repo.LastIndexOf('/');
		return slash >= 0 && slash < repo.Length - 1 ? repo[(slash + 1)..] : repo;
	}
}
