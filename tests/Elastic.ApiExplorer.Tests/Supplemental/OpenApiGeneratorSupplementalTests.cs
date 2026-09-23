// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.ApiExplorer.Landing;
using Elastic.ApiExplorer.Model;
using Elastic.ApiExplorer.Supplemental;
using Elastic.Documentation.Configuration.Products;
using Elastic.Documentation.Configuration.Toc;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elastic.ApiExplorer.Tests.Supplemental;

public class OpenApiGeneratorSupplementalTests(ApiExplorerFixture fixture) : IClassFixture<ApiExplorerFixture>
{
	[Fact]
	public void DiscoverSupplemental_MatchesFixtureFilesAndLeavesHtmlUnchanged()
	{
		var folder = "/docs/api/fixture";
		var fs = new MockFileSystem(new Dictionary<string, MockFileData>
		{
			[$"{folder}/op-search.md"] = new("# search"),
			[$"{folder}/tag-search.md"] = new("# tag"),
			[$"{folder}/random-notes.md"] = new("# notes"),
			[$"{folder}/op-nope.md"] = new("# unmatched")
		});
		var apiConfig = new ResolvedApiConfiguration
		{
			ProductKey = "fixture",
			Product = new Product { Id = "elasticsearch", DisplayName = "Elasticsearch" },
			SpecFileName = "api-explorer-fixture.json",
			ApiContentDirectory = fs.DirectoryInfo.New(folder)
		};

		var generator = new OpenApiGenerator(NullLoggerFactory.Instance, fixture.Context, PassthroughMarkdownRenderer.Instance);
		var result = generator.DiscoverSupplemental(fixture.Document, apiConfig);

		result.Operations.Should().ContainKey("search");
		result.Tags.Should().ContainKey("search");
		result.Ignored.Should().ContainSingle(f => f.Name == "random-notes.md");
		result.Unmatched.Should().ContainSingle(f => f.Name == "op-nope.md");

		var navigation = generator.CreateNavigation("fixture", fixture.Document, apiConfig);
		navigation.Should().NotBeNull();
	}

	[Fact]
	public void CreateNavigation_VersionSuffixedChild_IncludedOnlyForMatchingMajor()
	{
		var folder = "/docs/api/fixture";
		var fs = new MockFileSystem(new Dictionary<string, MockFileData>
		{
			[$"{folder}/getting-started.md"] = new("# Getting started"),
			[$"{folder}/knn-guide.v9.md"] = new("# kNN")
		});
		var apiConfig = new ResolvedApiConfiguration
		{
			ProductKey = "fixture",
			Product = new Product { Id = "elasticsearch", DisplayName = "Elasticsearch" },
			SpecFileName = "api-explorer-fixture.json",
			ApiContentDirectory = fs.DirectoryInfo.New(folder),
			Children = [fs.FileInfo.New($"{folder}/getting-started.md"), fs.FileInfo.New($"{folder}/knn-guide.v9.md")]
		};

		var generator = new OpenApiGenerator(NullLoggerFactory.Instance, fixture.Context, PassthroughMarkdownRenderer.Instance);
		var nav9 = generator.CreateNavigation("fixture", fixture.Document, apiConfig, versionMajor: 9);
		var nav8 = generator.CreateNavigation("fixture", fixture.Document, apiConfig, versionMajor: 8);

		nav9.NavigationItems.OfType<SimpleMarkdownNavigationItem>().Select(n => n.Slug).Should().Equal("getting-started", "knn-guide");
		nav8.NavigationItems.OfType<SimpleMarkdownNavigationItem>().Select(n => n.Slug).Should().Equal("getting-started");
	}

	[Fact]
	public void SupplementalMajor_ReleasedMajor_ReturnsThatMajor() =>
		OpenApiGenerator.SupplementalMajor(ApiSpecVersion.Major(8), 9).Should().Be(8);

	[Fact]
	public void SupplementalMajor_Latest_UsesHighestNumeric() =>
		OpenApiGenerator.SupplementalMajor(ApiSpecVersion.Latest, 9).Should().Be(9);

	[Fact]
	public void SupplementalMajor_LatestWithoutNumeric_ReturnsNull() =>
		OpenApiGenerator.SupplementalMajor(ApiSpecVersion.Latest, null).Should().BeNull();
}
