// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Microsoft.AspNetCore.Html;

namespace Elastic.Documentation.Site.Components.CodeBlock;

public static class CodeBlockExamples
{
	// Pre-highlighted markup, as the client-side highlighter would produce it, so the style guide shows the token colours without running scripts.
	private static HtmlString Highlighted =>
		new(
			"<code class=\"language-json hljs\">{\n  <span class=\"hljs-attr\">\"query\"</span>: {\n    <span class=\"hljs-attr\">\"match\"</span>: { <span class=\"hljs-attr\">\"title\"</span>: <span class=\"hljs-string\">\"design system\"</span> }\n  },\n  <span class=\"hljs-attr\">\"size\"</span>: <span class=\"hljs-number\">10</span>,\n  <span class=\"hljs-attr\">\"track_total_hits\"</span>: <span class=\"hljs-literal\">true</span>\n}</code>"
		);

	public static ComponentSection CodeBlocks { get; } = new(
		"Code block",
		"A dark panel with the language, an optional caption and a copy button. The button is hidden until the enhancement loads, because it cannot work without JavaScript.",
		"code-block",
		[
			ComponentExample.Of("Highlighted", _CodeBlock.Create, new CodeBlockModel { Language = "json", Body = Highlighted }),
			ComponentExample.Of(
				"With caption",
				_CodeBlock.Create,
				new CodeBlockModel
				{
					Language = "sh",
					Caption = "Install the plugin",
					Body = new HtmlString("<code class=\"language-sh\">bin/elasticsearch-plugin install analysis-icu</code>")
				}
			),
			ComponentExample.Of(
				"With callouts",
				_CodeBlock.Create,
				new CodeBlockModel
				{
					Language = "yaml",
					Body = new HtmlString(
						"<code class=\"language-yaml\">cluster.name: docs<span class=\"code-callout\" data-index=\"1\"></span>\nnode.roles: [ data ]<span class=\"code-callout\" data-index=\"2\"></span></code>"
					)
				}
			)
		]
	);
}
