// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using AwesomeAssertions;
using Elastic.Documentation.Search.Contract;

namespace Elastic.SiteSearch.Tests;

/// <summary>
/// Verifies that fields inherited from <see cref="SearchDocumentBase"/> appear in the
/// mapping JSON of every concrete document type, and that the <c>[Id]</c> attribute on
/// <c>Url</c> resolves correctly through each context's <c>GetId</c> delegate.
/// <para>
/// Note: <c>DocumentationDocument</c> mapping tests live in the docs-builder repo
/// (where the type now resides) rather than here.
/// </para>
/// </summary>
public class MappingStructureTests
{
	// ── [Id] resolves to Url for every document type ─────────────────────────

	[Fact]
	public void SiteDocument_GetId_ReturnsUrl()
	{
		var ctx = SiteMappingContext.SiteDocument.CreateContext(type: "blog", env: "test");
		var doc = new SiteDocument { Title = "t", SearchTitle = "t", Path = "https://www.elastic.co/blog/test" };
		ctx.GetId!(doc).Should().Be("https://www.elastic.co/blog/test");
	}

	[Fact]
	public void GuideDocument_GetId_ReturnsUrl()
	{
		var ctx = GuideMappingContext.GuideDocument.CreateContext(type: "en", env: "test");
		var doc = new GuideDocument { Title = "t", SearchTitle = "t", Path = "https://www.elastic.co/guide/en/elasticsearch/reference/current/index.html" };
		ctx.GetId!(doc).Should().Be("https://www.elastic.co/guide/en/elasticsearch/reference/current/index.html");
	}

	// ── Base fields from SearchDocumentBase present in SiteDocument mapping ──

	[Fact]
	public void SiteDocument_MappingJson_ContainsBaseFields()
	{
		var json = SiteMappingContext.SiteDocument.GetMappingJson();
		using var doc = JsonDocument.Parse(json);
		var props = doc.RootElement.GetProperty("properties");

		props.TryGetProperty("path", out _).Should().BeTrue("path is declared on SearchDocumentBase");
		props.TryGetProperty("title", out _).Should().BeTrue("title is declared on SearchDocumentBase");
		props.TryGetProperty("content_type", out _).Should().BeTrue("content_type is declared on SearchDocumentBase");
		props.TryGetProperty("content_tier", out _).Should().BeTrue("content_tier is declared on SearchDocumentBase");
		props.TryGetProperty("hash", out _).Should().BeTrue("hash is declared on SearchDocumentBase");
		props.TryGetProperty("body", out _).Should().BeTrue("body is declared on SearchDocumentBase");
		props.TryGetProperty("headings", out _).Should().BeTrue("headings is declared on SearchDocumentBase");
		props.TryGetProperty("navigation", out _).Should().BeTrue("navigation is declared on SearchDocumentBase");
	}

	// ── Base fields from SearchDocumentBase present in GuideDocument mapping ─

	[Fact]
	public void GuideDocument_MappingJson_ContainsBaseFields()
	{
		var json = GuideMappingContext.GuideDocument.GetMappingJson();
		using var doc = JsonDocument.Parse(json);
		var props = doc.RootElement.GetProperty("properties");

		props.TryGetProperty("path", out _).Should().BeTrue("path is declared on SearchDocumentBase");
		props.TryGetProperty("title", out _).Should().BeTrue("title is declared on SearchDocumentBase");
		props.TryGetProperty("content_type", out _).Should().BeTrue("content_type is declared on SearchDocumentBase");
		props.TryGetProperty("content_tier", out _).Should().BeTrue("content_tier is declared on SearchDocumentBase");
		props.TryGetProperty("hash", out _).Should().BeTrue("hash is declared on SearchDocumentBase");
		props.TryGetProperty("body", out _).Should().BeTrue("body is declared on SearchDocumentBase");
		props.TryGetProperty("navigation", out _).Should().BeTrue("navigation is declared on SearchDocumentBase");
	}

	// ── Url: [Id][Keyword] must map as keyword ────────────────────────────────

