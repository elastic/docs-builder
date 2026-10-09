// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Site.Components.SearchPage;

public static class SearchPageExamples
{
	public static ComponentSection Pages { get; } = new(
		"Search page",
		"The full-page search: a query form, facets and a result list drawn by script from the search API. The AI answer panel of the previous page is not part of it yet.",
		"search-page",
		[ComponentExample.Of("Search page", _SearchPage.Create, new SearchPageModel { Id = "example-search-page" })]
	);
}
