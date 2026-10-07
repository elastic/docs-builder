// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Generator.Equals;

namespace Elastic.Documentation.ReleaseNotes;

/// <summary>
/// Pivot configuration containing types, subtypes, and areas with label mappings
/// </summary>
/// <remarks>
/// Constructed through <c>ChangelogConfiguration</c>.
/// Optional members are <c>internal init</c>: only tests (<c>InternalsVisibleTo</c>) can set them.
/// </remarks>
[Equatable]
public partial record PivotConfiguration
{
	/// <summary>
	/// Type definitions with optional labels and type-specific subtypes
	/// Keys are type names (e.g., "bug-fix", "breaking-change")
	/// Values can be: null/empty (no labels), string (labels), or TypeEntry object
	/// </summary>
	[UnorderedEquality]
	public Dictionary<string, TypeEntry?>? Types { get; internal init; }

	/// <summary>
	/// Default subtype definitions with optional labels
	/// Used when a type doesn't define its own subtypes
	/// Keys are subtype names (e.g., "api", "behavioral")
	/// Values can be: null/empty (no labels) or string (labels)
	/// </summary>
	[UnorderedEquality]
	public Dictionary<string, string?>? Subtypes { get; internal init; }

	/// <summary>
	/// Area definitions with labels
	/// Keys are area display names (e.g., "Autoscaling", "Search")
	/// Values are label strings (e.g., ":Distributed/Auto")
	/// </summary>
	[UnorderedEquality]
	public Dictionary<string, string?>? Areas { get; internal init; }

	/// <summary>
	/// Product definitions with labels mapped to product spec strings.
	/// Keys are product spec strings (e.g., "elasticsearch", "kibana 9.2.0", "cloud-serverless 2025-06 ga").
	/// Values are label strings (e.g., ":stack/elasticsearch").
	/// When a PR has labels matching a product's value, that product is added to the changelog entry.
	/// Multiple matching product entries are all applied (same behavior as areas).
	/// </summary>
	[UnorderedEquality]
	public Dictionary<string, string?>? Products { get; internal init; }

	/// <summary>
	/// Feature ID definitions with labels mapped to feature-id values.
	/// Keys are feature-id strings (e.g., "feature:new-search-api").
	/// Values are label strings (e.g., "feature-flag:new-search-api").
	/// When a PR has labels matching a feature entry, that feature-id is set on the changelog entry.
	/// </summary>
	[UnorderedEquality]
	public Dictionary<string, string?>? Features { get; internal init; }

	/// <summary>
	/// Labels that trigger the highlight flag (comma-separated string).
	/// When a PR has any of these labels, highlight is set to true.
	/// Example: ">highlight, >release-highlight"
	/// </summary>
	public string? Highlight { get; internal init; }
}

/// <summary>
/// Configuration entry for a type in the pivot configuration.
/// Can represent either a simple label string or a complex object with labels and subtypes.
/// </summary>
/// <remarks>
/// Constructed through <c>PivotConfiguration</c>.
/// Optional members are <c>internal init</c>: only tests (<c>InternalsVisibleTo</c>) can set them.
/// </remarks>
[Equatable]
public partial record TypeEntry
{
	/// <summary>
	/// Labels for this type (comma-separated string)
	/// </summary>
	public string? Labels { get; internal init; }

	/// <summary>
	/// Type-specific subtype definitions (overrides pivot.subtypes for this type)
	/// Keys are subtype names, values are label strings
	/// </summary>
	[UnorderedEquality]
	public Dictionary<string, string?>? Subtypes { get; internal init; }

	/// <summary>
	/// Creates a TypeEntry from a simple label string
	/// </summary>
	public static TypeEntry FromLabels(string? labels) => new() { Labels = labels };
}
