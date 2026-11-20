// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using System.Text.Json.Nodes;
using AwesomeAssertions;
using Elastic.Documentation.Assembler.Building;
using Elastic.Documentation.Diagnostics;
using Nullean.Snapshots;

namespace Elastic.Documentation.Build.Tests;

public class SitemapTests
{
	private static readonly DateTimeOffset Date = new(2026, 11, 1, 0, 0, 0, TimeSpan.Zero);

	private static readonly Dictionary<string, DateTimeOffset> Entries = new()
	{
		["/docs/reference"] = Date,
		["/docs/apis-and-more"] = Date,
		["/docs/api"] = Date,
		["/docs/api/elasticsearch/operation/search"] = Date
	};

	private static (MockFileSystem FileSystem, SitemapOutput Output) Output(string host = "https://www.elastic.co")
	{
		var fileSystem = new MockFileSystem();
		return (fileSystem, new SitemapOutput(fileSystem, fileSystem.DirectoryInfo.New("/output/docs"), "docs", new Uri(host)));
	}

	[Test]
	public void Generate_WithApis_ListsANarrativeAndAnApiSitemapInTheIndex()
	{
		var (fileSystem, output) = Output();

		_ = SitemapBuilder.Generate(Entries, output, new DiagnosticsCollector([]));

		fileSystem
			.File
			.ReadAllText("/output/docs/sitemap.xml")
			.Should()
			.MatchInlineSnapshot(
				"""
			<?xml version="1.0" encoding="utf-8" standalone="yes"?>
			<sitemapindex xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">
			  <sitemap>
			    <loc>https://www.elastic.co/docs/sitemap-docs.xml</loc>
			  </sitemap>
			  <sitemap>
			    <loc>https://www.elastic.co/docs/sitemap-api.xml</loc>
			  </sitemap>
			</sitemapindex>
			"""
			);
		fileSystem
			.File
			.ReadAllText("/output/docs/sitemap-docs.xml")
			.Should()
			.MatchInlineSnapshot(
				"""
			<?xml version="1.0" encoding="utf-8" standalone="yes"?>
			<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">
			  <url>
			    <loc>https://www.elastic.co/docs/apis-and-more</loc>
			    <lastmod>2026-11-01T00:00:00.0000000+00:00</lastmod>
			  </url>
			  <url>
			    <loc>https://www.elastic.co/docs/reference</loc>
			    <lastmod>2026-11-01T00:00:00.0000000+00:00</lastmod>
			  </url>
			</urlset>
			"""
			);
		fileSystem
			.File
			.ReadAllText("/output/docs/sitemap-api.xml")
			.Should()
			.MatchInlineSnapshot(
				"""
			<?xml version="1.0" encoding="utf-8" standalone="yes"?>
			<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">
			  <url>
			    <loc>https://www.elastic.co/docs/api</loc>
			    <lastmod>2026-11-01T00:00:00.0000000+00:00</lastmod>
			  </url>
			  <url>
			    <loc>https://www.elastic.co/docs/api/elasticsearch/operation/search</loc>
			    <lastmod>2026-11-01T00:00:00.0000000+00:00</lastmod>
			  </url>
			</urlset>
			"""
			);
	}

	[Test]
	public void Generate_WithoutApiEntriesOrAnApiFile_ListsOnlyTheNarrativeSitemap()
	{
		var (fileSystem, output) = Output();

		var files = SitemapBuilder.Generate(
			Entries.Where(e => !output.IsApiUrl(e.Key)).ToDictionary(),
			output,
			new DiagnosticsCollector([])
		);

		files.Select(f => $"{f.Name} {f.EntryCount}").Should().Equal("sitemap-docs.xml 2");
		fileSystem.File.Exists("/output/docs/sitemap-api.xml").Should().BeFalse();
		fileSystem.File.ReadAllText("/output/docs/sitemap.xml").Should().NotContain("sitemap-api.xml");
	}

	[Test]
	public void Generate_NarrativePagesOnly_KeepsTheApiSitemapTheBuildWrote()
	{
		var (fileSystem, output) = Output();
		_ = SitemapBuilder.Generate(Entries, output, new DiagnosticsCollector([]));
		var apiSitemap = fileSystem.File.ReadAllText("/output/docs/sitemap-api.xml");

		// what `assembler sitemap` passes: the narrative pages of the search index, with their dates
		var files = SitemapBuilder.Generate(
			new Dictionary<string, DateTimeOffset> { ["/docs/reference"] = Date.AddDays(3) },
			output,
			new DiagnosticsCollector([])
		);

		files.Select(f => $"{f.Name} {f.EntryCount}").Should().Equal("sitemap-docs.xml 1");
		fileSystem
			.File
			.ReadAllText("/output/docs/sitemap-docs.xml")
			.Should()
			.MatchInlineSnapshot(
				"""
			<?xml version="1.0" encoding="utf-8" standalone="yes"?>
			<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">
			  <url>
			    <loc>https://www.elastic.co/docs/reference</loc>
			    <lastmod>2026-11-04T00:00:00.0000000+00:00</lastmod>
			  </url>
			</urlset>
			"""
			);
		fileSystem.File.ReadAllText("/output/docs/sitemap-api.xml").Should().Be(apiSitemap);
		fileSystem.File.ReadAllText("/output/docs/sitemap.xml").Should().Contain("sitemap-api.xml");
	}

