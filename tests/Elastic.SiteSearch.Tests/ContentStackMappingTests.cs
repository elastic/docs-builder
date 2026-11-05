// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using System.Text.Json.Nodes;
using AwesomeAssertions;
using Elastic.Documentation.Search.Contract;
using Elastic.SiteSearch.Cli.ContentStack;
using Nullean.Snapshots.Json;

namespace Elastic.SiteSearch.Tests;

/// <summary>
/// Tests ContentStack sync item to SiteDocument mapping for each structural group of content types.
/// See Fixtures/ContentStack/schema-groups.json for which content types each fixture covers.
/// </summary>
public class ContentStackMappingTests
{
	private static readonly JsonSerializerOptions DocumentOptions = new() { TypeInfoResolver = SourceGenerationContext.Default };

	/// <summary>The mapped document as JSON, without the fields stamped with the current time (index date, and last_updated when the entry has no modified date).</summary>
	private static string ToJson(SiteDocument doc)
	{
		var node = JsonNode.Parse(JsonSerializer.Serialize<ISearchDocument>(doc, DocumentOptions))!.AsObject();
		_ = node.Remove("batch_index_date");
		if (doc.ModifiedDate is null)
			_ = node.Remove("last_updated");
		return node.ToJsonString();
	}

	private static SyncItem LoadFixture(string fixtureName, string contentTypeUid)
	{
		var path = Path.Combine("Fixtures", "ContentStack", fixtureName);
		var json = File.ReadAllText(path);
		var data = JsonSerializer.Deserialize<JsonElement>(json);
		return new SyncItem { Type = "entry_published", ContentTypeUid = contentTypeUid, Data = data };
	}

	/// <summary>Covers: blog (legacy v1 with flat body_l10n)</summary>
	[Test]
	public void BlogLegacy_Maps_BodyAndMetadata()
	{
		var item = LoadFixture("blog_legacy.json", "blog");
		var doc = ContentStackMapper.ToSiteDocument(item);

		doc.Should().NotBeNull();
		ToJson(doc!).Should().MatchJsonSnapshot();
	}

	/// <summary>Covers: blog_v2 (modular blocks with nested title_text arrays)</summary>
	[Test]
	public void BlogModern_Maps_ModularBlocksBody()
	{
		var item = LoadFixture("blog_modern.json", "blog_v2");
		var doc = ContentStackMapper.ToSiteDocument(item);

		doc.Should().NotBeNull();
		ToJson(doc!).Should().MatchJsonSnapshot();
	}

	/// <summary>Covers: videos (paragraph_l10n body, presentation_date)</summary>
	[Test]
	public void Video_Maps_ParagraphAndPresenter()
	{
		var item = LoadFixture("video.json", "videos");
		var doc = ContentStackMapper.ToSiteDocument(item);

		doc.Should().NotBeNull();
		ToJson(doc!).Should().MatchJsonSnapshot();
	}

	/// <summary>Covers: press (intro_paragraph_l10n + paragraph_l10n, date field)</summary>
	[Test]
	public void PressRelease_Maps_CombinedParagraphs()
	{
		var item = LoadFixture("press_release.json", "press");
		var doc = ContentStackMapper.ToSiteDocument(item);

		doc.Should().NotBeNull();
		ToJson(doc!).Should().MatchJsonSnapshot();
	}

	/// <summary>Covers: product_versions (release_notes body, date, version_number)</summary>
	[Test]
	public void ProductRelease_Maps_ReleaseNotes()
	{
		var item = LoadFixture("product_release.json", "product_versions");
		var doc = ContentStackMapper.ToSiteDocument(item);

		doc.Should().NotBeNull();
		ToJson(doc!).Should().MatchJsonSnapshot();
	}

	/// <summary>Covers: agreements, forms (paragraph_l10n as single body)</summary>
	[Test]
	public void ParagraphPage_Maps_SingleBody()
	{
		var item = LoadFixture("paragraph_page.json", "agreements");
		var doc = ContentStackMapper.ToSiteDocument(item);

		doc.Should().NotBeNull();
		ToJson(doc!).Should().MatchJsonSnapshot();
	}

