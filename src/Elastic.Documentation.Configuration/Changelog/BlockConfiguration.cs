// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.ReleaseNotes;

namespace Elastic.Documentation.Configuration.Changelog;

/// <summary>
/// Top-level rules configuration for controlling changelog creation and publishing.
/// </summary>
public record RulesConfiguration
{
	/// <summary>
	/// Global match mode for multi-valued fields. Inherited by create, bundle, and publish sections.
	/// </summary>
	public MatchMode Match { get; init; } = MatchMode.Any;

	/// <summary>
	/// Rules controlling which PRs generate changelog entries.
	/// </summary>
	public CreateRules? Create { get; init; }

	/// <summary>
	/// Rules controlling which entries are included in a bundle file.
	/// </summary>
	public BundleRules? Bundle { get; init; }

	/// <summary>
	/// Rules controlling which entries appear in rendered output.
	/// </summary>
	public PublishRules? Publish { get; init; }
}

/// <summary>
/// Rules for create-time blocking based on PR labels.
/// </summary>
public record CreateRules
{
	/// <summary>
	/// Labels to match (semantics depend on <see cref="Mode"/>).
	/// </summary>
	public IReadOnlyList<string>? Labels { get; init; }

	/// <summary>
	/// Whether labels use exclude or include semantics.
	/// </summary>
	public FieldMode Mode { get; init; } = FieldMode.Exclude;

	/// <summary>
	/// Match mode for labels (any, all, or conjunction). Inherited from RulesConfiguration.Match if not set.
	/// </summary>
	public MatchMode Match { get; init; } = MatchMode.Any;

	/// <summary>
	/// Per-product create rule overrides. Keys are product IDs.
	/// </summary>
	public IReadOnlyDictionary<string, CreateRules>? ByProduct { get; init; }
}

/// <summary>
/// Per-product bundle rule combining product filtering with type/area blocking.
/// </summary>
public record BundlePerProductRule
{
	/// <summary>
	/// Optional type/area blocker (existing functionality).
	/// </summary>
	public PublishBlocker? Blocker { get; init; }

	/// <summary>
	/// Product IDs to include (mutually exclusive with ExcludeProducts).
	/// </summary>
	public IReadOnlyList<string>? IncludeProducts { get; init; }

	/// <summary>
	/// Product IDs to exclude (mutually exclusive with IncludeProducts).
	/// </summary>
	public IReadOnlyList<string>? ExcludeProducts { get; init; }

	/// <summary>
	/// Match mode for products (any, all, or conjunction).
	/// </summary>
	public MatchMode MatchProducts { get; init; } = MatchMode.Any;
}

/// <summary>
/// Result of bundle rule resolution for a changelog entry.
/// Provides explicit, type-safe indication of how the entry should be handled.
/// </summary>
public enum ResolveResult
{
	/// <summary>
	/// Use global bundle rules (no per-product rule applies).
	/// </summary>
	UseGlobal,

	/// <summary>
	/// Use the specified per-product rule.
	/// </summary>
	UsePerProduct,

	/// <summary>
	/// Exclude the entry because its products are disjoint from the bundle context.
	/// </summary>
	ExcludeDisjoint,

	/// <summary>
	/// Exclude the entry because it has no products declared.
	/// </summary>
	ExcludeMissingProducts,

	/// <summary>
	/// Include the entry without per-product filtering (per-product context mode when no override exists for the rule context product).
	/// </summary>
	PassThrough
}

/// <summary>
/// Container for rule resolution result when using a per-product rule.
/// </summary>
public record ResolveResultWithRule(ResolveResult Result, BundlePerProductRule? Rule)
{
	/// <summary>
	/// Creates a UseGlobal result.
	/// </summary>
	public static ResolveResultWithRule UseGlobal() => new(ResolveResult.UseGlobal, null);

	/// <summary>
	/// Creates a PassThrough result (no per-product rule applies; global rules are not used in per-product mode).
	/// </summary>
	public static ResolveResultWithRule PassThrough() => new(ResolveResult.PassThrough, null);

