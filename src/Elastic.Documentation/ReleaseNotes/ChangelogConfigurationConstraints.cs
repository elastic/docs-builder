// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Catalog;
using Elastic.Documentation.Constraints;

namespace Elastic.Documentation.ReleaseNotes;

/// <summary>A changelog configuration with the product catalog its ids refer to: what <see cref="ChangelogConfigurationConstraints"/> check.</summary>
public sealed record ChangelogConfigurationInCatalog(ChangelogConfiguration Configuration, ProductCatalog Products)
{
	internal bool IsKnownProduct(string id)
	{
		var normalized = id.Replace('_', '-');
		return Products.Products.Keys.Any(k => k.Equals(normalized, StringComparison.OrdinalIgnoreCase));
	}

	internal string AvailableProducts =>
		string.Join(", ", Products.Products.Keys.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(p => p));
}

/// <summary>
/// Every rule a <see cref="ChangelogConfiguration"/> must satisfy, in the order they are reported. The loader also resolves
/// product ids while reading, since it normalizes them and only it still sees the authored spelling; the product rules
/// here hold a configuration built any other way to the same standard.
/// </summary>
public static class ChangelogConfigurationConstraints
{
	private static readonly string BreakingChange = ChangelogEntryType.BreakingChange.ToStringFast(true);

