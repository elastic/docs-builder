// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Site.Components.SearchDialog;

public static class SearchDialogExamples
{
	public static IReadOnlyList<SearchDialogResult> SampleResults { get; } =
	[
		new(
			"Guides",
			"Elasticsearch / Search your data",
			"Hybrid search",
			"Combine lexical and semantic <mark>search</mark> in a single request using retrievers.",
			Selected: true
		),
		new(
			"Guides",
			"Elasticsearch / Retrievers",
			"Reciprocal rank fusion",
			"Merge results from several retrievers into one ranked list."
		),
		new("APIs", "Elasticsearch API", "Run a search", "POST /{index}/_search")
	];

	public static ComponentSection Dialogs { get; } = new(
		"Search dialog",
		"A native dialog opened by the search field or the keyboard shortcut. Results are drawn into the list from a template, so there is one markup source. The first example is the live dialog, closed; open it from the header on a page. The others are open specimens of each state.",
		"search-dialog",
		[
			ComponentExample.Of("Closed dialog markup", _SearchDialog.Create, new SearchDialogModel { Id = "search-dialog-example" }),
			ComponentExample.Of(
				"Idle",
				_SearchDialog.Create,
				new SearchDialogModel { Id = "search-dialog-idle", State = SearchDialogState.Idle }
			),
			ComponentExample.Of(
				"Loading",
				_SearchDialog.Create,
				new SearchDialogModel { Id = "search-dialog-loading", State = SearchDialogState.Loading, Query = "hybrid sea" }
			),
			ComponentExample.Of(
				"Results",
				_SearchDialog.Create,
				new SearchDialogModel
				{
					Id = "search-dialog-results",
					State = SearchDialogState.Results,
					Query = "hybrid search",
					Results = SampleResults
				}
			),
			ComponentExample.Of(
				"No results",
				_SearchDialog.Create,
				new SearchDialogModel { Id = "search-dialog-none", State = SearchDialogState.NoResults, Query = "hybrid sarch" }
			)
		]
	);
}
