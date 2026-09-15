// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer._Partials.Layout;
using Elastic.ApiExplorer.Operations;
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
	}
}
