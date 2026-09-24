// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer._Partials;
using Elastic.ApiExplorer.Infrastructure;
using RazorSlices;

namespace Elastic.ApiExplorer.Tests;

public class ApiBreadcrumbsRenderingTests
{
	[Test]
	public async Task Render_PinsFirstAndCurrent_AndOffersOverflowForMiddleCrumbs()
	{
		var trail = new ApiBreadcrumbTrail([
			new("APIs", "/api/"),
			new("Elasticsearch API", "/api/es/"),
			new("Search", "/api/es/search/"),
			new("Run a search", null)
		]);

		var html = await _ApiBreadcrumbs.Create(new ApiBreadcrumbsView(trail, "")).RenderAsync(
			cancellationToken: TestContext.Current!.Execution.CancellationToken
		);

		html.Should().Contain("api-breadcrumbs");
		html
			.IndexOf("data-crumb=\"start\"", StringComparison.Ordinal)
			.Should()
			.BeLessThan(html.IndexOf("data-crumb=\"middle\"", StringComparison.Ordinal));
		html.Should().Contain("data-crumb=\"end\"");
		html.Should().Contain("aria-current=\"page\"");
		html.Should().Contain(">APIs<");
		html.Should().Contain(">Run a search<");
		html.Should().Contain("api-breadcrumbs__overflow-dropdown");
		html.Should().Contain("data-overflow-index=\"0\"");
		html.Should().Contain("data-overflow-index=\"1\"");
		html.Should().Contain("href=\"/api/es/\"");
		html.Should().Contain("href=\"/api/es/search/\"");
		html.Should().Contain("More breadcrumbs");
		html.Should().Contain("api-page-actions-menu");
	}

	[Test]
	public async Task Render_TwoCrumbs_OmitsOverflowSlot()
	{
		var trail = new ApiBreadcrumbTrail([new("APIs", "/api/"), new("Elasticsearch API", null)]);

		var html = await _ApiBreadcrumbs.Create(new ApiBreadcrumbsView(trail, "")).RenderAsync(
			cancellationToken: TestContext.Current!.Execution.CancellationToken
		);

		html.Should().Contain("data-crumb=\"start\"");
		html.Should().Contain("data-crumb=\"end\"");
		html.Should().NotContain("data-crumb=\"middle\"");
		html.Should().NotContain("api-breadcrumbs__overflow");
	}
}
