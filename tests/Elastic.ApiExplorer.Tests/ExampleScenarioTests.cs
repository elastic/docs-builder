// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using AwesomeAssertions;
using Elastic.ApiExplorer._Partials.Layout;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Landing;
using Elastic.ApiExplorer.Model;
using Elastic.ApiExplorer.Operations;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.FileSystems;
using Elastic.Documentation.Site.FileProviders;
using Microsoft.AspNetCore.Html;
using Microsoft.OpenApi;
using RazorSlices;

namespace Elastic.ApiExplorer.Tests;

public class ExampleScenarioTests
{
	[Test]
	public void BuildExampleScenarios_MergesRequestAndResponseByTitle()
	{
		var request = new ExampleDisplay("Multimodal", null, /*lang=json,strict*/  """{"input":[{"type":"image"}]}""", null);
		var response = new ExampleDisplay("Multimodal", null, /*lang=json,strict*/  """{"embeddings":[]}""", null);

		var scenarios = OperationPageModel.BuildExampleScenarios([request], [response], []);

		scenarios.Should().ContainSingle();
		scenarios[0].Title.Should().Be("Multimodal");
		scenarios[0].RequestJson.Should().Contain("image");
		scenarios[0].Responses.Should().ContainSingle();
		scenarios[0].Responses[0].JsonValue.Should().Contain("embeddings");
	}

	[Test]
	public void BuildExampleScenarios_SuffixedSamplesBecomeTheirOwnExamples()
	{
		var ok = new ExampleDisplay("searchDashboardsResponse", null, /*lang=json,strict*/  """{"items":[]}""", null, "200");
		CodeSample[] samples =
		[
			new("Console", "GET kbn:/api/dashboards", "language-console"),
			new("curl", "curl ...", "language-curl"),
			new("Console", "GET kbn:/api/dashboards?tag_names=a", "language-console") { Scenario = "tag_names" },
			new("curl", "curl ...?tag_names=a", "language-curl") { Scenario = "tag_names" },
			new("curl", "curl ...?excluded_tag_names=b", "language-curl") { Scenario = "excluded_tag_names" }
		];

		var scenarios = OperationPageModel.BuildExampleScenarios([], [ok], samples);

		scenarios.Select(s => s.Title).Should().Equal("searchDashboardsResponse", "Tag names", "Excluded tag names");
		scenarios[0].CodeSamples.Should().HaveCount(2);
		scenarios[1].CodeSamples.Select(c => c.Language).Should().Equal("Console", "curl");
		scenarios[2].CodeSamples.Should().ContainSingle();
		scenarios.Should().AllSatisfy(s => s.Responses.Select(r => r.StatusCode).Should().Equal("200"));
		scenarios.Select(s => s.TabId).Should().OnlyHaveUniqueItems();
	}

	[Test]
	public void BuildExampleScenarios_GroupsResponsesByStatusCode()
	{
		var ok = new ExampleDisplay("Create", null, /*lang=json,strict*/  """{"ok":true}""", null, "200");
		var bad = new ExampleDisplay("Create", null, /*lang=json,strict*/  """{"error":"bad"}""", null, "400");

		var scenarios = OperationPageModel.BuildExampleScenarios([], [ok, bad], []);

		scenarios.Should().ContainSingle();
		scenarios[0].Responses.Select(r => r.StatusCode).Should().Equal("200", "400");
		scenarios[0].Responses[0].JsonValue.Should().Contain("ok");
		scenarios[0].Responses[1].JsonValue.Should().Contain("error");
	}

