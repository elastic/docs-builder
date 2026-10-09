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
			),
			ComponentExample.Of(
				"Nothing yet",
				_EmptyState.Create,
				new EmptyStateModel
				{
					Eyebrow = "Nothing yet",
					Title = "No saved searches",
					Description = "Searches you save will show up here.",
					Boxed = true
				}
			),
			ComponentExample.Of(
				"Error",
				_EmptyState.Create,
				new EmptyStateModel
				{
					Eyebrow = "Error",
					Title = "We couldn't load this",
					Description = "Something went wrong on our side. Try again in a moment.",
					Icon = "warningFilled",
					Boxed = true,
					ActionLabel = "Try again",
					ActionHref = "#retry"
				}
			)
		]
	);
}
