// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Operations._Partials;
using RazorSlices;

namespace Elastic.ApiExplorer.Tests;

public class ParamSectionHeaderRenderingTests
{
	[Test]
	public async Task Render_UnionRequestBody_ListsVariantsAsAlternatives()
	{
		var html = await Render(new ParamSectionHeader("Request", "request-body", ["EqlRule", "QueryRule"], NamesAreVariants: true));

		html.Should().Contain("data-param-more-noun=\"variant\"");
		html.Should().Contain(" | ");
		html.Should().NotContain("api-param-summary-brace");
	}

	[Test]
	public async Task Render_PropertyNames_KeepTheObjectBraces()
	{
		var html = await Render(new ParamSectionHeader("Request", "request-body", ["name", "query"]));

		html.Should().Contain("api-param-summary-brace");
		html.Should().NotContain("data-param-more-noun");
	}

	private static async Task<string> Render(ParamSectionHeader model) =>
		await _ParamSectionHeader.Create(model).RenderAsync(cancellationToken: TestContext.Current!.Execution.CancellationToken);
}
