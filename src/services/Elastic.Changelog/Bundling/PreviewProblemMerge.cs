// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration.ReleaseNotes;

namespace Elastic.Changelog.Bundling;

/// <summary>
/// Combines the problems of a run that refreshed only some bundles with the problems already on disk. A run limited by
/// <c>--products</c> or <c>--services</c> leaves the other bundles in place, so it must leave their problems in place too.
/// </summary>
public static class PreviewProblemMerge
{
	/// <summary>
	/// The problems of a refreshed product or repository come from <paramref name="fresh"/>, and every other problem of
	/// <paramref name="existing"/> stays. A DRA product is refreshed as a whole. A serverless product is shared by several services,
	/// so it is refreshed by repository: a run of one service must not drop the problems of another.
	/// </summary>
	/// <param name="existing">The problems on disk.</param>
	/// <param name="fresh">The problems of this run.</param>
	/// <param name="refreshedProducts">DRA products this run bundled.</param>
	/// <param name="refreshedRepositories">Product and repository pairs this run bundled, for the services it ran.</param>
	public static IReadOnlyList<PreviewProblem> Merge(
		IReadOnlyList<PreviewProblem> existing,
		IReadOnlyList<PreviewProblem> fresh,
		IReadOnlyCollection<string> refreshedProducts,
		IReadOnlyCollection<(string Product, string Repo)> refreshedRepositories
	)
	{
		var products = new HashSet<string>(refreshedProducts, StringComparer.Ordinal);
		var repositories = new HashSet<(string, string)>(refreshedRepositories);
		return [.. existing.Where(p => !products.Contains(p.Product) && !repositories.Contains((p.Product, p.Repo))), .. fresh];
	}
}
