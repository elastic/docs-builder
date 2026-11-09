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
using Elastic.Documentation.Search.Contract;
using Elastic.Documentation.Site.FileProviders;
using Elastic.Documentation.Versions;
using Nullean.Snapshots;
using RazorSlices;

namespace Elastic.ApiExplorer.Tests;

[ClassDataSource<ApiExplorerFixture>(Shared = SharedType.PerClass)]
public class ApiPropertyIndexTests(ApiExplorerFixture fixture)
{
	private const string NestedDictionaryAndUnionProperties =
		"""
		"flight":{"type":"object","properties":{"travel_agency":{"type":"string","description":"The **agency** <em>name</em>."}}},
		"labels":{"type":"object","additionalProperties":{"type":"object","properties":{"value":{"type":"string"}}}},
		"choice":{"oneOf":[
		  {"type":"object","properties":{"code":{"type":"string"}}},
		  {"type":"object","properties":{"number":{"type":"integer"}}}
		]},
		"rescore":{"type":"array","items":{"type":"object","properties":{"query":{"type":"string"}}}},
		"pets":{"type":"array","items":{"type":"object","properties":{"owner":{"type":"object","properties":{"name":{"type":"string"}}}}}}
		""";

	private static async Task<(OperationPropertyTrees Trees, ApiPropertyEntry[] Entries)> IndexRequestBody(string properties)
	{
		var spec = await TestSpecs.LoadSpecAsync(
			"""
			{
			  "openapi":"3.0.3", "info":{"title":"Fields","version":"1"},
			  "paths":{"/thing":{"post":{"operationId":"thing","responses":{"200":{"description":"OK"}},
			    "requestBody":{"content":{"application/json":{"schema":{"type":"object","properties":{
			""" +
				properties + "}}}}}}}}}"
		);
		var path = spec.Paths["/thing"];
		var operation = new ApiOperation(HttpMethod.Post, path.Operations![HttpMethod.Post], "/thing", path, "fields");
		var trees = OperationPageModel.BuildPropertyTrees(
			operation,
			new SchemaAnalyzer(spec),
			new ParameterRowSource(TestSpecs.BuilderFor(spec), null, OperationEndpoint.Build(operation, null, []))
		);
		return (trees, trees.IndexEntries(new Dictionary<string, string>()));
	}

	[Test]
	public async Task Export_NestedDictionaryAndUnion_IndexesRenderedFields()
	{
		var (trees, entries) = await IndexRequestBody(NestedDictionaryAndUnionProperties);

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
	public async Task Export_FieldNames_IndexTheirWords()
	{
		var (_, entries) = await IndexRequestBody(
			"""
			"unrated_docs":{"type":"integer"},
			"indexPattern":{"type":"string"},
			"min-score":{"type":"number"},
			"hits":{"type":"array","items":{"type":"object","properties":{"total_hits":{"type":"integer"}}}}
			"""
		);

		string
			.Join('\n', entries.Select(p => $"{p.Path} -> {p.NameWords}"))
			.Should()
			.MatchInlineSnapshot(
				"""
			unrated_docs -> unrated docs
			indexPattern -> index pattern
			min-score -> min score
			hits -> hits
			hits[].total_hits -> total hits
			"""
			);
	}

	[Test]
	public async Task Export_NestedDictionaryAndUnion_IndexesEachRowsParent()
	{
		var (_, entries) = await IndexRequestBody(NestedDictionaryAndUnionProperties);

		entries.Single(p => p.Path == "flight").Parent.Should().Be("");
		entries.Single(p => p.Path == "flight.travel_agency").Parent.Should().Be("flight");
		entries.Single(p => p.Path == "labels.<string>.value").Parent.Should().Be("labels");
		entries.Single(p => p.Path == "choice.code").Parent.Should().Be("choice");
		entries.Single(p => p.Path == "choice.number").Parent.Should().Be("choice");
		entries.Single(p => p.Path == "rescore[].query").Parent.Should().Be("rescore");
		entries.Single(p => p.Path == "pets[].owner").Parent.Should().Be("pets");
		entries.Single(p => p.Path == "pets[].owner.name").Parent.Should().Be("pets.owner");
	}

	[Test]
	public async Task Export_MarkedPaths_IndexesPathWordsWithoutMarkers()
	{
		var (_, entries) = await IndexRequestBody(NestedDictionaryAndUnionProperties);

		entries.Single(p => p.Path == "labels.<string>.value").PathWords.Should().Be("labels value");
		entries.Single(p => p.Path == "pets[].owner.name").PathWords.Should().Be("pets owner name");
		entries.Single(p => p.Path == "flight").PathWords.Should().Be("flight");
	}

	[Test]
	public async Task Export_UnionVariantsSharingAField_IndexesEachVariantsRow()
	{
		var (_, entries) = await IndexRequestBody(
			"""
			"pick":{"oneOf":[
			  {"type":"object","properties":{"x":{"type":"string"},"a":{"type":"string"}}},
			  {"type":"object","properties":{"x":{"type":"integer"},"b":{"type":"string"}}}
			]}
			"""
		);

		entries.Count(p => p.Path == "pick.x").Should().Be(2);
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
		var documents = new OpenApiDocumentExporter(versions)
			.ConvertToDocuments(new IndexedTree("fixture", "fixture", "main", fixture.Document))
			.ToArray();
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
