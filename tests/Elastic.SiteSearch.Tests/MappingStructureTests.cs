// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Search.Contract;
using Nullean.Snapshots.Json;

namespace Elastic.SiteSearch.Tests;

/// <summary>
/// Verifies that fields inherited from <see cref="SearchDocumentBase"/> appear in the
/// mapping JSON of every concrete document type, and that the <c>[Id]</c> attribute on
/// <c>Url</c> resolves correctly through each context's <c>GetId</c> delegate.
/// </summary>
public class MappingStructureTests
{
	[Test]
	public void SiteDocument_GetId_ReturnsUrl()
	{
		var ctx = SiteMappingContext.SiteDocument.CreateContext(type: "blog", env: "test");
		var doc = new SiteDocument { Title = "t", SearchTitle = "t", Path = "https://www.elastic.co/blog/test" };
		ctx.GetId!(doc).Should().Be("https://www.elastic.co/blog/test");
	}

	[Test]
	public void GuideDocument_GetId_ReturnsUrl()
	{
		var ctx = GuideMappingContext.GuideDocument.CreateContext(type: "en", env: "test");
		var doc = new GuideDocument
		{
			Title = "t",
			SearchTitle = "t",
			Path = "https://www.elastic.co/guide/en/elasticsearch/reference/current/index.html"
		};
		ctx.GetId!(doc).Should().Be("https://www.elastic.co/guide/en/elasticsearch/reference/current/index.html");
	}

	[Test]
	public void SiteDocument_ContentTier_DefaultsToNeutralReference()
	{
		var document = new SiteDocument { Title = "t", SearchTitle = "t", Path = "https://www.elastic.co/blog/test" };
		document.ContentTier.Should().Be(ContentTiers.Reference, "content_tier must default to a neutral tier, not a penalty");
	}

	[Test]
	public void SiteDocument_Fields_UrlMatchesJsonPropertyName() => SiteMappingContext.SiteDocument.Fields.Path.Should().Be("path");

	[Test]
	public void SiteDocument_Fields_TitleMatchesJsonPropertyName() => SiteMappingContext.SiteDocument.Fields.Title.Should().Be("title");

	[Test]
	public void SiteDocument_Fields_HashMatchesJsonPropertyName() => SiteMappingContext.SiteDocument.Fields.Hash.Should().Be("hash");

	[Test]
	public void GuideDocument_Fields_UrlMatchesJsonPropertyName() => GuideMappingContext.GuideDocument.Fields.Path.Should().Be("path");

	// ── Whole mapping per document type ──────────────────────────────────────
	// The snapshots pin everything the old per-field checks did: the SearchDocumentBase fields, keyword and
	// copy_to settings, the title/path/body/parents multi-fields, rank_feature navigation fields, the
	// semantic_text sub-fields that only the semantic variant has, and the applies_to / api topology.

	[Test]
	public void SiteDocument_MappingJson() => SiteMappingContext.SiteDocument.GetMappingJson().Should().MatchJsonSnapshot();

	[Test]
	public void SiteDocumentSemantic_MappingJson() => SiteMappingContext.SiteDocumentSemantic.GetMappingJson().Should().MatchJsonSnapshot();

	[Test]
	public void GuideDocument_MappingJson() => GuideMappingContext.GuideDocument.GetMappingJson().Should().MatchJsonSnapshot();

	/// <summary>The unified index merges in DocumentationDocument's applies_to topology, which it has no C# property for.</summary>
	[Test]
	public void WebsiteSearchDocument_MappingJson() =>
		WebsiteSearchMappingContext.WebsiteSearchDocument.GetMappingJson().Should().MatchJsonSnapshot();

	[Test]
	public void DocumentationDocument_MappingJson() =>
		DocumentationMappingContext.DocumentationDocument.GetMappingJson().Should().MatchJsonSnapshot();
}