	/// <summary>Covers: default_detail, account_based_marketing, product_detail (modular_blocks with flat title_text)</summary>
	[Test]
	public void ModularPage_Maps_FlatModularBlocks()
	{
		var item = LoadFixture("modular_page.json", "default_detail");
		var doc = ContentStackMapper.ToSiteDocument(item);

		doc.Should().NotBeNull();
		ToJson(doc!).Should().MatchJsonSnapshot();
	}

	/// <summary>Covers: use_cases (introduction + challenge_solution + modular_blocks)</summary>
	[Test]
	public void UseCase_Maps_IntroAndChallenges()
	{
		var item = LoadFixture("use_case.json", "use_cases");
		var doc = ContentStackMapper.ToSiteDocument(item);

		doc.Should().NotBeNull();
		ToJson(doc!).Should().MatchJsonSnapshot();
	}

	/// <summary>Covers: faq (topic[].subtopic[].paragraph_l10n)</summary>
	[Test]
	public void FaqPage_Maps_NestedTopics()
	{
		var item = LoadFixture("faq_page.json", "faq");
		var doc = ContentStackMapper.ToSiteDocument(item);

		doc.Should().NotBeNull();
		ToJson(doc!).Should().MatchJsonSnapshot();
	}

	/// <summary>Covers: customer_tile (short paragraph_l10n, partial URL coverage)</summary>
	[Test]
	public void CustomerTile_Maps_ShortContent()
	{
		var item = LoadFixture("customer_tile.json", "customer_tile");
		var doc = ContentStackMapper.ToSiteDocument(item);

		doc.Should().NotBeNull();
		// URL prefix wins over the entry's own (en-us) locale — see GetLanguageFromUrl.
		ToJson(doc!).Should().MatchJsonSnapshot();
	}

	/// <summary>
	/// Covers: product_icons, blog_overview, press_overview, videos_overview,
	/// elasticon_videos_overview, customers_overview, integrations, cloud_regions,
	/// subscriptions_redesign, subscriptions_cloud, pricing_redesign, contact_redesign,
	/// features_overview, demo_gallery_overview, blog_archive_overview, search,
	/// past_releases, about_our_source_code, site_navigation, support_matrix,
	/// downloads_redesign, blog_category_detail, demo_gallery_detail,
	/// about_leadership_and_board, pricing_calculator, events_overview, timeline
	/// </summary>
	[Test]
	public void MinimalPage_Maps_TitleUrlSeo()
	{
		var item = LoadFixture("minimal_page.json", "blog_overview");
		var doc = ContentStackMapper.ToSiteDocument(item);

		doc.Should().NotBeNull();
		// ContentTypeUid "blog_overview" is now mapped to "blog" via GetSectionFromContentType,
		// even though the URL alone ("/blog", no trailing slash) doesn't match the URL heuristic.
		ToJson(doc!).Should().MatchJsonSnapshot();
	}

	/// <summary>Covers: tutorial, tutorial_page, tutorial_chapter, labs_integration (rich-text JSON AST body + description)</summary>
	[Test]
	public void RichTextPage_Maps_ProseMirrorBodyAndDescription()
	{
		var item = LoadFixture("rich_text_page.json", "labs_integration");
		var doc = ContentStackMapper.ToSiteDocument(item);

		doc.Should().NotBeNull();
		ToJson(doc!).Should().MatchJsonSnapshot();
	}

	/// <summary>Covers: blog_v3 (rich-text body nested under main_content.body.content_l10n + summary_l10n description)</summary>
	[Test]
	public void BlogV3_Maps_NestedRichTextBodyAndSummary()
	{
		var item = LoadFixture("rich_text_blog_v3.json", "blog_v3");
		var doc = ContentStackMapper.ToSiteDocument(item);

		doc.Should().NotBeNull();
		ToJson(doc!).Should().MatchJsonSnapshot();
	}

