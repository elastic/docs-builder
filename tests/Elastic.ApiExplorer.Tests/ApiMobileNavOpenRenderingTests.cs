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

public class ApiMobileNavOpenRenderingTests
{
	[Fact]
	public async Task Render_FlagOff_IncludesOpenLabel()
	{
		var html = await Render(new FeatureFlags([]));

		AssertOpenLabel(html);
	}

	[Fact]
	public async Task Render_FlagOn_IncludesOpenLabel()
	{
		var html = await Render(new FeatureFlags(new Dictionary<string, bool> { ["navigation-preview"] = true }));

		AssertOpenLabel(html);
	}

	private static void AssertOpenLabel(string html)
	{
		html.Should().Contain("for=\"pages-nav-hamburger\"");
		html.Should().Contain("role=\"button\"");
		html.Should().Contain("md:hidden");
	}

	private static async Task<string> Render(FeatureFlags features)
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
			Features = features,
			StaticFileContentHashProvider = new StaticFileContentHashProvider(new EmbeddedOrPhysicalFileProvider(context)),
			TocItems = [],
			MarkdownUrl = "/api/doc/elasticsearch/v9.md",
		};

		return await _ApiMobileNavOpen.Create(model).RenderAsync(cancellationToken: TestContext.Current.CancellationToken);
	}
}
