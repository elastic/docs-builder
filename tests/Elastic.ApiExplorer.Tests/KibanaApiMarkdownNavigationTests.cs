// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.ApiExplorer;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Landing;
using Elastic.ApiExplorer.Model;
using Elastic.ApiExplorer.Operations;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Products;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.FileSystems;
using Elastic.Documentation.Navigation;
using Elastic.Documentation.Site.FileProviders;
using Elastic.Documentation.Site.Navigation;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Tests;

/// <summary>
/// Tests for intro markdown pages in API Explorer navigation.
/// </summary>
public class KibanaApiMarkdownNavigationTests
{
	private sealed class StubMarkdownRenderer : IMarkdownStringRenderer
	{
		public string Render(string markdown, IFileInfo? source) => "<p>stub-body</p>";
		public string RenderPreservingFirstHeading(string markdown, IFileInfo? source) => "<h1>Kibana spaces</h1><p>stub-body</p>";
	}

	private sealed class MetaTitleMarkdownRenderer : IMarkdownStringRenderer
	{
		public string Render(string markdown, IFileInfo? source) => "<p>stub-body</p>";
		public string RenderPreservingFirstHeading(string markdown, IFileInfo? source) => "<h1>Kibana spaces</h1><p>stub-body</p>";

		public MarkdownRenderResult RenderPreservingFirstHeadingWithMetadata(string markdown, IFileInfo? source) =>
			new(RenderPreservingFirstHeading(markdown, source), "Kibana spaces", "Kibana spaces for browser tabs");
	}

	private static (LandingNavigationItem navigation, SimpleMarkdownNavigationItem introNav) SetupKibanaNavigation()
	{
		var (navigation, introNav, _, _) = SetupKibanaRendering();
		return (navigation, introNav);
	}

	private static (LandingNavigationItem navigation, SimpleMarkdownNavigationItem introNav, BuildContext context, OpenApiDocument document) SetupKibanaRendering()
	{
		var root = Paths.WorkingDirectoryRoot.FullName;
		var introPath = Path.Combine(root, "tests", "Elastic.ApiExplorer.Tests", "TestData", "kibana-api-overview.md");
		var specPath = Path.Combine(root, "tests", "Elastic.ApiExplorer.Tests", "TestData", "kibana-openapi-no-x-req-auth-sample.json");
		var fs = new FileSystem();
		var introFile = fs.FileInfo.New(introPath);
		var specFile = fs.FileInfo.New(specPath);

		var apiConfig = new ResolvedApiConfiguration
		{
			ProductKey = "kibana",
			Product = new Product { Id = "kibana", DisplayName = "Kibana" },
			SpecFileName = "kibana-openapi-no-x-req-auth-sample.json",
			LocalSpecFile = specFile,
			Children = [introFile]
		};

		var collector = new DiagnosticsCollector([]);
		var configurationContext = TestHelpers.CreateConfigurationContext(fs);
		var context = new BuildContext(
			collector,
			DocumentationFileSystem.Resolve(Paths.WorkingDirectoryRoot.FullName),
			configurationContext
		);
		var doc = OpenApiReader.Instance.ReadAsync(specFile).GetAwaiter().GetResult();
		doc.Should().NotBeNull("OpenAPI document should load successfully");
		var generator = new OpenApiGenerator(NullLoggerFactory.Instance, context, NoopMarkdownStringRenderer.Instance);
		var navigation = generator.CreateNavigation("kibana", doc, apiConfig);
		var introNav = navigation.NavigationItems.OfType<SimpleMarkdownNavigationItem>().First();

		return (navigation, introNav, context, doc);
	}

	[Test]
	public async Task IntroNav_MetaTitle_OverridesOnlyTheBrowserTitle()
	{
		var (_, introNav, context, document) = SetupKibanaRendering();
		var renderContext = new ApiRenderContext(
			context,
			document,
			new StaticFileContentHashProvider(new EmbeddedOrPhysicalFileProvider(context))
		)
		{ NavigationHtml = string.Empty, CurrentNavigation = introNav, MarkdownRenderer = new MetaTitleMarkdownRenderer() };

		var fs = new MockFileSystem();
		await using (var stream = fs.FileStream.New("/out.html", FileMode.Create, FileAccess.Write))
			await introNav.RenderAsync(stream, renderContext, TestContext.Current!.Execution.CancellationToken);
		var html = fs.File.ReadAllText("/out.html");

		// The H1 ("Kibana spaces") and the navigation title ("Spaces") differ from the meta title.
		html.Should().Contain("<title>Kibana spaces for browser tabs | ");
		html.Should().Contain("<h1>Kibana spaces</h1>");
		// The structured breadcrumb uses the parsed H1, not the navigation title or the override.
		html.Should().Contain("\"name\": \"Kibana spaces\"");
		html.Should().NotContain("\"name\": \"Spaces\"");
		html.Should().NotContain("\"name\": \"Kibana spaces for browser tabs\"");
	}

	[Test]
	public void IntroNav_ShouldBeLeafNavigationItem()
	{
		var (_, introNav) = SetupKibanaNavigation();

		introNav.Should().BeAssignableTo<ILeafNavigationItem<IApiModel>>();
		introNav.NavigationTitle.Should().Be("Spaces");
		introNav.Slug.Should().Be("kibana-api-overview");
	}

	[Test]
	public void IntroNav_ShouldAppearFirstInNavigation()
	{
		var (navigation, introNav) = SetupKibanaNavigation();

		var firstItem = navigation.NavigationItems.First();
		firstItem.Should().Be(introNav);
		firstItem.Should().BeOfType<SimpleMarkdownNavigationItem>();
	}

	[Test]
	public void IntroNav_ShouldGenerateCorrectUrl()
	{
		var (_, introNav) = SetupKibanaNavigation();

		introNav.Url.Should().Be("/api/doc/kibana/kibana-api-overview/");
	}

	[Test]
	public void UrlCollisionValidation_ShouldAllowSlugMatchingOperationId()
	{
		var act = () => SimpleMarkdownNavigationItem.ValidateSlugForCollisions("search", "kibana", "/docs/search.md");

		act.Should().NotThrow();
	}

	[Test]
	public void UrlCollisionValidation_ShouldAllowValidSlugs()
	{
		var act = () => SimpleMarkdownNavigationItem.ValidateSlugForCollisions("overview", "kibana", "/docs/overview.md");

		act.Should().NotThrow();
	}
}