	public static IReadOnlyList<Constraint<ChangelogConfigurationInCatalog>> All { get; } =
	[
		new(
			"pivot-types-valid",
			"Every pivot.types key is a changelog entry type.",
			m => PivotTypes(m)
				.Keys
				.Where(t => !ChangelogEntryTypeExtensions.TryParse(t, out _, ignoreCase: true, allowMatchingMetadataAttribute: true))
				.Select(
					t => Finding.Error(
						$"Type '{t}' in pivot.types is not a valid type. Valid types: {string.Join(", ", ChangelogConfiguration.DefaultTypes)}"
					)
				)
		),
		new(
			"pivot-required-types-present",
			"A pivot.types section declares every required type.",
			m => PivotTypes(m) is { Count: > 0 } types
				? ChangelogConfiguration
					.RequiredTypes
					.Select(t => t.ToStringFast(true))
					.Where(required => !types.Keys.Any(k => k.Equals(required, StringComparison.OrdinalIgnoreCase)))
					.Select(
						required => Finding.Error(
							$"Required type '{required}' is missing from pivot.types. Required types: {string.Join(", ", ChangelogConfiguration.RequiredTypes.Select(t => t.ToStringFast(true)))}"
						)
					)
				: []
		),
		new(
			"subtypes-only-under-breaking-change",
			"Only the breaking-change type declares subtypes.",
			m => PivotTypes(m)
				.Where(kv => kv.Value?.Subtypes is { Count: > 0 } && !kv.Key.Equals(BreakingChange, StringComparison.OrdinalIgnoreCase))
				.Select(
					kv => Finding.Error($"Type '{kv.Key}' has subtypes defined, but subtypes are only allowed for 'breaking-change' type.")
				)
		),
		new(
			"type-subtypes-valid",
			"Every subtype under pivot.types.breaking-change is a changelog entry subtype.",
			m => PivotTypes(m)
				.Where(kv => kv.Key.Equals(BreakingChange, StringComparison.OrdinalIgnoreCase))
				.SelectMany(kv => Keys(kv.Value?.Subtypes).Where(s => !IsSubtype(s)).Select(s => (Type: kv.Key, Subtype: s)))
				.Select(
					x => Finding.Error(
						$"Subtype '{x.Subtype}' in pivot.types.{x.Type}.subtypes is not a valid subtype. Valid subtypes: {string.Join(", ", ChangelogConfiguration.DefaultSubtypes)}"
					)
				)
		),
		new(
			"pivot-subtypes-valid",
			"Every pivot.subtypes key is a changelog entry subtype.",
			m => Keys(m.Configuration.Pivot?.Subtypes)
				.Where(s => !IsSubtype(s))
				.Select(
					s => Finding.Error(
						$"Subtype '{s}' in pivot.subtypes is not a valid subtype. Valid subtypes: {string.Join(", ", ChangelogConfiguration.DefaultSubtypes)}"
					)
				)
		),
		new(
			"pivot-products-known",
			"Every pivot.products key starts with a product from products.yml.",
			m => Keys(m.Configuration.Pivot?.Products)
				.Select(spec => spec.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
				.Where(parts => parts.Length > 0 && !m.IsKnownProduct(parts[0]))
				.Select(
					parts => Finding.Error(
						$"Product '{parts[0]}' in pivot.products is not in the list of available products from config/products.yml. Available products: {m.AvailableProducts}"
					)
				)
		),
		new(
			"available-products-known",
			"Every products.available id is in products.yml.",
			m => (m.Configuration.ProductsConfiguration?.Available ?? []).Where(id => !m.IsKnownProduct(id)).Select(
				id => Finding.Error(
					$"Product '{id}' in changelog.yml is not in the list of available products from config/products.yml. Available products: {m.AvailableProducts}"
				)
			)
		),
		new(
			"default-product-has-id",
			"Every products.default entry names a product.",
			m => (m.Configuration.ProductsConfiguration?.Default ?? []).Where(d => string.IsNullOrWhiteSpace(d.Product)).Select(
				_ => Finding.Error("Default product in products_config.default must have a product ID")
			)
		),
		new(
			"default-product-known",
			"Every products.default product is in products.yml.",
			m => (m.Configuration.ProductsConfiguration?.Default ?? []).Where(
				d => !string.IsNullOrWhiteSpace(d.Product) && !m.IsKnownProduct(d.Product)
			).Select(
				d => Finding.Error(
					$"Product '{d.Product}' in products_config.default is not in the list of available products from config/products.yml. Available products: {m.AvailableProducts}"
				)
			)
		),
		new(
			"bundle-repo-single",
			"bundle.repo names a single repository.",
			m => IsMergedRepo(BundleRepo(m.Configuration.Bundle))
				? [Finding.Error("bundle.repo must name a single GitHub repository. Remove '+' merged-repo syntax from bundle.repo.")]
				: []
		),
		new(
			"profile-repo-single",
			"Every bundle profile's repo names a single repository.",
			m => Profiles(m)
				.Where(kv => IsMergedRepo(ProfileRepo(kv.Value)))
				.Select(
					kv => Finding.Error(
						$"bundle.profiles.{kv.Key}.repo must name a single GitHub repository. Remove '+' merged-repo syntax."
					)
				)
		),
		new(
			"link-allow-repos-owner-repo",
			"Every bundle.link_allow_repos entry is exactly owner/repo.",
			m => LinkAllowRepos(m.Configuration.Bundle)
				.Where(r => !IsOwnerRepo(r.Trim()))
				.Select(
					r => Finding.Error($"bundle.link_allow_repos: each entry must be exactly 'owner/repo' (one slash). Invalid: '{r}'.")
				)
		),
		new(
			"profile-fields-not-deprecated",
			"No bundle profile sets source, output_products or output_directory.",
			m => Profiles(m).SelectMany(kv => DeprecatedProfileFields(kv.Key, kv.Value))
		),
		new(
			"profile-product-known",
			"Every bundle profile's product is in products.yml.",
			m => Profiles(m)
				.Where(kv => !string.IsNullOrWhiteSpace(kv.Value.Product) && !m.IsKnownProduct(kv.Value.Product))
				.Select(
					kv => Finding.Error(
						$"bundle.profiles.{kv.Key}.product: '{kv.Value.Product}' is not in products.yml. Available products: {m.AvailableProducts}"
					)
				)
		),
		new(
			"release-tag-profile-defined",
			"Every bundle.releases.github tag maps to a defined profile.",
			m => ReleaseProfileFindings(m, m.Configuration.Bundle?.Releases?.Github, "github")
		),
		new(
			"release-product-profile-defined",
			"Every bundle.releases.products product is in products.yml and maps to a defined profile.",
			m => ReleaseProductFindings(m)
		),
		new(
			"bundle-rules-products-exclusive",
			"rules.bundle sets exclude_products or include_products, not both.",
			m => BundleRules(m) is { ExcludeProducts.Count: > 0, IncludeProducts.Count: > 0 }
				? [Finding.Error("rules.bundle: cannot have both 'exclude_products' and 'include_products'. Use one or the other.")]
				: []
		),
		new("bundle-rules-products-known", "Every product in rules.bundle is in products.yml.", m => BundleRuleProductFindings(m)),
		new(
			"per-product-products-exclusive",
			"No per-product bundle rule sets both exclude_products and include_products.",
			m => PerProductRules(m)
				.Where(kv => kv.Value is { ExcludeProducts.Count: > 0, IncludeProducts.Count: > 0 })
				.Select(
					kv => Finding.Error(
						$"rules.bundle.products.{kv.Key}: cannot have both 'exclude_products' and 'include_products'. Use one or the other."
					)
				)
		),
		new(
			"per-product-include-selective",
			"A per-product bundle rule with include_products does not match any product.",
			m => PerProductRules(m)
				.Where(kv => kv.Value is { MatchProducts: MatchMode.Any, IncludeProducts.Count: > 0 })
				.Select(
					kv => Finding.Warning(
						$"Configuration pattern 'match_products: any' with 'include_products' in per-product rule '{kv.Key}' provides no selective filtering. " +
							"Consider 'match_products: all' for strict filtering or 'exclude_products' for exclusion-based filtering. " +
							"Refer to https://github.com/elastic/docs-builder/blob/main/docs/contribute/configure-changelogs-ref.md"
					)
				)
		),
		new(
			"per-product-include-not-disjoint",
			"A per-product bundle rule includes only its own product.",
			m => PerProductRules(m).SelectMany(kv => DisjointProductFindings(kv.Key, kv.Value))
		),
		new(
			"create-rules-products-known",
			"Every product key in rules.create is in products.yml.",
			m => CreateRuleProductFindings(m, m.Configuration.Rules?.Create, "rules.create")
		),
		new(
			"filename-keyed-by-pr",
			"Changelog entries are keyed by pull request.",
			m =>
				m.Configuration.Filename switch
				{
					FilenameStrategy.Timestamp =>
						[
							Finding.Error(
								"filename: 'timestamp' is no longer supported. Changelog entries are keyed by PR number; for items with no PR use 'changelog note'."
							)
						],
					FilenameStrategy.Issue =>
						[
							Finding.Error(
								"filename: 'issue' is no longer supported. Changelog entries are keyed by PR number; for items with no PR use 'changelog note'."
							)
						],
					_ => []
				}
		)
	];

	public static IReadOnlyList<ConstraintViolation> Check(ChangelogConfiguration configuration, ProductCatalog products) =>
		All.CheckAll(new ChangelogConfigurationInCatalog(configuration, products));

	private static IReadOnlyDictionary<string, TypeEntry?> PivotTypes(ChangelogConfigurationInCatalog m) =>
		m.Configuration.Pivot?.Types ?? [];

	private static IEnumerable<string> Keys(IReadOnlyDictionary<string, string?>? map) => map?.Keys ?? [];

	private static bool IsSubtype(string subtype) =>
		ChangelogEntrySubtypeExtensions.TryParse(subtype, out _, ignoreCase: true, allowMatchingMetadataAttribute: true);

	private static IEnumerable<KeyValuePair<string, BundleProfile>> Profiles(ChangelogConfigurationInCatalog m) =>
		m.Configuration.Bundle?.Profiles ?? new Dictionary<string, BundleProfile>();

	private static bool IsMergedRepo(string? repo) => !string.IsNullOrWhiteSpace(repo) && repo.Contains('+', StringComparison.Ordinal);

	private static bool IsOwnerRepo(string entry)
	{
		var slash = entry.IndexOf('/');
		return slash > 0 && slash == entry.LastIndexOf('/') && slash < entry.Length - 1;
	}

#pragma warning disable CS0618 // the rules still check what the obsolete fields hold
	private static string? BundleRepo(BundleConfiguration? bundle) => bundle?.Repo;

	private static string? ProfileRepo(BundleProfile profile) => profile.Repo;

	private static IReadOnlyList<string> LinkAllowRepos(BundleConfiguration? bundle) => bundle?.LinkAllowRepos ?? [];

	private static IEnumerable<Finding> DeprecatedProfileFields(string name, BundleProfile profile)
	{
		if (!string.IsNullOrWhiteSpace(profile.Source))
			yield return Finding.Warning(
				$"bundle.profiles.{name}.source is deprecated. " + "Use bundle.releases.github to map release tags to profiles instead."
			);
		if (!string.IsNullOrWhiteSpace(profile.OutputProducts))
			yield return Finding.Warning($"bundle.profiles.{name}.output_products is deprecated. Use 'product: <id>' instead.");
		if (!string.IsNullOrWhiteSpace(profile.OutputDirectory))
			yield return Finding.Warning(
				$"bundle.profiles.{name}.output_directory is deprecated. The output directory is derived automatically as bundle.output_directory/{{product}}."
			);
	}
#pragma warning restore CS0618

	private static IEnumerable<Finding> ReleaseProfileFindings(
		ChangelogConfigurationInCatalog m,
		IReadOnlyDictionary<string, string>? releases,
		string section
	)
	{
		foreach (var (key, profile) in releases ?? new Dictionary<string, string>())
		{
			if (string.IsNullOrWhiteSpace(profile))
				yield return Finding.Error($"bundle.releases.{section}['{key}']: must map to a non-empty profile name.");
			else if (m.Configuration.Bundle?.Profiles?.ContainsKey(profile) != true)
				yield return Finding.Error($"bundle.releases.{section}['{key}']: profile '{profile}' is not defined in bundle.profiles.");
		}
	}

	private static IEnumerable<Finding> ReleaseProductFindings(ChangelogConfigurationInCatalog m)
	{
		var releases = m.Configuration.Bundle?.Releases?.Products ?? new Dictionary<string, string>();
		var unknown = releases
			.Keys
			.Where(p => !m.IsKnownProduct(p))
			.Select(
				p => Finding.Error($"bundle.releases.products key '{p}' is not in products.yml. Available products: {m.AvailableProducts}")
			);
		return unknown.Concat(ReleaseProfileFindings(m, releases, "products"));
	}

	private static BundleRules? BundleRules(ChangelogConfigurationInCatalog m) => m.Configuration.Rules?.Bundle;

	private static IEnumerable<KeyValuePair<string, BundlePerProductRule>> PerProductRules(ChangelogConfigurationInCatalog m) =>
		BundleRules(m)?.ByProduct ?? new Dictionary<string, BundlePerProductRule>();

	private static IEnumerable<Finding> BundleRuleProductFindings(ChangelogConfigurationInCatalog m)
	{
		if (BundleRules(m) is not { } rules)
			yield break;

		foreach (var finding in UnknownProducts(m, rules.ExcludeProducts, "rules.bundle.exclude_products"))
			yield return finding;
		foreach (var finding in UnknownProducts(m, rules.IncludeProducts, "rules.bundle.include_products"))
			yield return finding;

		foreach (var (product, rule) in rules.ByProduct ?? new Dictionary<string, BundlePerProductRule>())
		{
			if (!m.IsKnownProduct(product))
				yield return Finding.Error(
					$"rules.bundle.products: '{product}' not in available products. Available: {m.AvailableProducts}"
				);
			foreach (var finding in UnknownProducts(m, rule.ExcludeProducts, $"rules.bundle.products.{product}.exclude_products"))
				yield return finding;
			foreach (var finding in UnknownProducts(m, rule.IncludeProducts, $"rules.bundle.products.{product}.include_products"))
				yield return finding;
		}
	}

	private static IEnumerable<Finding> UnknownProducts(ChangelogConfigurationInCatalog m, IReadOnlyList<string>? ids, string path) =>
		(ids ?? []).Where(id => !m.IsKnownProduct(id)).Select(
			id => Finding.Error($"{path}: '{id}' is not in the list of available products. Available products: {m.AvailableProducts}")
		);

	private static IEnumerable<Finding> DisjointProductFindings(string product, BundlePerProductRule rule)
	{
		if (rule.IncludeProducts is not { Count: > 1 } include)
			return [];
		var disjoint = include.Where(p => !string.Equals(p, product, StringComparison.OrdinalIgnoreCase)).ToList();
		return disjoint.Count == 0
			? []
			: [
				Finding.Hint(
					$"Per-product rule '{product}' includes disjoint products [{string.Join(", ", disjoint)}] " +
						"which cannot be included due to single-product rule resolution. " +
						"Use separate bundles (each with a single product in output_products or profile output_products), or multi-product changelogs instead. " +
						"Refer to https://github.com/elastic/docs-builder/blob/main/docs/contribute/configure-changelogs.md"
				)
			];
	}

	private static IEnumerable<Finding> CreateRuleProductFindings(ChangelogConfigurationInCatalog m, CreateRules? rules, string path)
	{
		foreach (var (product, productRules) in rules?.ByProduct ?? new Dictionary<string, CreateRules>())
		{
			if (!m.IsKnownProduct(product))
				yield return Finding.Error($"{path}.products: '{product}' not in available products. Available: {m.AvailableProducts}");
			foreach (var finding in CreateRuleProductFindings(m, productRules, $"{path}.products.{product}"))
				yield return finding;
		}
	}
}
