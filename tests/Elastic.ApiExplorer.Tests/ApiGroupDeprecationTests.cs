// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer.Landing;
using Elastic.ApiExplorer.Operations;
using Elastic.Documentation.Site.Navigation;
using Nullean.Snapshots;
using Nullean.Snapshots.Html;
using RazorSlices;

namespace Elastic.ApiExplorer.Tests;

/// <summary>A group whose every operation is deprecated reads as retired; deprecated operations list after current ones.</summary>
public class ApiGroupDeprecationTests
{
	private static readonly string Spec = new MinimalSpec(
		[
			new("/retired", "retired-list", ["retired"], Summary: "List retired", Deprecated: true),
			new("/retired/{id}", "retired-delete", ["retired"], "delete", "Delete retired", Deprecated: true),
			new("/mixed", "mixed-create", ["mixed"], "post", "Create mixed", Deprecated: true),
			new("/mixed/{id}", "mixed-get", ["mixed"], Summary: "Get mixed")
		],
		[new("retired", DisplayName: "Retired"), new("mixed", DisplayName: "Mixed")]
	).ToJson();

	[Test]
	public async Task CreateNavigation_DeprecatedOperation_IsListedAfterCurrentOnes()
	{
		var navigation = await NavigationAsync();

		var listed = navigation
			.Tags()
			.Single(t => t.NavigationTitle == "Mixed")
			.NavigationItems
			.OfType<OperationNavigationItem>()
			.Select(
				static item =>
					$"{item.Endpoint.Rows[0].Method.ToUpperInvariant()} {item.Endpoint.Rows[0].Route}{(item.Model.Deprecated ? " deprecated" : "")}"
			);

		string
			.Join('\n', listed)
			.Should()
			.MatchInlineSnapshot("""
			GET /mixed/{id}
			POST /mixed deprecated
			""");
	}

	[Test]
	public async Task Deprecated_EveryOperationDeprecated_MarksTheGroupOnly()
	{
		var navigation = await NavigationAsync();

		navigation.Tags().Select(t => $"{t.NavigationTitle}: {t.Deprecated}").Should().BeEquivalentTo("Mixed: False", "Retired: True");
	}

	[Test]
	[Arguments("legacy", false)]
	[Arguments("preview", true)]
	public async Task Render_RetiredGroup_StrikesItsSidebarFolder(string sidebar, bool navigationPreview)
	{
		var navigation = await NavigationAsync();
		var model = NavigationRenderModel.Create(
			navigation,
			[navigation],
			isUsingNavigationDropdown: false,
			isPrimaryNavEnabled: false,
			isGlobalAssemblyBuild: false,
			navigationPreviewEnabled: navigationPreview
		);

		var html = await _TocTree.Create(model).RenderAsync(cancellationToken: TestContext.Current!.Execution.CancellationToken);

		html.Should().MatchHtmlSnapshot(
			new HtmlSnapshotOptions().Select(".nav-folder > .peer, .nav-tree__row"),
			options: new SnapshotOptions { NameReplacesArguments = true },
			name: sidebar
		);
	}

	[Test]
	public async Task Render_ProductLanding_StrikesRetiredGroupAndDeprecatedRows()
	{
		var navigation = await NavigationAsync();

		var html = await TestHelpers.RenderIndexAsync(navigation.Index, navigation);

		html.Should().MatchHtmlSnapshot(new HtmlSnapshotOptions().Select(".api-index-group"));
	}

	[Test]
	public async Task Render_RetiredGroupPage_ShowsTheDeprecatedBadgeBesideTheTitle()
	{
		var navigation = await NavigationAsync();
		var retired = navigation.Tags().Single(t => t.NavigationTitle == "Retired");

		var html = await TestHelpers.RenderTagAsync(retired);

		html.Should().MatchHtmlSnapshot(new HtmlSnapshotOptions().Select(".api-page-heading"));
	}

	[Test]
	public async Task Render_MixedGroupPage_ShowsNoBadge()
	{
		var navigation = await NavigationAsync();
		var mixed = navigation.Tags().Single(t => t.NavigationTitle == "Mixed");

		var html = await TestHelpers.RenderTagAsync(mixed);

		html.Should().MatchHtmlSnapshot(new HtmlSnapshotOptions().Select(".api-page-heading"));
	}

	private static async Task<LandingNavigationItem> NavigationAsync()
	{
		var (generator, document) = await TestSpecs.CreateGeneratorWithSpecAsync(Spec);
		return generator.CreateNavigation("test", document);
	}
}
