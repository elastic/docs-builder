// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.SiteSearch.Cli.LabsCrawl;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elastic.SiteSearch.Tests;

public class LabsHtmlExtractorTests
{
	private readonly LabsHtmlExtractor _extractor = new(NullLogger<LabsHtmlExtractor>.Instance);

	/// <summary>
	/// Realistic HTML mirroring the structure of a live Labs blog page, including
	/// boilerplate that should be stripped: CTA banner, feedback widget, share
	/// buttons, and related-content cards.
	/// </summary>
	private const string SynonymsHtml = """
		<!DOCTYPE html>
		<html>
		<head>
			<title>How to use the Synonyms UI to upload and manage Elasticsearch synonyms</title>
			<meta name="description" content="Learn how to use the Synonyms UI in Kibana to create synonym sets and assign them to indices." />
			<meta property="og:title" content="How to use the Synonyms UI" />
			<meta property="article:published_time" content="2025-10-14T00:00:00Z" />
		</head>
		<body>
			<main class="article-content">
				<!-- CTA banner (noise) -->
				<div class="mb-32">
					<p class="mb-16">New to Elasticsearch? Join our
						<a href="https://www.elastic.co/virtual-events/getting-started-elasticsearch">getting started with Elasticsearch</a> webinar.
						You can also start a <a href="https://cloud.elastic.co/registration">free cloud trial</a> or try Elastic on your machine now.
					</p>
					<div class="mt-24 blog_ctaDivider__29j3v"></div>
				</div>

				<!-- Actual article content -->
				<div class="mb-48">
					<h1>How to use the Synonyms UI</h1>
					<h2>Index vs search-time synonyms</h2>
					<p>There are two ways to configure synonyms in Elasticsearch.</p>
					<h2>Creating synonym sets with the Synonyms UI</h2>
					<p>Synonym sets are groups of words that act as logic containers.</p>
					<h2>Equivalent synonyms (bidirectional)</h2>
					<p>A bidirectional synonym rule implies the terms are interchangeable.</p>
					<h2>Explicit synonyms (one-direction)</h2>
					<p>Explicit synonyms only go one way.</p>
					<h2>Conclusion</h2>
					<p>Synonyms are essential in search.</p>
				</div>

				<!-- Copy/Share buttons (noise) -->
				<div class="PageActions_actionTrigger__o0u_T">
					<button>Copy</button><button>Share</button>
				</div>

				<!-- Feedback widget (noise) -->
				<div class="Rating_ratings__npNsI mt-32">
					<h4>How helpful was this content?</h4>
					<button>Not helpful</button>
					<button>Somewhat helpful</button>
					<button>Very helpful</button>
				</div>

				<!-- Related content cards (noise) -->
				<div class="blog_containerDivider__c6Cjy mt-64">
					<h2>Related Content</h2>
					<div class="PostPreview_postPreview__kfp1i">
						<h3>Some other blog post title</h3>
						<p>By: Some Author</p>
					</div>
					<div class="PostPreview_postPreview__kfp1i">
						<h3>Another related article</h3>
						<p>By: Another Author</p>
					</div>
				</div>
			</main>
		</body>
		</html>
		""";

	[Test]
	public async Task Url_Uses_Path_When_Absolute_Uri()
	{
		var doc = await _extractor.ExtractAsync(
			"https://www.elastic.co/search-labs/blog/elasticsearch-synonyms-ui",
			SynonymsHtml,
			null,
			"en",
			"search-labs"
		);

		doc.Should().NotBeNull();
		doc.Url.Should().Be("/search-labs/blog/elasticsearch-synonyms-ui");
	}

	[Test]
	public async Task Url_Strips_Query_And_Fragment()
	{
		var doc = await _extractor.ExtractAsync(
			"https://www.elastic.co/search-labs/blog/post?utm=foo#section",
			SynonymsHtml,
			null,
			"en",
			"search-labs"
		);

		doc.Should().NotBeNull();
		doc.Url.Should().Be("/search-labs/blog/post");
	}

	[Test]
	public async Task Extracts_Title_And_Headings()
	{
		var doc = await _extractor.ExtractAsync(
			"https://www.elastic.co/search-labs/blog/elasticsearch-synonyms-ui",
			SynonymsHtml,
			null,
			"en",
			"search-labs"
		);

		doc.Should().NotBeNull();
		doc.Title.Should().Be("How to use the Synonyms UI");
		doc.Headings.Should().Contain("Index vs search-time synonyms");
		doc.Headings.Should().Contain("Equivalent synonyms (bidirectional)");
	}

	[Test]
	public async Task Returns_Null_For_Missing_Title()
	{
		const string html = """
			<html><head></head><body><main><p>No title here</p></main></body></html>
			""";

		var doc = await _extractor.ExtractAsync(
			"https://www.elastic.co/search-labs/blog/no-title",
			html,
			null,
			"en",
			"search-labs"
		);

		doc.Should().BeNull();
	}

