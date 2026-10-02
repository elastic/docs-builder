// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using AwesomeAssertions;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Landing;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Products;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.FileSystems;
using Elastic.Documentation.Site.FileProviders;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Tests;

public class ApiMarkdownFrontMatterTests
{
	[Test]
	public void Write_EmitsLlmAndOkfFields()
	{
		var markdown = ApiMarkdownFrontMatter.Write(
			"# Run a search\n\nReturns hits.\n",
			new ApiPageFrontMatter(
				"Run a search",
				"Returns hits that match the query",
				"https://cdn.example/api/doc/elasticsearch/operation/operation-search",
				"Elasticsearch"
			)
		);

		markdown.Should().StartWith("---");
		markdown.Should().Contain("type: api");
		markdown.Should().Contain("title: Run a search");
		markdown.Should().Contain("description: Returns hits that match the query");
		markdown.Should().NotContain("navigation_title:");
		markdown.Should().Contain("url: https://cdn.example/api/doc/elasticsearch/operation/operation-search");
		markdown.Should().Contain("resource: https://cdn.example/api/doc/elasticsearch/operation/operation-search");
		markdown.Should().Contain("products:");
		markdown.Should().Contain("  - Elasticsearch");
		markdown.Should().NotContain("applies_to:");
		markdown.Should().Contain("# Run a search");
	}

	[Test]
	public void Write_OmitsOptionalKeysWhenMissing()
	{
		var markdown = ApiMarkdownFrontMatter.Write("# API Explorer\n", new ApiPageFrontMatter("API Explorer", null, "/api", null));

		markdown.Should().Contain("type: api");
		markdown.Should().Contain("title: API Explorer");
		markdown.Should().Contain("url: /api");
		markdown.Should().Contain("resource: /api");
		markdown.Should().NotContain("navigation_title:");
		markdown.Should().NotContain("description:");
		markdown.Should().NotContain("products:");
		markdown.Should().NotContain("applies_to:");
	}

	[Test]
	public void StripLeadingFrontMatter_RemovesAuthoredYaml()
	{
		var source =
			"""
			---
			navigation_title: Spaces
			---
			# Kibana spaces

			Spaces enable you to organize dashboards.
			""";

		var stripped = ApiMarkdownFrontMatter.StripLeadingFrontMatter(source);

		stripped.Should().StartWith("# Kibana spaces");
		stripped.Should().NotContain("navigation_title:");
	}

	[Test]
	public void Collect_Operation_SkipsSharedPathPreamble()
	{
		var indices = OperationFrontMatter(
			AllMethodsPreamble("Get high-level information about indices in a cluster."),
			"Get index information"
		);
		var spaces = OperationFrontMatter(SpacesPreamble("Creates an MCP server connection."), "Create MCP server");

		indices.Description.Should().Be("Get high-level information about indices in a cluster.");
		spaces.Description.Should().Be("Creates an MCP server connection.");
		indices.Description.Should().NotBe(spaces.Description);
	}

	[Test]
	public void Collect_Operation_EmptyAfterPreamble_UsesSummary()
	{
		var meta = OperationFrontMatter(AllMethodsPreamble(null), "Get index information");

		meta.Description.Should().Be("Get index information");
	}

	[Test]
	[Arguments("v8")]
	[Arguments("v9")]
	public void Collect_VersionLanding_EmptyDescription_GetsOne(string version)
	{
		var document = new OpenApiDocument
		{
			Info = new OpenApiInfo { Title = "Elasticsearch Request & Response Specification", Version = version }
		};
		var context = RenderContext(document, version);
		var meta = ApiMarkdownFrontMatter.Collect(
			"# Elasticsearch Request & Response Specification\n",
			context.CurrentNavigation,
			context,
			new ApiLanding()
		);

		meta.Description.Should().Be($"Elasticsearch API documentation ({version}).");
	}

	private static ApiPageFrontMatter OperationFrontMatter(string description, string summary)
	{
		var document = new OpenApiDocument { Info = new OpenApiInfo { Title = "Elasticsearch API", Version = "9.0" } };
		var context = RenderContext(document, "latest");
		var operation = new Elastic.ApiExplorer.Operations.ApiOperation(
			HttpMethod.Get,
			new OpenApiOperation { Summary = summary, Description = description },
			"/_cat/indices",
			new OpenApiPathItem(),
			summary
		);
		return ApiMarkdownFrontMatter.Collect($"# {summary}\n", context.CurrentNavigation, context, operation);
	}

	private static string AllMethodsPreamble(string? sentence) =>
		sentence is null
			? """
			**All methods and paths for this operation:**

			<div><span class="operation-verb get">GET</span> <span class="operation-path">/_cat/indices</span></div>
			"""
			: $"""
			**All methods and paths for this operation:**

			<div><span class="operation-verb get">GET</span> <span class="operation-path">/_cat/indices</span></div>

			{sentence}
			""";

	private static string SpacesPreamble(string sentence) =>
		$"""
		**Spaces method and path for this operation:**

		<div><span class="operation-verb post">POST</span>&nbsp;<span class="operation-path">/s/space_id/api/agentbuilder/mcp</span></div>

		Refer to [Spaces](https://www.elastic.co/docs/deploy-manage/manage-spaces) for more information.

		{sentence}
		""";

	private static ApiRenderContext RenderContext(OpenApiDocument document, string version)
	{
		var fs = new FileSystem();
		var build = new BuildContext(
			new DiagnosticsCollector([]),
			DocumentationFileSystem.Resolve(Paths.WorkingDirectoryRoot.FullName),
			TestHelpers.CreateConfigurationContext(fs)
		);
		return new ApiRenderContext(build, document, new StaticFileContentHashProvider(new EmbeddedOrPhysicalFileProvider(build)))
		{
			NavigationHtml = string.Empty,
			CurrentNavigation = new LandingNavigationItem("/api/doc/elasticsearch/").Index,
			MarkdownRenderer = PassthroughMarkdownRenderer.Instance,
			Product = new Product { Id = "elasticsearch", DisplayName = "Elasticsearch" },
			VersionSwitcherItems =
			[
				new("latest", "/api/doc/elasticsearch/", version == "latest"),
				new("v9", "/api/doc/elasticsearch/v9/", version == "v9"),
				new("v8", "/api/doc/elasticsearch/v8/", version == "v8")
			]
		};
	}
}
