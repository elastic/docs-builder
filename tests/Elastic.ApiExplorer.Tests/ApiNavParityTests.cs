// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using AwesomeAssertions;
using Elastic.ApiExplorer.Landing;
using Elastic.ApiExplorer.Operations;
using Elastic.ApiExplorer.Structural;
using Elastic.ApiExplorer.Types;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.FileSystems;
using Elastic.Documentation.Navigation;
using Elastic.Documentation.Site.Navigation;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;

namespace Elastic.ApiExplorer.Tests;

public class ApiNavParityTests
{
	[Test]
	public async Task CreateNavigation_WithServers_SeparatesIntroPagesFromEndpoints()
	{
		var openApiJson = /*lang=json,strict*/
			"""
			{
			  "openapi": "3.0.3",
			  "info": { "title": "ES", "version": "1.0" },
			  "servers": [ { "url": "https://example.com" } ],
			  "paths": {
			    "/a": { "get": { "operationId": "a1", "tags": ["search"], "responses": { "200": { "description": "ok" } } } }
			  },
			  "tags": [ { "name": "search" } ]
			}
			""";

		var (generator, document) = await CreateGeneratorWithSpec(openApiJson);
		var navigation = generator.CreateNavigation("elasticsearch", document);
		var ordered = navigation.NavigationItems.ToList();

		ordered[0].Should().BeOfType<StructuralNavigationItem>().Which.Model.Kind.Should().Be(ApiStructuralKind.Servers);
		ordered[1].Should().BeOfType<SidebarSeparatorNavigationItem>();
		ordered[2].Should().BeOfType<TagNavigationItem>().Which.NavigationTitle.Should().Be("search");
	}

	[Test]
	public async Task CreateNavigation_SharedSummaryOperations_CollapsesIntoOnePage()
	{
		var (generator, document) = await CreateGeneratorWithSpec(SharedSummarySpec);
		var navigation = generator.CreateNavigation("test", document);

		var page = navigation
			.NavigationItems
			.OfType<TagNavigationItem>()
			.Single()
			.NavigationItems
			.Should()
			.ContainSingle()
			.Which
			.Should()
			.BeOfType<OperationNavigationItem>()
			.Subject;

		page.Hidden.Should().BeFalse();
		page.Url.Should().Be("/api/doc/test/operation/operation-op-a");
		page.Siblings.Select(o => o.Route).Should().Equal("/b");
		page.AliasUrls.Should().Equal("/api/doc/test/operation/operation-op-b");
	}

	[Test]
	public async Task CreateNavigation_QueryContainerSchema_AddsTypesNav()
	{
		var (generator, document) = await CreateGeneratorWithSpec(QueryContainerSpec);
		var navigation = generator.CreateNavigation("test", document);

		var types = navigation
			.NavigationItems
			.OfType<SchemaCategoryNavigationItem>()
			.Should()
			.ContainSingle(item => item.NavigationTitle == "Types")
			.Subject;
		types.NavigationItems.OfType<SchemaCategoryNavigationItem>().Select(c => c.NavigationTitle).Should().Equal("Query DSL");
	}

	private const string SharedSummarySpec = /*lang=json,strict*/
		"""
		{
		  "openapi": "3.0.3",
		  "info": { "title": "T", "version": "1.0" },
		  "paths": {
		    "/a": {
		      "get": {
		        "operationId": "op-a",
		        "summary": "Search",
		        "tags": ["search"],
		        "responses": { "200": { "description": "ok" } }
		      }
		    },
		    "/b": {
		      "post": {
		        "operationId": "op-b",
		        "summary": "Search",
		        "tags": ["search"],
		        "responses": { "200": { "description": "ok" } }
		      }
		    }
		  },
		  "tags": [ { "name": "search" } ]
		}
		""";

	private const string QueryContainerSpec = /*lang=json,strict*/
		"""
		{
		  "openapi": "3.0.3",
		  "info": { "title": "T", "version": "1.0" },
		  "paths": {
		    "/q": {
		      "get": {
		        "operationId": "q1",
		        "tags": ["q"],
		        "responses": { "200": { "description": "ok" } }
		      }
		    }
		  },
		  "tags": [ { "name": "q" } ],
		  "components": {
		    "schemas": {
		      "_types.query_dsl.QueryContainer": { "type": "object" }
		    }
		  }
		}
		""";

	private static async Task<(OpenApiGenerator generator, OpenApiDocument document)> CreateGeneratorWithSpec(string openApiJson)
	{
		var collector = new DiagnosticsCollector([]);
		var configurationContext = TestHelpers.CreateConfigurationContext(new FileSystem());
		var context = new BuildContext(
			collector,
			DocumentationFileSystem.Resolve(Paths.WorkingDirectoryRoot.FullName),
			configurationContext
		);
		var generator = new OpenApiGenerator(NullLoggerFactory.Instance, context, NoopMarkdownStringRenderer.Instance);

		using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(openApiJson));
		var settings = new OpenApiReaderSettings { LeaveStreamOpen = false };
		var result = await OpenApiDocument.LoadAsync(stream, settings: settings);
		var parseErrors = result.Diagnostic?.Errors;
		if (parseErrors is not null && parseErrors.Any())
			throw new InvalidOperationException($"OpenAPI parsing failed: {string.Join(", ", parseErrors.Select(e => e.Message))}");

		return (generator, result.Document!);
	}

	private static string NodeLabel(NavigationRenderNode node) =>
		node.Kind == NavigationRenderNodeKind.Separator ? "---" : node.NavigationTitle;
}