	[Test]
	public void BuildExampleScenarios_SharesUnmatchedErrorResponsesAcrossRequestScenarios()
	{
		var ipRequest = new ExampleDisplay("ip", null, /*lang=json,strict*/  """{"type":"ip"}""", null);
		var keywordRequest = new ExampleDisplay("keyword", null, /*lang=json,strict*/  """{"type":"keyword"}""", null);
		var ipOk = new ExampleDisplay("ip", null, /*lang=json,strict*/  """{"id":"1"}""", null, "200");
		var keywordOk = new ExampleDisplay("keyword", null, /*lang=json,strict*/  """{"id":"2"}""", null, "200");
		var badRequest = new ExampleDisplay("badRequest", null, /*lang=json,strict*/  """{"error":"bad"}""", null, "400");
		var unauthorized = new ExampleDisplay("unauthorized", null, /*lang=json,strict*/  """{"error":"auth"}""", null, "401");

		var scenarios = OperationPageModel.BuildExampleScenarios(
			[ipRequest, keywordRequest],
			[ipOk, keywordOk, badRequest, unauthorized],
			[]
		);

		scenarios.Should().HaveCount(2);
		scenarios.Select(s => s.Title).Should().Equal("ip", "keyword");
		scenarios[0].Responses.Select(r => r.StatusCode).Should().Equal("200", "400", "401");
		scenarios[0].Responses[0].JsonValue.Should().Contain("\"id\":\"1\"");
		scenarios[1].Responses.Select(r => r.StatusCode).Should().Equal("200", "400", "401");
		scenarios[1].Responses[0].JsonValue.Should().Contain("\"id\":\"2\"");
		scenarios[0].Responses[1].JsonValue.Should().Contain("bad");
		scenarios[1].Responses[1].JsonValue.Should().Contain("bad");
	}

	[Test]
	public void BuildExampleScenarios_SharedResponseDoesNotOverwriteScenarioStatus()
	{
		var request = new ExampleDisplay("ip", null, /*lang=json,strict*/  """{"type":"ip"}""", null);
		var ok = new ExampleDisplay("ip", null, /*lang=json,strict*/  """{"id":"scenario"}""", null, "200");
		var sharedOk = new ExampleDisplay("genericOk", null, /*lang=json,strict*/  """{"id":"shared"}""", null, "200");

		var scenarios = OperationPageModel.BuildExampleScenarios([request], [ok, sharedOk], []);

		scenarios.Should().ContainSingle();
		scenarios[0].Responses.Should().ContainSingle();
		scenarios[0].Responses[0].JsonValue.Should().Contain("scenario");
	}

	[Test]
	public void BuildExampleScenarios_CollapsesResponseOnlyNamedStatusesIntoOneScenario()
	{
		var bad = new ExampleDisplay("badRequest", null, /*lang=json,strict*/  """{"error":"bad"}""", null, "400");
		var unauthorized = new ExampleDisplay("unauthorized", null, /*lang=json,strict*/  """{"error":"auth"}""", null, "401");

		var scenarios = OperationPageModel.BuildExampleScenarios([], [bad, unauthorized], []);

		scenarios.Should().ContainSingle();
		scenarios[0].Responses.Select(r => r.StatusCode).Should().Equal("400", "401");
	}

	[Test]
	public void BuildExampleScenarios_AttachesCodeSamplesToMatchingRequestBody()
	{
		var multimodal = new ExampleDisplay(
			"Multimodal embedding task",
			null,
			/*lang=json,strict*/
			"""{"input":[{"content":{"type":"image"}}]}""",
			null
		);
		var textOnly = new ExampleDisplay(
			"Text-only embedding task",
			null,
			/*lang=json,strict*/
			"""{"input":["The first text","The second text"]}""",
			null
		);
		var console = new CodeSample(
			"Console",
			"""
			POST _inference/embedding/my-endpoint
			{"input":[{"content":{"type":"image"}}]}
			""",
			"language-console"
		);
		var python = new CodeSample("Python", "client.inference()", "language-python");

		var scenarios = OperationPageModel.BuildExampleScenarios([multimodal, textOnly], [], [console, python]);

		scenarios.Should().HaveCount(2);
		scenarios[0].CodeSamples.Should().HaveCount(2);
		scenarios[1].CodeSamples.Should().BeEmpty();
		scenarios[0].ShowRequest.Should().BeFalse();
		scenarios[1].ShowRequest.Should().BeTrue();
	}

	[Test]
	public void BuildExampleScenarios_CodeSamplesWithDifferentBody_BecomeTheirOwnExample()
	{
		var request = new ExampleDisplay("createAgentRequestExample", null, /*lang=json,strict*/  """{"id":"created-agent-id"}""", null);
		var console = new CodeSample(
			"Console",
			"""
			POST kbn://api/agent_builder/agents
			{"id":"new-agent-id"}
			""",
			"language-console"
		);

		var scenarios = OperationPageModel.BuildExampleScenarios([request], [], [console]);

		scenarios.Select(static s => s.Title).Should().Equal("Example", "createAgentRequestExample");
		scenarios[0].CodeSamples.Should().ContainSingle();
		scenarios[1].CodeSamples.Should().BeEmpty();
		scenarios[1].ShowRequest.Should().BeTrue();
	}