	[Test]
	public async Task Body_Excludes_Cta_Banner()
	{
		var doc = await ExtractSynonymsDoc();

		doc.Should().NotBeNull();
		doc.Body.Should().NotContain("New to Elasticsearch");
		doc.Body.Should().NotContain("getting started with Elasticsearch");
		doc.Body.Should().NotContain("free cloud trial");
	}

	[Test]
	public async Task Body_Excludes_Feedback_Widget()
	{
		var doc = await ExtractSynonymsDoc();

		doc.Should().NotBeNull();
		doc.Body.Should().NotContain("How helpful was this content");
		doc.Body.Should().NotContain("Not helpful");
	}

	[Test]
	public async Task Body_Excludes_Related_Content()
	{
		var doc = await ExtractSynonymsDoc();

		doc.Should().NotBeNull();
		doc.Body.Should().NotContain("Related Content");
		doc.Body.Should().NotContain("Some other blog post title");
		doc.Body.Should().NotContain("Another related article");
	}

	[Test]
	public async Task Body_Excludes_Share_Buttons()
	{
		var doc = await ExtractSynonymsDoc();

		doc.Should().NotBeNull();
		doc.Body.Should().NotContain("CopyShare");
	}

	[Test]
	public async Task Headings_Exclude_Boilerplate_Headings()
	{
		var doc = await ExtractSynonymsDoc();

		doc.Should().NotBeNull();
		doc.Headings.Should().NotContain("How helpful was this content?");
		doc.Headings.Should().NotContain("Related Content");
		doc.Headings.Should().Contain("Index vs search-time synonyms");
		doc.Headings.Should().Contain("Conclusion");
	}

	[Test]
	public async Task Body_Retains_Article_Content()
	{
		var doc = await ExtractSynonymsDoc();

		doc.Should().NotBeNull();
		doc.Body.Should().Contain("two ways to configure synonyms");
		doc.Body.Should().Contain("Synonyms are essential in search");
	}

	[Test]
	[Arguments("https://www.elastic.co/search-labs/blog/post", "search-labs")]
	[Arguments("https://www.elastic.co/security-labs/research/finding", "security-labs")]
	[Arguments("https://www.elastic.co/blog/news-post", "blog")]
	[Arguments("https://www.elastic.co/what-is/elasticsearch", "concept")]
	public void GetNavigationSection_Resolves_Correctly(string url, string expected) =>
		LabsHtmlExtractor.GetNavigationSection(url).Should().Be(expected);

	/// <summary>
	/// Mirrors a real Labs blog page where the SEO <c>og:title</c>/<c>&lt;title&gt;</c> differs
	/// entirely from the article's own headline - e.g. "ES|QL Kibana: The ES|QL editor experience
	/// in Kibana" (og:title) vs. "Improving the ES|QL editor experience in Kibana" (h1).
	/// </summary>
	private const string EsqlEditorHtml = """
		<!DOCTYPE html>
		<html>
		<head>
			<title>ES|QL Kibana: The ES|QL editor experience in Kibana | Elastic</title>
			<meta name="description" content="With the new ES|QL language becoming GA, a new editor experience has been developed in Kibana to help users write faster and better queries. Features like live validation, improved autocomplete and quick fixes will streamline the ES|QL experience." />
			<meta property="og:title" content="ES|QL Kibana: The ES|QL editor experience in Kibana" />
			<meta property="article:published_time" content="2025-06-01T00:00:00Z" />
		</head>
		<body>
			<main>
				<h1>Improving the ES|QL editor experience in Kibana</h1>
				<p>With the new ES|QL language becoming GA, a new editor experience has been developed in Kibana.</p>
			</main>
		</body>
		</html>
		""";

	[Test]
	public async Task Title_Prefers_Article_Heading_Over_Seo_Title()
	{
		var doc = await _extractor.ExtractAsync(
			"https://www.elastic.co/search-labs/blog/improving-esql-editor-experience-in-kibana",
			EsqlEditorHtml,
			null,
			"en",
			"search-labs"
		);

		doc.Should().NotBeNull();
		doc.Title.Should().Be("Improving the ES|QL editor experience in Kibana");
		doc.SearchTitle.Should().Be("Improving the ES|QL editor experience in Kibana - Search Labs Blog");
		doc.OgTitle.Should().Be("ES|QL Kibana: The ES|QL editor experience in Kibana");
	}

	[Test]
	public async Task Abstract_Folds_In_Description_Without_Heading_Brackets()
	{
		var doc = await _extractor.ExtractAsync(
			"https://www.elastic.co/search-labs/blog/improving-esql-editor-experience-in-kibana",
			EsqlEditorHtml,
			null,
			"en",
			"search-labs"
		);

		doc.Should().NotBeNull();
		doc.Abstract.Should().StartWith("With the new ES|QL language becoming GA");
		doc.Abstract.Should().NotContain("[");
		doc.Abstract.Should().NotContain("]");
	}

