// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Catalog;
using Nullean.Entities;

namespace Elastic.Documentation.ReleaseNotes;

/// <summary>A changelog configuration with the product catalog its ids refer to: what <see cref="ChangelogConfigurationConstraints"/> check.</summary>
public sealed record ChangelogConfigurationInCatalog(
	ChangelogConfiguration Configuration,
	ProductCatalog Products
) : IHaveConstraints<ChangelogConfigurationInCatalog>
{
	public static ConstraintSet<ChangelogConfigurationInCatalog> Constraints => ChangelogConfigurationConstraints.All;

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
[Constraints<ChangelogConfigurationInCatalog>]
public static partial class ChangelogConfigurationConstraints
{
	private static readonly string BreakingChange = ChangelogEntryType.BreakingChange.ToStringFast(true);

	public static readonly Constraint<ChangelogConfigurationInCatalog> PivotTypesValid = Constraints<ChangelogConfigurationInCatalog>.Each(
		m => PivotTypes(m).Keys,
		t => ChangelogEntryTypeExtensions.TryParse(t, out _, ignoreCase: true, allowMatchingMetadataAttribute: true),
		t => $"Type '{t}' in pivot.types is not a valid type. Valid types: {string.Join(", ", ChangelogConfiguration.DefaultTypes)}"
	).Describe("Every pivot.types key is a changelog entry type.");

	public static readonly Constraint<ChangelogConfigurationInCatalog> PivotRequiredTypesPresent = Constraints<ChangelogConfigurationInCatalog>.References(
		m => PivotTypes(m).Count > 0 ? ChangelogConfiguration.RequiredTypes.Select(t => t.ToStringFast(true)) : [],
		required => Enumerable.Repeat(required, 1),
		m => PivotTypes(m).Keys,
		StringComparer.OrdinalIgnoreCase,
		describe: (_, required) =>
			$"Required type '{required}' is missing from pivot.types. Required types: {string.Join(", ", ChangelogConfiguration.RequiredTypes.Select(t => t.ToStringFast(true)))}"
	).Describe("A pivot.types section declares every required type.");

	public static readonly Constraint<ChangelogConfigurationInCatalog> SubtypesOnlyUnderBreakingChange = Constraints<ChangelogConfigurationInCatalog>.Each(
		PivotTypes,
		kv => !(kv.Value?.Subtypes is { Count: > 0 } && !kv.Key.Equals(BreakingChange, StringComparison.OrdinalIgnoreCase)),
		kv => $"Type '{kv.Key}' has subtypes defined, but subtypes are only allowed for 'breaking-change' type."
	).Describe("Only the breaking-change type declares subtypes.");

	public static readonly Constraint<ChangelogConfigurationInCatalog> TypeSubtypesValid = Constraints<ChangelogConfigurationInCatalog>.Each(
		m => PivotTypes(m)
			.Where(kv => kv.Key.Equals(BreakingChange, StringComparison.OrdinalIgnoreCase))
			.SelectMany(kv => Keys(kv.Value?.Subtypes).Select(s => (Type: kv.Key, Subtype: s))),
		x => IsSubtype(x.Subtype),
		x =>
			$"Subtype '{x.Subtype}' in pivot.types.{x.Type}.subtypes is not a valid subtype. Valid subtypes: {string.Join(", ", ChangelogConfiguration.DefaultSubtypes)}"
	).Describe("Every subtype under pivot.types.breaking-change is a changelog entry subtype.");

	public static readonly Constraint<ChangelogConfigurationInCatalog> PivotSubtypesValid = Constraints<ChangelogConfigurationInCatalog>.Each(
		m => Keys(m.Configuration.Pivot?.Subtypes),
		IsSubtype,
		s =>
			$"Subtype '{s}' in pivot.subtypes is not a valid subtype. Valid subtypes: {string.Join(", ", ChangelogConfiguration.DefaultSubtypes)}"
	).Describe("Every pivot.subtypes key is a changelog entry subtype.");

	public static readonly Constraint<ChangelogConfigurationInCatalog> PivotProductsKnown = Constraints<ChangelogConfigurationInCatalog>.Each(
		m => Keys(m.Configuration.Pivot?.Products)
			.Select(spec => spec.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
			.Where(parts => parts.Length > 0),
		(m, parts) => m.IsKnownProduct(parts[0])
			? null
			: $"Product '{parts[0]}' in pivot.products is not in the list of available products from config/products.yml. Available products: {m.AvailableProducts}"
	).Describe("Every pivot.products key starts with a product from products.yml.");

	public static readonly Constraint<ChangelogConfigurationInCatalog> AvailableProductsKnown = Constraints<ChangelogConfigurationInCatalog>.Each(
		m => m.Configuration.ProductsConfiguration?.Available ?? [],
		(m, id) => m.IsKnownProduct(id)
			? null
			: $"Product '{id}' in changelog.yml is not in the list of available products from config/products.yml. Available products: {m.AvailableProducts}"
	).Describe("Every products.available id is in products.yml.");

	public static readonly Constraint<ChangelogConfigurationInCatalog> DefaultProductHasId = Constraints<ChangelogConfigurationInCatalog>.Each(
		m => m.Configuration.ProductsConfiguration?.Default ?? [],
		d => !string.IsNullOrWhiteSpace(d.Product),
		_ => "Default product in products_config.default must have a product ID"
	).Describe("Every products.default entry names a product.");

	public static readonly Constraint<ChangelogConfigurationInCatalog> DefaultProductKnown = Constraints<ChangelogConfigurationInCatalog>.Each(
		m => m.Configuration.ProductsConfiguration?.Default ?? [],
		(m, d) => string.IsNullOrWhiteSpace(d.Product) || m.IsKnownProduct(d.Product)
			? null
			: $"Product '{d.Product}' in products_config.default is not in the list of available products from config/products.yml. Available products: {m.AvailableProducts}"
	).Describe("Every products.default product is in products.yml.");

	public static readonly Constraint<ChangelogConfigurationInCatalog> BundleRepoSingle = Constraints<ChangelogConfigurationInCatalog>.Always(
		m => !IsMergedRepo(BundleRepo(m.Configuration.Bundle)),
		"bundle.repo must name a single GitHub repository. Remove '+' merged-repo syntax from bundle.repo."
	).Describe("bundle.repo names a single repository.");

	public static readonly Constraint<ChangelogConfigurationInCatalog> ProfileRepoSingle = Constraints<ChangelogConfigurationInCatalog>.Each(
		Profiles,
		kv => !IsMergedRepo(ProfileRepo(kv.Value)),
		kv => $"bundle.profiles.{kv.Key}.repo must name a single GitHub repository. Remove '+' merged-repo syntax."
	).Describe("Every bundle profile's repo names a single repository.");

	public static readonly Constraint<ChangelogConfigurationInCatalog> LinkAllowReposOwnerRepo = Constraints<ChangelogConfigurationInCatalog>.Each(
		m => LinkAllowRepos(m.Configuration.Bundle),
		r => IsOwnerRepo(r.Trim()),
		r => $"bundle.link_allow_repos: each entry must be exactly 'owner/repo' (one slash). Invalid: '{r}'."
	).Describe("Every bundle.link_allow_repos entry is exactly owner/repo.");

	public static readonly Constraint<ChangelogConfigurationInCatalog> ProfileProductKnown = Constraints<ChangelogConfigurationInCatalog>.Each(
		Profiles,
		(m, kv) => string.IsNullOrWhiteSpace(kv.Value.Product) || m.IsKnownProduct(kv.Value.Product)
			? null
			: $"bundle.profiles.{kv.Key}.product: '{kv.Value.Product}' is not in products.yml. Available products: {m.AvailableProducts}"
	).Describe("Every bundle profile's product is in products.yml.");

	public static readonly Constraint<ChangelogConfigurationInCatalog> ReleaseTagProfileDefined = Constraints<ChangelogConfigurationInCatalog>.Each(
		m => m.Configuration.Bundle?.Releases?.Github ?? new Dictionary<string, string>(),
		(m, release) => ReleaseProfileProblem(m, release, "github")
	).Describe("Every bundle.releases.github tag maps to a defined profile.");

	public static readonly Constraint<ChangelogConfigurationInCatalog> ReleaseProductProfileDefined = Constraints<ChangelogConfigurationInCatalog>.EachCheck(
		m => m.Configuration.Bundle?.Releases?.Products ?? new Dictionary<string, string>(),
		(m, release) => ReleaseProductProblems(m, release)
	).Describe("Every bundle.releases.products product is in products.yml and maps to a defined profile.");

	public static readonly Constraint<ChangelogConfigurationInCatalog> BundleRulesProductsExclusive = Constraints<ChangelogConfigurationInCatalog>.Always(
		m => BundleRules(m) is not { ExcludeProducts.Count: > 0, IncludeProducts.Count: > 0 },
		"rules.bundle: cannot have both 'exclude_products' and 'include_products'. Use one or the other."
	).Describe("rules.bundle sets exclude_products or include_products, not both.");

	public static readonly Constraint<ChangelogConfigurationInCatalog> BundleRulesProductsKnown = Constraints<ChangelogConfigurationInCatalog>.Each(
		BundleRuleProductReferences,
		(m, r) => m.IsKnownProduct(r.Id)
			? null
			: r.IsProductRule
				? $"rules.bundle.products: '{r.Id}' not in available products. Available: {m.AvailableProducts}"
				: $"{r.Path}: '{r.Id}' is not in the list of available products. Available products: {m.AvailableProducts}"
	).Describe("Every product in rules.bundle is in products.yml.");

	public static readonly Constraint<ChangelogConfigurationInCatalog> PerProductProductsExclusive = Constraints<ChangelogConfigurationInCatalog>.Each(
		PerProductRules,
		kv => kv.Value is not { ExcludeProducts.Count: > 0, IncludeProducts.Count: > 0 },
		kv => $"rules.bundle.products.{kv.Key}: cannot have both 'exclude_products' and 'include_products'. Use one or the other."
	).Describe("No per-product bundle rule sets both exclude_products and include_products.");

	public static readonly Constraint<ChangelogConfigurationInCatalog> CreateRulesProductsKnown = Constraints<ChangelogConfigurationInCatalog>.Each(
		m => CreateRuleProducts(m.Configuration.Rules?.Create, "rules.create"),
		(m, r) => m.IsKnownProduct(r.Product)
			? null
			: $"{r.Path}.products: '{r.Product}' not in available products. Available: {m.AvailableProducts}"
	).Describe("Every product key in rules.create is in products.yml.");

	public static readonly Constraint<ChangelogConfigurationInCatalog> FilenameKeyedByPr = Constraints<ChangelogConfigurationInCatalog>.OneOf(
		m => m.Configuration.Filename,
		[FilenameStrategy.Pr],
		describe: (strategy, _) =>
			$"filename: '{strategy.ToStringFast(true)}' is no longer supported. Changelog entries are keyed by PR number; for items with no PR use 'changelog note'."
	).Describe("Changelog entries are keyed by pull request.");

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
#pragma warning restore CS0618

	private static string? ReleaseProfileProblem(ChangelogConfigurationInCatalog m, KeyValuePair<string, string> release, string section)
	{
		var (key, profile) = release;
		if (string.IsNullOrWhiteSpace(profile))
			return $"bundle.releases.{section}['{key}']: must map to a non-empty profile name.";
		return m.Configuration.Bundle?.Profiles?.ContainsKey(profile) != true
			? $"bundle.releases.{section}['{key}']: profile '{profile}' is not defined in bundle.profiles."
			: null;
	}

	private static IEnumerable<Violation> ReleaseProductProblems(ChangelogConfigurationInCatalog m, KeyValuePair<string, string> release)
	{
		if (!m.IsKnownProduct(release.Key))
			yield return Violation.Error(
				$"bundle.releases.products key '{release.Key}' is not in products.yml. Available products: {m.AvailableProducts}"
			);
		if (ReleaseProfileProblem(m, release, "products") is { } problem)
			yield return Violation.Error(problem);
	}

	private static BundleRules? BundleRules(ChangelogConfigurationInCatalog m) => m.Configuration.Rules?.Bundle;

	private static IEnumerable<KeyValuePair<string, BundlePerProductRule>> PerProductRules(ChangelogConfigurationInCatalog m) =>
		BundleRules(m)?.ByProduct ?? new Dictionary<string, BundlePerProductRule>();

	// Every product id rules.bundle names, with where it is named; IsProductRule marks a key of rules.bundle.products rather than an id in a list.
	private static IEnumerable<(string Path, string Id, bool IsProductRule)> BundleRuleProductReferences(ChangelogConfigurationInCatalog m)
	{
		if (BundleRules(m) is not { } rules)
			yield break;
		foreach (var id in ProductIds(rules.ExcludeProducts))
			yield return ("rules.bundle.exclude_products", id, false);
		foreach (var id in ProductIds(rules.IncludeProducts))
			yield return ("rules.bundle.include_products", id, false);
		foreach (var (product, rule) in rules.ByProduct ?? new Dictionary<string, BundlePerProductRule>())
		{
			yield return ("rules.bundle.products", product, true);
			foreach (var id in ProductIds(rule.ExcludeProducts))
				yield return ($"rules.bundle.products.{product}.exclude_products", id, false);
			foreach (var id in ProductIds(rule.IncludeProducts))
				yield return ($"rules.bundle.products.{product}.include_products", id, false);
		}
	}

	private static IEnumerable<string> ProductIds(IReadOnlyList<string>? ids) => ids ?? [];

	private static IEnumerable<(string Path, string Product)> CreateRuleProducts(CreateRules? rules, string path)
	{
		foreach (var (product, productRules) in rules?.ByProduct ?? new Dictionary<string, CreateRules>())
		{
			yield return (path, product);
			foreach (var nested in CreateRuleProducts(productRules, $"{path}.products.{product}"))
				yield return nested;
		}
	}
}
