// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Catalog;
using Nullean.Entities;

namespace Elastic.Documentation.ReleaseNotes;

/// <summary>A changelog configuration with the product catalog its ids refer to: what <see cref="ChangelogConfigurationConstraints"/> check.</summary>
public sealed record ChangelogConfigurationInCatalog(ChangelogConfiguration Configuration, ProductCatalog Products)
{
	internal bool IsKnownProduct(string id)
	{
		var normalized = ProductNames.Normalize(id);
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
[Constraints]
public static partial class ChangelogConfigurationConstraints
{
	private static readonly string BreakingChange = ChangelogEntryType.BreakingChange.ToStringFast(true);

	public static Constraint<ChangelogConfigurationInCatalog> PivotTypesValid { get; } = Constraints<ChangelogConfigurationInCatalog>.Each(
		m => PivotTypes(m).Keys,
		t => ChangelogEntryTypeExtensions.TryParse(t, out _, ignoreCase: true, allowMatchingMetadataAttribute: true),
		t => $"Type '{t}' in pivot.types is not a valid type. Valid types: {string.Join(", ", ChangelogConfiguration.DefaultTypes)}"
	).Describe("Every pivot.types key is a changelog entry type.");

	public static Constraint<ChangelogConfigurationInCatalog> PivotRequiredTypesPresent
	{
		get;
	} = Constraints<ChangelogConfigurationInCatalog>.References(
		m => PivotTypes(m).Count > 0 ? ChangelogConfiguration.RequiredTypes.Select(t => t.ToStringFast(true)) : [],
		required => Enumerable.Repeat(required, 1),
		m => PivotTypes(m).Keys,
		StringComparer.OrdinalIgnoreCase,
		describe: (_, required) =>
			$"Required type '{required}' is missing from pivot.types. Required types: {string.Join(", ", ChangelogConfiguration.RequiredTypes.Select(t => t.ToStringFast(true)))}"
	).Describe("A pivot.types section declares every required type.");

	public static Constraint<ChangelogConfigurationInCatalog> SubtypesOnlyUnderBreakingChange
	{
		get;
	} = Constraints<ChangelogConfigurationInCatalog>.Each(
		PivotTypes,
		kv => !(kv.Value?.Subtypes is { Count: > 0 } && !kv.Key.Equals(BreakingChange, StringComparison.OrdinalIgnoreCase)),
		kv => $"Type '{kv.Key}' has subtypes defined, but subtypes are only allowed for 'breaking-change' type."
	).Describe("Only the breaking-change type declares subtypes.");

	public static Constraint<ChangelogConfigurationInCatalog> TypeSubtypesValid
	{
		get;
	} = Constraints<ChangelogConfigurationInCatalog>.Each(
		m => PivotTypes(m)
			.Where(kv => kv.Key.Equals(BreakingChange, StringComparison.OrdinalIgnoreCase))
			.SelectMany(kv => Keys(kv.Value?.Subtypes).Select(s => (Type: kv.Key, Subtype: s))),
		x => IsSubtype(x.Subtype),
		x =>
			$"Subtype '{x.Subtype}' in pivot.types.{x.Type}.subtypes is not a valid subtype. Valid subtypes: {string.Join(", ", ChangelogConfiguration.DefaultSubtypes)}"
	).Describe("Every subtype under pivot.types.breaking-change is a changelog entry subtype.");

	public static Constraint<ChangelogConfigurationInCatalog> PivotSubtypesValid
	{
		get;
	} = Constraints<ChangelogConfigurationInCatalog>.Each(
		m => Keys(m.Configuration.Pivot?.Subtypes),
		IsSubtype,
		s =>
			$"Subtype '{s}' in pivot.subtypes is not a valid subtype. Valid subtypes: {string.Join(", ", ChangelogConfiguration.DefaultSubtypes)}"
	).Describe("Every pivot.subtypes key is a changelog entry subtype.");

	public static Constraint<ChangelogConfigurationInCatalog> PivotProductsKnown
	{
		get;
	} = Constraints<ChangelogConfigurationInCatalog>.Each(
		m => Keys(m.Configuration.Pivot?.Products)
			.Select(spec => spec.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
			.Where(parts => parts.Length > 0),
		(m, parts) => m.IsKnownProduct(parts[0])
			? null
			: $"Product '{parts[0]}' in pivot.products is not in the list of available products from config/products.yml. Available products: {m.AvailableProducts}"
	).Describe("Every pivot.products key starts with a product from products.yml.");

	public static Constraint<ChangelogConfigurationInCatalog> AvailableProductsKnown
	{
		get;
	} = Constraints<ChangelogConfigurationInCatalog>.Each(
		m => m.Configuration.ProductsConfiguration?.Available ?? [],
		(m, id) => m.IsKnownProduct(id)
			? null
			: $"Product '{id}' in changelog.yml is not in the list of available products from config/products.yml. Available products: {m.AvailableProducts}"
	).Describe("Every products.available id is in products.yml.");

	public static Constraint<ChangelogConfigurationInCatalog> DefaultProductHasId
	{
		get;
	} = Constraints<ChangelogConfigurationInCatalog>.Each(
		m => m.Configuration.ProductsConfiguration?.Default ?? [],
		d => !string.IsNullOrWhiteSpace(d.Product),
		_ => "Default product in products_config.default must have a product ID"
	).Describe("Every products.default entry names a product.");

	public static Constraint<ChangelogConfigurationInCatalog> DefaultProductKnown
	{
		get;
	} = Constraints<ChangelogConfigurationInCatalog>.Each(
		m => m.Configuration.ProductsConfiguration?.Default ?? [],
		(m, d) => string.IsNullOrWhiteSpace(d.Product) || m.IsKnownProduct(d.Product)
			? null
			: $"Product '{d.Product}' in products_config.default is not in the list of available products from config/products.yml. Available products: {m.AvailableProducts}"
	).Describe("Every products.default product is in products.yml.");

	public static Constraint<ChangelogConfigurationInCatalog> BundleRepoSingle
	{
		get;
	} = Constraints<ChangelogConfigurationInCatalog>.Always(
		m => !IsMergedRepo(BundleRepo(m.Configuration.Bundle)),
		"bundle.repo must name a single GitHub repository. Remove '+' merged-repo syntax from bundle.repo."
	).Describe("bundle.repo names a single repository.");

	public static Constraint<ChangelogConfigurationInCatalog> ProfileRepoSingle
	{
		get;
	} = Constraints<ChangelogConfigurationInCatalog>.Each(
		Profiles,
		kv => !IsMergedRepo(ProfileRepo(kv.Value)),
		kv => $"bundle.profiles.{kv.Key}.repo must name a single GitHub repository. Remove '+' merged-repo syntax."
	).Describe("Every bundle profile's repo names a single repository.");

	public static Constraint<ChangelogConfigurationInCatalog> LinkAllowReposOwnerRepo
	{
		get;
	} = Constraints<ChangelogConfigurationInCatalog>.Each(
		m => LinkAllowRepos(m.Configuration.Bundle),
		r => IsOwnerRepo(r.Trim()),
		r => $"bundle.link_allow_repos: each entry must be exactly 'owner/repo' (one slash). Invalid: '{r}'."
	).Describe("Every bundle.link_allow_repos entry is exactly owner/repo.");

	public static Constraint<ChangelogConfigurationInCatalog> ProfileFieldsNotDeprecated
	{
		get;
	} = Constraints<ChangelogConfigurationInCatalog>.EachCheck(Profiles, (_, kv) => DeprecatedProfileFields(kv.Key, kv.Value)).Describe(
		"No bundle profile sets source, output_products or output_directory."
	);

	public static Constraint<ChangelogConfigurationInCatalog> ProfileProductKnown
	{
		get;
	} = Constraints<ChangelogConfigurationInCatalog>.Each(
		Profiles,
		(m, kv) => string.IsNullOrWhiteSpace(kv.Value.Product) || m.IsKnownProduct(kv.Value.Product)
			? null
			: $"bundle.profiles.{kv.Key}.product: '{kv.Value.Product}' is not in products.yml. Available products: {m.AvailableProducts}"
	).Describe("Every bundle profile's product is in products.yml.");

	public static Constraint<ChangelogConfigurationInCatalog> ReleaseTagProfileDefined
	{
		get;
	} = Constraints<ChangelogConfigurationInCatalog>.Each(
		m => m.Configuration.Bundle?.Releases?.Github ?? new Dictionary<string, string>(),
		(m, release) => ReleaseProfileProblem(m, release, "github")
	).Describe("Every bundle.releases.github tag maps to a defined profile.");

	public static Constraint<ChangelogConfigurationInCatalog> ReleaseProductProfileDefined
	{
		get;
	} = Constraints<ChangelogConfigurationInCatalog>.EachCheck(
		m => m.Configuration.Bundle?.Releases?.Products ?? new Dictionary<string, string>(),
		(m, release) => ReleaseProductProblems(m, release)
	).Describe("Every bundle.releases.products product is in products.yml and maps to a defined profile.");

	public static Constraint<ChangelogConfigurationInCatalog> BundleRulesProductsExclusive
	{
		get;
	} = Constraints<ChangelogConfigurationInCatalog>.Always(
		m => BundleRules(m) is not { ExcludeProducts.Count: > 0, IncludeProducts.Count: > 0 },
		"rules.bundle: cannot have both 'exclude_products' and 'include_products'. Use one or the other."
	).Describe("rules.bundle sets exclude_products or include_products, not both.");

	public static Constraint<ChangelogConfigurationInCatalog> BundleRulesProductsKnown
	{
		get;
	} = Constraints<ChangelogConfigurationInCatalog>.Each(
		BundleRuleProductReferences,
		(m, r) => m.IsKnownProduct(r.Id)
			? null
			: r.IsProductRule
				? $"rules.bundle.products: '{r.Id}' not in available products. Available: {m.AvailableProducts}"
				: $"{r.Path}: '{r.Id}' is not in the list of available products. Available products: {m.AvailableProducts}"
	).Describe("Every product in rules.bundle is in products.yml.");

	public static Constraint<ChangelogConfigurationInCatalog> PerProductProductsExclusive
	{
		get;
	} = Constraints<ChangelogConfigurationInCatalog>.Each(
		PerProductRules,
		kv => kv.Value is not { ExcludeProducts.Count: > 0, IncludeProducts.Count: > 0 },
		kv => $"rules.bundle.products.{kv.Key}: cannot have both 'exclude_products' and 'include_products'. Use one or the other."
	).Describe("No per-product bundle rule sets both exclude_products and include_products.");

	public static Constraint<ChangelogConfigurationInCatalog> PerProductIncludeSelective
	{
		get;
	} = Constraints<ChangelogConfigurationInCatalog>
		.Each(
			PerProductRules,
			kv => kv.Value is not { MatchProducts: MatchMode.Any, IncludeProducts.Count: > 0 },
			kv => $"Configuration pattern 'match_products: any' with 'include_products' in per-product rule '{kv.Key}' provides no selective filtering. " +
				"Consider 'match_products: all' for strict filtering or 'exclude_products' for exclusion-based filtering. " +
				"Refer to https://github.com/elastic/docs-builder/blob/main/docs/contribute/configure-changelogs-ref.md"
		)
		.WithSeverity(Severity.Warning)
		.Describe("A per-product bundle rule with include_products does not match any product.");

	public static Constraint<ChangelogConfigurationInCatalog> PerProductIncludeNotDisjoint
	{
		get;
	} = Constraints<ChangelogConfigurationInCatalog>
		.Each(PerProductRules, kv => DisjointProductProblem(kv.Key, kv.Value))
		.WithSeverity(Severity.Hint)
		.Describe("A per-product bundle rule includes only its own product.");

	public static Constraint<ChangelogConfigurationInCatalog> CreateRulesProductsKnown
	{
		get;
	} = Constraints<ChangelogConfigurationInCatalog>.Each(
		m => CreateRuleProducts(m.Configuration.Rules?.Create, "rules.create"),
		(m, r) => m.IsKnownProduct(r.Product)
			? null
			: $"{r.Path}.products: '{r.Product}' not in available products. Available: {m.AvailableProducts}"
	).Describe("Every product key in rules.create is in products.yml.");

	public static Constraint<ChangelogConfigurationInCatalog> FilenameKeyedByPr
	{
		get;
	} = Constraints<ChangelogConfigurationInCatalog>.OneOf(
		m => m.Configuration.Filename,
		[FilenameStrategy.Pr],
		describe: (strategy, _) =>
			$"filename: '{strategy.ToStringFast(true)}' is no longer supported. Changelog entries are keyed by PR number; for items with no PR use 'changelog note'."
	).Describe("Changelog entries are keyed by pull request.");

	public static Constraint<ChangelogConfigurationInCatalog> PivotLabelUnique { get; } = Constraints<ChangelogConfigurationInCatalog>
		.Consistent(
			LabelMappings,
			x => (x.Section, x.Label),
			x => x.Owner,
			new SectionLabelComparer(),
			StringComparer.Ordinal,
			describe: (key, owners) =>
				$"Label '{key.Label}' is mapped in {key.Section} to {string.Join(", ", owners.Select(o => $"'{o}'"))}; only the last one wins."
		)
		.WithSeverity(Severity.Warning)
		.Describe(
			"R13: A label is mapped to one pivot type, one pivot product and one pivot feature; mapped twice, the last wins (areas allow many)."
		);

	public static Constraint<ChangelogConfigurationInCatalog> BundleRuleTypesAreasValid
	{
		get;
	} = Constraints<ChangelogConfigurationInCatalog>
		.Each(BlockerValues, (m, v) => BlockerProblem(m, v))
		.WithSeverity(Severity.Warning)
		.Describe("R17: The type and area values of rules.bundle (and its per-product rules) are configured types and areas.");

	public static Constraint<ChangelogConfigurationInCatalog> OutputDirectoryNotFile
	{
		get;
	} = Constraints<ChangelogConfigurationInCatalog>.Each(
		OutputDirectories,
		d => IsYamlFilePath(d.Directory)
			? d.Profile is null
				? $"bundle.output_directory '{d.Directory}' must be a directory, not a file path. The bundle file name is derived by convention."
				: $"Profile '{d.Profile}': 'output_directory' must be a directory, not a file path. The bundle file name is derived by convention as '{{repo}}-{{product}}-{{version}}.yaml'."
			: null
	).Describe("R26: A bundle output_directory is a directory, not a .yml or .yaml file path.");

	public static Constraint<ChangelogConfigurationInCatalog> ProfileOutputRemoved { get; } = Constraints<ChangelogConfigurationInCatalog>
		.Each(Profiles, ProfileOutputProblem)
		.WithSeverity(Severity.Warning)
		.Describe(
			"R27: No bundle profile sets 'output': it is removed, and only rejected (as an error) when a bundle is made; as a model rule it warns so a config that loads today still loads."
		);

	public static Constraint<ChangelogConfigurationInCatalog> ProfilePrimaryProductUnique
	{
		get;
	} = Constraints<ChangelogConfigurationInCatalog>.Unique(
		m => Profiles(m).Select(kv => (Name: kv.Key, Product: PrimaryProduct(kv.Value))).Where(p => !string.IsNullOrWhiteSpace(p.Product)),
		p => p.Product!,
		StringComparer.OrdinalIgnoreCase,
		describeGroup: (
			product,
			profiles
		) => $"Profiles '{string.Join("', '", profiles.Select(p => p.Name).Order(StringComparer.Ordinal))}' all resolve to the same '{{repo}}-{product}-{{version}}.yaml' bundle target. " +
			"Each profile must target a distinct product."
	).Describe("R29: No two bundle profiles share a primary product, since the bundle file name is derived from it.");

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

	private static IEnumerable<Violation> DeprecatedProfileFields(string name, BundleProfile profile)
	{
		if (!string.IsNullOrWhiteSpace(profile.Source))
			yield return Violation.Warning(
				$"bundle.profiles.{name}.source is deprecated. " + "Use bundle.releases.github to map release tags to profiles instead."
			);
		if (!string.IsNullOrWhiteSpace(profile.OutputProducts))
			yield return Violation.Warning($"bundle.profiles.{name}.output_products is deprecated. Use 'product: <id>' instead.");
		if (!string.IsNullOrWhiteSpace(profile.OutputDirectory))
			yield return Violation.Warning(
				$"bundle.profiles.{name}.output_directory is deprecated. The output directory is derived automatically as bundle.output_directory/{{product}}."
			);
	}
#pragma warning restore CS0618

#pragma warning disable CS0618 // the rules read the obsolete profile fields
	private static IEnumerable<(string? Profile, string? Directory)> OutputDirectories(ChangelogConfigurationInCatalog m)
	{
		yield return (null, m.Configuration.Bundle?.OutputDirectory);
		foreach (var (name, profile) in Profiles(m))
			yield return (name, profile.OutputDirectory);
	}

	private static bool IsYamlFilePath(string? path) =>
		!string.IsNullOrWhiteSpace(path)
			&& (path.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".yml", StringComparison.OrdinalIgnoreCase));

	private static string? ProfileOutputProblem(KeyValuePair<string, BundleProfile> profile) =>
		string.IsNullOrWhiteSpace(profile.Value.Output)
			? null
			: $"Profile '{profile.Key}': 'output' is no longer supported. Remove it: bundle output names are derived by convention as '{{repo}}-{{product}}-{{version}}.yaml'.";

	// The primary product is `product`, else the first product id of the (deprecated) output_products or the products pattern.
	private static string? PrimaryProduct(BundleProfile profile)
	{
		if (!string.IsNullOrWhiteSpace(profile.Product))
			return profile.Product;
		var pattern = profile.OutputProducts ?? profile.Products;
		if (string.IsNullOrWhiteSpace(pattern))
			return null;
		var group = pattern.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();
		var id = group?.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
		return !string.IsNullOrWhiteSpace(id) && id != "*" ? id : null;
	}
#pragma warning restore CS0618

	private static string[] SplitLabels(string? labels) =>
		labels?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [];

	private static IEnumerable<(string Section, string Owner, string Label)> LabelMappings(ChangelogConfigurationInCatalog m) =>
		m.Configuration.Pivot is not { } pivot
			? []
			: Mappings("pivot.types", (pivot.Types ?? []).Select(kv => (Owner: kv.Key, kv.Value?.Labels)))
				.Concat(Mappings("pivot.products", (pivot.Products ?? []).Select(kv => (Owner: kv.Key, Labels: kv.Value))))
				.Concat(Mappings("pivot.features", (pivot.Features ?? []).Select(kv => (Owner: kv.Key, Labels: kv.Value))));

	private static IEnumerable<(string Section, string Owner, string Label)> Mappings(
		string section,
		IEnumerable<(string Owner, string? Labels)> items
	) => items.SelectMany(i => SplitLabels(i.Labels).Select(label => (section, i.Owner, label)));

	// A label is the same label whatever its case, within one section.
	private sealed class SectionLabelComparer : IEqualityComparer<(string Section, string Label)>
	{
		public bool Equals((string Section, string Label) x, (string Section, string Label) y) =>
			x.Section == y.Section && string.Equals(x.Label, y.Label, StringComparison.OrdinalIgnoreCase);

		public int GetHashCode((string Section, string Label) key) =>
			HashCode.Combine(key.Section, StringComparer.OrdinalIgnoreCase.GetHashCode(key.Label));
	}

	// Every type and area a publish blocker names, with where it is named. Areas are only held to a configured list when there is one.
	private static IEnumerable<(string Path, bool IsType, string Value)> BlockerValues(ChangelogConfigurationInCatalog m)
	{
		if (BundleRules(m) is not { } rules)
			yield break;
		foreach (var value in BlockerValuesOf(m, rules.Blocker, "rules.bundle"))
			yield return value;
		foreach (var (product, rule) in rules.ByProduct ?? new Dictionary<string, BundlePerProductRule>())
		{
			foreach (var value in BlockerValuesOf(m, rule.Blocker, $"rules.bundle.products.{product}"))
				yield return value;
		}
	}

	private static IEnumerable<(string Path, bool IsType, string Value)> BlockerValuesOf(
		ChangelogConfigurationInCatalog m,
		PublishBlocker? blocker,
		string path
	)
	{
		foreach (var type in blocker?.Types ?? [])
			yield return (path, true, type);
		if (m.Configuration.Areas is not { Count: > 0 })
			yield break;
		foreach (var area in blocker?.Areas ?? [])
			yield return (path, false, area);
	}

	private static string? BlockerProblem(ChangelogConfigurationInCatalog m, (string Path, bool IsType, string Value) v)
	{
		var configured = v.IsType ? m.Configuration.Types : m.Configuration.Areas ?? [];
		return configured.Contains(v.Value, StringComparer.OrdinalIgnoreCase)
			? null
			: $"{v.Path}: {(v.IsType ? "type" : "area")} '{v.Value}' is not a configured {(v.IsType ? "type" : "area")}. Configured {(v.IsType ? "types" : "areas")}: {string.Join(", ", configured)}";
	}

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

	private static string? DisjointProductProblem(string product, BundlePerProductRule rule)
	{
		if (rule.IncludeProducts is not { Count: > 1 } include)
			return null;
		var disjoint = include.Where(p => !string.Equals(p, product, StringComparison.OrdinalIgnoreCase)).ToList();
		return disjoint.Count == 0
			? null
			: $"Per-product rule '{product}' includes disjoint products [{string.Join(", ", disjoint)}] "
				+ "which cannot be included due to single-product rule resolution. "
				+ "Use separate bundles (each with a single product in output_products or profile output_products), or multi-product changelogs instead. "
				+ "Refer to https://github.com/elastic/docs-builder/blob/main/docs/contribute/configure-changelogs.md";
	}

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
