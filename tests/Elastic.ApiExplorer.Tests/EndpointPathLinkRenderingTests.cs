// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Text.RegularExpressions;
using AwesomeAssertions;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Operations;
using Elastic.ApiExplorer.Operations._Partials;
using Elastic.Documentation;
using Elastic.Documentation.Site.FileProviders;
using Nullean.Snapshots.Html;
using RazorSlices;

namespace Elastic.ApiExplorer.Tests;

[ClassDataSource<ApiExplorerFixture>(Shared = SharedType.PerClass)]
public partial class EndpointPathLinkRenderingTests(ApiExplorerFixture fixture)
{
	[Test]
	public void AnchorFor_UndocumentedParameter_ReturnsNull()
	{
		var row = new EndpointRow("/{index}/{id}", "get", [], []);
		var model = new EndpointPathModel(row, row.Route, new HashSet<string> { "index" });

		model.AnchorFor(new EndpointSegment("{index}", false)).Should().Be("path-index");
		model.AnchorFor(new EndpointSegment("{id}", false)).Should().BeNull();
		new EndpointPathModel(row, row.Route).AnchorFor(new EndpointSegment("{index}", false)).Should().BeNull();
	}

	[Test]
	public async Task Render_PathPlaceholders_LinkToDocumentedPathParameters()
	{
		var nav = fixture.Walk().OfType<OperationNavigationItem>().First(n => n.Model.Operation.OperationId == "docs-get-source");
		var html = await fixture.RenderAsync(nav);

		var listing = Slice(html, "id=\"paths\"", "</ul>");
		var targets = PlaceholderLink().Matches(listing).Select(static m => m.Groups[1].Value).ToArray();

		targets.Should().NotBeEmpty();
		foreach (var target in targets)
			html.Should().Contain($"id=\"{target}\"");
	}

	[Test]
	public async Task Render_RootRoute_ShowsSlash()
	{
		var row = new EndpointRow("/", "get", [], []);
		var html = await _EndpointPath.Create(new EndpointPathModel(row, row.Route)).RenderAsync(
			cancellationToken: TestContext.Current!.Execution.CancellationToken
		);

		html.Should().MatchInlineHtmlSnapshot(
			"""
			<span class="api-url">
				<span class="api-method api-method-get">
					<span class="api-method-icon" aria-hidden="true">
						<svg width="12" height="12" viewBox="0 0 12 12">
							<use href="#icon-api-arrow"></use>
						</svg>
					</span>
					<span class="api-method-label">GET</span>
				</span>
				<span class="api-url-path"><span class="api-route-segment">/</span></span>
			</span>
			"""
		);
	}

	[Test]
	public async Task Render_OptionalSegments_AreOneElementEach()
	{
		var row = new EndpointRow(
			"/_cluster/state/{metric}/{index}",
			"get",
			[],
			[
				new EndpointSegment("_cluster", false),
				new EndpointSegment("state", false),
				new EndpointSegment("{metric}", true),
				new EndpointSegment("{index}", true)
			]
		);
		var model = new EndpointPathModel(row, "/_cluster/state", new HashSet<string> { "metric" });
		var html = await _EndpointPath.Create(model).RenderAsync(cancellationToken: TestContext.Current!.Execution.CancellationToken);

		html.Should().MatchInlineHtmlSnapshot(
			"""
			<span class="api-url">
				<span class="api-method api-method-get">
					<span class="api-method-icon" aria-hidden="true">
						<svg width="12" height="12" viewBox="0 0 12 12">
							<use href="#icon-api-arrow"></use>
						</svg>
					</span>
					<span class="api-method-label">GET</span>
				</span>
				<span class="api-url-path"><span class="api-route-segment">/_cluster</span><span class="api-route-segment">/state</span><span class="api-route-segment">/<a class="api-url-param api-url-optional" href="#path-metric" data-tooltip="Optional. Leave it out to call /_cluster/state">{metric}</a></span><span class="api-route-segment">/<span class="api-url-optional" data-tooltip="Optional. Leave it out to call /_cluster/state">{index}</span></span></span>
			</span>
			"""
		);
	}

	[GeneratedRegex("""<a class="api-url-param(?: api-url-optional)?" href="#([^"]+)"[^>]*>/?\{[^}]+\}</a>""", RegexOptions.None, RegexTimeouts.MatchTimeoutMilliseconds)]
	private static partial Regex PlaceholderLink();

	private static string Slice(string html, string startMarker, string endMarker)
	{
		var start = html.IndexOf(startMarker, StringComparison.Ordinal);
		start.Should().BeGreaterThanOrEqualTo(0);
		var end = html.IndexOf(endMarker, start, StringComparison.Ordinal);
		end.Should().BeGreaterThan(start);
		return html[start..end];
	}
}
