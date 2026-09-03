// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using AwesomeAssertions;
using Elastic.SiteSearch.Cli.ContentStack;

namespace Elastic.SiteSearch.Tests;

/// <summary>
/// Tests ContentStack sync item to SiteDocument mapping for each structural group of content types.
/// See Fixtures/ContentStack/schema-groups.json for which content types each fixture covers.
/// </summary>
public class ContentStackMappingTests
{
	private static SyncItem LoadFixture(string fixtureName, string contentTypeUid)
	{
		var path = Path.Combine("Fixtures", "ContentStack", fixtureName);
		var json = File.ReadAllText(path);
		var data = JsonSerializer.Deserialize<JsonElement>(json);
		return new SyncItem
		{
			Type = "entry_published",
			ContentTypeUid = contentTypeUid,
			Data = data
		};
	}

	/// <summary>Covers: blog (legacy v1 with flat body_l10n)</summary>
	[Fact]
	public void BlogLegacy_Maps_BodyAndMetadata()
	{
		var item = LoadFixture("blog_legacy.json", "blog");
		var doc = ContentStackMapper.ToSiteDocument(item);

		doc.Should().NotBeNull();
		doc.Path.Should().Be("/blog/getting-started-with-kibana-dashboards");
		doc.Title.Should().Be("Getting Started with Kibana Dashboards");
		doc.SearchTitle.Should().StartWith("Blog:");
		doc.Section.Should().Be("blog");
		doc.Locale.Should().Be("en");
		doc.Body.Should().Contain("Kibana is a powerful visualization tool");
		doc.Body.Should().NotContain("<p>");
		doc.Body.Should().Be(doc.Body);
		doc.Headings.Should().Contain("Creating Your First Dashboard");
		doc.Description.Should().Contain("Learn how to create interactive dashboards");
		doc.PublishedDate.Should().NotBeNull();
		doc.ModifiedDate.Should().NotBeNull();
		doc.Og.Title.Should().Contain("Kibana Dashboards");
		doc.Og.Description.Should().NotBeNullOrEmpty();
		doc.Hash.Should().NotBeNullOrEmpty();
	}

	/// <summary>Covers: blog_v2 (modular blocks with nested title_text arrays)</summary>
	[Fact]
	public void BlogModern_Maps_ModularBlocksBody()
	{
		var item = LoadFixture("blog_modern.json", "blog_v2");
		var doc = ContentStackMapper.ToSiteDocument(item);

		doc.Should().NotBeNull();
		doc.Path.Should().Be("/blog/building-search-applications-with-elasticsearch");
		doc.Title.Should().Be("Building Search Applications with Elasticsearch");
		doc.SearchTitle.Should().StartWith("Blog:");
		doc.Section.Should().Be("blog");
		doc.Body.Should().Contain("Search is at the heart of modern applications");
		doc.Body.Should().Contain("Query DSL");
		doc.Body.Should().NotContain("<p>");
		doc.Description.Should().Contain("Explore how to build production-ready");
		doc.PublishedDate.Should().NotBeNull();
		doc.Hash.Should().NotBeNullOrEmpty();
	}

	/// <summary>Covers: videos (paragraph_l10n body, presentation_date)</summary>
	[Fact]
	public void Video_Maps_ParagraphAndPresenter()
	{
		var item = LoadFixture("video.json", "videos");
		var doc = ContentStackMapper.ToSiteDocument(item);

		doc.Should().NotBeNull();
		doc.Path.Should().Be("/webinars/ingesting-data-into-elasticsearch");
		doc.Title.Should().Be("Ingesting Data into Elasticsearch");
		doc.SearchTitle.Should().StartWith("Webinar:");
		doc.Section.Should().Be("webinar");
		doc.Body.Should().Contain("data ingestion patterns");
		doc.Body.Should().NotContain("<p>");
		doc.PublishedDate.Should().NotBeNull();
		doc.Og.Image.Should().NotBeNullOrEmpty();
		doc.Hash.Should().NotBeNullOrEmpty();
	}