	[Test]
	public void BuildExampleScenarios_SyntheticCurlWithoutBody_KeepsRequestExampleVisible()
	{
		var request = new ExampleDisplay("default", null, /*lang=json,strict*/  """{"name":"x"}""", null);
		var curl = new CodeSample("curl", "curl -X POST \"${KIBANA_URL}/api/things\"", "language-bash");

		var scenarios = OperationPageModel.BuildExampleScenarios([request], [], [curl]);

		scenarios[0].ShowRequest.Should().BeTrue();
	}

	[Test]
	public void BuildExampleScenarios_CodeSamplesOnly_CreatesSingleScenario()
	{
		var samples = new[] { new CodeSample("Console", "GET /_search", "language-console") };

		var scenarios = OperationPageModel.BuildExampleScenarios([], [], samples);

		scenarios.Should().ContainSingle();
		scenarios[0].Title.Should().Be("Examples");
		scenarios[0].CodeSamples.Should().Equal(samples);
	}

	[Test]
	public void EnsureResponseTabs_FillsStatusTabsWhenScenariosHaveNoResponses()
	{
		var samples = new[] { new CodeSample("Console", "DELETE /api/dashboards/{id}", "language-console") };
		var scenarios = OperationPageModel.BuildExampleScenarios([], [], samples);
		var responses = new OpenApiResponses
		{
			["200"] = new OpenApiResponse { Description = "deleted" },
			["404"] = new OpenApiResponse
			{
				Description = "not found",
				Content = new Dictionary<string, IOpenApiMediaType>
				{
					["application/json"] = new OpenApiMediaType { Schema = new OpenApiSchema { Type = JsonSchemaType.Object } }
				}
			}
		};

		var withTabs = OperationPageModel.EnsureResponseTabs(scenarios, responses);

		withTabs.Should().ContainSingle();
		withTabs[0].Responses.Select(r => r.StatusCode).Should().Equal("200", "404");
		withTabs[0].Responses[0].IsNoBody.Should().BeTrue();
		withTabs[0].Responses[0].HasExampleBody.Should().BeFalse();
		withTabs[0].Responses[1].IsNoBody.Should().BeFalse();
		withTabs[0].ShowResponse.Should().BeTrue();
	}

	[Test]
	public void EnsureResponseTabs_DoesNotReplaceExistingResponseExamples()
	{
		var ok = new ExampleDisplay("Create", null, /*lang=json,strict*/  """{"ok":true}""", null, "200");
		var scenarios = OperationPageModel.BuildExampleScenarios([], [ok], []);
		var responses = new OpenApiResponses
		{
			["200"] = new OpenApiResponse { Description = "ok" },
			["400"] = new OpenApiResponse { Description = "bad" }
		};

		var withTabs = OperationPageModel.EnsureResponseTabs(scenarios, responses);

		withTabs.Should().ContainSingle();
		withTabs[0].Responses.Should().ContainSingle();
		withTabs[0].Responses[0].JsonValue.Should().Contain("ok");
	}

	[Test]
	public void BuildExampleScenarios_PreservesRequestOrderAsScenarioTabs()
	{
		var a = new ExampleDisplay("Alpha", new HtmlString("a"), "{}", null);
		var b = new ExampleDisplay("Beta", null, "{}", null);

		var scenarios = OperationPageModel.BuildExampleScenarios([a, b], [], []);

		scenarios.Select(s => s.Title).Should().Equal("Alpha", "Beta");
		scenarios[0].DescriptionHtml.Should().NotBeNull();
	}

	[Test]
	public void WithOperationIdentity_StampsMethodAndRouteOnEveryScenario()
	{
		var scenarios = OperationPageModel.WithOperationIdentity(
			[
				new ExampleScenario { Title = "Match all", TabId = "match-all" },
				new ExampleScenario { Title = "Query string", TabId = "query-string" }
			],
			"get",
			"/_search"
		);

		scenarios.Should().AllSatisfy(s =>
		{
			s.HttpMethod.Should().Be("get");
			s.Route.Should().Be("/_search");
		});
	}

