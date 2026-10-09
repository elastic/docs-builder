// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Site.Components.SearchField;

public sealed record SearchFieldModel
{
	public required string Id { get; init; }
	public required string Label { get; init; }
	public string Placeholder { get; init; } = "Search";
	public string Name { get; init; } = "q";

	/// <summary>Where the form submits. Search works without JavaScript through a plain GET.</summary>
	public string Action { get; init; } = "/search";

	/// <summary>Renders a button that opens the search dialog instead of a form. It stays hidden until the script can open the dialog.</summary>
	public bool Trigger { get; init; }

	/// <summary>The shortcut hint shown at the end of the field, such as the keys that open search.</summary>
	public IReadOnlyList<string>? Shortcut { get; init; }
}