	[Test]
	public void EsSearchBody_ReadsVisibleNarrativePagesAndEveryApiDocument() =>
		JsonNode.Parse(EsSitemapReader.BuildSearchBody("pit", null))!["query"]!
			.ToJsonString()
			.Should()
			.MatchInlineSnapshot(
				"{\"bool\":{\"should\":[{\"term\":{\"content_type\":\"api\"}},{\"bool\":{\"must_not\":[{\"term\":{\"hidden\":true}}]}}],\"minimum_should_match\":1}}"
			);

	[Test]
	public void ApiDates_DateEachPageOfTheBuildFromTheDocumentOfItsTree()
	{
		var build = Date.AddDays(10);
		Dictionary<string, DateTimeOffset> buildPages = new()
		{
			["/docs/api"] = build,
			["/docs/api/elasticsearch"] = build,
			["/docs/api/elasticsearch/operation/search"] = build,
			["/docs/api/elasticsearch/v8/operation/search"] = build,
			["/docs/api/elasticsearch/group/endpoint-search"] = build,
			["/docs/api/elasticsearch/v8/group/endpoint-search"] = build,
			["/docs/api/kibana/operation/get-status"] = build
		};
		SitemapEntry[] documents =
		[
			new("/docs/reference", Date, []),
			new("/docs/api/elasticsearch", Date.AddDays(1), ["/docs/api/elasticsearch"], ApiLanding: true),
			new("/docs/api/kibana", Date.AddDays(4), ["/docs/api/kibana"], ApiLanding: true),
			// one document for the page in both trees
			new("/docs/api/elasticsearch/operation/search", Date.AddDays(2), ["/docs/api/elasticsearch", "/docs/api/elasticsearch/v8"]),
			// the page differs per tree: two documents
			new("/docs/api/elasticsearch/group/endpoint-search", Date.AddDays(3), ["/docs/api/elasticsearch"]),
			new("/docs/api/elasticsearch/v8/group/endpoint-search", Date, ["/docs/api/elasticsearch/v8"])
		];

		var api = ApiSitemapDates.Resolve(buildPages, documents, "/docs/api");

		api
			.Entries
			.OrderBy(e => e.Key, StringComparer.Ordinal)
			.Select(e => $"{e.Key} {e.Value:yyyy-MM-dd}")
			.Should()
			.Equal(
				"/docs/api 2026-11-05",
				"/docs/api/elasticsearch 2026-11-02",
				"/docs/api/elasticsearch/group/endpoint-search 2026-11-04",
				"/docs/api/elasticsearch/operation/search 2026-11-03",
				"/docs/api/elasticsearch/v8/group/endpoint-search 2026-11-01",
				"/docs/api/elasticsearch/v8/operation/search 2026-11-03",
				"/docs/api/kibana/operation/get-status 2026-11-11"
			);
		api.Missing.Should().Equal(["/docs/api/kibana/operation/get-status"], "no document stands for it, so it keeps the build date");
	}

	[Test]
	public void ReadApiPages_ReadsThePathsAndDatesTheBuildWrote()
	{
		var (_, output) = Output();
		_ = SitemapBuilder.Generate(Entries, output, new DiagnosticsCollector([]));

		SitemapBuilder
			.ReadApiPages(output)
			.Should()
			.BeEquivalentTo(new Dictionary<string, DateTimeOffset>
			{
				["/docs/api"] = Date,
				["/docs/api/elasticsearch/operation/search"] = Date
			});
	}

	// only AssemblerBuildService decides to clean the output; `assembler sitemap` needs the API sitemap `assembler build` left there
	[Test]
	public async Task Build_IntoAnEarlierBuildsOutput_KeepsItsApiSitemapListedInTheIndex()
	{
		var (fileSystem, earlier) = Output();
		_ = SitemapBuilder.Generate(Entries, earlier, new DiagnosticsCollector([]));

		var site = await InMemoryAssemblerSite.BuildAsync(CancellationToken.None, fileSystem: fileSystem);

		site.Read("sitemap.xml").Should().Contain("<loc>https://www.elastic.co/docs/sitemap-api.xml</loc>");
	}

	[Test]
	public void Generate_OnAPreviewHost_PointsTheIndexAtTheFilesOnThatHost_AndKeepsCanonicalPageUrls()
	{
		var (fileSystem, output) = Output(host: "https://docs-v3-preview.elastic.dev");

		_ = SitemapBuilder.Generate(Entries, output, new DiagnosticsCollector([]));

		fileSystem
			.File
			.ReadAllText("/output/docs/sitemap.xml")
			.Should()
			.Contain("<loc>https://docs-v3-preview.elastic.dev/docs/sitemap-api.xml</loc>");
		fileSystem.File.ReadAllText("/output/docs/sitemap-docs.xml").Should().Contain("<loc>https://www.elastic.co/docs/reference</loc>");
	}

	[Test]
	public void Generate_OverTheSizeLimit_ThrowsAndKeepsThePreviousFile()
	{
		var (fileSystem, output) = Output();
		_ = SitemapBuilder.Generate(Entries, output, new DiagnosticsCollector([]));
		var previous = fileSystem.File.ReadAllText("/output/docs/sitemap-docs.xml");
		// below the URL limit, but long URLs take the file over 50 MiB
		var segment = new string('a', 1_000);
		var oversized = Enumerable.Range(0, 49_000).ToDictionary(i => $"/docs/{i}/{segment}", _ => Date);

		var generate = () => SitemapBuilder.Generate(oversized, output, new DiagnosticsCollector([]));

		_ = generate.Should().Throw<InvalidOperationException>().WithMessage("sitemap-docs.xml is * MB, which exceeds *");
		fileSystem.File.ReadAllText("/output/docs/sitemap-docs.xml").Should().Be(previous);
	}
}
