// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using AwesomeAssertions;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Landing;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.FileSystems;
using Elastic.Documentation.Site.FileProviders;
using Microsoft.OpenApi;
using RazorSlices;

namespace Elastic.ApiExplorer.Tests;

public class ApiLayoutFeatureFlagsTests
{
	[Fact]
	public async Task CreateGlobalLayoutModel_WhenNavigationPreviewEnabled_CopiesBuildContextFeatures()
	{
		var (context, viewModel) = CreateViewModel(navigationPreviewEnabled: true);
		var layout = viewModel.CreateGlobalLayoutModel();

		layout.Features.Should().BeSameAs(context.Configuration.Features);
		layout.Features.NavigationPreviewEnabled.Should().BeTrue();

		var html = await ApiCatalogView.Create(viewModel).RenderAsync(cancellationToken: TestContext.Current.CancellationToken);
		html.Should().Contain("navigation-preview");
	}

	[Fact]
	public void CreateGlobalLayoutModel_WhenNavigationPreviewDisabled_KeepsFlagOff()
	{
		var (context, viewModel) = CreateViewModel(navigationPreviewEnabled: false);
		var layout = viewModel.CreateGlobalLayoutModel();

		layout.Features.Should().BeSameAs(context.Configuration.Features);
		layout.Features.NavigationPreviewEnabled.Should().BeFalse();
	}

	private static (BuildContext Context, ApiCatalogViewModel ViewModel) CreateViewModel(bool navigationPreviewEnabled)
	{
		var fs = new FileSystem();
		var context = new BuildContext(
			new DiagnosticsCollector([]),
			DocumentationFileSystem.Resolve(Paths.WorkingDirectoryRoot.FullName),
			TestHelpers.CreateConfigurationContext(fs)
		);
		context.Configuration.Features.NavigationPreviewEnabled = navigationPreviewEnabled;

		var renderContext = new ApiRenderContext(
			context,
			new OpenApiDocument { Info = new OpenApiInfo { Title = "Test API", Version = "1.0" } },
			new StaticFileContentHashProvider(new EmbeddedOrPhysicalFileProvider(context))
		)
		{
			NavigationHtml = string.Empty,
			CurrentNavigation = new LandingNavigationItem("/api/doc/test/").Index,
			MarkdownRenderer = PassthroughMarkdownRenderer.Instance
		};

		var viewModel = new ApiCatalogViewModel(renderContext) { Entries = [] };
		return (context, viewModel);
	}
}
