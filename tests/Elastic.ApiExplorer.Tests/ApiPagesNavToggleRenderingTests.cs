// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer._Partials.Layout;
using RazorSlices;

namespace Elastic.ApiExplorer.Tests;

public class ApiPagesNavToggleRenderingTests
{
	[Fact]
	public async Task Render_OpensTheSameHamburgerCheckboxAsDocs()
	{
		var html = await _ApiPagesNavToggle.Create().RenderAsync(cancellationToken: TestContext.Current.CancellationToken);

		html.Should().Contain("for=\"pages-nav-hamburger\"");
		html.Should().Contain("aria-label=\"Open navigation\"");
		html.Should().Contain("md:hidden");
	}
}