	[Test]
	public async Task Create_BudgetSpec_Renders200ExampleAndOmitsEmptyResponse()
	{
		const string exampleBody = "{\n  \"id\": 432433423\n}";
		var page = await CreatePage(CreateBudgetSpec);
		var html = await _OperationExamplesPanel.Create(new OperationExamplesPanelModel { Scenarios = page.Scenarios }).RenderAsync(
			cancellationToken: TestContext.Current!.Execution.CancellationToken
		);

		var responses = page.Scenarios.SelectMany(static s => s.Responses).ToArray();
		responses.Should().ContainSingle();
		responses[0].StatusCode.Should().Be("200");
		responses[0].JsonValue?.ReplaceLineEndings("\n").Should().Be(exampleBody);
		html.Should().Contain("432433423");
		html.Should().NotContain("example-empty");
		html.Should().NotContain(">400</span>");
	}

	[Test]
	public async Task Create_ExampleDescriptions_AreKeptExactlyAsTheSpecWritesThem()
	{
		var page = await CreatePage(DescribedExamplesSpec);

		var term = page.Scenarios.Single(static s => s.Title == "A simple term search");
		var lead = page.Scenarios.Single(static s => s.Title == "A lead-in");
		// The test renderer leaves backticks alone; on the site they become <code>. The words must all be there.
		var termText = term.DescriptionHtml!.Value.ToString();
		termText.Should().Contain("Run ").And.Contain("GET /my-index-000001/_search?from=40&amp;size=20").And.Contain("to run a search.");
		var leadText = lead.DescriptionHtml!.Value.ToString();
		leadText.Should().Contain("A successful response from ").And.Contain("POST /x");
	}

	[Test]
	[Arguments("executeBuiltinEsqlToolRequest", "Execute builtin ES|QL tool")]
	[Arguments("executeBuiltinEsqlToolExample", "Execute builtin ES|QL tool")]
	[Arguments("SearchResponseExample1", "Search response example1")]
	[Arguments("Already readable", "Already readable")]
	public void HumanizeExampleKey_ReadsLikeATitle(string key, string expected) =>
		OperationPageModel.HumanizeExampleKey(key).Should().Be(expected);

	[Test]
	public void BuildExampleScenarios_SamplesMatchingNoExample_BecomeTheirOwnFirstExample()
	{
		var esql = new ExampleDisplay("Execute builtin ES|QL tool", null, /*lang=json,strict*/  """{"tool_id":"esql"}""", null);
		var samples = new[] { new CodeSample("Console", "POST kbn:/api/tools/_execute\n{\"tool_id\":\"search\"}", "language-console") };

		var scenarios = OperationPageModel.BuildExampleScenarios([esql], [], samples);

		scenarios.Select(static s => s.Title).Should().Equal("Example", "Execute builtin ES|QL tool");
		scenarios[0].CodeSamples.Should().ContainSingle();
		scenarios[1].CodeSamples.Should().BeEmpty();
	}

	[Test]
	public void GeneratedCodeSamples_JsonOnlyExample_GetsConsoleAndCurl()
	{
		var slicing = new ExampleScenario
		{
			Title = "Search slicing",
			TabId = "slicing",
			RequestJson = /*lang=json,strict*/  """{"slice":{"id":0}}""",
			RequestLine = ("GET", "/_search")
		};
		var spec = new[]
		{
			new CodeSample("Console", "GET /my-index/_search\n{}", "language-console"),
			new CodeSample(
				"curl",
				"curl -X GET -H \"Authorization: ApiKey $ELASTIC_API_KEY\" -d '{}' \"$ELASTICSEARCH_URL/my-index/_search\"",
				"language-curl"
			)
		};
		var endpoint = OperationEndpoint.FromVariants([new EndpointVariant("post", "/_search")], "/_search", mergeMethods: true);

		var filled = GeneratedCodeSamples.Fill([slicing], spec, endpoint);

		var samples = filled.Single().CodeSamples;
		samples.Select(static s => s.Language).Should().Equal("Console", "curl");
		samples.Should().OnlyContain(static s => s.Generated);
		samples[0].Source.Should().Be("GET /_search\n{\"slice\":{\"id\":0}}");
		samples[1]
			.Source
			.Should()
			.Be("curl -X GET -H \"Authorization: ApiKey $ELASTIC_API_KEY\" -d '{\"slice\":{\"id\":0}}' \"$ELASTICSEARCH_URL/_search\"");
		filled.Single().CodeSamplesIncludeRequest.Should().BeTrue();
	}

