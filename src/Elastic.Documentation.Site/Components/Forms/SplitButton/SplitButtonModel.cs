// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Site.Components.Navigation.LinkMenu;

namespace Elastic.Documentation.Site.Components.Forms.SplitButton;

/// <summary>A primary action with a menu of related actions beside it. The menu is a native disclosure, so it works without script.</summary>
public sealed record SplitButtonModel
{
	public required string Label { get; init; }
	public required string Href { get; init; }
	public required string MenuLabel { get; init; }
	public required IReadOnlyList<LinkMenuItem> Items { get; init; }
}
