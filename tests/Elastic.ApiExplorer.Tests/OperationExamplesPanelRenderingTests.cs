// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer._Partials;
using Elastic.ApiExplorer._Partials.Layout;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Model;
using Elastic.ApiExplorer.Operations;
using Elastic.ApiExplorer.Operations._Partials;
using RazorSlices;

namespace Elastic.ApiExplorer.Tests;

public class OperationExamplesPanelRenderingTests
{
	[Test]
	public async Task Render_MultipleScenarios_ListsEveryExample()
	{
		var model = new OperationExamplesPanelModel
		{
			Scenarios =
			[
				new ExampleScenario
				{
					Title = "Match all",
					TabId = "match-all",
					HttpMethod = "get",
					Route = "/_search",
					CodeSamples =
					[
						new("Console", "GET /_search\n{\"query\":{\"match_all\":{}}}", "language-console"),
						new("curl", "curl match-all", "language-bash")
					],
					Responses = [new ExampleResponse { StatusCode = "200", JsonValue = "{}" }]
				},
				new ExampleScenario
				{
					Title = "Query string",
					TabId = "query-string",
					HttpMethod = "get",
					Route = "/_search",
					CodeSamples =
					[
						new("Console", "GET /_search\n{\"query\":{\"query_string\":{}}}", "language-console"),
						new("curl", "curl query-string", "language-bash")
					],
					Responses = [new ExampleResponse { StatusCode = "200", JsonValue = "{}" }]
				}
			]
		};

		var html = await _OperationExamplesPanel.Create(model).RenderAsync(
			cancellationToken: TestContext.Current!.Execution.CancellationToken
		);

		html.Should().Contain("aria-label=\"Examples\"");
		html.Should().Contain("api-examples-index-list");
		html.Should().Contain(">Examples</p>");
		html.Should().Contain("href=\"#api-example-match-all\">Match all</a>");
		html.Should().Contain("href=\"#api-example-query-string\">Query string</a>");
		html.Should().Contain("id=\"api-example-match-all\"");
		html.Should().Contain("id=\"api-example-query-string\"");
		html.Should().Contain(">Match all</h3>");
		html.Should().Contain(">Query string</h3>");
		html.Should().Contain("match_all");
		html.Should().Contain("query_string");
		html.Should().Contain("api-code-sample-lang");
		html.Should().Contain("data-lang=\"Console\"");
		html.Should().NotContain("data-lang=\"Console\" hidden");
		html.Should().Contain("data-lang=\"curl\" hidden=\"hidden\"");
		html.Should().NotContain("api-scenario-select");
		html.Should().NotContain("data-api-scenarios");
		html.Should().NotContain("api-examples-switcher");
		html.Should().NotContain("api-examples-switcher-title");
		html.Should().NotContain("<select");
		html.Should().NotContain("<option");
		html.Should().NotContain("id=\"api-examples-scenario-switcher\"");
		html.Should().NotContain("class=\"nav-select\"");
		html.Should().NotContain("api-examples-heading");
		html.Should().NotContain("max-[1023px]:hidden");
		var matchAll = html.IndexOf("id=\"api-example-match-all\"", StringComparison.Ordinal);
		var queryString = html.IndexOf("id=\"api-example-query-string\"", StringComparison.Ordinal);
		queryString.Should().BeGreaterThan(matchAll);
	}

	[Test]
	public async Task Render_SingleScenario_OmitsExamplesSwitcher()
	{
		var html = await _OperationExamplesPanel.Create(new OperationExamplesPanelModel
		{
			Scenarios =
			[
				new ExampleScenario
				{
					Title = "Match all",
					TabId = "match-all",
					Responses = [new ExampleResponse { StatusCode = "200", JsonValue = "{}" }]
				}
			]
		}).RenderAsync(cancellationToken: TestContext.Current!.Execution.CancellationToken);

		html.Should().Contain("id=\"api-examples-panel\"");
		html.Should().Contain("example-block--response");
		html.Should().NotContain("api-examples-index");
		html.Should().NotContain("api-examples-scenario-title");
		html.Should().NotContain("api-scenario-select");
		html.Should().NotContain("api-examples-switcher");
		html.Should().NotContain("api-examples-heading");
		html.Should().NotContain(">Examples</span>");
		html.Should().NotContain("max-[1023px]:hidden");
	}

