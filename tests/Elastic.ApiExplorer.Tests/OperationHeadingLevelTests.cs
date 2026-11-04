// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.RegularExpressions;
using AwesomeAssertions;
using Elastic.ApiExplorer.Operations;
using Nullean.Snapshots.Html;

namespace Elastic.ApiExplorer.Tests;

[ClassDataSource<ApiExplorerFixture>(Shared = SharedType.PerClass)]
public partial class OperationHeadingLevelTests(ApiExplorerFixture fixture)
{
	[Test]
	public async Task Render_SearchOperation_UsesOneH1ThenH2SectionsAndH3Nested()
	{
		var nav = fixture.Walk().OfType<OperationNavigationItem>().First(n => n.Model.Operation.OperationId == "search");
		var html = await fixture.RenderAsync(nav);

		HeadingOpenTags()
			.Matches(html)
			.Select(match => match.Groups[1].Value)
			.Should()
			.Equal("h1", "h2", "h2", "h2", "h2", "h2", "h2", "h3");
	}

	[Test]
	public async Task Render_SearchOperation_PathParameters() => await SnapshotSectionBodyAsync("path-params-list");

	[Test]
	public async Task Render_SearchOperation_QueryParameters() => await SnapshotSectionBodyAsync("query-params-list");

	[Test]
	public async Task Render_SearchOperation_RequestBody() => await SnapshotSectionBodyAsync("request-body-list");

	// One snapshot per section body, so a markup change shows up in the section it touches and not in a whole page.
	// The page chrome around them embeds the frontend bundle name and the CI git branch, commit and ref.
	private async Task SnapshotSectionBodyAsync(string bodyId)
	{
		var nav = fixture.Walk().OfType<OperationNavigationItem>().First(n => n.Model.Operation.OperationId == "search");
		var html = await fixture.RenderAsync(nav);

		html.Should().MatchHtmlSnapshot(new HtmlSnapshotOptions().Select($"#{bodyId}"));
	}

	[GeneratedRegex("<(h[1-6])\\b")]
	private static partial Regex HeadingOpenTags();
}
