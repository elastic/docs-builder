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
	[Fact]
	public async Task CreateNavigation_XTagGroupsWithMultipleGroups_Default_TagsHangOffLanding()
	{
		var openApiJson = /*lang=json,strict*/
			"""
			{
			  "openapi": "3.0.3",
			  "info": { "title": "ES", "version": "1.0" },
			  "paths": {
			    "/a": { "get": { "operationId": "a1", "tags": ["watcher"], "responses": { "200": { "description": "ok" } } } },
			    "/b": { "get": { "operationId": "b1", "tags": ["tasks"], "responses": { "200": { "description": "ok" } } } },
			    "/c": { "get": { "operationId": "c1", "tags": ["search"], "responses": { "200": { "description": "ok" } } } }
			  },
			  "tags": [
			    { "name": "watcher", "x-displayName": "Watcher" },
			    { "name": "tasks", "x-displayName": "Task management" },
			    { "name": "search" }
			  ],
			  "x-tagGroups": [
			    { "name": "Information", "tags": ["watcher", "tasks"] },
			    { "name": "Search", "tags": ["search"] }
			  ]
			}
			""";

		var (generator, document) = await CreateGeneratorWithSpec(openApiJson);
		var navigation = generator.CreateNavigation("elasticsearch", document);

		navigation.NavigationItems.OfType<ClassificationNavigationItem>().Should().BeEmpty();
		var tags = navigation.NavigationItems.OfType<TagNavigationItem>().ToList();
		tags.Select(t => t.NavigationTitle).Should().Equal("Task management", "Watcher", "search");
		tags
			.Select(t => t.Url)
			.Should()
			.Equal(
				"/api/doc/elasticsearch/group/endpoint-tasks",
				"/api/doc/elasticsearch/group/endpoint-watcher",
				"/api/doc/elasticsearch/group/endpoint-search"
			);

		navigation.NavigationItems.OfType<StructuralNavigationItem>().Should().BeEmpty();
		navigation.NavigationItems.OfType<SidebarSeparatorNavigationItem>().Should().BeEmpty();
		navigation.NavigationItems.Should().AllBeOfType<TagNavigationItem>();

		var model = NavigationRenderModel.Create(
			navigation,
			[],
			isUsingNavigationDropdown: false,
			isPrimaryNavEnabled: false,
			isGlobalAssemblyBuild: false,
			navigationPreviewEnabled: true
		);
		model.RootIndex!.NavigationTitle.Should().Be("Api Overview");
		model.TreeHasSeparator.Should().BeFalse();
		model.Tree.Select(NodeLabel).Should().Equal("Task management", "Watcher", "search");
	}

	[Fact]
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

	[Fact]
	public async Task CreateNavigation_SharedSummaryOperations_Default_EachOperationIsVisible()
	{
		var (generator, document) = await CreateGeneratorWithSpec(SharedSummarySpec);
		var navigation = generator.CreateNavigation("test", document);

		navigation
			.NavigationItems
			.OfType<TagNavigationItem>()
			.SelectMany(tag => tag.NavigationItems)
			.OfType<EndpointNavigationItem>()
			.Should()
			.BeEmpty();
		var operations = navigation
			.NavigationItems
			.OfType<TagNavigationItem>()
			.Single()
			.NavigationItems
			.OfType<OperationNavigationItem>()
			.ToList();

		operations.Should().HaveCount(2);
		operations.Should().OnlyContain(op => !op.Hidden);
		operations.Select(op => op.Url).Should().Equal("/api/doc/test/operation/operation-op-a", "/api/doc/test/operation/operation-op-b");
	}

	[Fact]
	public async Task CreateNavigation_SharedSummaryOperations_GroupingEnabled_CollapsesIntoHiddenEndpoint()
	{
		var (generator, document) = await CreateGeneratorWithSpec(SharedSummarySpec, apiNavGroupingEnabled: true);
		var navigation = generator.CreateNavigation("test", document);

		var endpoint = navigation
			.NavigationItems
			.OfType<TagNavigationItem>()
			.Single()
			.NavigationItems
			.OfType<EndpointNavigationItem>()
			.Should()
			.ContainSingle()
			.Subject;

		endpoint.Hidden.Should().BeFalse();
		var operations = endpoint.NavigationItems.ToList();
		operations.Should().HaveCount(2);
		operations.Should().OnlyContain(op => op.Hidden);
		operations.Select(op => op.Url).Should().Equal("/api/doc/test/operation/operation-op-a", "/api/doc/test/operation/operation-op-b");
	}

	[Fact]
	public async Task CreateNavigation_QueryContainerSchema_Default_OmitsTypesNav()
	{
		var (generator, document) = await CreateGeneratorWithSpec(QueryContainerSpec);
		var navigation = generator.CreateNavigation("test", document);

		navigation.NavigationItems.OfType<SchemaCategoryNavigationItem>().Should().BeEmpty();
	}

	[Fact]
	public async Task CreateNavigation_QueryContainerSchema_GroupingEnabled_AddsTypesNav()
	{
		var (generator, document) = await CreateGeneratorWithSpec(QueryContainerSpec, apiNavGroupingEnabled: true);
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

	private static async Task<(OpenApiGenerator generator, OpenApiDocument document)> CreateGeneratorWithSpec(
		string openApiJson,
		bool apiNavGroupingEnabled = false
	)
	{
		var collector = new DiagnosticsCollector([]);
		var configurationContext = TestHelpers.CreateConfigurationContext(new FileSystem());
		var context = new BuildContext(
			collector,
			DocumentationFileSystem.Resolve(Paths.WorkingDirectoryRoot.FullName),
			configurationContext
		);
		context.Configuration.Features.ApiNavGroupingEnabled = apiNavGroupingEnabled;
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