	[Test]
	public async Task Render_RequestAndResponse_ShareTheCodeCardClass()
	{
		var request = await _ApiCodeSample.Create(new ApiCodeSampleModel("rail-one", [new("JSON", "{}", "language-json")])).RenderAsync(
			cancellationToken: TestContext.Current!.Execution.CancellationToken
		);

		request.Should().Contain("api-code-card");
		request.Should().Contain("api-code-sample");

		var response = await _ExampleScenarioContent.Create(new ExampleScenario
		{
			Title = "Match all",
			TabId = "match-all",
			Responses = [new ExampleResponse { StatusCode = "200", JsonValue = "{}" }]
		}).RenderAsync(cancellationToken: TestContext.Current!.Execution.CancellationToken);

		response.Should().Contain("api-code-card");
		response.Should().Contain("example-block--response");
	}

	[Test]
	public async Task Render_RequestHeader_ShowsMethodChipAndRoute_NotLanguage()
	{
		var html = await _ApiCodeSample.Create(
			new ApiCodeSampleModel(
				"rail-one",
				[new("Console", "GET /_search", "language-console"), new("Python", "es.search()", "language-python")],
				"get",
				"/_search"
			)
		).RenderAsync(cancellationToken: TestContext.Current!.Execution.CancellationToken);

		html.Should().Contain("api-code-sample-title");
		html.Should().Contain("api-method-get");
		html.Should().Contain(">GET</span>");
		html.Should().Contain("class=\"api-url\"");
		html.Should().Contain("/_search");
		html.Should().Contain("api-code-sample-lang");
		html.Should().Contain("api-select");
		html.Should().Contain("data-value=\"Console\"");
		html.Should().Contain("data-value=\"Python\"");
		html.Should().Contain(">Console</button>");
		html.Should().NotContain("<select");
		html.Should().NotContain("<option");
		html.Should().NotContain("api-code-sample-label");
	}

	[Test]
	public async Task Render_ScenarioContent_PassesMethodAndRouteToRequestCard()
	{
		var html = await _ExampleScenarioContent.Create(new ExampleScenario
		{
			Title = "Match all",
			TabId = "match-all",
			HttpMethod = "post",
			Route = "/_search",
			CodeSamples = [new("Console", "POST /_search", "language-console")]
		}).RenderAsync(cancellationToken: TestContext.Current!.Execution.CancellationToken);

		html.Should().Contain("api-method-post");
		html.Should().Contain("/_search");
		html.Should().NotContain("api-code-sample-label");
	}

	[Test]
	public async Task Render_ScenarioContent_CodeSamplesWithoutBody_AlsoRendersRequestBodyCard()
	{
		var html = await _ExampleScenarioContent.Create(new ExampleScenario
		{
			Title = "createAgentRequestExample",
			TabId = "create",
			HttpMethod = "post",
			Route = "/api/agent_builder/agents",
			RequestJson = /*lang=json,strict*/  """{"id":"created-agent-id"}""",
			CodeSamples = [new("curl", "curl -X POST", "language-bash")]
		}).RenderAsync(cancellationToken: TestContext.Current!.Execution.CancellationToken);

		html.Should().Contain("curl -X POST");
		html.Should().Contain("created-agent-id");
		html.Should().Contain("api-code-sample-heading");
		html.Should().Contain(">Request example</span>");
		var codeSample = html.IndexOf("curl -X POST", StringComparison.Ordinal);
		var requestBody = html.IndexOf("created-agent-id", StringComparison.Ordinal);
		requestBody.Should().BeGreaterThan(codeSample);
	}

	[Test]
	public async Task Render_ScenarioContent_CodeSamplesEmbeddingBody_OmitsRequestBodyCard()
	{
		var html = await _ExampleScenarioContent.Create(new ExampleScenario
		{
			Title = "Match all",
			TabId = "match-all",
			RequestJson = /*lang=json,strict*/  """{"query":{}}""",
			CodeSamples = [new("Console", "POST /_search\n{\"query\":{}}", "language-console")],
			CodeSamplesIncludeRequest = true
		}).RenderAsync(cancellationToken: TestContext.Current!.Execution.CancellationToken);

		html.Should().NotContain("api-code-sample-heading");
		html.Should().NotContain("rail-match-all-request");
	}

	[Test]
	public async Task Render_ResponseHeader_DoesNotIncludeScenarioSelect()
	{
		var html = await _ExampleScenarioContent.Create(new ExampleScenario
		{
			Title = "Match all",
			TabId = "match-all",
			Responses = [new ExampleResponse { StatusCode = "200", JsonValue = "{}" }]
		}).RenderAsync(cancellationToken: TestContext.Current!.Execution.CancellationToken);

		html.Should().Contain("example-block--response");
		html.Should().Contain("example-response-tab-label");
		html.Should().Contain(">200</span>");
		html.Should().NotContain("api-scenario-select");
		html.Should().NotContain("api-examples-switcher");
	}
}
