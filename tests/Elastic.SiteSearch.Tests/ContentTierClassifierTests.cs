// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Search.Contract;
using Elastic.SiteSearch.Cli.Elasticsearch;

namespace Elastic.SiteSearch.Tests;

/// <summary>
/// Verifies the shared essc content_tier classification used by both ContentStackMapper
/// (site) and LabsHtmlExtractor (labs) — see <see cref="ContentTierClassifier"/>.
/// </summary>
public class ContentTierClassifierTests
{
	[Theory]
	[InlineData("concept", ContentTiers.Primary)]
	[InlineData("product", ContentTiers.Primary)]
	[InlineData("marketing", ContentTiers.Peripheral)]
	[InlineData("legal", ContentTiers.Peripheral)]
	[InlineData("download", ContentTiers.Peripheral)]
	[InlineData("press", ContentTiers.Peripheral)]
	[InlineData("pricing", ContentTiers.Peripheral)]
	[InlineData("webinar", ContentTiers.Supplementary)]
	[InlineData("event", ContentTiers.Supplementary)]
	[InlineData("customer-story", ContentTiers.Supplementary)]
	[InlineData("demo", ContentTiers.Supplementary)]
	[InlineData("about", ContentTiers.Supplementary)]
	[InlineData("training", ContentTiers.Supplementary)]
	[InlineData("resource", ContentTiers.Supplementary)]
	[InlineData("industry", ContentTiers.Supplementary)]
	[InlineData("partner", ContentTiers.Supplementary)]
	[InlineData("blog", ContentTiers.Reference)]
	[InlineData("search-labs", ContentTiers.Reference)]
	[InlineData("security-labs", ContentTiers.Reference)]
	[InlineData("observability-labs", ContentTiers.Reference)]
	[InlineData(null, ContentTiers.Reference)]
	[InlineData("unrecognised-section", ContentTiers.Reference)]
	public void FromNavigationSection_ClassifiesKnownSections(string? section, string expectedTier) =>
		ContentTierClassifier.FromNavigationSection(section).Should().Be(expectedTier);
}