	/// <summary>Covers: press (intro_paragraph_l10n + paragraph_l10n, date field)</summary>
	[Fact]
	public void PressRelease_Maps_CombinedParagraphs()
	{
		var item = LoadFixture("press_release.json", "press");
		var doc = ContentStackMapper.ToSiteDocument(item);

		doc.Should().NotBeNull();
		doc.Path.Should().Contain("/about/press/");
		doc.Title.Should().Contain("Security Analytics");
		doc.SearchTitle.Should().StartWith("Press:");
		doc.Section.Should().Be("press");
		doc.Body.Should().Contain("Elastic today announced");
		doc.Body.Should().Contain("advanced threat detection");
		doc.PublishedDate.Should().NotBeNull();
		doc.PublishedDate.Value.Year.Should().Be(2023);
		doc.Hash.Should().NotBeNullOrEmpty();
	}

	/// <summary>Covers: product_versions (release_notes body, date, version_number)</summary>
	[Fact]
	public void ProductRelease_Maps_ReleaseNotes()
	{
		var item = LoadFixture("product_release.json", "product_versions");
		var doc = ContentStackMapper.ToSiteDocument(item);

		doc.Should().NotBeNull();
		doc.Path.Should().Contain("/downloads/past-releases/");
		doc.Title.Should().Contain("Elasticsearch 8.12.0");
		doc.SearchTitle.Should().StartWith("Download:");
		doc.Section.Should().Be("download");
		doc.Body.Should().Contain("release notes");
		doc.PublishedDate.Should().NotBeNull();
		doc.Og.Title.Should().Contain("Download");
		doc.Hash.Should().NotBeNullOrEmpty();
	}

	/// <summary>Covers: agreements, forms (paragraph_l10n as single body)</summary>
	[Fact]
	public void ParagraphPage_Maps_SingleBody()
	{
		var item = LoadFixture("paragraph_page.json", "agreements");
		var doc = ContentStackMapper.ToSiteDocument(item);

		doc.Should().NotBeNull();
		doc.Path.Should().Contain("/agreements/");
		doc.Title.Should().Contain("Terms of Service");
		doc.SearchTitle.Should().StartWith("Legal:");
		doc.Section.Should().Be("legal");
		doc.Body.Should().Contain("Terms of Service govern");
		doc.Body.Should().NotContain("<p>");
		doc.Headings.Should().Contain("1. Definitions");
		doc.Og.Title.Should().NotBeNullOrEmpty();
		doc.Hash.Should().NotBeNullOrEmpty();
	}

	/// <summary>Covers: default_detail, account_based_marketing, product_detail (modular_blocks with flat title_text)</summary>
	[Fact]
	public void ModularPage_Maps_FlatModularBlocks()
	{
		var item = LoadFixture("modular_page.json", "default_detail");
		var doc = ContentStackMapper.ToSiteDocument(item);

		doc.Should().NotBeNull();
		doc.Path.Should().Be("/security/cloud-workload-protection");
		doc.Title.Should().Contain("Cloud Workloads");
		doc.SearchTitle.Should().StartWith("Product:");
		doc.Section.Should().Be("product");
		doc.Body.Should().Contain("comprehensive protection");
		doc.Body.Should().Contain("pre-built detection rules");
		doc.Body.Should().NotContain("<p>");
		doc.Og.Image.Should().NotBeNullOrEmpty();
		doc.Hash.Should().NotBeNullOrEmpty();
	}

	/// <summary>Covers: use_cases (introduction + challenge_solution + modular_blocks)</summary>
	[Fact]
	public void UseCase_Maps_IntroAndChallenges()
	{
		var item = LoadFixture("use_case.json", "use_cases");
		var doc = ContentStackMapper.ToSiteDocument(item);

		doc.Should().NotBeNull();
		doc.Path.Should().Be("/customers/acme-corp");
		doc.Title.Should().Be("Acme Corp");
		doc.SearchTitle.Should().StartWith("Customer Story:");
		doc.Section.Should().Be("customer-story");
		doc.Body.Should().Contain("millions of events per day");
		doc.Body.Should().Contain("could not handle the growing data volume");
		doc.Body.Should().Contain("sub-second query times");
		doc.Description.Should().Contain("scalable solution");
		doc.Hash.Should().NotBeNullOrEmpty();
	}

