// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.Text.RegularExpressions;
using AwesomeAssertions;
using Elastic.ApiExplorer._Partials.Layout;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Landing;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Configuration.Builder;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Site;
using Elastic.Documentation.Site.FileProviders;
using Nullean.ScopedFileSystem;
using RazorSlices;

namespace Elastic.ApiExplorer.Tests;

public partial class ApiPagesNavRenderingTests
{
	[Fact]
	public async Task Render_MarksOnlyCurrentVersionSelected()
	{
		var fs = new FileSystem();
		var context = new BuildContext(
			new DiagnosticsCollector([]),
			FileSystemFactory.RealGitRootForPath(null),
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
			Htmx = new DefaultHtmxAttributeProvider("/"),
			AllowIndexing = false,
			CanonicalBaseUrl = null,
			GoogleTagManager = new GoogleTagManagerConfiguration(),
			Optimizely = new OptimizelyConfiguration(),
			Features = new FeatureFlags([]),
			StaticFileContentHashProvider = new StaticFileContentHashProvider(new EmbeddedOrPhysicalFileProvider(context)),
			TocItems = [],
			Breadcrumbs = ApiBreadcrumbTrail.Empty,
			VersionSwitcherItems =
			[
				new("Latest", "/api/doc/elasticsearch/", Selected: false),
				new("9.x", "/api/doc/elasticsearch/v9/", Selected: true),
				new("8.x", "/api/doc/elasticsearch/v8/", Selected: false),
			],
		};

		var html = await _ApiPagesNav.Create(model).RenderAsync(cancellationToken: TestContext.Current.CancellationToken);

		html.Should().NotContain("selected=\"False\"");
		html.Should().NotContain("selected=\"True\"");
		html.Should().Contain("<option value=\"/api/doc/elasticsearch/v9/\" selected>9.x</option>");

		var selectedOptions = OptionTag().Matches(html).Count(m => m.Value.Contains(" selected", StringComparison.Ordinal));
		selectedOptions.Should().Be(1);
	}

	[GeneratedRegex("<option[^>]*>", RegexOptions.IgnoreCase)]
	private static partial Regex OptionTag();
}
