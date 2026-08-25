// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
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
}
