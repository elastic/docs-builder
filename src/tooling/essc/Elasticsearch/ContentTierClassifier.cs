// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Search.Contract;

namespace Elastic.SiteSearch.Cli.Elasticsearch;

/// <summary>
/// Derives <c>content_tier</c> (see <see cref="ContentTiers"/>) from the <c>navigation_section</c>
/// values already computed by <c>ContentStackMapper.GetNavigationSection</c> and
/// <c>LabsHtmlExtractor.GetNavigationSection</c>. Shared between both essc producers so
/// site/labs content is tiered consistently; docs-builder classifies its own docs/api content
/// separately (it agrees with essc only on the <see cref="ContentTiers"/> string values).
/// </summary>
internal static class ContentTierClassifier
{
	public static string FromNavigationSection(string? navigationSection) => navigationSection switch
	{
		// Editorial overviews and flagship product pages.
		"concept" or "product" => ContentTiers.Primary,

		// Marketing, legal, and low-signal pages — demoted.
		"marketing" or "legal" or "download" or "press" or "pricing" => ContentTiers.Peripheral,

		// Useful but secondary content.
		"webinar" or "event" or "customer-story" or "demo" or "about"
			or "training" or "resource" or "industry" or "partner" => ContentTiers.Supplementary,

		// Blog, labs sub-sections, and anything unrecognised default to the neutral tier.
		_ => ContentTiers.Reference,
	};
}
