// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Security.Cryptography;
using System.Text.Json;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.Documentation.Search.Contract;

namespace Elastic.ApiExplorer.Export;

/// <summary>
/// Merges one API's per-tree documents: an operation that reads the same in several trees becomes one document listing
/// every tree, and a document whose trees leave out the API's default tree is hidden (a sitemap-only document stays hidden), so readers that ignore trees see
/// one document per operation.
/// </summary>
internal static class ApiTreeCollapse
{
	/// <summary>
	/// <paramref name="renderings"/> are documents of one API with one tree each, as <see cref="OpenApiDocumentExporter.ConvertToDocuments"/> returns them.
	/// Each result's trees are ordered default tree first, then <c>main</c>, then majors descending, and its URLs point into the first.
	/// </summary>
	public static IEnumerable<DocumentationDocument> Collapse(IEnumerable<DocumentationDocument> renderings, string defaultTree)
	{
		var groups = new Dictionary<string, (DocumentationDocument Lead, List<ApiTreeEntry> Trees)>(StringComparer.Ordinal);
		foreach (var rendering in renderings)
		{
			var key = Fingerprint(rendering);
			var tree = rendering.Api!.Trees[0];
			if (!groups.TryGetValue(key, out var group))
				groups[key] = (rendering, [tree]);
			else
			{
				group.Trees.Add(tree);
				if (Comparer<(bool, int)>.Default.Compare(Rank(tree, defaultTree), Rank(group.Lead.Api!.Trees[0], defaultTree)) > 0)
					groups[key] = (rendering, group.Trees);
			}
		}

		foreach (var (lead, trees) in groups.Values)
		{
			ApiTreeEntry[] ordered = [.. trees.DistinctBy(static t => t.Tree).OrderByDescending(t => Rank(t, defaultTree))];
			yield return lead with { Api = lead.Api! with { Trees = ordered }, Hidden = lead.Hidden || ordered[0].Tree != defaultTree };
		}
	}

	private static (bool IsDefault, int Recency) Rank(ApiTreeEntry tree, string defaultTree) =>
		(tree.Tree == defaultTree, ApiVersions.Recency(tree.Tree));

	/// <summary>
	/// Everything the index stores for the document except the URLs, which differ per tree by construction. A page's identity across
	/// trees is its content hash and its path relative to the tree root: the same hash the sitemap dates the page by, so a page that
	/// shares a search document also shares its <c>lastmod</c>. Its body, whose links point into its tree, counts through that hash.
	/// </summary>
	private static string Fingerprint(DocumentationDocument doc)
	{
		var root = doc.Api!.Trees[0].Root;
		var withoutUrls = doc with
		{
			Path = doc.Path.StartsWith(root, StringComparison.Ordinal) ? doc.Path[root.Length..] : doc.Path,
			Body = doc.Api.Page ? "" : doc.Body,
			Parents = [.. doc.Parents.Select(static p => p with { Path = "" })],
			Api = doc.Api! with { Trees = [] }
		};
		var json = JsonSerializer.SerializeToUtf8Bytes(withoutUrls, SourceGenerationContext.Default.DocumentationDocument);
		return Convert.ToHexString(SHA256.HashData(json));
	}
}
