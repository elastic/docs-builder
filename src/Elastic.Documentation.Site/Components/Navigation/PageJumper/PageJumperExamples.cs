// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Site.Components.Overlays.SearchDialog;

namespace Elastic.Documentation.Site.Components.Navigation.PageJumper;

public static class PageJumperExamples
{
	private static readonly SearchDialogResult[] Pages =
	[
		new("Guides", "Elasticsearch / Search your data", "Hybrid search", string.Empty, Selected: true),
		new("Guides", "Elasticsearch / Retrievers", "Reciprocal rank fusion", string.Empty),
		new("APIs", "Elasticsearch API", "Run a search", string.Empty)
	];

	public static ComponentSection Jumpers { get; } = new(
		"Page jumper",
		"Search as you type, with no dialog: matching pages are listed in a panel under the field. Without script it is a plain search form. The first example is the live field; the others are specimens of each state.",
		"page-jumper",
		[
			ComponentExample.Of("Field", _PageJumper.Create, new PageJumperModel { Id = "page-jumper-live" }),
			ComponentExample.Of(
				"Loading",
				_PageJumper.Create,
				new PageJumperModel { Id = "page-jumper-loading", State = SearchDialogState.Loading, Query = "hybrid sea" }
			),
			ComponentExample.Of(
				"Results",
				_PageJumper.Create,
				new PageJumperModel { Id = "page-jumper-results", State = SearchDialogState.Results, Query = "hybrid", Results = Pages }
			),
			ComponentExample.Of(
				"No results",
				_PageJumper.Create,
				new PageJumperModel { Id = "page-jumper-none", State = SearchDialogState.NoResults, Query = "hybrid sarch" }
			)
		]
	);
}
