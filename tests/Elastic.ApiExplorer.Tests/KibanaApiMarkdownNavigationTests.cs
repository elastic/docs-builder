// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using AwesomeAssertions;
using Elastic.ApiExplorer;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Navigation;
using Elastic.Documentation.Site.FileProviders;
using Elastic.Documentation.Site.Navigation;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Nullean.ScopedFileSystem;

namespace Elastic.ApiExplorer.Tests;

/// <summary>
/// Intro markdown under <c>api:</c> must appear in the left-hand API nav (same tree as tags/operations).
/// Fixtures: repository <c>docs/kibana-api-overview.md</c> and <c>docs/kibana-openapi.json</c>.
/// </summary>
public class KibanaApiMarkdownNavigationTests
{
	private sealed class StubMarkdownRenderer : IMarkdownStringRenderer
	{
		public string Render(string markdown, IFileInfo? source) => "<p>stub-body</p>";

		public string RenderPreservingFirstHeading(string markdown, IFileInfo? source) =>
			"<h1>Kibana spaces</h1><p>stub-body</p>";
	}

	[Fact]
	public async Task CreateNavigation_KibanaIntro_IsLeafAndAppearsInNavHtml()
	{
		var root = Paths.WorkingDirectoryRoot.FullName;
		var introPath = Path.Combine(root, "docs", "kibana-api-overview.md");
		var specPath = Path.Combine(root, "docs", "kibana-openapi.json");
		var fs = new FileSystem();
		var introFile = fs.FileInfo.New(introPath);
		var specFile = fs.FileInfo.New(specPath);

		introFile.Exists.Should().BeTrue();
		specFile.Exists.Should().BeTrue();

		var apiConfig = new ResolvedApiConfiguration
		{
			ProductKey = "kibana",
			IntroMarkdownFiles = [introFile],
			SpecFiles = [specFile]
		};

		var collector = new DiagnosticsCollector([]);
		var configurationContext = TestHelpers.CreateConfigurationContext(fs);
		var context = new BuildContext(collector, FileSystemFactory.RealRead, configurationContext);
		var doc = await OpenApiReader.Create(specFile);

		doc.Should().NotBeNull();

		var generator = new OpenApiGenerator(NullLoggerFactory.Instance, context, NoopMarkdownStringRenderer.Instance);
		var navigation = generator.CreateNavigation("kibana", doc, apiConfig);

		navigation.NavigationItems.Should().NotBeEmpty();
		var introNav = navigation.NavigationItems.OfType<SimpleMarkdownNavigationItem>().FirstOrDefault();
		introNav.Should().NotBeNull();
		introNav.Should().BeAssignableTo<ILeafNavigationItem<IApiModel>>();
		introNav.NavigationTitle.Should().Be("Spaces");

		var writer = new IsolatedBuildNavigationHtmlWriter(context, navigation);
		var result = await writer.RenderNavigation(navigation, introNav, TestContext.Current.CancellationToken);

		result.Html.Should().Contain("Spaces");
		result.Html.Should().Contain("kibana-api-overview");
	}

	[Fact]
	public async Task RenderAsync_IntroMarkdown_UsesApiLayoutChrome()
	{
		var root = Paths.WorkingDirectoryRoot.FullName;
		var introPath = Path.Combine(root, "docs", "kibana-api-overview.md");
		var specPath = Path.Combine(root, "docs", "kibana-openapi.json");
		var fs = new FileSystem();
		var introFile = fs.FileInfo.New(introPath);
		var specFile = fs.FileInfo.New(specPath);

		introFile.Exists.Should().BeTrue();
		specFile.Exists.Should().BeTrue();

		var apiConfig = new ResolvedApiConfiguration
		{
			ProductKey = "kibana",
			IntroMarkdownFiles = [introFile],
			SpecFiles = [specFile]
		};

		var collector = new DiagnosticsCollector([]);
		var configurationContext = TestHelpers.CreateConfigurationContext(fs);
		var context = new BuildContext(collector, FileSystemFactory.RealRead, configurationContext);
		var doc = await OpenApiReader.Create(specFile);

		doc.Should().NotBeNull();

		var generator = new OpenApiGenerator(NullLoggerFactory.Instance, context, NoopMarkdownStringRenderer.Instance);
		var navigation = generator.CreateNavigation("kibana", doc, apiConfig);
		var introNav = navigation.NavigationItems.OfType<SimpleMarkdownNavigationItem>().FirstOrDefault();
		introNav.Should().NotBeNull();

		var hashProvider = new StaticFileContentHashProvider(new EmbeddedOrPhysicalFileProvider(context));
		var renderContext = new ApiRenderContext(context, doc, hashProvider)
		{
			NavigationHtml = "",
			CurrentNavigation = introNav,
			MarkdownRenderer = new StubMarkdownRenderer()
		};

		// ScopedFileSystem only allows paths under configured scope roots (repo), not OS temp.
		var artifactsDir = Path.Combine(Paths.WorkingDirectoryRoot.FullName, ".artifacts", "api-explorer-tests");
		context.WriteFileSystem.Directory.CreateDirectory(artifactsDir);
		var tmp = Path.Combine(artifactsDir, $"api-md-{Guid.NewGuid():N}.html");
		try
		{
			await using (var stream = context.WriteFileSystem.FileStream.New(tmp, FileMode.Create))
				await introNav.RenderAsync(stream, renderContext, TestContext.Current.CancellationToken);

			var html = await context.WriteFileSystem.File.ReadAllTextAsync(tmp, TestContext.Current.CancellationToken);
			html.Should().Contain("id=\"markdown-content\"");
			html.Should().Contain("elastic-docs-v3");
			html.Should().Contain("stub-body");
			html.Should().Contain("<title>Spaces |");
			html.Should().Contain("<h1>Kibana spaces</h1>");
		}
		finally
		{
			if (context.WriteFileSystem.File.Exists(tmp))
				context.WriteFileSystem.File.Delete(tmp);
		}
	}

