// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Site.Components.Primitives.Kbd;

namespace Elastic.Documentation.Site.Components.Forms.SearchField;

public static class SearchFieldExamples
{
	public static ComponentSection Searches { get; } = new(
		"Search field",
		"A search form that works with a plain GET. Enhancements can intercept it to open the search palette.",
		"search-field",
		[
			ComponentExample.Of(
				"Default",
				_SearchField.Create,
				new SearchFieldModel { Id = "ex-search", Label = "Search the docs", Placeholder = "Search the docs" }
			),
			ComponentExample.Of(
				"With shortcut",
				_SearchField.Create,
				new SearchFieldModel { Id = "ex-search2", Label = "Search", Shortcut = ["Ctrl", "K"] }
			),
			ComponentExample.Of(
				"Opens the dialog",
				_SearchField.Create,
				new SearchFieldModel { Id = "ex-search3", Label = "Search", Trigger = true, Shortcut = ["Ctrl", "K"] }
			)
		]
	);
}
