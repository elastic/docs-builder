// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Operations;
using Elastic.ApiExplorer.Operations._Partials;
using RazorSlices;

namespace Elastic.ApiExplorer.Tests;

public class ParamSectionHeaderRenderingTests
{
	[Test]
	public async Task Render_UnionRequestBody_ListsVariantsAsAlternatives()
	{
		var html = await Render(new ParamSectionHeader("Request", "request-body", ["EqlRule", "QueryRule"], SummaryStyle.Variants));

		html.Should().Contain("data-param-more-noun=\"variant\"");
		html.Should().Contain("api-param-summary is-variants");
		html.Should().NotContain("api-param-summary-brace");
	}

	[Test]
	public async Task Render_JsonFields_AreBracedAsAnObject()
	{
		var html = await Render(new ParamSectionHeader("Request", "request-body", ["name", "query"], SummaryStyle.Fields));

		html.Should().NotContain("is-fields", "a style with no rule of its own adds no class");
		html.Should().Contain("api-param-summary-brace\">{ ");
		html.Should().Contain("api-param-summary-brace\"> }");
		html.Should().NotContain("data-param-more-noun");
	}

	[Test]
	public async Task Render_ParametersAndOtherNames_ArePlainLists()
	{
		var html = await Render(new ParamSectionHeader("Query parameters", "query-params", ["pretty", "human"], SummaryStyle.List));

		html.Should().NotContain("is-list", "a style with no rule of its own adds no class");
		html.Should().Contain("<code class=\"api-param-summary-item\" data-name=\"pretty\"></code>");
		html.Should().NotContain("api-param-summary-brace", "a parameter list is not a JSON object");
	}

	[Test]
	public async Task Render_SummaryNames_AreGeneratedContentSoFindInPageSkipsThem()
	{
		var html = await Render(new ParamSectionHeader("Query parameters", "query-params", ["pretty"], SummaryStyle.List));

		html.Should().Contain("<code class=\"api-param-summary-item\" data-name=\"pretty\"></code>");
		html.Should().NotContain(">pretty<", "the name is not text in the page, so Ctrl+F finds the field in the section body instead");
	}

	[Test]
	public async Task Render_FieldsWithNoNames_HaveNothingToBrace()
	{
		var html = await Render(new ParamSectionHeader("Request body", "request-body", [], SummaryStyle.Fields));

		html.Should().NotContain("api-param-summary-brace");
	}

	private static async Task<string> Render(ParamSectionHeader model) =>
		await _ParamSectionHeader.Create(model).RenderAsync(cancellationToken: TestContext.Current!.Execution.CancellationToken);
}
