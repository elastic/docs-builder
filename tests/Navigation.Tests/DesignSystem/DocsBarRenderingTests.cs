// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Configuration.Builder;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Navigation.Assembler;
using Elastic.Documentation.Navigation.Tests.Isolation;
using Elastic.Documentation.Site;
using Elastic.Documentation.Site.FileProviders;
using Elastic.Documentation.Site.Layout;
using FakeItEasy;
using RazorSlices;

namespace Elastic.Documentation.Navigation.Tests.DesignSystem;

public class DocsBarRenderingTests : DocumentationSetNavigationTestBase
{
	private static readonly TopNavRenderModel TopNav = new([
		new TopNavLinkItem("Guides", "/docs/guides", false, "guides"),
		new TopNavLinkItem("Reference", "/docs/reference", false, "reference"),
		new TopNavDropdownItem("Products", [new TopNavGroup("Search", [new TopNavLinkItem("Elasticsearch", "/docs/es", false)])]),
		new TopNavLinkItem("elastic.co", "https://www.elastic.co", true)
	]);

	private const string VersionJson =
		"""[{"name":"9.0","href":"/docs/9.0","disabled":false},{"name":"8.19","href":"/docs/8.19","disabled":false}]""";

	[Test]
	public async Task DocsBar_MarksTheActiveSection_AndRendersDropdownAndExternalLinks()
	{
		var html = await RenderHeader(airGapped: false);

		html.Should().Contain("id=\"docs-bar\"");
		html.Should().Contain("href=\"/docs/guides\" aria-current=\"page\"", "the guides section is current");
		html.Should().NotContain("href=\"/docs/reference\" aria-current=\"page\"");
		html.Should().Contain("<details class=\"docs-bar__dropdown\" data-menu>");
		html.Should().Contain("Elasticsearch");
		html.Should().Contain("rel=\"noopener noreferrer\"").And.Contain("(opens in a new tab)");
	}

	[Test]
	public async Task DocsBar_KeepsTheExternalNavigationSlot()
	{
		var html = await RenderHeader(airGapped: false);

		html.Should().Contain("id=\"elastic-nav-wrapper\"").And.Contain("hx-preserve").And.Contain("id=\"elastic-nav\"");
		html
			.IndexOf("id=\"elastic-nav\"", StringComparison.Ordinal)
			.Should()
			.BeLessThan(html.IndexOf("id=\"docs-bar\"", StringComparison.Ordinal));
	}

	[Test]
	public async Task DocsBar_RendersTheVersionPickerAsNativeLinks()
	{
		var html = await RenderHeader(airGapped: false);

		html.Should().Contain("link-menu");
		html.Should().Contain("href=\"/docs/8.19\"");
		html.Should().Contain("All versions");
		html.Should().NotContain("<version-dropdown");
	}

	[Test]
	public async Task AirGapped_UsesTheDesignSystemHeaderInsteadOfTheReactOne()
	{
		var html = await RenderHeader(airGapped: true);

		html.Should().Contain("class=\"site-header\"").And.Contain("Elastic Docs (air-gapped)");
		html.Should().NotContain("<elastic-docs-header").And.NotContain("id=\"docs-bar\"");
	}

	private async Task<string> RenderHeader(bool airGapped)
	{
		var fileSystem = new MockFileSystem();
		fileSystem.AddDirectory("/docs");
		var context = CreateContext(fileSystem);
		var flags = new Dictionary<string, bool> { ["navigation-preview"] = true };
		if (airGapped)
			flags["air-gapped"] = true;

		var model = new GlobalLayoutViewModel
		{
			DocSetName = "test",
			Description = "",
			CurrentNavigationItem = FakeNavigationItem("/docs/guides/start", "guides"),
			Previous = null,
			Next = null,
			NavigationHtml = "",
			UrlPathPrefix = "/docs",
			CanonicalBaseUrl = null,
			AllowIndexing = false,
			Features = new FeatureFlags(flags),
			GoogleTagManager = new GoogleTagManagerConfiguration(),
			Optimizely = new OptimizelyConfiguration(),
			StaticFileContentHashProvider = new StaticFileContentHashProvider(new EmbeddedOrPhysicalFileProvider(context)),
			BuildType = BuildType.Assembler,
			ShowVersionDropdown = true,
			VersionDropdownSerializedModel = VersionJson,
			CurrentVersion = "9.0",
			AllVersionsUrl = "/docs/versions"
		};

		return await _DesignSystemAssemblerHeader.Create(model).RenderAsync(
			cancellationToken: TestContext.Current!.Execution.CancellationToken
		);
	}

	// The top navigation is found by walking up from the current item to a site navigation root, so the fake is both.
	private static INavigationItem FakeNavigationItem(string url, string rootId)
	{
		var root = A.Fake<IRootNavigationItem<INavigationModel, INavigationItem>>();
		A.CallTo(() => root.Id).Returns(rootId);
		var item = A.Fake<INavigationItem>(o => o.Implements<ISiteNavigationRoot>());
		A.CallTo(() => item.Url).Returns(url);
		A.CallTo(() => item.NavigationRoot).Returns(root);
		A.CallTo(() => ((ISiteNavigationRoot)item).TopNav).Returns(TopNav);
		return item;
	}
}