	/// <summary>Covers: tutorials_landing, integrations_landing, blog_landing (page_info.subheading_l10n description, no body)</summary>
	[Test]
	public void LandingPage_Maps_SubheadingDescription_NoBody()
	{
		var item = LoadFixture("landing_page.json", "tutorials_landing");
		var doc = ContentStackMapper.ToSiteDocument(item);

		doc.Should().NotBeNull();
		// No main_content on landing pages — body falls back to the description.
		ToJson(doc!).Should().MatchJsonSnapshot();
	}

	/// <summary>Covers: notebook, series, labs_category, labs_homepage, glossary, examples_landing (plain description_l10n, no body)</summary>
	[Test]
	public void DescriptionOnly_Maps_PlainDescription_NoBody()
	{
		var item = LoadFixture("description_only.json", "notebook");
		var doc = ContentStackMapper.ToSiteDocument(item);

		doc.Should().NotBeNull();
		ToJson(doc!).Should().MatchJsonSnapshot();
	}

	/// <summary>Covers: reports, threat_command (main_content.markdown_l10n plain-Markdown body + summary_l10n description)</summary>
	[Test]
	public void MarkdownBodyPage_Maps_MarkdownAsHeadingsAndPlainBody()
	{
		var item = LoadFixture("markdown_body.json", "threat_command");
		var doc = ContentStackMapper.ToSiteDocument(item);

		doc.Should().NotBeNull();
		// Links keep their visible text, drop the URL.
		// Bold/inline-code markers are stripped but the wrapped text is kept.
		// Fenced code blocks are dropped entirely.
		ToJson(doc!).Should().MatchJsonSnapshot();
	}

	/// <summary>Covers: reports_landing, threat_command_landing (top-level subheading_l10n description, no body)</summary>
	[Test]
	public void TopLevelSubheadingLandingPage_Maps_SubheadingDescription_NoBody()
	{
		var item = LoadFixture("top_level_subheading_landing.json", "reports_landing");
		var doc = ContentStackMapper.ToSiteDocument(item);

		doc.Should().NotBeNull();
		ToJson(doc!).Should().MatchJsonSnapshot();
	}

	/// <summary>Covers: security_labs_homepage, observability_labs_homepage (page_description_l10n description, no body)</summary>
	[Test]
	public void PropertyHomepage_Maps_PageDescription_NoBody()
	{
		var item = LoadFixture("property_homepage.json", "security_labs_homepage");
		var doc = ContentStackMapper.ToSiteDocument(item);

		doc.Should().NotBeNull();
		ToJson(doc!).Should().MatchJsonSnapshot();
	}

	/// <summary>blog_description_l10n is used when page_description_l10n is absent.</summary>
	[Test]
	public void ToSiteDocument_PropertyHomepage_FallsBackToBlogDescription_WhenNoPageDescription()
	{
		var item = LoadFromJson(/*lang=json,strict*/
			"""
			{ "title": "Observability Labs", "url": "/observability-labs", "locale": "en-us",
			  "blog_description_l10n": "The latest observability research from Elastic." }
			""",
			"observability_labs_homepage"
		);
		var doc = ContentStackMapper.ToSiteDocument(item);

		doc.Should().NotBeNull();
		doc.Description.Should().Be("The latest observability research from Elastic.");
		doc.Section.Should().Be("observability-labs");
	}

	/// <summary>Empty page_info.description.content_simple_l10n must not win over the SEO fallback.</summary>
	[Test]
	public void ToSiteDocument_LandingPage_WithEmptyRichDescription_FallsBackToSeo()
	{
		var item = LoadFromJson(/*lang=json,strict*/
			"""
			{ "title": "Integrations", "url": "/search-labs/integrations", "locale": "en-us",
			  "page_info": { "description": { "content_simple_l10n": { "type": "doc", "children": [] } } },
			  "seo": { "seo_description_l10n": "Browse Search Labs integrations." } }
			""",
			"integrations_landing"
		);
		var doc = ContentStackMapper.ToSiteDocument(item);

		doc.Should().NotBeNull();
		doc.Description.Should().Be("Browse Search Labs integrations.");
	}

