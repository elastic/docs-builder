// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Site.Components.SearchDialog;

namespace Elastic.Documentation.Site.Components.PageJumper;

/// <summary>A search field that lists matching pages in a panel under it as you type, with no dialog. Without script it submits to the search page.</summary>
public sealed record PageJumperModel
{
	public string Id { get; init; } = "page-jumper";
	public string Label { get; init; } = "Jump to a page";
	public string Placeholder { get; init; } = "Jump to a page";
	public string Action { get; init; } = "/search";

	/// <summary>Anything but <see cref="SearchDialogState.Closed" /> draws the panel open and inert, to show that state.</summary>
	public SearchDialogState State { get; init; } = SearchDialogState.Closed;

	public string Query { get; init; } = string.Empty;
	public IReadOnlyList<SearchDialogResult> Results { get; init; } = [];

	public bool Live => State is SearchDialogState.Closed;

	public bool PanelOpen => State is not (SearchDialogState.Closed or SearchDialogState.Idle);

	public string StatusText => State switch
	{
		SearchDialogState.Loading => "Searching…",
		SearchDialogState.Results => $"{Results.Count} {(Results.Count == 1 ? "page" : "pages")}.",
		SearchDialogState.NoResults => "No pages match.",
		_ => string.Empty
	};
}
