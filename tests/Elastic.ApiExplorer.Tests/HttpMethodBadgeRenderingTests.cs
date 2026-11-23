// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer._Partials;
using Nullean.Snapshots;
using RazorSlices;

namespace Elastic.ApiExplorer.Tests;

public class HttpMethodBadgeRenderingTests
{
	private static ValueTask<string> RenderAsync(params string[] methods) =>
		_HttpMethodBadge.Create(methods).RenderAsync(cancellationToken: TestContext.Current!.Execution.CancellationToken);

	[Test]
	public async Task Render_OneMethod_IsALoneChip()
	{
		var html = await RenderAsync("get");

		html.Should().NotContain("api-method-group");
		html.Should().Contain("api-method api-method-get");
	}

	[Test]
	public async Task Render_SeveralMethods_JoinsOneChipPerMethodDominantFirstUnderOneTooltip()
	{
		var html = await RenderAsync("post", "get");

		html.Should().MatchInlineSnapshot(
			"""
				<span class="api-method-group" data-tooltip="POST is the main method. GET works the same way: same parameters, body, and response.">
			<span class="api-method api-method-post">
				<span class="api-method-icon" aria-hidden="true">
						<svg width="12" height="12" viewBox="0 0 12 12"><use href="#icon-api-arrow"/></svg>
				</span>
				<span class="api-method-label">POST</span>
			</span>
			<span class="api-method api-method-get">
				<span class="api-method-icon" aria-hidden="true">
						<svg width="12" height="12" viewBox="0 0 12 12"><use href="#icon-api-arrow"/></svg>
				</span>
				<span class="api-method-label">GET</span>
			</span>
				</span>
			"""
		);
	}
}
