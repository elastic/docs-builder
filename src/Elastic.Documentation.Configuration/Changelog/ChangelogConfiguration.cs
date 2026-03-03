// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration.Products;

namespace Elastic.Documentation.Configuration.Changelog;

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
	/// Rules configuration for create and publish blockers
	/// </summary>
	public RulesConfiguration? Rules { get; init; }

	/// <summary>
	/// Extraction configuration for release notes and issues.
	/// </summary>
	public ExtractConfiguration Extract { get; init; } = new();

	/// <summary>
	/// Products configuration with available and default products.
	/// </summary>
	public ProductsConfig? ProductsConfiguration { get; init; }

	/// <summary>
	/// Bundle configuration with profiles and defaults.
	/// </summary>
	public BundleConfiguration? Bundle { get; init; }

	/// <summary>
	/// Labels that trigger the highlight flag for changelog entries (computed from Pivot.HighlightLabels).
	/// When a PR has any of these labels, highlight is set to true.
	/// </summary>
	public IReadOnlyList<string>? HighlightLabels { get; init; }

	private static readonly Lazy<ChangelogConfiguration> DefaultLazy = new(() => new ChangelogConfiguration());

	public static ChangelogConfiguration Default => DefaultLazy.Value;
}
