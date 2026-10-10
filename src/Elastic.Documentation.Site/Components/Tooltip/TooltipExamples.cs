// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Site.Components.Tooltip;

public static class TooltipExamples
{
	public static ComponentSection Tooltips { get; } = new(
		"Tooltip",
		"Appears on hover and on keyboard focus, and is tied to its trigger with aria-describedby, so assistive technology reads it.",
		"tooltip",
		[
			ComponentExample.Of(
				"Tooltip",
				_Tooltip.Create,
				new TooltipModel
				{
					Id = "example-tooltip",
					Label = "Quantization",
					Text = "Compresses vectors to reduce memory, at some cost to accuracy."
				}
			),
			ComponentExample.Of(
				"Shown",
				_Tooltip.Create,
				new TooltipModel
				{
					Id = "example-tooltip-open",
					Label = "Quantization",
					Text = "Compresses vectors to reduce memory, at some cost to accuracy.",
					Open = true
				}
			)
		]
	);
}