	[Test]
	public void GeneratedCodeSamples_KibanaExample_KeepsTheKbnPrefixAndUsesTheShortestRoute()
	{
		var esql = new ExampleScenario { Title = "ES|QL", TabId = "esql", RequestJson = /*lang=json,strict*/  """{"tool_id":"esql"}""" };
		var spec = new[] { new CodeSample("Console", "POST kbn:/api/agent_builder/tools/_execute\n{}", "language-console") };
		var endpoint = OperationEndpoint.FromVariants(
			[
				new EndpointVariant("post", "/api/agent_builder/tools/_execute"),
				new EndpointVariant("post", "/s/{space_id}/api/agent_builder/tools/_execute")
			],
			"/api/agent_builder/tools/_execute",
			mergeMethods: true
		);

		var console = GeneratedCodeSamples.Fill([esql], spec, endpoint).Single().CodeSamples.Single();

		console.Source.Should().StartWith("POST kbn:/api/agent_builder/tools/_execute\n");
	}

	[Test]
	public void ParseRequestLine_ReadsTheRunInstruction()
	{
		GeneratedCodeSamples
			.ParseRequestLine("Run `GET /my-index-000001/_search?from=40&size=20` to run a search.")
			.Should()
			.Be(("GET", "/my-index-000001/_search?from=40&size=20"));
		GeneratedCodeSamples.ParseRequestLine("Returns documents.").Should().BeNull();
	}

	private static async Task<OperationPageModel> CreatePage(string spec)
	{
		var path = Path.Join(Path.GetTempPath(), $"response-example-{Guid.NewGuid():N}.json");
		try
		{
			await File.WriteAllTextAsync(path, spec, TestContext.Current!.Execution.CancellationToken);
			var document = await OpenApiReader.Instance.ReadAsync(new FileSystem().FileInfo.New(path));
			document.Should().NotBeNull();
			var pathItem = document!.Paths!["/api/v1/billing/organization/{organization_id}/budget"];
			var operation = pathItem.Operations![HttpMethod.Post];
			var apiOperation = new ApiOperation(
				HttpMethod.Post,
				operation,
				"/api/v1/billing/organization/{organization_id}/budget",
				pathItem,
				"Create a budget"
			);
			var build = new BuildContext(
				new DiagnosticsCollector([]),
				DocumentationFileSystem.Resolve(Paths.WorkingDirectoryRoot.FullName),
				TestHelpers.CreateConfigurationContext(new FileSystem())
			);
			var context = new ApiRenderContext(
				build,
				document,
				new StaticFileContentHashProvider(new EmbeddedOrPhysicalFileProvider(build))
			)
			{
				NavigationHtml = string.Empty,
				CurrentNavigation = new LandingNavigationItem("/api/doc/cloud-billing").Index,
				MarkdownRenderer = PassthroughMarkdownRenderer.Instance
			};
			return OperationPageModel.Create(apiOperation, context);
		}
		finally
		{
			if (File.Exists(path))
				File.Delete(path);
		}
	}

	private const string CreateBudgetSpec =
		"""
		{
		  "openapi": "3.0.3",
		  "info": { "title": "Cloud Billing", "version": "1" },
		  "paths": {
		    "/api/v1/billing/organization/{organization_id}/budget": {
		      "post": {
		        "operationId": "createBudgetV1",
		        "summary": "Create a budget for the organization.",
		        "responses": {
		          "200": {
		            "description": "Budget created successfully",
		            "content": {
		              "application/json": {
		                "example": { "id": 432433423 }
		              }
		            }
		          },
		          "400": { "description": "Invalid request" }
		        }
		      }
		    }
		  }
		}
		""";

	private const string DescribedExamplesSpec =
		"""
		{
		  "openapi": "3.0.3",
		  "info": { "title": "Cloud Billing", "version": "1" },
		  "paths": {
		    "/api/v1/billing/organization/{organization_id}/budget": {
		      "post": {
		        "operationId": "describedExamples",
		        "summary": "Create a budget for the organization.",
		        "requestBody": {
		          "content": {
		            "application/json": {
		              "examples": {
		                "SearchRequestExample1": {
		                  "summary": "A simple term search",
		                  "description": "Run `GET /my-index-000001/_search?from=40&size=20` to run a search.\n",
		                  "value": { "query": { "term": { "user.id": "kimchy" } } }
		                },
		                "LeadInExample": {
		                  "summary": "A lead-in",
		                  "description": "A successful response from `POST /x`.",
		                  "value": { "ok": true }
		                }
		              }
		            }
		          }
		        },
		        "responses": { "200": { "description": "ok" } }
		      }
		    }
		  }
		}
		""";
}
