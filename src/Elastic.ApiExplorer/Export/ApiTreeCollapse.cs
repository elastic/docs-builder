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
/// every tree, and a document whose trees leave out the API's default tree is hidden, so readers that ignore trees see
/// one document per operation.
/// </summary>
internal static class ApiTreeCollapse
{
	/// <summary>
	/// <paramref name="renderings"/> are documents of one API with one tree each, as <see cref="OpenApiDocumentExporter.ConvertToDocuments"/> returns them.
	/// Each result's trees are ordered default tree first, then <c>main</c>, then majors descending, and its URLs point into the first.
	/// </summary>
	public static IEnumerable<DocumentationDocument> Collapse(IEnumerable<DocumentationDocument> renderings, string defaultTree) =>
		renderings.GroupBy(Fingerprint, StringComparer.Ordinal).Select(group => Merge([.. group], defaultTree));

	private static DocumentationDocument Merge(IReadOnlyList<DocumentationDocument> group, string defaultTree)
	{
		var trees = group
			.SelectMany(static d => d.Api!.Trees)
			.DistinctBy(static t => t.Tree)
			.OrderByDescending(t => t.Tree == defaultTree)
			.ThenByDescending(static t => t.Tree == "main")
			.ThenByDescending(static t => ApiVersions.TryParseMajor(t.Tree))
			.ToArray();
		var lead = group.First(d => d.Api!.Trees[0].Tree == trees[0].Tree);
		return lead with { Api = lead.Api! with { Trees = trees }, Hidden = trees.All(t => t.Tree != defaultTree) };
	}

	/// <summary>Everything the index stores for the document except the URLs, which differ per tree by construction.</summary>
	private static string Fingerprint(DocumentationDocument doc)
	{
		var withoutUrls = doc with
		{
			Path = "",
			Parents = [.. doc.Parents.Select(static p => p with { Path = "" })],
			Api = doc.Api! with { Trees = [] }
		};
		var json = JsonSerializer.SerializeToUtf8Bytes(withoutUrls, SourceGenerationContext.Default.DocumentationDocument);
		return Convert.ToHexString(SHA256.HashData(json));
	}
}
