// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Site.Components.Primitives.Tooltip;

/// <summary>A word with a short explanation, shown by the shared tooltip (Assets/tooltip.ts) on hover, keyboard focus and a touch hold.</summary>
public sealed record TooltipModel
{
	public required string Label { get; init; }
	public required string Text { get; init; }

	/// <summary>Draws the tooltip open above the trigger, for the style guide.</summary>
	public bool Open { get; init; }
}
