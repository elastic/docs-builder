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
	[Test]
	[Arguments("concept", ContentTiers.Primary)]
	[Arguments("product", ContentTiers.Primary)]
	[Arguments("marketing", ContentTiers.Peripheral)]
	[Arguments("legal", ContentTiers.Peripheral)]
	[Arguments("download", ContentTiers.Peripheral)]
	[Arguments("press", ContentTiers.Peripheral)]
	[Arguments("pricing", ContentTiers.Peripheral)]
	[Arguments("webinar", ContentTiers.Supplementary)]
	[Arguments("event", ContentTiers.Supplementary)]
	[Arguments("customer-story", ContentTiers.Supplementary)]
	[Arguments("demo", ContentTiers.Supplementary)]
	[Arguments("about", ContentTiers.Supplementary)]
	[Arguments("training", ContentTiers.Supplementary)]
	[Arguments("resource", ContentTiers.Supplementary)]
	[Arguments("industry", ContentTiers.Supplementary)]
	[Arguments("partner", ContentTiers.Supplementary)]
	[Arguments("blog", ContentTiers.Reference)]
	[Arguments("search-labs", ContentTiers.Reference)]
	[Arguments("security-labs", ContentTiers.Reference)]
	[Arguments("observability-labs", ContentTiers.Reference)]
	[Arguments(null, ContentTiers.Reference)]
	[Arguments("unrecognised-section", ContentTiers.Reference)]
	public void FromNavigationSection_ClassifiesKnownSections(string? section, string expectedTier) =>
		ContentTierClassifier.FromNavigationSection(section).Should().Be(expectedTier);
}
