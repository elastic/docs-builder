// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Microsoft.AspNetCore.Html;

namespace Elastic.Documentation.Site.Components.Flyout;

public static class FlyoutExamples
{
	public static ComponentSection Flyouts { get; } = new(
		"Flyout",
		"A side panel in a native modal dialog. The trigger needs script and stays hidden without it. The slide only runs when the reader hasn't asked for reduced motion.",
		"flyout",
		[
			ComponentExample.Of(
				"Flyout",
				_Flyout.Create,
				new FlyoutModel
				{
					Id = "example-flyout",
					Title = "Details",
					OpenLabel = "Open details",
					Body = new HtmlString("<p>The panel holds any content.</p>")
				}
			)
		]
	);
}
