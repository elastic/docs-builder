// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Site.Components.Card;

public static class CardExamples
{
	public static ComponentSection Cards { get; } = new(
		"Card",
		"One card for every feature, guide and product tile. The title link covers the whole card. On hover the card gains a slab shadow, and it lifts only when the system allows motion.",
		"card",
		[
			ComponentExample.Of(
				"Default",
				_Card.Create,
				new CardModel
				{
					Title = "Search",
					Description = "Find the right data fast with full-text, vector and hybrid search.",
					Href = "#",
					Icon = "search"
				}
			),
			ComponentExample.Of(
				"Compact",
				_Card.Create,
				new CardModel
				{
					Title = "Observability",
					Description = "Logs, metrics and traces in one place.",
					Href = "#",
					Icon = "app_apm",
					Compact = true,
					CtaLabel = "Open guide"
				}
			),
			ComponentExample.Of(
				"Without icon",
				_Card.Create,
				new CardModel { Title = "Release notes", Description = "What changed in each release.", Href = "#" }
			)
		]
	);
}
