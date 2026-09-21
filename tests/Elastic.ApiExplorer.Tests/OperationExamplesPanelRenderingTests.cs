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
	public async Task Render_UsesNavSelectForMultipleScenarios()
	{
		var model = new OperationExamplesPanelModel
		{
			Scenarios =
			[
				new ExampleScenario { Title = "Match all", TabId = "match-all" },
				new ExampleScenario { Title = "Query string", TabId = "query-string" }
			]
		};

		var html = await _OperationExamplesPanel.Create(model).RenderAsync(cancellationToken: TestContext.Current.CancellationToken);

		html.Should().Contain("id=\"api-examples-scenario-switcher\"");
		html.Should().Contain("class=\"nav-select\"");
		html.Should().Contain("data-scenario=\"match-all\"");
		html.Should().Contain("data-scenario=\"query-string\"");
		html.Should().Contain("aria-selected=\"true\"");
		html.Should().Contain("aria-selected=\"false\"");
		html.Should().NotContain("selected=\"False\"");
		html.Should().NotContain("selected=\"True\"");
		html.Should().NotContain("<select");
		html.Should().NotContain("<option");
		html.Should().NotContain("max-[1023px]:hidden");
	}

	[Fact]
	public async Task Render_SingleScenario_KeepsPanelVisibleForStackedColumn()
	{
		var html = await _OperationExamplesPanel.Create(new OperationExamplesPanelModel
		{
			Scenarios = [new ExampleScenario { Title = "Match all", TabId = "match-all" }]
		}).RenderAsync(cancellationToken: TestContext.Current.CancellationToken);

		html.Should().Contain("id=\"api-examples-panel\"");
		html.Should().Contain("api-examples-heading--stacked");
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
}
