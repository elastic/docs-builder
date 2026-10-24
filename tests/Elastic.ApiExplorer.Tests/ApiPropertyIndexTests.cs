// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.ApiExplorer.Components.PropertyTree;
using Elastic.ApiExplorer.Components.PropertyTree._Partials;
using Elastic.ApiExplorer.Export;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Model;
using Elastic.ApiExplorer.Operations;
using Elastic.Documentation;
using Elastic.Documentation.Configuration.Versions;
using Elastic.Documentation.Site.FileProviders;
using Elastic.Documentation.Versions;
using RazorSlices;

namespace Elastic.ApiExplorer.Tests;

[ClassDataSource<ApiExplorerFixture>(Shared = SharedType.PerClass)]
public class ApiPropertyIndexTests(ApiExplorerFixture fixture)
{
	[Test]
	public async Task Export_NestedDictionaryAndUnion_IndexesRenderedFields()
	{
		var spec = await TestSpecs.LoadSpecAsync(
			"""
		{
		  "openapi":"3.0.3", "info":{"title":"Fields","version":"1"},
		  "paths":{"/thing":{"post":{"operationId":"thing","responses":{"200":{"description":"OK"}},
		    "requestBody":{"content":{"application/json":{"schema":{"type":"object","properties":{
		      "flight":{"type":"object","properties":{"travel_agency":{"type":"string","description":"The **agency** <em>name</em>."}}},
		      "labels":{"type":"object","additionalProperties":{"type":"object","properties":{"value":{"type":"string"}}}},
		      "choice":{"oneOf":[
		        {"type":"object","properties":{"code":{"type":"string"}}},
		        {"type":"object","properties":{"number":{"type":"integer"}}}
		      ]}
		    }}}}}}}}
		}
		"""
		);
		var path = spec.Paths["/thing"];
		var operation = new ApiOperation(HttpMethod.Post, path.Operations![HttpMethod.Post], "/thing", path, "fields");
		var trees = OperationPageModel.BuildPropertyTrees(
			operation,
			new SchemaAnalyzer(spec),
			new ParameterRowSource(TestSpecs.BuilderFor(spec), null, OperationEndpoint.Build(operation, null, []))
		);
		var entries = trees.IndexEntries();

		entries.Should().Contain(
			p => p.Path == "flight.travel_agency" && p.Anchor == "req-flight-travel_agency" && p.Type == "string" && p.Description ==
				"The agency name."
		);
		entries.Should().Contain(p => p.Path == "labels.<string>.value" && p.Anchor == "req-labels-string-value");
		entries.Should().Contain(p => p.Path == "choice.code");
		entries.Should().Contain(p => p.Path == "choice.number");
		var html = await _PropertyList.Create(trees.Request[0].Properties!).RenderAsync(
			cancellationToken: TestContext.Current!.Execution.CancellationToken
		);
		foreach (var entry in entries)
			html.Should().Contain($"id=\"{entry.Anchor}\"");
	}

	[Test]
	public async Task Export_FixtureOperations_EveryPropertyAnchorExistsInRenderedPage()
	{
		var versions = new VersionsConfiguration
		{
			VersioningSystems = new Dictionary<VersioningSystemId, VersioningSystem>
			{
				[VersioningSystemId.Stack] = new VersioningSystem
				{
					Id = VersioningSystemId.Stack,
					Base = new SemVersion(8, 0, 0),
					Current = new SemVersion(99, 0, 0)
				}
			}
		};
		var documents = new OpenApiDocumentExporter(versions).ConvertToDocuments(fixture.Document, "fixture").ToArray();
		var propertyCount = 0;
		foreach (var document in documents)
		{
			var navigation = fixture
				.Walk()
				.OfType<OperationNavigationItem>()
				.First(
					n => n.Model.Route == document.Api!.Path && string.Equals(
						n.Model.OperationType.Method,
						document.Api.Method,
						StringComparison.OrdinalIgnoreCase
					)
				);
			var context = new ApiRenderContext(
				fixture.Context,
				fixture.Document,
				new StaticFileContentHashProvider(new EmbeddedOrPhysicalFileProvider(fixture.Context))
			)
			{ NavigationHtml = "", CurrentNavigation = navigation, MarkdownRenderer = PassthroughMarkdownRenderer.Instance };
			var fs = new MockFileSystem();
			await using (var stream = fs.FileStream.New("/out.html", FileMode.Create, FileAccess.Write))
				await navigation.Model.RenderAsync(stream, context, null, TestContext.Current!.Execution.CancellationToken);
			var html = fs.File.ReadAllText("/out.html");
			foreach (var property in document.Api!.Properties)
			{
				html.Should().Contain($"id=\"{property.Anchor}\"", $"{property.Path} must link to a rendered row");
				property.Path.Should().NotBeNullOrEmpty();
				property.Type.Should().NotBeNullOrEmpty();
				propertyCount++;
			}
		}
		propertyCount.Should().BeGreaterThan(20);
		var search = documents.Single(d => d.Api!.OperationId == "search");
		search.Body.Should().Contain("**pretty**").And.Contain("Pretty-print the response body.");
		search.Api!.Properties.Should().Contain(p => p.Path == "fields" && p.Anchor == "req-fields" && p.Type == "union string | string[]");
		search.Api.Properties.Should().Contain(p => p.Location == "response");
		search.Api.Properties.Should().Contain(p => p.Location == "query" && p.Name == "pretty");
		search.Api.Properties.Should().Contain(p => p.Path == "pit.id" && p.Anchor == "req-pit-id" && p.Type == "string");
	}
}
