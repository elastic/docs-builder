// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer._Partials.Layout;
using Elastic.Documentation.Configuration.Builder;
using Nullean.Snapshots.Html;
using RazorSlices;

namespace Elastic.ApiExplorer.Tests;

public partial class ApiPagesNavRenderingTests
{
	private static readonly FeatureFlags NavigationPreview = new(new Dictionary<string, bool> { ["navigation-preview"] = true });

	[Test]
	public async Task DesignSystemNav_RendersSwitchersAsLinkMenusAboveTheTree()
	{
		var model = CreateLayoutModel(
			"/api/elasticsearch/v9/",
			"/api/elasticsearch/v9.md",
			apiContext: CreateApiContext(versions: [
				new("v9", "/api/elasticsearch/v9/", Selected: true),
				new("v8", "/api/elasticsearch/v8/", Selected: false)
			]),
			features: NavigationPreview
		);

		var html = await _DesignSystemApiNav.Create(model).RenderAsync(cancellationToken: TestContext.Current!.Execution.CancellationToken);

		html.Should().MatchHtmlSnapshot();
		html
			.IndexOf("class=\"link-menu\"", StringComparison.Ordinal)
			.Should()
			.BeLessThan(html.IndexOf("<nav>tree</nav>", StringComparison.Ordinal));
	}

	[Test]
	public async Task DesignSystemNav_VersionTriggerNamesTheVersionWhileTheMenuKeepsTheHint()
	{
		var model = CreateLayoutModel(
			"/api/elasticsearch/next/",
			"/api/elasticsearch/next.md",
			apiContext: CreateApiContext(versions: [
				new("Next (unreleased)", "/api/elasticsearch/next/", Selected: true, ShortLabel: "Next"),
				new("v9 (latest)", "/api/elasticsearch/", Selected: false, ShortLabel: "v9")
			]),
			features: NavigationPreview
		);

		var html = await _DesignSystemApiNav.Create(model).RenderAsync(cancellationToken: TestContext.Current!.Execution.CancellationToken);

		html.Should().Contain("<span class=\"link-menu__current\">Next</span>");
		html.Should().Contain(">Next (unreleased)</a>");
		html.Should().Contain(">v9 (latest)</a>");
	}

	[Test]
	public async Task DesignSystemNav_JumpFieldStaysHiddenUntilScriptRevealsIt()
	{
		var model = CreateLayoutModel(
			"/api/elasticsearch/",
			"/api/elasticsearch.md",
			features: NavigationPreview,
			buildType: Elastic.Documentation.BuildType.Assembler
		);

		var html = await _DesignSystemApiNav.Create(model).RenderAsync(cancellationToken: TestContext.Current!.Execution.CancellationToken);

		html.Should().MatchHtmlSnapshot();
	}

	[Test]
	public async Task DesignSystemNav_Assembler_OmitsTheHubSwitcher()
	{
		var model = CreateLayoutModel(
			"/api/elasticsearch/",
			"/api/elasticsearch.md",
			apiContext: CreateApiContext(),
			features: NavigationPreview,
			buildType: Elastic.Documentation.BuildType.Assembler
		);

		var html = await _DesignSystemApiNav.Create(model).RenderAsync(cancellationToken: TestContext.Current!.Execution.CancellationToken);

		html.Should().NotContain("API product");
	}
}
