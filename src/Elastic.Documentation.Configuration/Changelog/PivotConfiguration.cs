// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Configuration.Changelog;

/// <summary>
/// Pivot configuration containing types, subtypes, and areas with label mappings
/// </summary>
public record PivotConfiguration
{
	/// <summary>
	/// Type definitions with optional labels and type-specific subtypes
	/// Keys are type names (e.g., "bug-fix", "breaking-change")
	/// Values can be: null/empty (no labels), string (labels), or TypeEntry object
	/// </summary>
	public Dictionary<string, TypeEntry?>? Types { get; init; }

	/// <summary>
	/// Default subtype definitions with optional labels
	/// Used when a type doesn't define its own subtypes
	/// Keys are subtype names (e.g., "api", "behavioral")
	/// Values can be: null/empty (no labels) or string (labels)
	/// </summary>
	public Dictionary<string, string?>? Subtypes { get; init; }

	/// <summary>
	/// Area definitions with labels
	/// Keys are area display names (e.g., "Autoscaling", "Search")
	/// Values are label strings (e.g., ":Distributed/Auto")
	/// </summary>
	public Dictionary<string, string?>? Areas { get; init; }

	/// <summary>
	/// Labels that trigger the highlight flag (comma-separated string).
	/// When a PR has any of these labels, highlight is set to true.
	/// Example: ">highlight, >release-highlight"
	/// </summary>
	public string? Highlight { get; init; }
}

/// <summary>
/// Configuration entry for a type in the pivot configuration.
/// Can represent either a simple label string or a complex object with labels and subtypes.
/// </summary>
public record TypeEntry
{
	/// <summary>
	/// Labels for this type (comma-separated string)
	/// </summary>
	public string? Labels { get; init; }

	/// <summary>
	/// Type-specific subtype definitions (overrides pivot.subtypes for this type)
	/// Keys are subtype names, values are label strings
	/// </summary>
	public Dictionary<string, string?>? Subtypes { get; init; }

	/// <summary>
	/// Creates a TypeEntry from a simple label string
	/// </summary>
	public static TypeEntry FromLabels(string? labels) => new() { Labels = labels };
}
