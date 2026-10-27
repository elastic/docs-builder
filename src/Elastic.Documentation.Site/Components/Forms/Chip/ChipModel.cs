// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Site.Components.Forms.Chip;

public sealed record ChipModel
{
	public required string Id { get; init; }
	public required string Name { get; init; }
	public required string Label { get; init; }
	public string Value { get; init; } = "on";
	public bool Selected { get; init; }
}
