// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.ApiExplorer.Landing;
using Elastic.ApiExplorer.Model;
using Elastic.ApiExplorer.Operations;
using Elastic.ApiExplorer.Structural;

namespace Elastic.ApiExplorer.Infrastructure;

/// <summary>
/// The meta description of a page whose spec and authored text say nothing. Each sentence names the page and the product
/// and lists facts the spec declares (method, route, titles, schemes, server URLs), so no page claims more than the spec does.
/// It is plain text, so it skips the Markdown excerpt that spec text goes through. The HTML pages and the Markdown front matter
/// both call <see cref="For"/>, so the two cannot disagree.
/// </summary>
internal static class ApiFallbackDescription
{
	private const int ListedItems = 4;

	/// <summary>
	/// The fallback for <paramref name="page"/>, or null for a page kind with none. <paramref name="navigationTitle"/> is the page's
	/// entry in the navigation. <paramref name="tagRows"/> are the rows of a group page when the caller has built them already.
	/// </summary>
	internal static string? For(
		IApiModel page,
		ApiRenderContext context,
		string navigationTitle,
		IReadOnlyList<ApiOverviewRow>? tagRows = null
	) => Sentence(page, context, navigationTitle, tagRows) is { } text ? Cap(text) : null;

	/// <summary>
	/// Cuts at or before <see cref="ApiSeoDescription.MaxLength"/> characters, ellipsis included, so a long route or URL cannot push
	/// a description past the limit. A cut never leaves half of a surrogate pair, which would encode as a replacement character.
	/// </summary>
	private static string Cap(string text)
	{
		if (text.Length <= ApiSeoDescription.MaxLength)
			return text;

		var end = ApiSeoDescription.MaxLength - 3;
		if (char.IsHighSurrogate(text[end - 1]))
			end--;
		return string.Concat(text.AsSpan(0, end).TrimEnd(), "...");
	}

	private static string? Sentence(
		IApiModel page,
		ApiRenderContext context,
		string navigationTitle,
		IReadOnlyList<ApiOverviewRow>? tagRows
	)
	{
		var product = context.ProductName;
		return page switch
		{
			ApiLanding => $"Reference documentation for the {product} API.",
			ApiTag tag =>
				WithList(
					$"{tag.DisplayName} endpoints in the {product} API",
					(tagRows ?? ApiTag.OverviewRows(context)).Where(static row => row.Kind == OverviewRowKind.Operation).Select(
						static row => row.Title
					)
				),
			ApiOperation operation =>
				$"{OpenApiExtensionReader.GetSummary(operation.Operation) ?? navigationTitle}: {operation.OperationType.Method.ToUpperInvariant()} {operation.Route} in the {product} API.",
			ApiStructuralPage { Kind: ApiStructuralKind.Authentication } =>
				Authentication(
					product,
					[.. StructuralViewModel.ReadRequirements(context.Model).SelectMany(static r => r.Schemes).Select(static s => s.Label)]
				),
			ApiStructuralPage =>
				WithList(
					$"Server URLs for the {product} API",
					StructuralViewModel.ReadServers(context.Model).Select(static server => server.Url)
				),
			_ => null
		};
	}

	/// <summary>No label means no document-level authentication: the spec has no schemes, or allows anonymous access as a whole. Individual operations can still declare their own, so the sentence claims nothing about them.</summary>
	internal static string Authentication(string product, IReadOnlyList<string> schemeLabels) =>
		schemeLabels.Count == 0
			? $"The {product} API declares no authentication for the API as a whole."
			: WithList($"How to authenticate requests to the {product} API", schemeLabels);

	private static string WithList(string sentence, IEnumerable<string> items)
	{
		var listed = items.Distinct(StringComparer.Ordinal).Take(ListedItems).ToArray();
		return listed.Length == 0 ? $"{sentence}." : $"{sentence}: {string.Join(", ", listed)}.";
	}
}
