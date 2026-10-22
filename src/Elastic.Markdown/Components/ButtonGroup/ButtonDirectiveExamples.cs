// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Site.Components;
using Microsoft.AspNetCore.Html;

namespace Elastic.Markdown.Components.ButtonGroup;

public static class ButtonDirectiveExamples
{
	public static ComponentSection Buttons { get; } = new(
		"Button directive",
		"The button directive wraps the link Markdown produces. The link is styled with the same tokens as the button component, so authors get a consistent call to action without raw HTML.",
		"button-directive",
		[
			ComponentExample.Of(
				"Primary",
				_ButtonDirective.Create,
				new ButtonDirectiveModel { Link = new HtmlString("<a href=\"#\">Get started</a>") }
			),
			ComponentExample.Of(
				"Secondary",
				_ButtonDirective.Create,
				new ButtonDirectiveModel
				{
					Link = new HtmlString("<a href=\"#\">View the docs</a>"),
					Style = ButtonDirectiveStyle.Secondary
				}
			)
		]
	);
}