	[Fact]
	public void SiteDocument_UrlField_IsKeyword()
	{
		var json = SiteMappingContext.SiteDocument.GetMappingJson();
		using var doc = JsonDocument.Parse(json);
		doc.RootElement.GetProperty("properties").GetProperty("path")
			.GetProperty("type").GetString().Should().Be("keyword");
	}

	[Fact]
	public void GuideDocument_UrlField_IsKeyword()
	{
		var json = GuideMappingContext.GuideDocument.GetMappingJson();
		using var doc = JsonDocument.Parse(json);
		doc.RootElement.GetProperty("properties").GetProperty("path")
			.GetProperty("type").GetString().Should().Be("keyword");
	}

	// ── content_type: [Keyword] on base ──────────────────────────────────────

	[Fact]
	public void SiteDocument_ContentTypeField_IsKeyword()
	{
		var json = SiteMappingContext.SiteDocument.GetMappingJson();
		using var doc = JsonDocument.Parse(json);
		doc.RootElement.GetProperty("properties").GetProperty("content_type")
			.GetProperty("type").GetString().Should().Be("keyword");
	}

	// ── tags: copy_to target for content_type/section ─────

	[Fact]
	public void SiteDocument_ContentTypeField_CopiesToContentTags()
	{
		var json = SiteMappingContext.SiteDocument.GetMappingJson();
		using var doc = JsonDocument.Parse(json);
		doc.RootElement.GetProperty("properties").GetProperty("content_type")
			.GetProperty("copy_to").GetString().Should().Be("tags");
	}

	[Fact]
	public void SiteDocument_NavigationSectionField_CopiesToContentTags()
	{
		var json = SiteMappingContext.SiteDocument.GetMappingJson();
		using var doc = JsonDocument.Parse(json);
		doc.RootElement.GetProperty("properties").GetProperty("section")
			.GetProperty("copy_to").GetString().Should().Be("tags");
	}

	[Fact]
	public void SiteDocument_ContentTagsField_IsTextWithContentTagsAnalyzer()
	{
		var json = SiteMappingContext.SiteDocument.GetMappingJson();
		using var doc = JsonDocument.Parse(json);
		var contentTags = doc.RootElement.GetProperty("properties").GetProperty("tags");
		contentTags.GetProperty("type").GetString().Should().Be("text");
		contentTags.GetProperty("analyzer").GetString().Should().Be("content_tags_analyzer");
	}

	// ── content_tier: [Keyword] on base, neutral default ──────────────────────

	[Fact]
	public void SiteDocument_ContentTierField_IsKeyword()
	{
		var json = SiteMappingContext.SiteDocument.GetMappingJson();
		using var doc = JsonDocument.Parse(json);
		doc.RootElement.GetProperty("properties").GetProperty("content_tier")
			.GetProperty("type").GetString().Should().Be("keyword");
	}

	[Fact]
	public void SiteDocument_ContentTier_DefaultsToNeutralReference()
	{
		var document = new SiteDocument { Title = "t", SearchTitle = "t", Path = "https://www.elastic.co/blog/test" };
		document.ContentTier.Should().Be(ContentTiers.Reference, "content_tier must default to a neutral tier, not a penalty");
	}

	// ── hash: [Keyword] on base ───────────────────────────────────────────────

	[Fact]
	public void SiteDocument_HashField_IsKeyword()
	{
		var json = SiteMappingContext.SiteDocument.GetMappingJson();
		using var doc = JsonDocument.Parse(json);
		doc.RootElement.GetProperty("properties").GetProperty("hash")
			.GetProperty("type").GetString().Should().Be("keyword");
	}

	// ── Title multi-fields from AddSearchDocumentMappings ────────────────────

	[Fact]
	public void SiteDocument_TitleField_HasKeywordNormalizedMultiField()
	{
		var json = SiteMappingContext.SiteDocument.GetMappingJson();
		using var doc = JsonDocument.Parse(json);
		var titleFields = doc.RootElement.GetProperty("properties").GetProperty("title").GetProperty("fields");

		var keyword = titleFields.GetProperty("keyword");
		keyword.GetProperty("type").GetString().Should().Be("keyword");
		keyword.GetProperty("normalizer").GetString().Should().Be("keyword_normalizer");
	}

