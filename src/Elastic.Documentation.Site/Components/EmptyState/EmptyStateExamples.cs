// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Site.Components.EmptyState;

public static class EmptyStateExamples
{
	public static ComponentSection EmptyStates { get; } = new(
		"Empty state",
		"Says what is missing and offers one next step.",
		"empty-state",
		[
			ComponentExample.Of(
				"With an action",
				_EmptyState.Create,
				new EmptyStateModel
				{
					Title = "No results",
					Description = "Nothing matches your search. Try a different term, or browse the documentation.",
					ActionLabel = "Browse the docs",
					ActionHref = "#docs"
				}
			)
		]
	);
}
