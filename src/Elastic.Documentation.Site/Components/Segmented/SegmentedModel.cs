// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Site.Components.Segmented;

public sealed record SegmentedOption(string Value, string Label, bool Selected = false, int? Count = null);

/// <summary>A single-choice control for a few short options. Radios underneath, so it works without script.</summary>
public sealed record SegmentedModel
{
	public required string Name { get; init; }
	public required string Legend { get; init; }
	public required IReadOnlyList<SegmentedOption> Options { get; init; }
}
