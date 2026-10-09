// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer.Components.PropertyTree;
using Elastic.ApiExplorer.Components.PropertyTree._Partials;
using RazorSlices;
using static Elastic.ApiExplorer.Tests.TestSpecs;

namespace Elastic.ApiExplorer.Tests;

/// <summary>How a property row lays out its reference link.</summary>
public class PropertyItemRenderingTests
{
	[Test]
	public async Task Render_ReferenceLink_SitsUnderTheNameBeforeTheDescription()
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
			            "type": "string",
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

		html
			.IndexOf("Read the reference documentation", StringComparison.Ordinal)
			.Should()
			.BeLessThan(
				html.IndexOf("How the input is chunked.", StringComparison.Ordinal),
				"the link is found without reading the description"
			);
		html.Should().Contain(
			"""href="https://docs.oracle.com/javase/8/docs/api/java/util/regex/Matcher.html" class="docs-reference-btn" target="_blank" rel="noopener">"""
		);
		html.Should().Contain(
			"""href="https://www.elastic.co/docs/explore-analyze/elastic-inference/inference-api" class="docs-reference-btn">""",
			"an elastic.co link stays in the tab"
		);
		html.Should().NotContain("&quot;_blank&quot;");
	}
}
