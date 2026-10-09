// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Site.Components.PageCard;
using Microsoft.AspNetCore.Html;

namespace Elastic.Documentation.Site.Components.Listing;

public static class ListingExamples
{
	private static PageCardModel Card(string title, string description) =>
		new() { Title = title, Url = $"#{title.ToLowerInvariant().Replace(' ', '-')}", Description = new HtmlString(description) };

	public static ComponentSection Listings { get; } = new(
		"Listing",
		"A list of page cards that filters as you type, with group chips. The filter bar needs script, so it stays hidden until script reveals it.",
		"listing",
		[
			ComponentExample.Of(
				"Grouped",
				_Listing.Create,
				new ListingModel
				{
					Groups =
					[
						new ListingGroup("guides", "Guides", [Card("Quick start", "Index your first document.")]),
						new ListingGroup("reference", "Reference", [Card("Query DSL", "Every query type and its options.")])
					]
				}
			)
		]
	);
}
