// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer._Partials.Layout;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.Documentation;
using Elastic.Documentation.Configuration.Builder;
using Nullean.Snapshots;
using Nullean.Snapshots.Html;
using RazorSlices;

namespace Elastic.ApiExplorer.Tests;

public class ApiBetaBannerTests
{
	[Test]
	public void For_WithCanonicalBaseUrl_FillsInThePageLink() =>
		ApiBetaBanner
			.For(new Uri("https://www.elastic.co"), "/docs/api/elasticsearch/operation/operation-search")
			.FeedbackUrl
			.Should()
			.MatchInlineSnapshot(
				"""https://github.com/elastic/docs-content/issues/new?template=issue-report.yaml&labels=source:web&link=https%3A%2F%2Fwww.elastic.co%2Fdocs%2Fapi%2Felasticsearch%2Foperation%2Foperation-search"""
			);

	[Test]
	public void For_WithoutCanonicalBaseUrl_LeavesTheLinkOut() =>
		ApiBetaBanner
			.For(null, "/api/elasticsearch")
			.FeedbackUrl
			.Should()
			.MatchInlineSnapshot("""https://github.com/elastic/docs-content/issues/new?template=issue-report.yaml&labels=source:web""");

	[Test]
	public async Task Render_FeedbackOff_LinksToTheIssueForm() =>
		(await RenderAsync(buildType: BuildType.Assembler)).Should().MatchHtmlSnapshot();

	[Test]
	public async Task Render_AirGapped_LinksToTheIssueForm() =>
		(await RenderAsync(["page-feedback", "air-gapped"], BuildType.Assembler)).Should().MatchHtmlSnapshot();

	[Test]
	public async Task Render_FeedbackOn_OpensThePageFeedbackInADialog() =>
		(await RenderAsync(["page-feedback"], BuildType.Assembler)).Should().MatchHtmlSnapshot();

	[Test]
	public async Task Render_FeedbackOnWithTheDesignSystem_OpensTheDesignSystemWidgetInADialog() =>
		(await RenderAsync(["page-feedback", "navigation-preview"], BuildType.Assembler)).Should().MatchHtmlSnapshot();

	private static async Task<string> RenderAsync(string[]? enabledFlags = null, BuildType buildType = BuildType.Isolated)
	{
		var features = new FeatureFlags((enabledFlags ?? []).ToDictionary(static f => f, static _ => true));
		var model = ApiPagesNavRenderingTests.CreateLayoutModel(
			"/docs/api/elasticsearch",
			"/docs/api/elasticsearch.md",
			null,
			features,
			buildType
		) with
		{ PageFeedbackSurface = "api" };
		return await _ApiBetaBanner.Create(model).RenderAsync(cancellationToken: TestContext.Current!.Execution.CancellationToken);
	}
}