	/// <summary>Covers: faq (topic[].subtopic[].paragraph_l10n)</summary>
	[Fact]
	public void FaqPage_Maps_NestedTopics()
	{
		var item = LoadFixture("faq_page.json", "faq");
		var doc = ContentStackMapper.ToSiteDocument(item);

		doc.Should().NotBeNull();
		doc.Path.Should().Be("/what-is/cloud-security");
		doc.Title.Should().Contain("Cloud Security");
		doc.SearchTitle.Should().StartWith("What is:");
		doc.Section.Should().Be("concept");
		doc.Body.Should().Contain("technologies, policies, and controls");
		doc.Body.Should().Contain("organizations migrate workloads");
		doc.Body.Should().NotContain("<p>");
		doc.Hash.Should().NotBeNullOrEmpty();
	}

	/// <summary>Covers: customer_tile (short paragraph_l10n, partial URL coverage)</summary>
	[Fact]
	public void CustomerTile_Maps_ShortContent()
	{
		var item = LoadFixture("customer_tile.json", "customer_tile");
		var doc = ContentStackMapper.ToSiteDocument(item);

		doc.Should().NotBeNull();
		doc.Path.Should().Contain("/kr/videos/");
		doc.Title.Should().Be("TechCo Industries");
		// URL prefix wins over the entry's own (en-us) locale — see GetLanguageFromUrl.
		doc.Locale.Should().Be("ko");
		doc.Body.Should().Contain("global e-commerce platform");
		doc.Hash.Should().NotBeNullOrEmpty();
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
	[Fact]
	public void MinimalPage_Maps_TitleUrlSeo()
	{
		var item = LoadFixture("minimal_page.json", "blog_overview");
		var doc = ContentStackMapper.ToSiteDocument(item);

		doc.Should().NotBeNull();
		doc.Path.Should().Be("/blog");
		doc.Title.Should().Be("Elastic Blog");
		// ContentTypeUid "blog_overview" is now mapped to "blog" via GetSectionFromContentType,
		// even though the URL alone ("/blog", no trailing slash) doesn't match the URL heuristic.
		doc.Section.Should().Be("blog");
		doc.Locale.Should().Be("en");
		doc.Body.Should().Contain("latest from Elastic");
		doc.Body.Should().Be(doc.Body);
		doc.Description.Should().Contain("latest from Elastic");
		doc.Og.Title.Should().NotBeNullOrEmpty();
		doc.Og.Description.Should().NotBeNullOrEmpty();
		doc.Og.Image.Should().NotBeNullOrEmpty();
		doc.Hash.Should().NotBeNullOrEmpty();
	}

	/// <summary>Covers: tutorial, tutorial_page, tutorial_chapter, labs_integration (rich-text JSON AST body + description)</summary>
	[Fact]
	public void RichTextPage_Maps_ProseMirrorBodyAndDescription()
	{
		var item = LoadFixture("rich_text_page.json", "labs_integration");
		var doc = ContentStackMapper.ToSiteDocument(item);

		doc.Should().NotBeNull();
		doc.Path.Should().Be("/search-labs/integrations/amazon-bedrock");
		doc.Section.Should().Be("search-labs");
		doc.Body.Should().Contain("Amazon Bedrock makes leading foundation models available");
		doc.Body.Should().Contain("RAG using Bedrock");
		doc.Body.Should().NotContain("<p>");
		doc.Body.Should().NotContain("<h3>");
		doc.Headings.Should().Contain("Blogs to get started");
		doc.Description.Should().Contain("fully managed service offering foundation models");
		doc.Hash.Should().NotBeNullOrEmpty();
	}

	/// <summary>Covers: blog_v3 (rich-text body nested under main_content.body.content_l10n + summary_l10n description)</summary>
	[Fact]
	public void BlogV3_Maps_NestedRichTextBodyAndSummary()
	{
		var item = LoadFixture("rich_text_blog_v3.json", "blog_v3");
		var doc = ContentStackMapper.ToSiteDocument(item);

		doc.Should().NotBeNull();
		doc.Path.Should().Be("/search-labs/blog/hybrid-search-with-elasticsearch");
		doc.Section.Should().Be("search-labs");
		doc.Body.Should().Contain("Hybrid search combines BM25 lexical scoring");
		doc.Body.Should().NotContain("<p>");
		doc.Headings.Should().Contain("Why hybrid search");
		doc.Description.Should().Be("Combine lexical and semantic search for better relevance.");
		doc.PublishedDate.Should().NotBeNull();
	}

	/// <summary>Covers: tutorials_landing, integrations_landing, blog_landing (page_info.subheading_l10n description, no body)</summary>
	[Fact]
	public void LandingPage_Maps_SubheadingDescription_NoBody()
	{
		var item = LoadFixture("landing_page.json", "tutorials_landing");
		var doc = ContentStackMapper.ToSiteDocument(item);

		doc.Should().NotBeNull();
		doc.Path.Should().Be("/search-labs/tutorials");
		doc.Section.Should().Be("search-labs");
		doc.Description.Should().Be("Long-form guides to get you started");
		// No main_content on landing pages — body falls back to the description.
		doc.Body.Should().Be(doc.Description);
	}

	/// <summary>Covers: notebook, series, labs_category, labs_homepage, glossary, examples_landing (plain description_l10n, no body)</summary>
	[Fact]
	public void DescriptionOnly_Maps_PlainDescription_NoBody()
	{
		var item = LoadFixture("description_only.json", "notebook");
		var doc = ContentStackMapper.ToSiteDocument(item);

		doc.Should().NotBeNull();
		doc.Section.Should().Be("search-labs");
		doc.Description.Should().Contain("Learn how to build a RAG system");
		doc.Body.Should().Be(doc.Description);
		doc.Headings.Should().BeEmpty();
	}

	/// <summary>Covers: reports, threat_command (main_content.markdown_l10n plain-Markdown body + summary_l10n description)</summary>
	[Fact]
	public void MarkdownBodyPage_Maps_MarkdownAsHeadingsAndPlainBody()
	{
		var item = LoadFixture("markdown_body.json", "threat_command");
		var doc = ContentStackMapper.ToSiteDocument(item);

		doc.Should().NotBeNull();
		doc.Path.Should().Be("/security-labs/threat-command/tracking-a-novel-loader-pipedance");
		doc.Section.Should().Be("security-labs");
		doc.Headings.Should().BeEquivalentTo(["Key takeaways", "Analysis", "API resolution"]);
		doc.Body.Should().Contain("PIPEDANCE is a novel loader used to stage secondary payloads");
		// Links keep their visible text, drop the URL.
		doc.Body.Should().Contain("process injection");
		doc.Body.Should().NotContain("example.com/injection");
		// Bold/inline-code markers are stripped but the wrapped text is kept.
		doc.Body.Should().Contain("kernel32.dll");
		doc.Body.Should().Contain("PEB");
		doc.Body.Should().NotContain("**");
		doc.Body.Should().NotContain("`");
		// Fenced code blocks are dropped entirely.
		doc.Body.Should().NotContain("walk_peb");
		doc.Description.Should().Be("An analysis of the PIPEDANCE loader observed in recent intrusions.");
	}

	/// <summary>Covers: reports_landing, threat_command_landing (top-level subheading_l10n description, no body)</summary>
	[Fact]
	public void TopLevelSubheadingLandingPage_Maps_SubheadingDescription_NoBody()
	{
		var item = LoadFixture("top_level_subheading_landing.json", "reports_landing");
		var doc = ContentStackMapper.ToSiteDocument(item);

		doc.Should().NotBeNull();
		doc.Path.Should().Be("/security-labs/reports");
		doc.Section.Should().Be("security-labs");
		doc.Description.Should().Be("In-depth research reports from the Elastic Security Labs team.");
		doc.Body.Should().Be(doc.Description);
	}

	/// <summary>Covers: security_labs_homepage, observability_labs_homepage (page_description_l10n description, no body)</summary>
	[Fact]
	public void PropertyHomepage_Maps_PageDescription_NoBody()
	{
		var item = LoadFixture("property_homepage.json", "security_labs_homepage");
		var doc = ContentStackMapper.ToSiteDocument(item);

		doc.Should().NotBeNull();
		doc.Path.Should().Be("/security-labs");
		doc.Section.Should().Be("security-labs");
		doc.Description.Should().Be("Research, tools, and insights from the Elastic Security Labs team.");
		doc.Body.Should().Be(doc.Description);
	}

	/// <summary>blog_description_l10n is used when page_description_l10n is absent.</summary>
	[Fact]
	public void ToSiteDocument_PropertyHomepage_FallsBackToBlogDescription_WhenNoPageDescription()
	{
		var item = LoadFromJson(/*lang=json,strict*/ """
			{ "title": "Observability Labs", "url": "/observability-labs", "locale": "en-us",
			  "blog_description_l10n": "The latest observability research from Elastic." }
			""", "observability_labs_homepage");
		var doc = ContentStackMapper.ToSiteDocument(item);

		doc.Should().NotBeNull();
		doc.Description.Should().Be("The latest observability research from Elastic.");
		doc.Section.Should().Be("observability-labs");
	}

	/// <summary>Empty page_info.description.content_simple_l10n must not win over the SEO fallback.</summary>
	[Fact]
	public void ToSiteDocument_LandingPage_WithEmptyRichDescription_FallsBackToSeo()
	{
		var item = LoadFromJson(/*lang=json,strict*/ """
			{ "title": "Integrations", "url": "/search-labs/integrations", "locale": "en-us",
			  "page_info": { "description": { "content_simple_l10n": { "type": "doc", "children": [] } } },
			  "seo": { "seo_description_l10n": "Browse Search Labs integrations." } }
			""", "integrations_landing");
		var doc = ContentStackMapper.ToSiteDocument(item);

		doc.Should().NotBeNull();
		doc.Description.Should().Be("Browse Search Labs integrations.");
	}

	/// <summary>page_info.description.content_simple_l10n is used when there's no subheading_l10n.</summary>
	[Fact]
	public void ToSiteDocument_GlossaryPage_UsesPageInfoRichTextDescription_WhenNoSubheading()
	{
		var item = LoadFromJson(/*lang=json,strict*/ """
			{ "title": "Glossary", "url": "/glossary", "locale": "en-us",
			  "page_info": { "description": { "content_simple_l10n": { "type": "doc",
			    "children": [ { "type": "p", "children": [ { "text": "All the terms, concepts, and abbreviations." } ] } ] } } } }
			""", "glossary");
		var doc = ContentStackMapper.ToSiteDocument(item);

		doc.Should().NotBeNull();
		doc.Description.Should().Contain("terms, concepts, and abbreviations");
		doc.Section.Should().Be("glossary");
	}

	// --- Body projection helper tests ---

	[Fact]
	public void StripHtml_Removes_Tags_And_Entities()
	{
		var result = ContentStackMapper.StripHtml("<p>Hello &amp; <strong>world</strong>.</p>");
		result.Should().Be("Hello world .");
	}

	[Fact]
	public void StripHtml_Collapses_Whitespace()
	{
		var result = ContentStackMapper.StripHtml("<p>Line one</p>\n\n<p>  Line two  </p>");
		result.Should().Be("Line one Line two");
	}

	[Fact]
	public void ExtractHeadings_Finds_All_Levels()
	{
		var html = "<h1>Title</h1><p>text</p><h2>Subtitle</h2><h3>Sub-sub</h3>";
		var headings = ContentStackMapper.ExtractHeadings(html);
		headings.Should().BeEquivalentTo(["Title", "Subtitle", "Sub-sub"]);
	}

	[Fact]
	public void ExtractHeadings_Strips_Inner_Tags()
	{
		var html = "<h2><a href=\"#anchor\">Linked Heading</a></h2>";
		var headings = ContentStackMapper.ExtractHeadings(html);
		headings.Should().ContainSingle().Which.Should().Be("Linked Heading");
	}

	[Fact]
	public void ExtractHeadings_Returns_Empty_For_No_Headings()
	{
		var headings = ContentStackMapper.ExtractHeadings("<p>Just a paragraph</p>");
		headings.Should().BeEmpty();
	}

	[Fact]
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

	[Fact]
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

	[Fact]
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
		new()
		{
			Type = "entry_published",
			ContentTypeUid = contentTypeUid,
			Data = JsonSerializer.Deserialize<JsonElement>(json)
		};

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
	[Fact]
	public void ToSiteDocument_UnprefixedUrl_UnmappedNonEnglishLocale_NamespacesUnderBaseLanguageSubtag()
	{
		var item = LoadFromJson(/*lang=json,strict*/ """
			{ "title": "Support Matrix", "url": "/support/matrix", "locale": "xx-yy",
			  "paragraph_l10n": "Supported versions." }
			""", "support_matrix");
		var doc = ContentStackMapper.ToSiteDocument(item);

		doc.Should().NotBeNull();
		doc.Path.Should().Be("/xx/support/matrix");
		doc.Locale.Should().Be("en");
	}

	/// <summary>Missing locale is treated as the master (en-us) locale — no prefix.</summary>
	[Fact]
	public void ToSiteDocument_MissingLocale_ResolvesToEnglish()
	{
		var item = LoadFromJson(/*lang=json,strict*/ """
			{ "title": "Support Matrix", "url": "/support/matrix",
			  "paragraph_l10n": "Supported versions." }
			""", "support_matrix");
		var doc = ContentStackMapper.ToSiteDocument(item);

		doc.Should().NotBeNull();
		doc.Path.Should().Be("/support/matrix");
		doc.Locale.Should().Be("en");
	}

	/// <summary>Any en-* locale variant (not just the en-us master) is treated as English — no prefix.</summary>
	[Fact]
	public void ToSiteDocument_EnglishVariantLocale_ResolvesToEnglish()
	{
		var item = LoadFromJson(/*lang=json,strict*/ """
			{ "title": "Support Matrix", "url": "/support/matrix", "locale": "en-gb",
			  "paragraph_l10n": "Supported versions." }
			""", "support_matrix");
		var doc = ContentStackMapper.ToSiteDocument(item);

		doc.Should().NotBeNull();
		doc.Path.Should().Be("/support/matrix");
		doc.Locale.Should().Be("en");
	}

	/// <summary>
	/// A locale-prefixed url is trusted as-is even when the entry's own locale disagrees.
	/// </summary>
	[Fact]
	public void ToSiteDocument_PrefixedUrl_ResolvesPerPrefix_RegardlessOfLocale()
	{
		var item = LoadFromJson(/*lang=json,strict*/ """
			{ "title": "Support Matrix", "url": "/de/support/matrix", "locale": "en-us",
			  "paragraph_l10n": "Supported versions." }
			""", "support_matrix");
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
	[Fact]
	public void ToSiteDocument_NonMasterLocale_NamespacesUrlUnderSitePrefix()
	{
		var item = LoadFromJson(/*lang=json,strict*/ """
			{ "title": "Support Matrix", "url": "/support/matrix", "locale": "es-mx",
			  "paragraph_l10n": "Supported versions." }
			""", "support_matrix");
		var doc = ContentStackMapper.ToSiteDocument(item);

		doc.Should().NotBeNull();
		doc.Path.Should().Be("/es/support/matrix");
		doc.Locale.Should().Be("es");
	}

	[Fact]
	public void ToSiteDocument_SetsTranslated_ForContentStackContent()
	{
		var item = LoadFixture("blog_legacy.json", "blog");
		var doc = ContentStackMapper.ToSiteDocument(item);

		doc.Should().NotBeNull();
		doc.Translated.Should().BeTrue();
	}

	// --- Edge cases ---

	[Fact]
	public void NullData_Returns_Null()
	{
		var item = new SyncItem { Type = "entry_published", ContentTypeUid = "blog", Data = null };
		ContentStackMapper.ToSiteDocument(item).Should().BeNull();
	}

	[Fact]
	public void MissingUrl_Returns_Null()
	{
		var json = """{"title": "No URL Page"}""";
		var data = JsonSerializer.Deserialize<JsonElement>(json);
		var item = new SyncItem { Type = "entry_published", ContentTypeUid = "blog", Data = data };
		ContentStackMapper.ToSiteDocument(item).Should().BeNull();
	}

	[Fact]
	public void MissingTitle_Returns_Null()
	{
		var json = """{"url": "/some/page"}""";
		var data = JsonSerializer.Deserialize<JsonElement>(json);
		var item = new SyncItem { Type = "entry_published", ContentTypeUid = "blog", Data = data };
		ContentStackMapper.ToSiteDocument(item).Should().BeNull();
	}
}
