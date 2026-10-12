// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Site.Components.Overlays.SearchDialog;

/// <summary>What a search surface is showing. Only <see cref="Closed" /> is the live dialog; the others are open specimens of a state.</summary>
public enum SearchDialogState
{
	Closed,
	Idle,
	Loading,
	Results,
	NoResults
}

/// <param name="Group">The section the result belongs to, such as Guides or APIs.</param>
/// <param name="Excerpt">Markup with the matched words in <c>mark</c> elements.</param>
public sealed record SearchDialogResult(string Group, string Path, string Title, string Excerpt, bool Selected = false);

public sealed record SearchDialogModel
{
	/// <summary>Prefix for ids, so a second dialog on one page, as in the style guide, never shares an id.</summary>
	public string Id { get; init; } = "search-dialog";

	public string Label { get; init; } = "Search the documentation";
	public string Placeholder { get; init; } = "Search the documentation";

	/// <summary>The shortcut that opens the dialog, shown in the footer hint and wired by the script.</summary>
	public IReadOnlyList<string> Shortcut { get; init; } = ["Ctrl", "K"];

	/// <summary>Anything but <see cref="SearchDialogState.Closed" /> draws the dialog open and inert, to show that state.</summary>
	public SearchDialogState State { get; init; } = SearchDialogState.Closed;

	public string Query { get; init; } = string.Empty;
	public IReadOnlyList<SearchDialogResult> Results { get; init; } = [];

	public bool Live => State is SearchDialogState.Closed;

	public string StatusText => State switch
	{
		SearchDialogState.Loading => "Searching…",
		SearchDialogState.Results => $"{Results.Count} {(Results.Count == 1 ? "result" : "results")}.",
		SearchDialogState.NoResults => "No results.",
		_ => "Type to search."
	};
}
