// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Site.Components.SearchDialog;

public sealed record SearchDialogModel
{
	/// <summary>Prefix for ids, so a second dialog on one page, as in the style guide, never shares an id.</summary>
	public string Id { get; init; } = "search-dialog";

	public string Label { get; init; } = "Search the documentation";
	public string Placeholder { get; init; } = "Search the documentation";

	/// <summary>The shortcut that opens the dialog, shown in the footer hint and wired by the script.</summary>
	public IReadOnlyList<string> Shortcut { get; init; } = ["Ctrl", "K"];
}
