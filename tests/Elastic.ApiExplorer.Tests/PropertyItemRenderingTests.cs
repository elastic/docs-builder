// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer.Components.PropertyTree;
using Elastic.ApiExplorer.Components.PropertyTree._Partials;
using Nullean.Snapshots.Html;
using RazorSlices;
using static Elastic.ApiExplorer.Tests.TestSpecs;

namespace Elastic.ApiExplorer.Tests;

/// <summary>How a property row lays out its reference link.</summary>
public class PropertyItemRenderingTests
{
	[Test]
	public async Task Render_ReferenceLink_SitsAfterTheDescriptionAboveTheToggle()
	{
		var json =
			"""
			{
			  "openapi": "3.0.3",
			  "info": { "title": "t", "version": "1" },
			  "paths": {},
			  "components": {
			    "schemas": {
			      "Holder": {
			        "type": "object",
			        "properties": {
			          "chunking": {
			            "type": "object",
			            "properties": { "strategy": { "type": "string" }, "max_chunk_size": { "type": "integer" } },
			            "description": "How the input is chunked.",
			            "externalDocs": { "url": "https://www.elastic.co/docs/explore-analyze/elastic-inference/inference-api" }
			          },
			          "pattern": {
			            "type": "string",
			            "description": "A replacement pattern.",
			            "externalDocs": { "url": "https://docs.oracle.com/javase/8/docs/api/java/util/regex/Matcher.html" }
			          }
			        }
			      }
			    }
			  }
			}
			""";
		var document = await LoadSpecAsync(json);
		var list = BuilderFor(document).BuildPropertyList(document.Components!.Schemas!["Holder"], new PropertyTreeScope { Prefix = "" })!;

		var html = await _PropertyList.Create(list).RenderAsync(cancellationToken: TestContext.Current!.Execution.CancellationToken);

		// The elastic.co link stays in the tab, the external one opens a new one.
		html.Should().MatchHtmlSnapshot();
		var row = html[html.IndexOf("<dt id=\"chunking\">", StringComparison.Ordinal)..];

		int At(string marker) => row.IndexOf(marker, StringComparison.Ordinal);

		At("How the input is chunked.").Should().BeLessThan(At("reference-link-row"), "the link follows the description");
		At("reference-link-row").Should().BeLessThan(At("expand-toggle-row"), "and sits right above show properties");
		row[..At("</dt>")].Should().NotContain(">Reference</a>", "the name line stays short");
	}

	[Test]
	public async Task Render_LongObjectDefault_ShowsADefaultRowWithTheFullValue()
	{
		var json = WithSchemas(
			"""
			{ "Holder": { "type": "object", "properties": {
			    "options": { "type": "object", "default": { "auto_apply_filters": true, "use_margins": true, "sync_cursor": true } } } } }
			"""
		);
		var document = await LoadSpecAsync(json);
		var list = BuilderFor(document).BuildPropertyList(document.Components!.Schemas!["Holder"], new PropertyTreeScope { Prefix = "" })!;

		var html = await _PropertyList.Create(list).RenderAsync(cancellationToken: TestContext.Current!.Execution.CancellationToken);
		html = string.Join(' ', html.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

		html.Should().Contain("""<dd class="example-value"> <span class="values-label">Default:</span> <code>""");
		html.Should().Contain("&quot;sync_cursor&quot;:true}</code>", "the whole value is listed, not cut");
		html.Should().NotContain("type-constraint", "no chip carries it");
	}
}
