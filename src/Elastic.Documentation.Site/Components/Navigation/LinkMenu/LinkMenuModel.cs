// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Site.Components.Navigation.LinkMenu;

public sealed record LinkMenuItem(string Title, string Url, bool Active = false, bool External = false, bool Download = false);

public sealed record LinkMenuModel
{
	/// <summary>Names the menu for assistive technology, for example "Version" or "Section".</summary>
	public required string Label { get; init; }

	/// <summary>The text on the trigger; defaults to the active item.</summary>
	public string? Current { get; init; }

	public required IReadOnlyList<LinkMenuItem> Items { get; init; }

	/// <summary>A final link below the list, such as "All versions".</summary>
	public LinkMenuItem? Footer { get; init; }

	public string TriggerText => Current ?? Items.FirstOrDefault(i => i.Active)?.Title ?? Label;
}