	/// <summary>
	/// Creates a UsePerProduct result with the specified rule.
	/// </summary>
	public static ResolveResultWithRule UsePerProduct(BundlePerProductRule rule) => new(ResolveResult.UsePerProduct, rule);

	/// <summary>
	/// Creates an ExcludeDisjoint result.
	/// </summary>
	public static ResolveResultWithRule ExcludeDisjoint() => new(ResolveResult.ExcludeDisjoint, null);

	/// <summary>
	/// Creates an ExcludeMissingProducts result.
	/// </summary>
	public static ResolveResultWithRule ExcludeMissingProducts() => new(ResolveResult.ExcludeMissingProducts, null);
}

/// <summary>
/// Rules for bundle-time filtering by product, type, and area.
/// Applied during <c>changelog bundle</c> after the input stage gathers entries.
/// Always applies regardless of input method (<c>--input-products</c>, <c>--prs</c>, <c>--all</c>, etc.).
/// </summary>
public record BundleRules
{
	/// <summary>
	/// Product IDs to exclude from the bundle. Cannot be combined with <see cref="IncludeProducts"/>.
	/// </summary>
	public IReadOnlyList<string>? ExcludeProducts { get; init; }

	/// <summary>
	/// Product IDs to include in the bundle (all others excluded). Cannot be combined with <see cref="ExcludeProducts"/>.
	/// </summary>
	public IReadOnlyList<string>? IncludeProducts { get; init; }

	/// <summary>
	/// Match mode for products (any, all, or conjunction). Inherited from RulesConfiguration.Match if not set.
	/// </summary>
	public MatchMode MatchProducts { get; init; } = MatchMode.Any;

	/// <summary>
	/// Global type/area blocker applied to all entries. Mirrors <c>rules.publish</c> blocker semantics.
	/// </summary>
	public PublishBlocker? Blocker { get; init; }

	/// <summary>
	/// Per-product rule overrides. Keys are product IDs.
	/// </summary>
	public IReadOnlyDictionary<string, BundlePerProductRule>? ByProduct { get; init; }
}

/// <summary>
/// Bundle-time filtering mode: none, global rules (changelog content only), or per-product rule context.
/// </summary>
public enum BundleFilterMode
{
	/// <summary>
	/// No bundle rules apply (no product/type/area filtering from rules.bundle).
	/// </summary>
	NoFiltering,

	/// <summary>
	/// Global rules.bundle only; filters use each changelog's fields (no disjoint exclusion).
	/// </summary>
	GlobalContent,

	/// <summary>
	/// Non-empty <c>rules.bundle.products</c>; single rule-context product and per-product rules only (global bundle keys ignored).
	/// </summary>
	PerProductContext
}

/// <summary>
/// Resolves <see cref="BundleFilterMode"/> from parsed <see cref="BundleRules"/>.
/// </summary>
public static class BundleRulesExtensions
{
	/// <summary>
	/// Determines mode: per-product when <see cref="BundleRules.ByProduct"/> is non-empty; else global when any global filter exists; else no filtering.
	/// </summary>
	public static BundleFilterMode DetermineFilterMode(this BundleRules bundleRules)
	{
		if (bundleRules.ByProduct is { Count: > 0 })
			return BundleFilterMode.PerProductContext;

		if ((bundleRules.ExcludeProducts?.Count ?? 0) > 0 ||
			(bundleRules.IncludeProducts?.Count ?? 0) > 0 ||
			bundleRules.Blocker != null)
			return BundleFilterMode.GlobalContent;

		return BundleFilterMode.NoFiltering;
	}
}

/// <summary>
/// Rules for publish-time blocking based on entry type and area.
/// </summary>
public record PublishRules
{
	/// <summary>
	/// Global publish blocker configuration.
	/// </summary>
	public PublishBlocker? Blocker { get; init; }

	/// <summary>
	/// Per-product publish blocker overrides. Keys are product IDs.
	/// </summary>
	public IReadOnlyDictionary<string, PublishBlocker>? ByProduct { get; init; }
}
