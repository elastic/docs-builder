// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer._Partials.Layout;
using Elastic.Documentation.Configuration.Builder;
using RazorSlices;

namespace Elastic.ApiExplorer.Tests;

public partial class ApiPagesNavRenderingTests
{
	private static readonly FeatureFlags NavigationPreview = new(new Dictionary<string, bool> { ["navigation-preview"] = true });

	[Test]
	public async Task DesignSystemNav_RendersSwitchersAsLinkMenusAboveTheTree()
	{
		var model = CreateLayoutModel(
			"/api/doc/elasticsearch/v9/",
			"/api/doc/elasticsearch/v9.md",
			apiContext: CreateApiContext(versions: [
				new("v9", "/api/doc/elasticsearch/v9/", Selected: true),
				new("v8", "/api/doc/elasticsearch/v8/", Selected: false)
			]),
			features: NavigationPreview
		);

		var html = await _DesignSystemApiNav.Create(model).RenderAsync(cancellationToken: TestContext.Current!.Execution.CancellationToken);

		html.Should().Contain("class=\"link-menu\"");
		html.Should().Contain("aria-label=\"API version: v9\"");
		html.Should().Contain("aria-label=\"API product: Elasticsearch\"");
		html.Should().Contain("href=\"/api/doc/elasticsearch/v8/\"");
		html.Should().NotContain("<select");
		html.Should().NotContain("onchange");
		html
			.IndexOf("class=\"link-menu\"", StringComparison.Ordinal)
			.Should()
			.BeLessThan(html.IndexOf("<nav>tree</nav>", StringComparison.Ordinal));
	}

	[Test]
	public async Task DesignSystemNav_JumpFieldStaysHiddenUntilScriptRevealsIt()
	{
		var model = CreateLayoutModel(
			"/api/doc/elasticsearch/",
			"/api/doc/elasticsearch.md",
			features: NavigationPreview,
			buildType: Elastic.Documentation.BuildType.Assembler
		);

		var html = await _DesignSystemApiNav.Create(model).RenderAsync(cancellationToken: TestContext.Current!.Execution.CancellationToken);

		html.Should().Contain("data-nav-filter hidden");
		html.Should().NotContain("<navigation-search");
	}

	[Test]
	public async Task DesignSystemNav_Assembler_OmitsTheHubSwitcher()
	{
		var model = CreateLayoutModel(
			"/api/doc/elasticsearch/",
			"/api/doc/elasticsearch.md",
			apiContext: CreateApiContext(),
			features: NavigationPreview,
			buildType: Elastic.Documentation.BuildType.Assembler
		);

		var html = await _DesignSystemApiNav.Create(model).RenderAsync(cancellationToken: TestContext.Current!.Execution.CancellationToken);

		html.Should().NotContain("API product");
	}
}