	/// <summary>page_info.description.content_simple_l10n is used when there's no subheading_l10n.</summary>
	[Test]
	public void ToSiteDocument_GlossaryPage_UsesPageInfoRichTextDescription_WhenNoSubheading()
	{
		var item = LoadFromJson(/*lang=json,strict*/
			"""
			{ "title": "Glossary", "url": "/glossary", "locale": "en-us",
			  "page_info": { "description": { "content_simple_l10n": { "type": "doc",
			    "children": [ { "type": "p", "children": [ { "text": "All the terms, concepts, and abbreviations." } ] } ] } } } }
			""",
			"glossary"
		);
		var doc = ContentStackMapper.ToSiteDocument(item);

		doc.Should().NotBeNull();
		doc.Description.Should().Contain("terms, concepts, and abbreviations");
		doc.Section.Should().Be("glossary");
	}

	// --- Body projection helper tests ---

	[Test]
	public void StripHtml_Removes_Tags_And_Entities()
	{
		var result = ContentStackMapper.StripHtml("<p>Hello &amp; <strong>world</strong>.</p>");
		result.Should().Be("Hello world .");
	}

	[Test]
	public void StripHtml_Collapses_Whitespace()
	{
		var result = ContentStackMapper.StripHtml("<p>Line one</p>\n\n<p>  Line two  </p>");
		result.Should().Be("Line one Line two");
	}

	[Test]
	public void ExtractHeadings_Finds_All_Levels()
	{
		var html = "<h1>Title</h1><p>text</p><h2>Subtitle</h2><h3>Sub-sub</h3>";
		var headings = ContentStackMapper.ExtractHeadings(html);
		headings.Should().BeEquivalentTo(["Title", "Subtitle", "Sub-sub"]);
	}

	[Test]
	public void ExtractHeadings_Strips_Inner_Tags()
	{
		var html = "<h2><a href=\"#anchor\">Linked Heading</a></h2>";
		var headings = ContentStackMapper.ExtractHeadings(html);
		headings.Should().ContainSingle().Which.Should().Be("Linked Heading");
	}

	[Test]
	public void ExtractHeadings_Returns_Empty_For_No_Headings()
	{
		var headings = ContentStackMapper.ExtractHeadings("<p>Just a paragraph</p>");
		headings.Should().BeEmpty();
	}

	[Test]
	public void GetNavigationSection_Classifies_Known_Paths()
	{
		ContentStackMapper.GetNavigationSection("/search-labs").Should().Be("search-labs");
		ContentStackMapper.GetNavigationSection("/search-labs/blog/some-post").Should().Be("search-labs");
		ContentStackMapper.GetNavigationSection("/security-labs").Should().Be("security-labs");
		ContentStackMapper.GetNavigationSection("/security-labs/threat-command/some-report").Should().Be("security-labs");
		ContentStackMapper.GetNavigationSection("/observability-labs").Should().Be("observability-labs");
		ContentStackMapper.GetNavigationSection("/observability-labs/blog/some-post").Should().Be("observability-labs");
		ContentStackMapper.GetNavigationSection("/glossary").Should().Be("glossary");
		ContentStackMapper.GetNavigationSection("/blog/some-post").Should().Be("blog");
		ContentStackMapper.GetNavigationSection("/what-is/elasticsearch").Should().Be("concept");
		ContentStackMapper.GetNavigationSection("/webinars/live-event").Should().Be("webinar");
		ContentStackMapper.GetNavigationSection("/customers/acme").Should().Be("customer-story");
		ContentStackMapper.GetNavigationSection("/downloads/past-releases/es-8").Should().Be("download");
		ContentStackMapper.GetNavigationSection("/about/press/announcement").Should().Be("press");
		ContentStackMapper.GetNavigationSection("/about/leadership").Should().Be("about");
		ContentStackMapper.GetNavigationSection("/agreements/eula").Should().Be("legal");
		ContentStackMapper.GetNavigationSection("/pricing/cloud").Should().Be("pricing");
		ContentStackMapper.GetNavigationSection("/security/siem").Should().Be("product");
		ContentStackMapper.GetNavigationSection("/cloud/signup").Should().Be("marketing");
	}

