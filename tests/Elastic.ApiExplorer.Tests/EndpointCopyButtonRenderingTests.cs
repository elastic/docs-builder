// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer._Partials;
using Elastic.ApiExplorer.Operations._Partials;
using Elastic.ApiExplorer.Structural;
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

		html.Should().Contain("api-url-copy");
		html.Should().Contain("data-copy=\"/_search\"");
		html.Should().Contain("M6 1C5.44771 1 5 1.44772 5 2V10");
		html.Should().NotContain("M9 12h3.75");
	}

	[Test]
	public async Task ServerUrlField_CopiesTheBaseUrl()
	{
		var html = await _ServerUrlField.Create(new ApiServerDisplay("https://api.example.com", "Production")).RenderAsync(
			cancellationToken: TestContext.Current!.Execution.CancellationToken
		);

		html.Should().Contain("api-server-field");
		html.Should().Contain("data-copy=\"https://api.example.com\"");
		html.Should().Contain("Production");
	}

	[Test]
	public async Task ServerUrlField_OmitsDescriptionWhenMissing()
	{
		var html = await _ServerUrlField.Create(new ApiServerDisplay("https://api.example.com", null)).RenderAsync(
			cancellationToken: TestContext.Current!.Execution.CancellationToken
		);

		html.Should().NotContain("api-server-description");
	}
}
