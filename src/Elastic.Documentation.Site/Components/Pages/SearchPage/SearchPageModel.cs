// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Site.Components.Pages.SearchPage;

/// <summary>The full-page search. The form, filters and results are all drawn by script from the search API.</summary>
public sealed record SearchPageModel
{
	/// <summary>Prefix for element ids, so two search pages never share an id.</summary>
	public string Id { get; init; } = "search-page";
}
