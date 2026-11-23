// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer.Landing;
using Elastic.ApiExplorer.Landing._Partials;
using Elastic.ApiExplorer.Operations;
using Elastic.Documentation.Navigation;
using Nullean.Snapshots.Html;
using RazorSlices;

namespace Elastic.ApiExplorer.Tests;

[ClassDataSource<ApiExplorerFixture>(Shared = SharedType.PerClass)]
public class OverviewRowLifecycleTests(ApiExplorerFixture fixture)
{
	[Test]
	public void BuildTagChildren_DeprecatedOperation_FlagsTheRow()
	{
		var (row, siblings) = RowFor("docs-get-source");

		row.Deprecated.Should().BeTrue();
		siblings.Should().OnlyContain(r => !r.Deprecated);
	}

	[Test]
	public void BuildTagChildren_XStateOperation_CarriesABadgeWithoutPopover()
	{
		var row = RowFor("search").Row;

		row.Availability.Should().NotBeNull();
		row.Availability.ShowTooltip.Should().BeFalse("the row is a link, so the badge must not capture the tap");
	}

	[Test]
	public async Task Render_BetaAndDeprecatedOperation_ShowsBothBadgesWithoutPopover()
	{
		var row = RowFor("search").Row with { Deprecated = true };

		var html = await RenderAsync(row);

		html.Should().MatchHtmlSnapshot();
	}

	[Test]
	public async Task Render_BetaFlaggedOperation_ShowsBetaBadge()
	{
		var beta = fixture.Walk().OfType<OperationNavigationItem>().First(n => n.Model.Operation.Extensions?.ContainsKey("x-beta") == true);
		var row = ApiOverviewBuilder.BuildTagChildren(beta.Parent!, fixture.Context.VersionsConfiguration).Single(r => r.Url == beta.Url);

		var html = await RenderAsync(row);

		html.Should().MatchHtmlSnapshot();
	}

	[Test]
	public async Task Render_PlainOperation_ShowsNoBadges()
	{
		var row = RowFor("docs-get").Row;

		var html = await RenderAsync(row);

		html.Should().MatchHtmlSnapshot();
	}

	[Test]
	public async Task Render_RouteWithOptionalSegment_BracketsTheSegmentSomeVariantsLeaveOut()
	{
		var route = new EndpointRow(
			"/{index}/_search",
			"post",
			["get"],
			[new EndpointSegment("{index}", Optional: true), new EndpointSegment("_search", Optional: false)]
		);

		var html = await _GroupRoute.Create(route).RenderAsync(cancellationToken: TestContext.Current!.Execution.CancellationToken);

		html.Should().MatchInlineHtmlSnapshot(
			"""
			<code class="api-group-route">
				<wbr>[/{index}]<wbr>/_search</code>
			"""
		);
	}

	[Test]
	public async Task Render_RouteWithSeveralOptionalSegments_KeepsTheRouteAsWritten()
	{
		var route = new EndpointRow(
			"/items/by-owner/{owner}",
			"get",
			[],
			[
				new EndpointSegment("items", Optional: false),
				new EndpointSegment("by-owner", Optional: true),
				new EndpointSegment("{owner}", Optional: true)
			]
		);

		var html = await _GroupRoute.Create(route).RenderAsync(cancellationToken: TestContext.Current!.Execution.CancellationToken);

		// Brackets on each segment would suggest /items/{owner} and /items/by-owner are routes too.
		html.Should().MatchInlineHtmlSnapshot(
			"""
			<code class="api-group-route">
				<wbr>/items<wbr>/by-owner<wbr>/{owner}</code>
			"""
		);
	}

	private (ApiOverviewRow Row, IReadOnlyList<ApiOverviewRow> Siblings) RowFor(string operationId) =>
		RowFor(fixture.Walk().OfType<OperationNavigationItem>().First(n => n.Model.Operation.OperationId == operationId));

	private (ApiOverviewRow Row, IReadOnlyList<ApiOverviewRow> Siblings) RowFor(OperationNavigationItem operation)
	{
		var rows = ApiOverviewBuilder.BuildTagChildren(operation.Parent!, fixture.Context.VersionsConfiguration);
		return (rows.Single(r => r.Url == operation.Url), [.. rows.Where(r => r.Url != operation.Url)]);
	}

	private static async Task<string> RenderAsync(ApiOverviewRow row)
	{
		var slice = _ApiGroupList.Create(new ApiGroupListModel([row], string.Empty));
		return await slice.RenderAsync(cancellationToken: TestContext.Current!.Execution.CancellationToken);
	}
}
