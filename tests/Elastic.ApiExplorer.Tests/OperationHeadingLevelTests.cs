// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.RegularExpressions;
using AngleSharp.Html.Parser;
using AwesomeAssertions;
using Elastic.ApiExplorer.Operations;

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

		// Snapshot the operation content only. The page chrome around it embeds the frontend bundle name and the
		// CI git branch, commit and ref, which differ on every build.
		var document = await new HtmlParser().ParseDocumentAsync(html);
		var content = document.QuerySelector("main#content-container")!.OuterHtml;
		await Verify(content, "html").IgnoreParametersForVerified();
	}

	[GeneratedRegex("<(h[1-6])\\b")]
	private static partial Regex HeadingOpenTags();
}
