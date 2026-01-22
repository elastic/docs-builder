// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation;
using Elastic.Documentation.Configuration.Products;

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
	/// Default lifecycle values (strongly typed)
	/// </summary>
	public static IReadOnlyList<Lifecycle> DefaultLifecycles { get; } =
	[
		Lifecycle.Preview,
		Lifecycle.Beta,
		Lifecycle.Ga
	];

	/// <summary>
	/// Pivot configuration for types, subtypes, and areas with label mappings
	/// </summary>
	public PivotConfiguration? Pivot { get; init; }

	/// <summary>
	/// Available types for changelog entries (computed from Pivot.Types or defaults)
	/// </summary>
	public IReadOnlyList<string> Types { get; init; } = DefaultTypes;

	/// <summary>
	/// Available subtypes for breaking changes (computed from Pivot.Subtypes or defaults)
	/// </summary>
	public IReadOnlyList<string> SubTypes { get; init; } = DefaultSubtypes;

	/// <summary>
	/// Available lifecycle values (strongly typed, from config or defaults)
	/// </summary>
	public IReadOnlyList<Lifecycle> Lifecycles { get; init; } = DefaultLifecycles;

	/// <summary>
	/// Available areas (computed from Pivot.Areas keys)
	/// </summary>
	public IReadOnlyList<string>? Areas { get; init; }

	/// <summary>
	/// Available products (resolved Product objects from products.yml)
	/// </summary>
	public IReadOnlyList<Product>? Products { get; init; }

	/// <summary>
	/// Mapping from GitHub label names to changelog type values (computed from Pivot.Types)
	/// </summary>
	public IReadOnlyDictionary<string, string>? LabelToType { get; init; }

	/// <summary>
	/// Mapping from GitHub label names to changelog area values (computed from Pivot.Areas)
	/// Multiple labels can map to the same area, and a single label can map to multiple areas (comma-separated)
	/// </summary>
	public IReadOnlyDictionary<string, string>? LabelToAreas { get; init; }

	/// <summary>
	/// Combined block configuration for create and publish blockers
	/// </summary>
	public BlockConfiguration? Block { get; init; }

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

/// <summary>
/// Combined block configuration for create and publish blockers
/// </summary>
public record BlockConfiguration
{
	/// <summary>
	/// Global labels that block changelog creation
	/// </summary>
	public IReadOnlyList<string>? Create { get; init; }

	/// <summary>
	/// Global labels that block changelog publishing/rendering
	/// </summary>
	public IReadOnlyList<string>? Publish { get; init; }

	/// <summary>
	/// Per-product block overrides (overrides global blockers, does not merge)
	/// Keys are product IDs
	/// </summary>
	public IReadOnlyDictionary<string, ProductBlockers>? ByProduct { get; init; }
}

/// <summary>
/// Product-specific blockers
/// </summary>
public record ProductBlockers
{
	/// <summary>
	/// Labels that block creation for this product (overrides global create blockers)
	/// </summary>
	public IReadOnlyList<string>? Create { get; init; }

	/// <summary>
	/// Labels that block publishing for this product (overrides global publish blockers)
	/// </summary>
	public IReadOnlyList<string>? Publish { get; init; }
}
