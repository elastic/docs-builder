// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using System.Xml.Linq;
using Elastic.Documentation.Assembler.Building;
using FluentAssertions;

namespace Elastic.Documentation.Build.Tests;

public class SitemapTests
{
	[Fact]
	public void Generate_WritesValidSitemapXml_WithCorrectLastModDates()
	{
		// Arrange
		var fs = new MockFileSystem();
		var outputDir = fs.DirectoryInfo.New("/output");

		var entries = new Dictionary<string, DateTimeOffset>
		{
			["/docs/elasticsearch/getting-started"] = new DateTimeOffset(2025, 6, 15, 10, 30, 0, TimeSpan.Zero),
			["/docs/kibana/dashboard"] = new DateTimeOffset(2025, 7, 20, 14, 0, 0, TimeSpan.Zero),
		};

		// Act
		SitemapBuilder.Generate(entries, fs, outputDir);

		// Assert
		var sitemapPath = fs.Path.Combine("/output", "sitemap.xml");
		fs.File.Exists(sitemapPath).Should().BeTrue();

		var content = fs.File.ReadAllText(sitemapPath);
		var doc = XDocument.Parse(content);
		XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";

		var urls = doc.Descendants(ns + "url").ToList();
		urls.Should().HaveCount(2);

		// Entries are ordered by URL
		urls[0].Element(ns + "loc")!.Value.Should().Be("https://www.elastic.co/docs/elasticsearch/getting-started");
		urls[0].Element(ns + "lastmod")!.Value.Should().Contain("2025-06-15");

		urls[1].Element(ns + "loc")!.Value.Should().Be("https://www.elastic.co/docs/kibana/dashboard");
		urls[1].Element(ns + "lastmod")!.Value.Should().Contain("2025-07-20");
	}

	[Fact]
	public void Generate_CreatesOutputDirectory_WhenItDoesNotExist()
	{
		// Arrange
		var fs = new MockFileSystem();
		var outputDir = fs.DirectoryInfo.New("/nonexistent/output");

		var entries = new Dictionary<string, DateTimeOffset>
		{
			["/docs/test"] = DateTimeOffset.UtcNow,
		};

		// Act
		SitemapBuilder.Generate(entries, fs, outputDir);

		// Assert
		fs.File.Exists(fs.Path.Combine("/nonexistent/output", "sitemap.xml")).Should().BeTrue();
	}

	[Fact]
	public void Generate_OrdersUrlsAlphabetically()
	{
		// Arrange
		var fs = new MockFileSystem();
		var outputDir = fs.DirectoryInfo.New("/output");
		fs.Directory.CreateDirectory("/output");

		var now = DateTimeOffset.UtcNow;
		var entries = new Dictionary<string, DateTimeOffset>
		{
			["/docs/z-last"] = now,
			["/docs/a-first"] = now,
			["/docs/m-middle"] = now,
		};

		// Act
		SitemapBuilder.Generate(entries, fs, outputDir);

		// Assert
		var content = fs.File.ReadAllText(fs.Path.Combine("/output", "sitemap.xml"));
		var doc = XDocument.Parse(content);
		XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";

		var locs = doc.Descendants(ns + "loc").Select(e => e.Value).ToList();
		locs.Should().BeInAscendingOrder();
	}

	[Fact]
	public void BuildSearchBody_FirstPage_HasNoPitAndNoSearchAfter()
	{
		// Act
		var body = EsSitemapReader.BuildSearchBody("test-pit-id", null);

		// Assert
		body.Should().Contain("\"pit\"");
		body.Should().Contain("\"test-pit-id\"");
		body.Should().Contain("\"must_not\"");
		body.Should().Contain("\"hidden\": true");
		body.Should().NotContain("search_after");
	}

	[Fact]
	public void BuildSearchBody_SubsequentPage_IncludesSearchAfter()
	{
		// Act
		var body = EsSitemapReader.BuildSearchBody("test-pit-id", ["/docs/last-url"]);

		// Assert
		body.Should().Contain("\"search_after\"");
		body.Should().Contain("/docs/last-url");
	}

	[Fact]
	public void BuildSearchBody_EscapesSpecialCharactersInPitId()
	{
		// Act
		var body = EsSitemapReader.BuildSearchBody("pit-with-\"quotes\"", null);

		// Assert
		body.Should().Contain("pit-with-\\\"quotes\\\"");
		body.Should().NotContain("pit-with-\"quotes\"");
	}
}
