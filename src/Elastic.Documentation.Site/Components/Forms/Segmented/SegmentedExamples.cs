// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Site.Components.Forms.Segmented;

public static class SegmentedExamples
{
	public static ComponentSection Segmenteds { get; } = new(
		"Segmented control",
		"A single choice among a few short options, such as a view mode.",
		"segmented",
		[
			ComponentExample.Of(
				"Default",
				_Segmented.Create,
				new SegmentedModel
				{
					Name = "ex-view",
					Legend = "View",
					Options = [new("list", "List", Selected: true), new("grid", "Grid"), new("table", "Table")]
				}
			)
		]
	);
}
