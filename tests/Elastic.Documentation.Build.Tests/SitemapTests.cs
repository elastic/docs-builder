// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using System.Text.Json;
using System.Xml.Linq;
using AwesomeAssertions;
using Elastic.Documentation.Assembler.Building;

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
	public void BuildSearchBody_FirstPage_HasPitButNoSearchAfter()
	{
		// Act
		var json = EsSitemapReader.BuildSearchBody("test-pit-id", null);

		// Assert
		json.Should().Contain("\"pit\"");
		json.Should().Contain("\"test-pit-id\"");
		json.Should().Contain("\"must_not\"");
		json.Should().Contain("\"hidden\":true");
		json.Should().NotContain("search_after");
	}

	[Fact]
	public void BuildSearchBody_SubsequentPage_IncludesSearchAfter()
	{
		// Act
		var json = EsSitemapReader.BuildSearchBody("test-pit-id", ["/docs/last-url"]);

		// Assert
		json.Should().Contain("\"search_after\"");
		json.Should().Contain("/docs/last-url");
	}

	[Fact]
	public void BuildSearchBody_EscapesSpecialCharactersInPitId()
	{
		// Act
		var json = EsSitemapReader.BuildSearchBody("pit-with-\"quotes\"", null);
		var doc = JsonDocument.Parse(json);

		// Assert — verify the value round-trips correctly through serialization
		doc.RootElement.GetProperty("pit").GetProperty("id").GetString().Should().Be("pit-with-\"quotes\"");
	}
}
