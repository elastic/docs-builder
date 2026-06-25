// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Serialization;

namespace Elastic.Documentation.Search;

/// <summary>
/// Flat wire-format entry for the <c>applies_to</c> nested array on <see cref="DocumentationDocument"/>.
/// One entry per (type, sub_type, lifecycle, version) tuple.
/// </summary>
public record AppliesToEntry
{
	[JsonPropertyName("type")]
	public required string Type { get; set; }

	[JsonPropertyName("sub_type")]
	public required string SubType { get; set; }

	[JsonPropertyName("lifecycle")]
	public required string Lifecycle { get; set; }

	[JsonPropertyName("version")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Version { get; set; }
}
