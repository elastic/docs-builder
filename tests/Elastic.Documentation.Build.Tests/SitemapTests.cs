// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
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

	private static (MockFileSystem FileSystem, SitemapOutput Output) Output()
	{
		var fileSystem = new MockFileSystem();
		return (fileSystem, new SitemapOutput(fileSystem, fileSystem.DirectoryInfo.New("/output/docs"), "docs"));
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
		fileSystem.File.ReadAllText("/output/docs/sitemap-api.xml").Should().Be(apiSitemap);
		fileSystem.File.ReadAllText("/output/docs/sitemap.xml").Should().Contain("sitemap-api.xml");
	}
}
