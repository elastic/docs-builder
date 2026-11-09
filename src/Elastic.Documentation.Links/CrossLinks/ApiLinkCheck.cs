// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Diagnostics;

namespace Elastic.Documentation.Links.CrossLinks;

/// <summary>Checks a link to an API reference page against the API sections the cross-link resolver holds.</summary>
public static class ApiLinkCheck
{
	/// <summary>
	/// Returns a hint when <paramref name="url"/> targets an API page that the resolver knows does not exist, and
	/// <c>null</c> for any other link, and for an API it holds no section for. A link to a root with no pages at all always hints. Old bump.sh URLs are checked at the
	/// page the edge redirects them to.
	/// </summary>
	public static string? GetHint(ICrossLinkResolver resolver, string? url, string? urlPathPrefix, HashSet<HintType>? suppressions)
	{
		if (suppressions.ShouldSuppress(HintType.ApiLinkMissingPage))
			return null;

		if (
			!ApiLinkMatcher.TryCanonicalize(url, urlPathPrefix, out var canonical) || !ApiLinkMatcher.TryGetApiPath(canonical, out var path)
		)
			return null;

		// The spec downloads next to a product root, kibana.json and kibana.yaml, are files, not pages of its section.
		if (Path.GetExtension(path) is ".json" or ".yaml")
			return null;

		return ApiLegacyRedirects.IsGone(path) || resolver.CheckApiPage(ApiLegacyRedirects.Resolve(path)) == ApiPageStatus.Missing
			? $"API link '{canonical}' does not resolve to a page of that API. Link to a page that exists, such as one in a versioned tree like "
				+ "/docs/api/doc/<product>/v8/..., or confirm the endpoint was removed intentionally. Links in API descriptions are fixed in the OpenAPI spec."
			: null;
	}
}
