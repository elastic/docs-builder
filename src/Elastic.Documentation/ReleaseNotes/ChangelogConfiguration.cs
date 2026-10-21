// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Catalog;
using Elastic.Documentation.Equality;
using Generator.Equals;

namespace Elastic.Documentation.ReleaseNotes;

/// <summary>
/// Configuration for changelog generation
/// </summary>
/// <remarks>
/// Constructed with <c>ChangelogConfigurationLoader</c>.
/// Optional members are <c>internal init</c>: only tests (<c>InternalsVisibleTo</c>) can set them.
/// </remarks>
[Equatable]
public partial record ChangelogConfiguration
{
	/// <summary>
	/// Default types for changelog entries (derived from ChangelogEntryType enum)
	/// </summary>
	public static IReadOnlyList<string> DefaultTypes { get; } = ChangelogEntryTypeExtensions
		.GetValues()
		.Select(t => t.ToStringFast(true))
		.ToList();

	/// <summary>
	/// Default subtypes for breaking changes (derived from ChangelogEntrySubtype enum)
	/// </summary>
	public static IReadOnlyList<string> DefaultSubtypes { get; } = ChangelogEntrySubtypeExtensions
		.GetValues()
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
		Lifecycle.Ga,
		Lifecycle.Experimental
	];

	/// <summary>
	/// Pivot configuration for types, subtypes, and areas with label mappings
	/// </summary>
	public PivotConfiguration? Pivot { get; internal init; }

	/// <summary>
	/// Available types for changelog entries (computed from Pivot.Types or defaults)
	/// </summary>
	[OrderedEquality]
	public IReadOnlyList<string> Types { get; internal init; } = DefaultTypes;

	/// <summary>
	/// Available subtypes for breaking changes (computed from Pivot.Subtypes or defaults)
	/// </summary>
	[OrderedEquality]
	public IReadOnlyList<string> SubTypes { get; internal init; } = DefaultSubtypes;

	/// <summary>
	/// Available lifecycle values (strongly typed, from config or defaults)
	/// </summary>
	[OrderedEquality]
	public IReadOnlyList<Lifecycle> Lifecycles { get; internal init; } = DefaultLifecycles;

	/// <summary>
	/// Available areas (computed from Pivot.Areas keys)
	/// </summary>
	[OrderedEquality]
	public IReadOnlyList<string>? Areas { get; internal init; }

	/// <summary>
	/// Available products (resolved Product objects from products.yml)
	/// </summary>
	// Product is a record of scalars, so compiler record equality is already structural; it needs no [Equatable].
#pragma warning disable GE003
	[OrderedEquality]
	public IReadOnlyList<Product>? Products { get; internal init; }
#pragma warning restore GE003

	/// <summary>
	/// Mapping from GitHub label names to changelog type values (computed from Pivot.Types)
	/// </summary>
	[UnorderedEquality]
	public IReadOnlyDictionary<string, string>? LabelToType { get; internal init; }

	/// <summary>
	/// Mapping from GitHub label names to changelog area values (computed from Pivot.Areas)
	/// Multiple labels can map to the same area. To map one label to multiple areas, repeat the label under each area in pivot.areas.
	/// </summary>
	[UnorderedEquality(typeof(StringListEntryComparer))]
	public IReadOnlyDictionary<string, IReadOnlyList<string>>? LabelToAreas { get; internal init; }

	/// <summary>
	/// Mapping from GitHub label names to product spec strings (computed from Pivot.Products).
	/// Each label maps to a product spec string (e.g., "elasticsearch", "kibana 9.2.0").
	/// When a PR has labels matching multiple entries, all matching products are collected.
	/// </summary>
	[UnorderedEquality]
	public IReadOnlyDictionary<string, string>? LabelToProducts { get; internal init; }

	/// <summary>
	/// Mapping from GitHub label names to feature-id values (computed from Pivot.Features).
	/// Each label maps to a feature-id string (e.g., "feature:new-search-api").
	/// When a PR has labels matching multiple distinct feature-ids, the first match is used.
	/// </summary>
	[UnorderedEquality]
	public IReadOnlyDictionary<string, string>? LabelToFeatures { get; internal init; }

	/// <summary>
	/// Rules configuration for create and publish blockers
	/// </summary>
	public RulesConfiguration? Rules { get; internal init; }

	/// <summary>
	/// Extraction configuration for release notes and issues.
	/// </summary>
	public ExtractConfiguration Extract { get; internal init; } = new();

	/// <summary>
	/// Products configuration with available and default products.
	/// </summary>
	public ProductsConfig? ProductsConfiguration { get; internal init; }

	/// <summary>
	/// Filename strategy for generated changelog files.
	/// Controls how files created by 'changelog add' are named.
	/// </summary>
	public FilenameStrategy Filename { get; internal init; } = FilenameStrategy.Pr;

	/// <summary>
	/// Bundle configuration with profiles and defaults.
	/// </summary>
	public BundleConfiguration? Bundle { get; internal init; }

	/// <summary>
	/// Labels that trigger the highlight flag for changelog entries (computed from Pivot.HighlightLabels).
	/// When a PR has any of these labels, highlight is set to true.
	/// </summary>
	[OrderedEquality]
	public IReadOnlyList<string>? HighlightLabels { get; internal init; }

	private static readonly Lazy<ChangelogConfiguration> DefaultLazy = new(() => new ChangelogConfiguration());

	public static ChangelogConfiguration Default => DefaultLazy.Value;
}
