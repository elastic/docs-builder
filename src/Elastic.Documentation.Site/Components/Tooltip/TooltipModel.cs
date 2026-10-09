// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Site.Components.Tooltip;

/// <summary>A short explanation shown on hover and keyboard focus, wired with aria-describedby. It needs no script.</summary>
public sealed record TooltipModel
{
	public required string Id { get; init; }
	public required string Label { get; init; }
	public required string Text { get; init; }
}
