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
	[Fact]
	public async Task Render_PutsExampleSelectInTheRequestHeader()
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
					CodeSamples = [new("Console", "GET /_search", "language-console"), new("curl", "curl", "language-bash")],
					Responses = [new ExampleResponse { StatusCode = "200", JsonValue = "{}" }]
				},
				new ExampleScenario
				{
					Title = "Query string",
					TabId = "query-string",
					HttpMethod = "get",
					Route = "/_search",
					CodeSamples = [new("Console", "GET /_search", "language-console"), new("curl", "curl", "language-bash")],
					Responses = [new ExampleResponse { StatusCode = "200", JsonValue = "{}" }]
				}
			]
		};

		var html = await _OperationExamplesPanel.Create(model).RenderAsync(cancellationToken: TestContext.Current.CancellationToken);

		html.Should().Contain("data-api-scenarios");
		html.Should().Contain("api-code-sample-header");
		html.Should().Contain("api-scenario-select");
		html.Should().Contain("api-code-sample-lang");
		html.Should().NotContain("api-examples-switcher");
		html.Should().NotContain("api-examples-switcher-title");
		html.Should().NotContain(">Examples</span>");
		html.Should().Contain("data-scenario=\"match-all\"");
		html.Should().Contain("data-scenario=\"query-string\"");
		html.Should().Contain("data-value=\"match-all\"");
		html.Should().Contain("data-value=\"query-string\"");
		html.Should().Contain(">Match all</button>");
		html.Should().Contain(">Query string</button>");
		var requestHeader = html.IndexOf("api-code-sample-header", StringComparison.Ordinal);
		var exampleSelect = html.IndexOf("api-scenario-select", StringComparison.Ordinal);
		var responseHeader = html.IndexOf("example-block-header", StringComparison.Ordinal);
		exampleSelect.Should().BeGreaterThan(requestHeader);
		responseHeader.Should().BeGreaterThan(exampleSelect);
		html.Should().NotContain("<select");
		html.Should().NotContain("<option");
		html.Should().NotContain("id=\"api-examples-scenario-switcher\"");
		html.Should().NotContain("class=\"nav-select\"");
		html.Should().NotContain("api-examples-heading");
		html.Should().NotContain("max-[1023px]:hidden");
	}

	[Fact]
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
		}).RenderAsync(cancellationToken: TestContext.Current.CancellationToken);

		html.Should().Contain("id=\"api-examples-panel\"");
		html.Should().Contain("example-block--response");
		html.Should().NotContain("api-scenario-select");
		html.Should().NotContain("api-examples-switcher");
		html.Should().NotContain("api-examples-heading");
		html.Should().NotContain(">Examples</span>");
		html.Should().NotContain("max-[1023px]:hidden");
	}

	[Fact]
	public async Task Render_RequestAndResponse_ShareTheCodeCardClass()
	{
		var request = await _ApiCodeSample.Create(new ApiCodeSampleModel("rail-one", [new("JSON", "{}", "language-json")])).RenderAsync(
			cancellationToken: TestContext.Current.CancellationToken
		);

		request.Should().Contain("api-code-card");
		request.Should().Contain("api-code-sample");

		var response = await _ExampleScenarioContent.Create(new ExampleScenario
		{
			Title = "Match all",
			TabId = "match-all",
			Responses = [new ExampleResponse { StatusCode = "200", JsonValue = "{}" }]
		}).RenderAsync(cancellationToken: TestContext.Current.CancellationToken);

		response.Should().Contain("api-code-card");
		response.Should().Contain("example-block--response");
	}

	[Fact]
	public async Task Render_RequestHeader_ShowsMethodChipAndRoute_NotLanguage()
	{
		var html = await _ApiCodeSample.Create(
			new ApiCodeSampleModel(
				"rail-one",
				[new("Console", "GET /_search", "language-console"), new("Python", "es.search()", "language-python")],
				"get",
				"/_search"
			)
		).RenderAsync(cancellationToken: TestContext.Current.CancellationToken);

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

	[Fact]
	public async Task Render_ScenarioContent_PassesMethodAndRouteToRequestCard()
	{
		var html = await _ExampleScenarioContent.Create(new ExampleScenario
		{
			Title = "Match all",
			TabId = "match-all",
			HttpMethod = "post",
			Route = "/_search",
			CodeSamples = [new("Console", "POST /_search", "language-console")]
		}).RenderAsync(cancellationToken: TestContext.Current.CancellationToken);

		html.Should().Contain("api-method-post");
		html.Should().Contain("/_search");
		html.Should().NotContain("api-code-sample-label");
	}

	[Fact]
	public async Task Render_ResponseHeader_DoesNotIncludeScenarioSelect()
	{
		var html = await _ExampleScenarioContent.Create(new ExampleScenario
		{
			Title = "Match all",
			TabId = "match-all",
			Responses = [new ExampleResponse { StatusCode = "200", JsonValue = "{}" }]
		}).RenderAsync(cancellationToken: TestContext.Current.CancellationToken);

		html.Should().Contain("example-block--response");
		html.Should().Contain("example-response-tab-label");
		html.Should().Contain(">200</span>");
		html.Should().NotContain("api-scenario-select");
		html.Should().NotContain("api-examples-switcher");
	}
}
