// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Site.Components.Check;

public sealed record CheckModel
{
	public required string Id { get; init; }
	public required string Name { get; init; }
	public required string Label { get; init; }
	public string Value { get; init; } = "on";
	public bool Checked { get; init; }

	/// <summary>Renders a radio button; radios sharing a <see cref="Name"/> form one group.</summary>
	public bool Radio { get; init; }

	public bool Disabled { get; init; }

	public string CssClass => Radio ? "check check--radio" : "check";
}
