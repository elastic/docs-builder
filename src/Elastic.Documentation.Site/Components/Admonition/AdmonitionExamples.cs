// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Microsoft.AspNetCore.Html;

namespace Elastic.Documentation.Site.Components.Admonition;

public static class AdmonitionExamples
{
	private static HtmlString Text(string text) => new($"<p>{text}</p>");

	public static ComponentSection Admonitions { get; } = new(
		"Admonition",
		"A callout for information that deserves attention. The kind sets the tone and icon; the body holds any content.",
		"admonition",
		[
			ComponentExample.Of(
				"Note",
				_Admonition.Create,
				new AdmonitionModel
				{
					Kind = AdmonitionKind.Note,
					Title = "Note",
					Body = Text("Index templates apply when an index is created.")
				}
			),
			ComponentExample.Of(
				"Tip",
				_Admonition.Create,
				new AdmonitionModel
				{
					Kind = AdmonitionKind.Tip,
					Title = "Tip",
					Body = Text("Use a data stream for append-only time series data.")
				}
			),
			ComponentExample.Of(
				"Important",
				_Admonition.Create,
				new AdmonitionModel
				{
					Kind = AdmonitionKind.Important,
					Title = "Important",
					Body = Text("Back up your cluster before you upgrade.")
				}
			),
			ComponentExample.Of(
				"Warning",
				_Admonition.Create,
				new AdmonitionModel
				{
					Kind = AdmonitionKind.Warning,
					Title = "Warning",
					Body = Text("Closing an index blocks reads and writes.")
				}
			),
			ComponentExample.Of(
				"Caution",
				_Admonition.Create,
				new AdmonitionModel { Kind = AdmonitionKind.Caution, Title = "Caution", Body = Text("Deleting an index cannot be undone.") }
			)
		]
	);
}