	[Fact]
	public void SiteDocument_TitleField_HasStartsWithMultiField()
	{
		var json = SiteMappingContext.SiteDocument.GetMappingJson();
		using var doc = JsonDocument.Parse(json);
		var titleFields = doc.RootElement.GetProperty("properties").GetProperty("title").GetProperty("fields");

		titleFields.TryGetProperty("starts_with", out _).Should().BeTrue("starts_with is configured in AddSearchDocumentMappings");
	}

	[Fact]
	public void SiteDocument_TitleField_HasCompletionMultiField()
	{
		var json = SiteMappingContext.SiteDocument.GetMappingJson();
		using var doc = JsonDocument.Parse(json);
		var titleFields = doc.RootElement.GetProperty("properties").GetProperty("title").GetProperty("fields");

		titleFields.TryGetProperty("completion", out _).Should().BeTrue("completion is configured in AddSearchDocumentMappings");
	}

	// ── ai_search_query: search_as_you_type completion sub-field, no semantic_text ──

	[Fact]
	public void SiteDocument_AiSearchQueryField_IsKeywordWithCompletionMultiField()
	{
		var json = SiteMappingContext.SiteDocument.GetMappingJson();
		using var doc = JsonDocument.Parse(json);
		var aiSearchQuery = doc.RootElement.GetProperty("properties").GetProperty("ai_search_query");
		aiSearchQuery.GetProperty("type").GetString().Should().Be("keyword");

		var completion = aiSearchQuery.GetProperty("fields").GetProperty("completion");
		completion.GetProperty("type").GetString().Should().Be("search_as_you_type");
	}

	[Fact]
	public void SiteDocument_SemanticVariant_AiSearchQueryHasNoSemanticTextField()
	{
		var json = SiteMappingContext.SiteDocumentSemantic.GetMappingJson();
		using var doc = JsonDocument.Parse(json);
		var aiSearchQuery = doc.RootElement.GetProperty("properties").GetProperty("ai_search_query");
		if (aiSearchQuery.TryGetProperty("fields", out var fields))
			fields.TryGetProperty("semantic_text", out _).Should().BeFalse("ai_search_query is typeahead-only — never semantic_text");
	}

	// ── Path multi-fields from AddCommonTitleMappings ──────────────────────────

	[Fact]
	public void SiteDocument_UrlField_HasMatchAndPrefixMultiFields()
	{
		var json = SiteMappingContext.SiteDocument.GetMappingJson();
		using var doc = JsonDocument.Parse(json);
		var urlFields = doc.RootElement.GetProperty("properties").GetProperty("path").GetProperty("fields");

		urlFields.TryGetProperty("match", out _).Should().BeTrue("path.match is configured in AddCommonTitleMappings");
		urlFields.TryGetProperty("prefix", out _).Should().BeTrue("path.prefix (hierarchy_analyzer) is configured in AddCommonTitleMappings");
		urlFields.GetProperty("prefix").GetProperty("analyzer").GetString().Should().Be("hierarchy_analyzer");
	}

	// ── Navigation fields must be rank_feature with negative score impact ─────

	[Fact]
	public void SiteDocument_NavigationDepth_IsRankFeatureWithNegativeImpact()
	{
		var json = SiteMappingContext.SiteDocument.GetMappingJson();
		using var doc = JsonDocument.Parse(json);
		var depth = doc.RootElement.GetProperty("properties").GetProperty("navigation").GetProperty("properties").GetProperty("depth");
		depth.GetProperty("type").GetString().Should().Be("rank_feature");
		depth.GetProperty("positive_score_impact").GetBoolean().Should().BeFalse();
	}

