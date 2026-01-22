// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Changelog;

namespace Elastic.Changelog.Configuration;

/// <summary>
/// Configuration for changelog generation
/// </summary>
public record ChangelogConfiguration
{
	/// <summary>
	/// Default types for changelog entries (derived from ChangelogEntryType enum)
	/// </summary>
	public static IReadOnlyList<string> DefaultTypes { get; } =
		ChangelogEntryTypeExtensions.GetValues()
			.Select(t => t.ToStringFast(true))
			.ToList();

	/// <summary>
	/// Default subtypes for breaking changes (derived from ChangelogEntrySubtype enum)
	/// </summary>
	public static IReadOnlyList<string> DefaultSubtypes { get; } =
		ChangelogEntrySubtypeExtensions.GetValues()
			.Select(s => s.ToStringFast(true))
			.ToList();

	/// <summary>
	/// Required types that must be present in the configuration.
	/// At minimum, 'feature', 'bug-fix', and 'breaking-change' must be configured.
	/// </summary>
	public static IReadOnlyList<ChangelogEntryType> RequiredTypes { get; } =
	[
		ChangelogEntryType.Feature,
		ChangelogEntryType.BugFix,
		ChangelogEntryType.BreakingChange
	];

	/// <summary>
	/// Default lifecycle values
	/// </summary>
	public static IReadOnlyList<string> DefaultLifecycles { get; } =
	[
		"preview", // A technical preview of a feature or enhancement.
		"beta", // A beta release of a feature or enhancement.
		"ga", // A generally available release of a feature or enhancement.
	];

	/// <summary>
	/// Pivot configuration for types, subtypes, and areas with label mappings
	/// </summary>
	public PivotConfiguration? Pivot { get; set; }

	/// <summary>
	/// Available types for changelog entries (computed from Pivot.Types or defaults)
	/// </summary>
	public IReadOnlyList<string> AvailableTypes { get; set; } = DefaultTypes;

	/// <summary>
	/// Available subtypes for breaking changes (computed from Pivot.Subtypes or defaults)
	/// </summary>
	public IReadOnlyList<string> AvailableSubtypes { get; set; } = DefaultSubtypes;

	/// <summary>
	/// Available lifecycle values (from config or defaults)
	/// </summary>
	public IReadOnlyList<string> AvailableLifecycles { get; set; } = DefaultLifecycles;

	/// <summary>
	/// Available areas (computed from Pivot.Areas keys)
	/// </summary>
	public IReadOnlyList<string>? AvailableAreas { get; set; }

	/// <summary>
	/// Available products (from config)
	/// </summary>
	public IReadOnlyList<string>? AvailableProducts { get; set; }

	/// <summary>
	/// Mapping from GitHub label names to changelog type values (computed from Pivot.Types)
	/// </summary>
	public IReadOnlyDictionary<string, string>? LabelToType { get; set; }

	/// <summary>
	/// Mapping from GitHub label names to changelog area values (computed from Pivot.Areas)
	/// Multiple labels can map to the same area, and a single label can map to multiple areas (comma-separated)
	/// </summary>
	public IReadOnlyDictionary<string, string>? LabelToAreas { get; set; }

	/// <summary>
	/// Product-specific label blocking configuration
	/// Maps product IDs to lists of labels that should prevent changelog creation for that product
	/// Keys can be comma-separated product IDs to share the same list of labels across multiple products
	/// </summary>
	public IReadOnlyDictionary<string, IReadOnlyList<string>>? AddBlockers { get; set; }

	/// <summary>
	/// Configuration for blocking changelogs from being rendered (commented out in markdown output)
	/// Dictionary key can be a single product ID or comma-separated product IDs (e.g., "elasticsearch, cloud-serverless")
	/// Dictionary value contains areas and/or types that should be blocked for those products
	/// Changelogs matching any product key and any area/type in the corresponding entry will be commented out
	/// </summary>
	public IReadOnlyDictionary<string, RenderBlockersEntry>? RenderBlockers { get; set; }

	private static readonly Lazy<ChangelogConfiguration> DefaultLazy = new(() => new ChangelogConfiguration());

	public static ChangelogConfiguration Default => DefaultLazy.Value;
}

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
	public Dictionary<string, TypeEntry?>? Types { get; set; }

	/// <summary>
	/// Default subtype definitions with optional labels
	/// Used when a type doesn't define its own subtypes
	/// Keys are subtype names (e.g., "api", "behavioral")
	/// Values can be: null/empty (no labels) or string (labels)
	/// </summary>
	public Dictionary<string, string?>? Subtypes { get; set; }

	/// <summary>
	/// Area definitions with labels
	/// Keys are area display names (e.g., "Autoscaling", "Search")
	/// Values are label strings (e.g., ":Distributed/Auto")
	/// </summary>
	public Dictionary<string, string?>? Areas { get; set; }
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
	public string? Labels { get; set; }

	/// <summary>
	/// Type-specific subtype definitions (overrides pivot.subtypes for this type)
	/// Keys are subtype names, values are label strings
	/// </summary>
	public Dictionary<string, string?>? Subtypes { get; set; }

	/// <summary>
	/// Creates a TypeEntry from a simple label string
	/// </summary>
	public static TypeEntry FromLabels(string? labels) => new() { Labels = labels };
}

/// <summary>
/// Configuration entry for blocking changelogs during render
/// </summary>
public record RenderBlockersEntry
{
	/// <summary>
	/// List of area values that should be blocked (commented out) during render
	/// </summary>
	public IReadOnlyList<string>? Areas { get; set; }

	/// <summary>
	/// List of type values that should be blocked (commented out) during render
	/// Types must exist in the available_types list (or default AvailableTypes if not specified)
	/// </summary>
	public IReadOnlyList<string>? Types { get; set; }
}
