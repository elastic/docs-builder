// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer.Operations._Partials;
using RazorSlices;

namespace Elastic.ApiExplorer.Tests;

public class EndpointCopyButtonRenderingTests
{
	[Fact]
	public async Task Render_UsesTheEuiCopyIcon()
	{
		var html = await _EndpointCopyButton.Create("/_search").RenderAsync(cancellationToken: TestContext.Current.CancellationToken);

		html.Should().Contain("api-url-copy");
		html.Should().Contain("data-copy=\"/_search\"");
		html.Should().Contain("M6 1C5.44771 1 5 1.44772 5 2V10");
		html.Should().NotContain("M9 12h3.75");
	}
}