	[Fact]
	public void SiteDocument_NavigationTableOfContents_IsRankFeatureWithNegativeImpact()
	{
		var json = SiteMappingContext.SiteDocument.GetMappingJson();
		using var doc = JsonDocument.Parse(json);
		var toc = doc.RootElement.GetProperty("properties").GetProperty("navigation").GetProperty("properties").GetProperty("table_of_contents");
		toc.GetProperty("type").GetString().Should().Be("rank_feature");
		toc.GetProperty("positive_score_impact").GetBoolean().Should().BeFalse();
	}

	// ── Body multi-language fields from AddSearchDocumentMappings ────────────

	[Fact]
	public void SiteDocument_BodyField_HasLanguageMultiFields()
	{
		var json = SiteMappingContext.SiteDocument.GetMappingJson();
		using var doc = JsonDocument.Parse(json);
		var bodyFields = doc.RootElement.GetProperty("properties").GetProperty("body").GetProperty("fields");

		bodyFields.GetProperty("en").GetProperty("analyzer").GetString().Should().Be("english");
		bodyFields.GetProperty("de").GetProperty("analyzer").GetString().Should().Be("german");
		bodyFields.GetProperty("fr").GetProperty("analyzer").GetString().Should().Be("french");
		bodyFields.TryGetProperty("ja", out _).Should().BeTrue("ja (cjk) body multi-field is configured");
		bodyFields.TryGetProperty("ko", out _).Should().BeTrue("ko (cjk) body multi-field is configured");
		bodyFields.TryGetProperty("zh", out _).Should().BeTrue("zh (cjk) body multi-field is configured");
		bodyFields.TryGetProperty("es", out _).Should().BeTrue("es (spanish) body multi-field is configured");
		bodyFields.TryGetProperty("pt", out _).Should().BeTrue("pt (portuguese) body multi-field is configured");
	}

	// ── Semantic variant adds semantic_text multi-fields ─────────────────────

	[Fact]
	public void SiteDocument_SemanticVariant_TitleHasSemanticTextField()
	{
		var json = SiteMappingContext.SiteDocumentSemantic.GetMappingJson();
		using var doc = JsonDocument.Parse(json);
		var titleFields = doc.RootElement.GetProperty("properties").GetProperty("title").GetProperty("fields");
		titleFields.GetProperty("semantic_text").GetProperty("type").GetString().Should().Be("semantic_text");
	}

	[Fact]
	public void SiteDocument_SemanticVariant_StrippedBodyHasSemanticTextField()
	{
		var json = SiteMappingContext.SiteDocumentSemantic.GetMappingJson();
		using var doc = JsonDocument.Parse(json);
		var bodyFields = doc.RootElement.GetProperty("properties").GetProperty("body").GetProperty("fields");
		bodyFields.GetProperty("semantic_text").GetProperty("type").GetString().Should().Be("semantic_text");
	}

	[Fact]
	public void SiteDocument_LexicalVariant_DoesNotHaveSemanticTextField()
	{
		var json = SiteMappingContext.SiteDocument.GetMappingJson();
		using var doc = JsonDocument.Parse(json);
		var title = doc.RootElement.GetProperty("properties").GetProperty("title");
		if (title.TryGetProperty("fields", out var titleFields))
			titleFields.TryGetProperty("semantic_text", out _).Should().BeFalse("lexical variant has no semantic_text");
	}

	// ── Field name constants match JSON property names ────────────────────────

	[Fact]
	public void SiteDocument_Fields_UrlMatchesJsonPropertyName() =>
		SiteMappingContext.SiteDocument.Fields.Path.Should().Be("path");

	[Fact]
	public void SiteDocument_Fields_TitleMatchesJsonPropertyName() =>
		SiteMappingContext.SiteDocument.Fields.Title.Should().Be("title");

	[Fact]
	public void SiteDocument_Fields_HashMatchesJsonPropertyName() =>
		SiteMappingContext.SiteDocument.Fields.Hash.Should().Be("hash");

	[Fact]
	public void GuideDocument_Fields_UrlMatchesJsonPropertyName() =>
		GuideMappingContext.GuideDocument.Fields.Path.Should().Be("path");
}
