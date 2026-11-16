// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer._Partials;
using Elastic.ApiExplorer.Operations._Partials;
using Elastic.ApiExplorer.Structural;
using Nullean.Snapshots.Html;
using RazorSlices;

namespace Elastic.ApiExplorer.Tests;

public class EndpointCopyButtonRenderingTests
{
	[Test]
	public async Task Render_UsesTheEuiCopyIcon()
	{
		var html = await _EndpointCopyButton.Create("/_search").RenderAsync(
			cancellationToken: TestContext.Current!.Execution.CancellationToken
		);

		html.Should().MatchHtmlSnapshot();
	}

	[Test]
	public async Task ServerUrlField_CopiesTheBaseUrl()
	{
		var html = await _ServerUrlField.Create(new ApiServerDisplay("https://api.example.com", "Production")).RenderAsync(
			cancellationToken: TestContext.Current!.Execution.CancellationToken
		);

		html.Should().MatchInlineHtmlSnapshot(
			"""
			<span class="api-server">
				<span class="api-server-field">
					<code>https://api.example.com</code>
					<button type="button" class="copybtn copybtn--in-header api-url-copy" aria-label="Copy endpoint" data-tippy-content="Copy" data-copy="https://api.example.com">
						<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 16 16" fill="none" aria-hidden="true">
							<path fill-rule="evenodd" clip-rule="evenodd" d="M6 1C5.44771 1 5 1.44772 5 2V10C5 10.5523 5.44772 11 6 11H14C14.5523 11 15 10.5523 15 10V2C15 1.44771 14.5523 1 14 1H6ZM6 2L14 2V10H6V2Z" fill="currentColor"></path>
							<path d="M2 5H4V6H2V14H10V12H11V14C11 14.5523 10.5523 15 10 15H2C1.44772 15 1 14.5523 1 14V6C1 5.44772 1.44771 5 2 5Z" fill="currentColor"></path>
						</svg>
					</button>
				</span>
				<span class="api-server-description">Production</span>
			</span>
			"""
		);
	}

	[Test]
	public async Task ServerUrlField_OmitsDescriptionWhenMissing()
	{
		var html = await _ServerUrlField.Create(new ApiServerDisplay("https://api.example.com", null)).RenderAsync(
			cancellationToken: TestContext.Current!.Execution.CancellationToken
		);

		html.Should().MatchInlineHtmlSnapshot(
			"""
			<span class="api-server">
				<span class="api-server-field">
					<code>https://api.example.com</code>
					<button type="button" class="copybtn copybtn--in-header api-url-copy" aria-label="Copy endpoint" data-tippy-content="Copy" data-copy="https://api.example.com">
						<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 16 16" fill="none" aria-hidden="true">
							<path fill-rule="evenodd" clip-rule="evenodd" d="M6 1C5.44771 1 5 1.44772 5 2V10C5 10.5523 5.44772 11 6 11H14C14.5523 11 15 10.5523 15 10V2C15 1.44771 14.5523 1 14 1H6ZM6 2L14 2V10H6V2Z" fill="currentColor"></path>
							<path d="M2 5H4V6H2V14H10V12H11V14C11 14.5523 10.5523 15 10 15H2C1.44772 15 1 14.5523 1 14V6C1 5.44772 1.44771 5 2 5Z" fill="currentColor"></path>
						</svg>
					</button>
				</span>
			</span>
			"""
		);
	}
}
