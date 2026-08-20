// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using AwesomeAssertions;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Landing;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Builder;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.FileSystems;
using Elastic.Documentation.Navigation;
using Elastic.Documentation.Site.Navigation;

namespace Elastic.ApiExplorer.Tests;

public class IsolatedBuildNavigationHtmlWriterTests
{
	[Fact]
	public async Task RenderNavigation_WithSuppressedDropdown_OmitsNavDropdown()
	{
		var context = CreateContext(primaryNavEnabled: true);
		var navigation = new LandingNavigationItem("/api/doc/elasticsearch/");
		var writer = new IsolatedBuildNavigationHtmlWriter(context, navigation, suppressNavigationDropdown: true);

		var result = await writer.RenderNavigation(navigation, navigation.Index, TestContext.Current.CancellationToken);

		result.Html.Should().NotContain("id=\"pages-dropdown\"");
		result.Html.Should().NotContain("id=\"nav-dropdown\"");
	}

	[Fact]
	public async Task RenderNavigation_WithoutSuppression_RendersNavDropdownWhenPrimaryNavEnabled()
	{
		var context = CreateContext(primaryNavEnabled: true);
		var navigation = new LandingNavigationItem("/api/doc/elasticsearch/");
		var writer = new IsolatedBuildNavigationHtmlWriter(context, navigation);

		var result = await writer.RenderNavigation(navigation, navigation.Index, TestContext.Current.CancellationToken);

		result.Html.Should().Contain("id=\"nav-dropdown\"");
	}

	private static BuildContext CreateContext(bool primaryNavEnabled)
	{
		var fs = new FileSystem();
		var context = new BuildContext(
			new DiagnosticsCollector([]),
			DocumentationFileSystem.Resolve(Paths.WorkingDirectoryRoot.FullName),
			TestHelpers.CreateConfigurationContext(fs));
		context.Configuration.Features.PrimaryNavEnabled = primaryNavEnabled;
		return context;
	}
}