	[Test]
	public void NavigationDepth_And_Toc_Are_Populated()
	{
		// /blog/getting-started-with-kibana-dashboards → 2 segments + 1 = 3
		var blogItem = LoadFixture("blog_legacy.json", "blog");
		var blogDoc = ContentStackMapper.ToSiteDocument(blogItem)!;
		blogDoc.Navigation.Depth.Should().Be(2 + 1);
		blogDoc.Navigation.TableOfContents.Should().Be(100);

		// /blog → 1 segment + 1 = 2, no body → 0 headings
		var minimalItem = LoadFixture("minimal_page.json", "blog_overview");
		var minimalDoc = ContentStackMapper.ToSiteDocument(minimalItem)!;
		minimalDoc.Navigation.Depth.Should().Be(1 + 1);
		minimalDoc.Navigation.TableOfContents.Should().Be(100);
	}

	[Test]
	public void GetLanguageFromUrl_Detects_Locale_Prefixes()
	{
		ContentStackMapper.GetLanguageFromUrl("/de/blog/post").Should().Be("de");
		ContentStackMapper.GetLanguageFromUrl("/fr/about").Should().Be("fr");
		ContentStackMapper.GetLanguageFromUrl("/jp/products").Should().Be("ja");
		ContentStackMapper.GetLanguageFromUrl("/kr/videos/talk").Should().Be("ko");
		ContentStackMapper.GetLanguageFromUrl("/cn/what-is/elk").Should().Be("zh");
		ContentStackMapper.GetLanguageFromUrl("/es/blog/post").Should().Be("es");
		ContentStackMapper.GetLanguageFromUrl("/pt/contact").Should().Be("pt");
		ContentStackMapper.GetLanguageFromUrl("/blog/english-post").Should().Be("en");
	}

	private static SyncItem LoadFromJson(string json, string contentTypeUid) =>
		new() { Type = "entry_published", ContentTypeUid = contentTypeUid, Data = JsonSerializer.Deserialize<JsonElement>(json) };

	/// <summary>
	/// Root cause: ContentStack "publishes" the same entry into multiple locale variants that
	/// share the same (unlocalized) url. A locale outside <c>LocaleUrlPrefixes</c> still must get
	/// its own document id — falling back to its base language subtag as the prefix (site-served
	/// prefixes are always two letters, never the full locale code) — otherwise it silently
	/// collides with (and can 409 against) another locale variant of the same url.
	/// Known gap: <see cref="ContentStackMapper.GetLanguageFromUrl"/> only recognizes the short
	/// prefixes in <c>LocaleUrlPrefixes</c>, so the reported <c>Locale</c> still falls back to
	/// "en" for an unmapped fallback prefix — the path is correctly namespaced (collision avoided)
	/// even though the locale label itself isn't accurate for this case.
	/// </summary>
	[Test]
	public void ToSiteDocument_UnprefixedUrl_UnmappedNonEnglishLocale_NamespacesUnderBaseLanguageSubtag()
	{
		var item = LoadFromJson(/*lang=json,strict*/
			"""
			{ "title": "Support Matrix", "url": "/support/matrix", "locale": "xx-yy",
			  "paragraph_l10n": "Supported versions." }
			""",
			"support_matrix"
		);
		var doc = ContentStackMapper.ToSiteDocument(item);

		doc.Should().NotBeNull();
		doc.Path.Should().Be("/xx/support/matrix");
		doc.Locale.Should().Be("en");
	}

