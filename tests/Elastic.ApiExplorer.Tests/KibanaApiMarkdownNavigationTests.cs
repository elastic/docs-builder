// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using AwesomeAssertions;
using Elastic.ApiExplorer;
using Elastic.ApiExplorer.Landing;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Navigation;
using Elastic.Documentation.Site.FileProviders;
using Elastic.Documentation.Site.Navigation;
using Microsoft.Extensions.Logging.Abstractions;
using Nullean.ScopedFileSystem;

namespace Elastic.ApiExplorer.Tests;

/// <summary>
/// Tests for intro markdown pages in API Explorer navigation.
/// </summary>
public class KibanaApiMarkdownNavigationTests
{
	private sealed class StubMarkdownRenderer : IMarkdownStringRenderer
	{
		public string Render(string markdown, IFileInfo? source) => "<p>stub-body</p>";
		public string RenderPreservingFirstHeading(string markdown, IFileInfo? source) =>
			"<h1>Kibana spaces</h1><p>stub-body</p>";
	}

	private static (LandingNavigationItem navigation, SimpleMarkdownNavigationItem introNav) SetupKibanaNavigation()
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
		var doc = OpenApiReader.Create(specFile).GetAwaiter().GetResult();
		doc.Should().NotBeNull("OpenAPI document should load successfully");
		var generator = new OpenApiGenerator(NullLoggerFactory.Instance, context, NoopMarkdownStringRenderer.Instance);
		var navigation = generator.CreateNavigation("kibana", doc, apiConfig);
		var introNav = navigation.NavigationItems.OfType<SimpleMarkdownNavigationItem>().First();

		return (navigation, introNav);
	}

	[Fact]
	public void IntroNav_ShouldBeLeafNavigationItem()
	{
		var (_, introNav) = SetupKibanaNavigation();

		introNav.Should().BeAssignableTo<ILeafNavigationItem<IApiModel>>();
		introNav.NavigationTitle.Should().Be("Spaces");
	}

	[Fact]
	public void IntroNav_ShouldAppearFirstInNavigation()
	{
		var (navigation, introNav) = SetupKibanaNavigation();

		var firstItem = navigation.NavigationItems.First();
		firstItem.Should().Be(introNav);
		firstItem.Should().BeOfType<SimpleMarkdownNavigationItem>();
	}

	[Fact]
	public void IntroNav_ShouldGenerateCorrectUrl()
	{
		var (_, introNav) = SetupKibanaNavigation();

		introNav.Url.Should().Be("/api/kibana/kibana-api-overview/");
	}

	[Fact]
	public void UrlCollisionValidation_ShouldDetectReservedSegments()
	{
		var actTypes = () => SimpleMarkdownNavigationItem.ValidateSlugForCollisions("types", "kibana", "/docs/types.md");
		var actTags = () => SimpleMarkdownNavigationItem.ValidateSlugForCollisions("tags", "kibana", "/docs/tags.md");

		actTypes.Should().Throw<InvalidOperationException>().WithMessage("*conflicts with reserved API Explorer segment*types*");
		actTags.Should().Throw<InvalidOperationException>().WithMessage("*conflicts with reserved API Explorer segment*tags*");
	}

	[Fact]
	public void UrlCollisionValidation_ShouldDetectOperationMonikers()
	{
		var operationMonikers = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "search", "index" };

		var act = () => SimpleMarkdownNavigationItem.ValidateSlugForCollisions("search", "kibana", "/docs/search.md", operationMonikers);

		act.Should().Throw<InvalidOperationException>().WithMessage("*conflicts with existing operation moniker*");
	}

	[Fact]
	public void UrlCollisionValidation_ShouldAllowValidSlugs()
	{
		var operationMonikers = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "search", "index" };

		var act = () => SimpleMarkdownNavigationItem.ValidateSlugForCollisions("overview", "kibana", "/docs/overview.md", operationMonikers);

		act.Should().NotThrow();
	}
}
