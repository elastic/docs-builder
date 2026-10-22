// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Site.StyleGuide;

namespace Elastic.Documentation.Navigation.Tests.DesignSystem;

public class LayoutSamplesTests
{
	private static string StaticUrl(string path) => $"/_static/{path}";

	public static IEnumerable<string> Names() => LayoutSamples.Names;

	[Test]
	[MethodDataSource(nameof(Names))]
	public async Task Sample_IsACompletePageOfTheLayout(string name)
	{
		var html = await LayoutSamples.RenderAsync(name, StaticUrl, TestContext.Current!.Execution.CancellationToken);

		html.Should().NotBeNull().And.Contain("/_static/ds.css").And.Contain("band__content").And.Contain("id=\"main-container\"");
		if (name.EndsWith("-full", StringComparison.Ordinal))
			html.Should().Contain("layout-full");
		else
			html.Should().NotContain("layout-full");
	}

	[Test]
	public async Task SidebarSamples_CarryTheDrawer()
	{
		var html = await LayoutSamples.RenderAsync("three", StaticUrl, TestContext.Current!.Execution.CancellationToken);

		html.Should().Contain("layout__drawer-toggle").And.Contain("layout__menu-button").And.Contain("layout__rail");
	}

	[Test]
	public async Task UnknownSample_IsNull()
	{
		var html = await LayoutSamples.RenderAsync("nope", StaticUrl, TestContext.Current!.Execution.CancellationToken);

		html.Should().BeNull();
	}
}
