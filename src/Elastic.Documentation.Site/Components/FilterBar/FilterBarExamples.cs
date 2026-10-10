// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Site.Components.SearchField;
using Elastic.Documentation.Site.Components.Segmented;

namespace Elastic.Documentation.Site.Components.FilterBar;

public static class FilterBarExamples
{
	public static ComponentSection FilterBars { get; } = new(
		"Filter bar",
		"Views of the same list on the left, never navigation, and a text filter on the right.",
		"filter-bar",
		[
			ComponentExample.Of(
				"With counts",
				_FilterBar.Create,
				new FilterBarModel
				{
					Views = new SegmentedModel
					{
						Name = "ex-filter-view",
						Legend = "Show",
						Options =
						[
							new("all", "All", Selected: true, Count: 24),
							new("cloud", "Cloud", Count: 4),
							new("serverless", "Serverless", Count: 9)
						]
					},
					Filter = new SearchFieldModel
					{
						Id = "ex-filter-q",
						Label = "Filter APIs",
						Placeholder = "Filter APIs",
						Action = "#",
						Shortcut = ["/"]
					},
					Summary = "4 of 24"
				}
			)
		]
	);
}
