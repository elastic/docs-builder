// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer._Partials.Layout;
using Elastic.ApiExplorer.Infrastructure;
using Nullean.Snapshots;
using Nullean.Snapshots.Html;
using RazorSlices;

namespace Elastic.ApiExplorer.Tests;

public class ApiBetaBannerTests
{
	[Test]
	public void For_WithCanonicalBaseUrl_FillsInThePageLink() =>
		ApiBetaBanner
			.For(new Uri("https://www.elastic.co"), "/docs/api/doc/elasticsearch/operation/operation-search")
			.FeedbackUrl
			.Should()
			.MatchInlineSnapshot(
				"""https://github.com/elastic/docs-content/issues/new?template=issue-report.yaml&labels=source:web&link=https%3A%2F%2Fwww.elastic.co%2Fdocs%2Fapi%2Fdoc%2Felasticsearch%2Foperation%2Foperation-search"""
			);

	[Test]
	public void For_WithoutCanonicalBaseUrl_LeavesTheLinkOut() =>
		ApiBetaBanner
			.For(null, "/api/doc/elasticsearch")
			.FeedbackUrl
			.Should()
			.MatchInlineSnapshot("""https://github.com/elastic/docs-content/issues/new?template=issue-report.yaml&labels=source:web""");

	[Test]
	public async Task Render_LabelsTheNoticeAndLinksToFeedback()
	{
		var html = await _ApiBetaBanner.Create(
			ApiBetaBanner.For(new Uri("https://www.elastic.co"), "/docs/api/doc/elasticsearch")
		).RenderAsync(cancellationToken: TestContext.Current!.Execution.CancellationToken);

		html.Should().MatchHtmlSnapshot();
	}
}
