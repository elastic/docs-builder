// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Operations;
using Elastic.ApiExplorer.Operations._Partials;
using Nullean.Snapshots.Html;
using RazorSlices;

namespace Elastic.ApiExplorer.Tests;

public class ParamSectionHeaderRenderingTests
{
	[Test]
	public async Task Render_UnionRequestBody_ListsVariantsAsAlternatives()
	{
		var html = await Render(new ParamSectionHeader("Request", "request-body", ["EqlRule", "QueryRule"], SummaryStyle.Variants));

		html.Should().MatchInlineHtmlSnapshot(
			"""
			<h2 class="section-header api-param-section-header" id="request-body" data-section="request-body" aria-label="Request">
				<a class="headerlink api-param-section-title" href="#request-body">Request</a>
				<button type="button" class="api-param-section-toggle" aria-expanded="true" aria-controls="request-body-list" aria-label="Toggle Request">
					<span class="api-param-summary is-variants" data-param-summary="" data-param-more-noun="variant">
						<span class="api-param-summary-list">
							<code class="api-param-summary-item" data-name="EqlRule"></code>
							<code class="api-param-summary-item" data-name="QueryRule"></code>
						</span>
						<span class="api-param-more" data-param-more="" hidden="">
							<span class="api-param-more-badge" data-param-more-count=""></span>
						</span>
					</span>
					<span class="api-param-chevron" aria-hidden="true"></span>
				</button>
			</h2>
			"""
		);
	}

	[Test]
	public async Task Render_JsonFields_AreBracedAsAnObject()
	{
		var html = await Render(new ParamSectionHeader("Request", "request-body", ["name", "query"], SummaryStyle.Fields));

		html.Should().MatchInlineHtmlSnapshot(
			"""
			<h2 class="section-header api-param-section-header" id="request-body" data-section="request-body" aria-label="Request">
				<a class="headerlink api-param-section-title" href="#request-body">Request</a>
				<button type="button" class="api-param-section-toggle" aria-expanded="true" aria-controls="request-body-list" aria-label="Toggle Request">
					<span class="api-param-summary" data-param-summary="">
						<span class="api-param-summary-brace">{
						</span>
						<span class="api-param-summary-list">
							<code class="api-param-summary-item" data-name="name"></code>
							<code class="api-param-summary-item" data-name="query"></code>
						</span>
						<span class="api-param-more" data-param-more="" hidden="">
							<span class="api-param-more-badge" data-param-more-count=""></span>
						</span>
						<span class="api-param-summary-brace">
							}</span>
					</span>
					<span class="api-param-chevron" aria-hidden="true"></span>
				</button>
			</h2>
			"""
		);
	}

	[Test]
	public async Task Render_ParametersAndOtherNames_ArePlainLists()
	{
		var html = await Render(new ParamSectionHeader("Query parameters", "query-params", ["pretty", "human"], SummaryStyle.List));

		html.Should().MatchInlineHtmlSnapshot(
			"""
			<h2 class="section-header api-param-section-header" id="query-params" data-section="query-params" aria-label="Query parameters">
				<a class="headerlink api-param-section-title" href="#query-params">Query parameters</a>
				<button type="button" class="api-param-section-toggle" aria-expanded="true" aria-controls="query-params-list" aria-label="Toggle Query parameters">
					<span class="api-param-summary" data-param-summary="">
						<span class="api-param-summary-list">
							<code class="api-param-summary-item" data-name="pretty"></code>
							<code class="api-param-summary-item" data-name="human"></code>
						</span>
						<span class="api-param-more" data-param-more="" hidden="">
							<span class="api-param-more-badge" data-param-more-count=""></span>
						</span>
					</span>
					<span class="api-param-chevron" aria-hidden="true"></span>
				</button>
			</h2>
			"""
		);
	}

	[Test]
	public async Task Render_SummaryNames_AreGeneratedContentSoFindInPageSkipsThem()
	{
		var html = await Render(new ParamSectionHeader("Query parameters", "query-params", ["pretty"], SummaryStyle.List));

		// The name is an attribute, not text in the page, so Ctrl+F finds the field in the section body instead.
		html.Should().MatchInlineHtmlSnapshot(
			"""
			<h2 class="section-header api-param-section-header" id="query-params" data-section="query-params" aria-label="Query parameters">
				<a class="headerlink api-param-section-title" href="#query-params">Query parameters</a>
				<button type="button" class="api-param-section-toggle" aria-expanded="true" aria-controls="query-params-list" aria-label="Toggle Query parameters">
					<span class="api-param-summary" data-param-summary="">
						<span class="api-param-summary-list">
							<code class="api-param-summary-item" data-name="pretty"></code>
						</span>
						<span class="api-param-more" data-param-more="" hidden="">
							<span class="api-param-more-badge" data-param-more-count=""></span>
						</span>
					</span>
					<span class="api-param-chevron" aria-hidden="true"></span>
				</button>
			</h2>
			"""
		);
	}

	[Test]
	public async Task Render_FieldsWithNoNames_HaveNothingToBrace()
	{
		var html = await Render(new ParamSectionHeader("Request body", "request-body", [], SummaryStyle.Fields));

		html.Should().MatchInlineHtmlSnapshot(
			"""
			<h2 class="section-header api-param-section-header" id="request-body" data-section="request-body" aria-label="Request body">
				<a class="headerlink api-param-section-title" href="#request-body">Request body</a>
				<button type="button" class="api-param-section-toggle" aria-expanded="true" aria-controls="request-body-list" aria-label="Toggle Request body">
					<span class="api-param-summary" data-param-summary="">
						<span class="api-param-summary-list">
						</span>
						<span class="api-param-more" data-param-more="" hidden="">
							<span class="api-param-more-badge" data-param-more-count=""></span>
						</span>
					</span>
					<span class="api-param-chevron" aria-hidden="true"></span>
				</button>
			</h2>
			"""
		);
	}

	private static async Task<string> Render(ParamSectionHeader model) =>
		await _ParamSectionHeader.Create(model).RenderAsync(cancellationToken: TestContext.Current!.Execution.CancellationToken);
}
