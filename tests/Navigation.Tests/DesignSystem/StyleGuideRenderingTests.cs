// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Site.StyleGuide;

namespace Elastic.Documentation.Navigation.Tests.DesignSystem;

public class StyleGuideRenderingTests
{
	private static string StaticUrl(string path) => $"/_static/{path}";

	public static IEnumerable<string> Slugs() => StyleGuideCatalog.Pages.Select(p => p.Slug);

	[Test]
	[MethodDataSource(nameof(Slugs))]
	public async Task Page_Renders_WithDesignSystemAssetsAndNavigation(string slug)
	{
		var html = await StyleGuideCatalog.RenderAsync(slug, StaticUrl, TestContext.Current!.Execution.CancellationToken);

		html.Should().NotBeNull();
		html.Should().Contain("/_static/ds.css").And.Contain("/_static/ds-guide.css").And.Contain("/_static/ds.js");
		html.Should().NotContain("styles.css", "the style guide is built on the design system alone, with no legacy bundle");
		html.Should().Contain("data-theme-toggle");
		html.Should().Contain($"<h1>{StyleGuideCatalog.Find(slug)!.Title}</h1>");
		foreach (var page in StyleGuideCatalog.Pages)
			html.Should().Contain($"href=\"/style-guide/{page.Slug}\"");
	}

	[Test]
	public async Task LayoutsPage_ShowsBothWidthModesWithTheirTokens()
	{
		var html = await StyleGuideCatalog.RenderAsync("layouts", StaticUrl, TestContext.Current!.Execution.CancellationToken);

		html.Should().Contain("sg-device__screen layout-full").And.Contain("--layout-measure").And.Contain("--layout-rail-max");
	}

	[Test]
	public async Task PageLayoutsPage_DrawsEveryLayoutInBothWidthModes()
	{
		var html = await StyleGuideCatalog.RenderAsync("page-layouts", StaticUrl, TestContext.Current!.Execution.CancellationToken);

		html
			.Should()
			.Contain("layout layout--three")
			.And
			.Contain("layout layout--two")
			.And
			.Contain("layout layout--single layout--hub")
			.And
			.Contain("layout layout--single layout--narrow")
			.And
			.Contain("sg-device__screen layout-full");
	}

	[Test]
	public async Task GridPage_ShowsTheGridAndTheRhythmTokens()
	{
		var html = await StyleGuideCatalog.RenderAsync("grid", StaticUrl, TestContext.Current!.Execution.CancellationToken);

		html.Should().Contain("grid grid--4").And.Contain("--rhythm-section").And.Contain("stack--group");
	}

	[Test]
	public void ComponentPages_AllHaveANavigationEntry()
	{
		var slugs = StyleGuideCatalog.Pages.Select(p => p.Slug).ToHashSet();

		StyleGuideCatalog
			.ComponentPages
			.Keys
			.Where(k => !slugs.Contains(k))
			.Should()
			.BeEmpty("every component page needs an entry in the navigation");
	}

	[Test]
	public async Task EmptySlug_RendersDefaultPage()
	{
		var html = await StyleGuideCatalog.RenderAsync(null, StaticUrl, TestContext.Current!.Execution.CancellationToken);

		html.Should().Contain($"<h1>{StyleGuideCatalog.Default.Title}</h1>");
	}

	[Test]
	public async Task UnknownSlug_ReturnsNull()
	{
		var html = await StyleGuideCatalog.RenderAsync("does-not-exist", StaticUrl, TestContext.Current!.Execution.CancellationToken);

		html.Should().BeNull();
	}

	[Test]
	public async Task Foundations_RendersEveryDocumentedToken()
	{
		var html = await StyleGuideCatalog.RenderAsync("foundations", StaticUrl, TestContext.Current!.Execution.CancellationToken);

		foreach (var token in StyleGuideTokens
			.Surfaces
			.Concat(StyleGuideTokens.Accent)
			.Concat(StyleGuideTokens.Palette)
			.Concat(StyleGuideTokens.Spacing))
			html.Should().Contain(token.Name);
	}
}
