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
using Elastic.Documentation.Site;
using Elastic.Documentation.Site.FileProviders;
using RazorSlices;

namespace Elastic.ApiExplorer.Tests;

public class ApiPagesNavRenderingTests
{
	[Fact]
	public async Task Render_ShowsNavDropdownDesignAndMarksCurrentVersion()
	{
		var fs = new FileSystem();
		var context = new BuildContext(
			new DiagnosticsCollector([]),
			DocumentationFileSystem.Resolve(Paths.WorkingDirectoryRoot.FullName),
			TestHelpers.CreateConfigurationContext(fs));
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
			TocItems = [],
			VersionSwitcherItems =
			[
				new("9.x (latest)", "/api/doc/elasticsearch/", IsActive: false),
				new("9.x", "/api/doc/elasticsearch/v9/", IsActive: true),
				new("8.x", "/api/doc/elasticsearch/v8/", IsActive: false),
			],
		};

		var html = await _ApiPagesNav.Create(model).RenderAsync(cancellationToken: TestContext.Current.CancellationToken);

		html.Should().Contain("id=\"api-version-dropdown\"");
		html.Should().Contain("id=\"api-version-pages-dropdown\"");
		html.Should().Contain("pages-dropdown_active text-blue-elastic");
		html.Should().Contain("9.x");
		html.Should().Contain("href=\"/api/doc/elasticsearch/v8/\"");
		html.Should().Contain("text-blue-elastic");
		html.Should().NotContain("<select");
		html.Should().NotContain("id=\"nav-dropdown\"");
	}

	[Fact]
	public async Task Render_SingleVersion_HidesSwitcher()
	{
		var fs = new FileSystem();
		var context = new BuildContext(
			new DiagnosticsCollector([]),
			FileSystemFactory.RealGitRootForPath(null),
			TestHelpers.CreateConfigurationContext(fs));
		var navigationItem = new LandingNavigationItem("/api/doc/elasticsearch/").Index;
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
			TocItems = [],
			VersionSwitcherItems = [new("9.x (latest)", "/api/doc/elasticsearch/", IsActive: true)],
		};

		var html = await _ApiPagesNav.Create(model).RenderAsync(cancellationToken: TestContext.Current.CancellationToken);

		html.Should().NotContain("id=\"api-version-dropdown\"");
	}
}
