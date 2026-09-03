// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using AwesomeAssertions;
using Elastic.ApiExplorer._Partials.Layout;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Landing;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Configuration.Builder;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.FileSystems;
using Elastic.Documentation.Site.FileProviders;
using RazorSlices;

namespace Elastic.ApiExplorer.Tests;

public class ApiTocRenderingTests
{
	[Fact]
	public async Task Render_EmptyToc_IncludesViewAsMarkdownLink()
	{
		var html = await Render([]);

		html.Should().Contain("""<a href="/api/doc/elasticsearch/v9.md" class="link text-sm" target="_blank">""");
		html.Should().NotContain("On this page");
	}

	[Fact]
	public async Task Render_WithTocItems_IncludesHeadings()
	{
		var html = await Render([new ApiTocItem("Paths", "paths")]);

		html.Should().Contain("On this page");
		html.Should().Contain("href=\"#paths\"");
		html.Should().Contain("Paths");
	}

	private static async Task<string> Render(IReadOnlyList<ApiTocItem> tocItems)
	{
		var fs = new FileSystem();
		var context = new BuildContext(
			new DiagnosticsCollector([]),
			DocumentationFileSystem.Resolve(Paths.WorkingDirectoryRoot.FullName),
			TestHelpers.CreateConfigurationContext(fs)
		);
		var navigationItem = new LandingNavigationItem("/api/doc/elasticsearch/v9/").Index;
		var model = new ApiLayoutViewModel
		{
			DocsBuilderVersion = "test",
			DocSetName = "Api Explorer",
			Description = string.Empty,
			CurrentNavigationItem = navigationItem,
			Previous = null,
			Next = null,
			NavigationHtml = string.Empty,
			UrlPathPrefix = string.Empty,
			AllowIndexing = false,
			CanonicalBaseUrl = null,
			GoogleTagManager = new GoogleTagManagerConfiguration(),
			Optimizely = new OptimizelyConfiguration(),
			Features = new FeatureFlags([]),
			StaticFileContentHashProvider = new StaticFileContentHashProvider(new EmbeddedOrPhysicalFileProvider(context)),
			TocItems = tocItems,
			MarkdownUrl = "/api/doc/elasticsearch/v9.md",
		};

		return await _ApiToc.Create(model).RenderAsync(cancellationToken: TestContext.Current.CancellationToken);
	}
}