	[Test]
	public async Task Root_Overview_Title_Gets_No_Redundant_SearchTitle_Suffix()
	{
		const string html = """
			<html><head><title>Search Labs | Elastic</title></head>
			<body><main><h1>Search Labs</h1><p>Technical content from the team behind Elasticsearch.</p></main></body></html>
			""";

		var doc = await _extractor.ExtractAsync("https://www.elastic.co/search-labs", html, null, "en", "search-labs");

		doc.Should().NotBeNull();
		doc.Title.Should().Be("Search Labs");
		doc.SearchTitle.Should().Be("Search Labs");
		doc.NavigationTableOfContents.Should().Be(10); // depth <= 1 → 10
	}

	private const string TagListingHtml = """
		<html>
		<head><title>google-cloud | Observability Labs | Elastic</title></head>
		<body>
		<main>
			<h1>google-cloud</h1>
			<div class="PostPreview_postPreview__kfp1i">
				<h3>Monitoring GCP with Elastic</h3>
				<time datetime="2025-03-01T00:00:00Z">March 1, 2025</time>
			</div>
			<div class="PostPreview_postPreview__kfp1i">
				<h3>GCP logs ingestion</h3>
				<time datetime="2025-08-15T00:00:00Z">August 15, 2025</time>
			</div>
		</main>
		</body>
		</html>
		""";

	[Test]
	public async Task Tag_Listing_Gets_Clean_Title_And_Static_Description()
	{
		var doc = await _extractor.ExtractAsync(
			"https://www.elastic.co/observability-labs/blog/tag/google-cloud",
			TagListingHtml,
			null,
			"en",
			"observability-labs"
		);

		doc.Should().NotBeNull();
		doc.Title.Should().Be("Articles tagged with 'Google Cloud'");
		doc.Description.Should().Be(
			"Recent Observability Labs articles tagged google-cloud. A curated listing of Observability " +
			"Labs blog posts, tutorials, and articles about google-cloud.");
		doc.Abstract.Should().Be(doc.Description);
		doc.Body.Should().BeEmpty();
		doc.Headings.Should().BeEmpty();
	}

	[Test]
	public async Task Tag_Listing_Uses_Most_Recent_Article_Date_As_Published_Date()
	{
		var doc = await _extractor.ExtractAsync(
			"https://www.elastic.co/observability-labs/blog/tag/google-cloud",
			TagListingHtml,
			null,
			"en",
			"observability-labs"
		);

		doc.Should().NotBeNull();
		doc.PublishedDate.Should().Be(DateTimeOffset.Parse("2025-08-15T00:00:00Z", System.Globalization.CultureInfo.InvariantCulture));
	}

	[Test]
	public async Task Tag_Listing_NavigationTableOfContents_Is_Not_Penalized_By_Missing_Headings()
	{
		var doc = await _extractor.ExtractAsync(
			"https://www.elastic.co/observability-labs/blog/tag/google-cloud",
			TagListingHtml,
			null,
			"en",
			"observability-labs"
		);

		doc.Should().NotBeNull();
		// 4 URL tokens (observability-labs, blog, tag, google-cloud) → depth > 1 → 100
		doc.NavigationTableOfContents.Should().Be(100);
	}

	[Test]
	public async Task Author_Listing_Gets_Clean_Title_And_Static_Description()
	{
		const string html = """
			<html>
			<head><title>Elastic Security Labs | Security Labs | Elastic</title></head>
			<body><main><h1>Elastic Security Labs</h1></main></body>
			</html>
			""";

		var doc = await _extractor.ExtractAsync(
			"https://www.elastic.co/security-labs/author/elastic-security-labs",
			html,
			null,
			"en",
			"security-labs"
		);

		doc.Should().NotBeNull();
		doc.Title.Should().Be("Articles written by Elastic Security Labs");
		doc.Description.Should().Be(
			"Articles written by Elastic Security Labs for Security Labs. A listing of Security Labs " +
			"blog posts, tutorials, and articles authored by Elastic Security Labs.");
		doc.Body.Should().BeEmpty();
	}

	[Test]
	[Arguments("https://www.elastic.co/search-labs/blog/post", "en")]
	[Arguments("https://www.elastic.co/de/blog/post", "de")]
	[Arguments("https://www.elastic.co/fr/blog/post", "fr")]
	[Arguments("https://www.elastic.co/jp/blog/post", "ja")]
	public void GetLanguageFromUrl_Resolves_Correctly(string url, string expected) =>
		LabsHtmlExtractor.GetLanguageFromUrl(url).Should().Be(expected);

	private Task<Elastic.Documentation.Search.Contract.LabsDocument?> ExtractSynonymsDoc() =>
		_extractor.ExtractAsync(
			"https://www.elastic.co/search-labs/blog/elasticsearch-synonyms-ui",
			SynonymsHtml,
			null,
			"en",
			"search-labs"
		);
}