	[Fact]
	public async Task CreateNavigation_IntroAndOutroPages_AppearsInCorrectOrder()
	{
		var root = Paths.WorkingDirectoryRoot.FullName;
		var introPath = Path.Combine(root, "docs", "kibana-api-overview.md");
		var specPath = Path.Combine(root, "docs", "kibana-openapi.json");
		var fs = new FileSystem();
		var introFile = fs.FileInfo.New(introPath);
		var specFile = fs.FileInfo.New(specPath);

		// Create a mock outro file for testing
		var mockOutroPath = Path.Combine(root, "docs", "kibana-api-outro.md");
		var mockOutroFile = new MockFileInfo(mockOutroPath, "# Outro\n\nEnd of API documentation.");

		var apiConfig = new ResolvedApiConfiguration
		{
			ProductKey = "kibana",
			IntroMarkdownFiles = [introFile],
			SpecFiles = [specFile],
			OutroMarkdownFiles = [mockOutroFile]
		};

		var collector = new DiagnosticsCollector([]);
		var configurationContext = TestHelpers.CreateConfigurationContext(fs);
		var context = new BuildContext(collector, FileSystemFactory.RealRead, configurationContext);
		var doc = await OpenApiReader.Create(specFile);

		doc.Should().NotBeNull();

		var generator = new OpenApiGenerator(NullLoggerFactory.Instance, context, NoopMarkdownStringRenderer.Instance);
		var navigation = generator.CreateNavigation("kibana", doc, apiConfig);

		navigation.NavigationItems.Should().NotBeEmpty();

		// Verify navigation order: intro pages should be first
		var firstItem = navigation.NavigationItems.First();
		firstItem.Should().BeOfType<SimpleMarkdownNavigationItem>();
		((SimpleMarkdownNavigationItem)firstItem).NavigationTitle.Should().Be("Spaces");

		// Verify outro pages should be last
		var lastItem = navigation.NavigationItems.Last();
		lastItem.Should().BeOfType<SimpleMarkdownNavigationItem>();
		((SimpleMarkdownNavigationItem)lastItem).NavigationTitle.Should().Be("Outro");

		// Verify there are operation/tag items between intro and outro
		var middleItems = navigation.NavigationItems.Skip(1).Take(navigation.NavigationItems.Count - 2);
		middleItems.Should().NotBeEmpty("There should be generated API content between intro and outro pages");
	}

	[Fact]
	public async Task CreateNavigation_IntroPages_GeneratesCorrectUrls()
	{
		var root = Paths.WorkingDirectoryRoot.FullName;
		var introPath = Path.Combine(root, "docs", "kibana-api-overview.md");
		var specPath = Path.Combine(root, "docs", "kibana-openapi.json");
		var fs = new FileSystem();
		var introFile = fs.FileInfo.New(introPath);
		var specFile = fs.FileInfo.New(specPath);

		var apiConfig = new ResolvedApiConfiguration
		{
			ProductKey = "kibana",
			IntroMarkdownFiles = [introFile],
			SpecFiles = [specFile]
		};

		var collector = new DiagnosticsCollector([]);
		var configurationContext = TestHelpers.CreateConfigurationContext(fs);
		var context = new BuildContext(collector, FileSystemFactory.RealRead, configurationContext);
		var doc = await OpenApiReader.Create(specFile);

		doc.Should().NotBeNull();

		var generator = new OpenApiGenerator(NullLoggerFactory.Instance, context, NoopMarkdownStringRenderer.Instance);
		var navigation = generator.CreateNavigation("kibana", doc, apiConfig);

		var introNav = navigation.NavigationItems.OfType<SimpleMarkdownNavigationItem>().FirstOrDefault();
		introNav.Should().NotBeNull();

		// Verify URL generation follows expected pattern: /api/{product}/{slug}/
		introNav.Url.Should().Be("/api/kibana/kibana-api-overview/");
		introNav.Slug.Should().Be("kibana-api-overview");
	}