	/// <summary>Missing locale is treated as the master (en-us) locale — no prefix.</summary>
	[Test]
	public void ToSiteDocument_MissingLocale_ResolvesToEnglish()
	{
		var item = LoadFromJson(/*lang=json,strict*/
			"""
			{ "title": "Support Matrix", "url": "/support/matrix",
			  "paragraph_l10n": "Supported versions." }
			""",
			"support_matrix"
		);
		var doc = ContentStackMapper.ToSiteDocument(item);

		doc.Should().NotBeNull();
		doc.Path.Should().Be("/support/matrix");
		doc.Locale.Should().Be("en");
	}

	/// <summary>Any en-* locale variant (not just the en-us master) is treated as English — no prefix.</summary>
	[Test]
	public void ToSiteDocument_EnglishVariantLocale_ResolvesToEnglish()
	{
		var item = LoadFromJson(/*lang=json,strict*/
			"""
			{ "title": "Support Matrix", "url": "/support/matrix", "locale": "en-gb",
			  "paragraph_l10n": "Supported versions." }
			""",
			"support_matrix"
		);
		var doc = ContentStackMapper.ToSiteDocument(item);

		doc.Should().NotBeNull();
		doc.Path.Should().Be("/support/matrix");
		doc.Locale.Should().Be("en");
	}

	/// <summary>
	/// A locale-prefixed url is trusted as-is even when the entry's own locale disagrees.
	/// </summary>
	[Test]
	public void ToSiteDocument_PrefixedUrl_ResolvesPerPrefix_RegardlessOfLocale()
	{
		var item = LoadFromJson(/*lang=json,strict*/
			"""
			{ "title": "Support Matrix", "url": "/de/support/matrix", "locale": "en-us",
			  "paragraph_l10n": "Supported versions." }
			""",
			"support_matrix"
		);
		var doc = ContentStackMapper.ToSiteDocument(item);

		doc.Should().NotBeNull();
		doc.Path.Should().Be("/de/support/matrix");
		doc.Locale.Should().Be("de");
	}

	/// <summary>
	/// Non-master-locale variants of an otherwise-unprefixed url get namespaced under the
	/// site-served locale prefix (https://www.elastic.co/es/support/matrix is a real, live route)
	/// so they land on their own document instead of colliding with the master (en-us) variant at
	/// the same Elasticsearch id.
	/// </summary>
	[Test]
	public void ToSiteDocument_NonMasterLocale_NamespacesUrlUnderSitePrefix()
	{
		var item = LoadFromJson(/*lang=json,strict*/
			"""
			{ "title": "Support Matrix", "url": "/support/matrix", "locale": "es-mx",
			  "paragraph_l10n": "Supported versions." }
			""",
			"support_matrix"
		);
		var doc = ContentStackMapper.ToSiteDocument(item);

		doc.Should().NotBeNull();
		doc.Path.Should().Be("/es/support/matrix");
		doc.Locale.Should().Be("es");
	}

	[Test]
	public void ToSiteDocument_SetsTranslated_ForContentStackContent()
	{
		var item = LoadFixture("blog_legacy.json", "blog");
		var doc = ContentStackMapper.ToSiteDocument(item);

		doc.Should().NotBeNull();
		doc.Translated.Should().BeTrue();
	}

	// --- Edge cases ---

	[Test]
	public void NullData_Returns_Null()
	{
		var item = new SyncItem { Type = "entry_published", ContentTypeUid = "blog", Data = null };
		ContentStackMapper.ToSiteDocument(item).Should().BeNull();
	}

	[Test]
	public void MissingUrl_Returns_Null()
	{
		var json = """{"title": "No URL Page"}""";
		var data = JsonSerializer.Deserialize<JsonElement>(json);
		var item = new SyncItem { Type = "entry_published", ContentTypeUid = "blog", Data = data };
		ContentStackMapper.ToSiteDocument(item).Should().BeNull();
	}

	[Test]
	public void MissingTitle_Returns_Null()
	{
		var json = """{"url": "/some/page"}""";
		var data = JsonSerializer.Deserialize<JsonElement>(json);
		var item = new SyncItem { Type = "entry_published", ContentTypeUid = "blog", Data = data };
		ContentStackMapper.ToSiteDocument(item).Should().BeNull();
	}
}