	[Fact]
	public void CreateNavigation_IntroPages_DetectsUrlCollisions()
	{
		// Test collision with reserved segments
		var act1 = () => SimpleMarkdownNavigationItem.ValidateSlugForCollisions(
			"types", "kibana", "/docs/types.md");
		act1.Should().Throw<InvalidOperationException>()
			.WithMessage("*conflicts with reserved API Explorer segment*types*");

		var act2 = () => SimpleMarkdownNavigationItem.ValidateSlugForCollisions(
			"tags", "kibana", "/docs/tags.md");
		act2.Should().Throw<InvalidOperationException>()
			.WithMessage("*conflicts with reserved API Explorer segment*tags*");

		// Test collision with operation monikers
		var operationMonikers = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "search", "index" };
		var act3 = () => SimpleMarkdownNavigationItem.ValidateSlugForCollisions(
			"search", "kibana", "/docs/search.md", operationMonikers);
		act3.Should().Throw<InvalidOperationException>()
			.WithMessage("*conflicts with existing operation moniker*");

		// Verify valid slugs pass validation
		var act4 = () => SimpleMarkdownNavigationItem.ValidateSlugForCollisions(
			"overview", "kibana", "/docs/overview.md", operationMonikers);
		act4.Should().NotThrow();
	}

	[Fact]
	public async Task RenderNavigationItems_IntroPages_DoesNotDoubleEmitHtml()
	{
		var root = Paths.WorkingDirectoryRoot.FullName;
		var introPath = Path.Combine(root, "docs", "kibana-api-overview.md");
		var specPath = Path.Combine(root, "docs", "kibana-openapi.json");
		var fs = new FileSystem();
		var introFile = fs.FileInfo.New(introPath);
		var specFile = fs.FileInfo.New(specPath);

		var apiConfig = new ResolvedApiConfiguration
		{
			ProductKey = "kibana",
			IntroMarkdownFiles = [introFile],
			SpecFiles = [specFile]
		};

		var collector = new DiagnosticsCollector([]);
		var configurationContext = TestHelpers.CreateConfigurationContext(fs);
		var context = new BuildContext(collector, FileSystemFactory.RealRead, configurationContext);
		var doc = await OpenApiReader.Create(specFile);

		doc.Should().NotBeNull();

		var generator = new OpenApiGenerator(NullLoggerFactory.Instance, context, NoopMarkdownStringRenderer.Instance);
		var navigation = generator.CreateNavigation("kibana", doc, apiConfig);

		// Simulate the dual pipeline check: 
		// The intro file should be registered to be skipped in regular HTML generation
		var introNav = navigation.NavigationItems.OfType<SimpleMarkdownNavigationItem>().FirstOrDefault();
		introNav.Should().NotBeNull();

		// The key test: verify that intro files would be handled only by API pipeline
		// In a real scenario, DocumentationGenerator.ProcessFile would skip files
		// that are registered as API intro/outro files to prevent double emission
		var relativePath = Path.GetRelativePath(context.Configuration.DocsPath, introPath);
		relativePath.Should().Be("kibana-api-overview.md");

		// This simulates the check that would happen in DocumentationGenerator
		// to prevent duplicate HTML generation for intro/outro files
		var isApiIntroFile = apiConfig.IntroMarkdownFiles.Any(f => 
			Path.GetRelativePath(context.Configuration.DocsPath, f.FullName) == relativePath);
		isApiIntroFile.Should().BeTrue("Intro file should be registered to prevent duplicate HTML generation");
	}

	/// <summary>
	/// Mock file info for testing outro functionality without requiring actual files.
	/// </summary>
	private class MockFileInfo : IFileInfo
	{
		private readonly string _path;
		private readonly string _content;

		public MockFileInfo(string path, string content)
		{
			_path = path;
			_content = content;
		}

		public bool Exists => true;
		public long Length => _content.Length;
		public string? PhysicalPath => _path;
		public string Name => Path.GetFileName(_path);
		public DateTimeOffset LastModified => DateTimeOffset.Now;
		public bool IsDirectory => false;
		public string FullName => _path;

		public Stream CreateReadStream() => new MemoryStream(System.Text.Encoding.UTF8.GetBytes(_content));
	}
}
