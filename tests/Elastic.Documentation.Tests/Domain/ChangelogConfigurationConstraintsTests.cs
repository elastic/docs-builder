// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using CsCheck;
using Elastic.Documentation.Catalog;
using Elastic.Documentation.ReleaseNotes;
using Nullean.Entities.CsCheck;

namespace Elastic.Documentation.Tests.Domain;

/// <summary>
/// The changelog configuration constraints are checked on configurations built directly, with no loader involved. The
/// valid configuration sets every section a rule looks at, so each counter-example changes one value and breaks one rule.
/// </summary>
public class ChangelogConfigurationConstraintsTests
{
	private static ProductCatalog Products =>
		ProductCatalog.FromPublicProducts(new Dictionary<string, Product>
		{
			["elasticsearch"] = new() { Id = "elasticsearch", DisplayName = "Elasticsearch" },
			["kibana"] = new() { Id = "kibana", DisplayName = "Kibana" }
		});

	private static string BreakingChange => ChangelogEntryType.BreakingChange.ToStringFast(true);

	private static string Subtype => ChangelogConfiguration.DefaultSubtypes[0];

	private static Dictionary<string, TypeEntry?> Types() =>
		ChangelogConfiguration.DefaultTypes.ToDictionary(
			t => t,
			t => t == BreakingChange ? new TypeEntry { Subtypes = new() { [Subtype] = null } } : null
		);

	private static BundlePerProductRule PerProductRule => new() { IncludeProducts = ["elasticsearch"], MatchProducts = MatchMode.All };

	private static ChangelogConfiguration ValidConfiguration() =>
		ChangelogConfiguration.Default with
		{
			Pivot = new PivotConfiguration
			{
				Types = Types(),
				Subtypes = new() { [Subtype] = null },
				Products = new() { ["elasticsearch 9.0"] = "elasticsearch" }
			},
			ProductsConfiguration = new ProductsConfig
			{
				Available = ["elasticsearch"],
				Default = [new DefaultProduct { Product = "elasticsearch" }]
			},
			Bundle = Bundle(new BundleProfile { Product = "elasticsearch" }),
			Rules = new RulesConfiguration
			{
				Create = new CreateRules { ByProduct = new Dictionary<string, CreateRules> { ["elasticsearch"] = new() } },
				Bundle = BundleRules(PerProductRule, includeProducts: ["elasticsearch"])
			}
		};

#pragma warning disable CS0618 // the rules check the obsolete fields
	private static BundleConfiguration Bundle(BundleProfile profile) =>
		new()
		{
			Repo = "elasticsearch",
			LinkAllowRepos = ["elastic/elasticsearch"],
			Profiles = new Dictionary<string, BundleProfile> { ["release"] = profile },
			Releases = new BundleReleases
			{
				Github = new Dictionary<string, string> { ["v*"] = "release" },
				Products = new Dictionary<string, string> { ["elasticsearch"] = "release" }
			}
		};
#pragma warning restore CS0618

	private static BundleRules BundleRules(
		BundlePerProductRule rule,
		IReadOnlyList<string>? includeProducts = null,
		IReadOnlyList<string>? excludeProducts = null
	) =>
		new()
		{
			IncludeProducts = includeProducts,
			ExcludeProducts = excludeProducts,
			ByProduct = new Dictionary<string, BundlePerProductRule> { ["elasticsearch"] = rule }
		};

	private static ChangelogConfiguration WithPivot(ChangelogConfiguration c, Func<PivotConfiguration, PivotConfiguration> change) =>
		c with { Pivot = change(c.Pivot!) };

	private static ChangelogConfiguration WithBundle(ChangelogConfiguration c, Func<BundleConfiguration, BundleConfiguration> change) =>
		c with { Bundle = change(c.Bundle!) };

	private static ChangelogConfiguration WithBundleRules(ChangelogConfiguration c, BundleRules rules) =>
		c with { Rules = c.Rules! with { Bundle = rules } };

	private static ChangelogConfiguration WithProfile(ChangelogConfiguration c, BundleProfile profile) =>
		WithBundle(c, b => b with { Profiles = new Dictionary<string, BundleProfile> { ["release"] = profile } });

	private static Dictionary<string, Func<ChangelogConfiguration, ChangelogConfiguration>> ConfigurationCounterExamples =>
		new()
		{
			["pivot-types-valid"] = c => WithPivot(c, p => p with { Types = new(Types()) { ["nope"] = null } }),
			["pivot-required-types-present"] =
				c => WithPivot(
					c,
					p => p with { Types = Types().Where(kv => kv.Key != ChangelogEntryType.BugFix.ToStringFast(true)).ToDictionary() }
				),
			["subtypes-only-under-breaking-change"] =
				c => WithPivot(
					c,
					p =>
						p with
						{
							Types = new(Types())
							{
								[ChangelogEntryType.Feature.ToStringFast(true)] = new TypeEntry { Subtypes = new() { [Subtype] = null } }
							}
						}
				),
			["type-subtypes-valid"] =
				c => WithPivot(
					c,
					p => p with { Types = new(Types()) { [BreakingChange] = new TypeEntry { Subtypes = new() { ["nope"] = null } } } }
				),
#pragma warning disable CS0618 // the rules check the obsolete fields
			["link-allow-repos-owner-repo"] = c => WithBundle(c, b => b with { LinkAllowRepos = ["elastic"] }),
#pragma warning restore CS0618
			["bundle-rules-products-exclusive"] =
				c => WithBundleRules(c, BundleRules(PerProductRule, includeProducts: ["elasticsearch"], excludeProducts: ["kibana"])),
			["per-product-products-exclusive"] =
				c => WithBundleRules(
					c,
					BundleRules(PerProductRule with { ExcludeProducts = ["kibana"] }, includeProducts: ["elasticsearch"])
				),
		};

	// Stays hand-written: the rules are referential across Pivot, Bundle, Rules and ProductsConfiguration (a profile names a product of the
	// catalog, a release names a profile, a rule names a product), so a valid configuration is a consistent web of ids, and a generator for it
	// would be the loader's resolution logic written again.
	[Test]
	[MethodDataSource(nameof(Axes))]
	public Task EveryDeclaredAxisHolds(AxisCase axis) => axis.RunAsync();

	public static IEnumerable<Func<AxisCase>> Axes() =>
		AutoTest
			.For(ChangelogConfigurationConstraints.All, Gen.Const(0).Select(_ => Valid()))
			.Seed(RunSeed.Value)
			.CounterExamples(CounterExamples)
			.AsDataSource();

	/// <summary>The text of each error is what the loader has always reported, so a configuration built any other way gets the same message.</summary>
	[Test]
	[MethodDataSource(nameof(Messages))]
	public void BrokenConfiguration_ReportsTheLoadersMessage(MessageCase message) =>
		ChangelogConfigurationConstraints
			.All
			.Report(new ChangelogConfigurationInCatalog(message.Break(ValidConfiguration()), Products))
			.Violations
			.Select(v => v.Message)
			.Should()
			.Equal(message.Expected);

	public static IEnumerable<Func<MessageCase>> Messages() =>
		new[]
		{
			new MessageCase(
				"pivot-types-valid",
				c => WithPivot(c, p => p with { Types = new(Types()) { ["nope"] = null } }),
				$"Type 'nope' in pivot.types is not a valid type. Valid types: {string.Join(", ", ChangelogConfiguration.DefaultTypes)}"
			),
			new MessageCase(
				"pivot-required-types-present",
				c => WithPivot(
					c,
					p => p with { Types = Types().Where(kv => kv.Key != ChangelogEntryType.BugFix.ToStringFast(true)).ToDictionary() }
				),
				$"Required type 'bug-fix' is missing from pivot.types. Required types: feature, bug-fix, breaking-change"
			),
			new MessageCase(
				"subtypes-only-under-breaking-change",
				c => WithPivot(
					c,
					p =>
						p with
						{
							Types = new(Types())
							{
								[ChangelogEntryType.Feature.ToStringFast(true)] = new TypeEntry { Subtypes = new() { [Subtype] = null } }
							}
						}
				),
				"Type 'feature' has subtypes defined, but subtypes are only allowed for 'breaking-change' type."
			),
			new MessageCase(
				"type-subtypes-valid",
				c => WithPivot(
					c,
					p => p with { Types = new(Types()) { [BreakingChange] = new TypeEntry { Subtypes = new() { ["nope"] = null } } } }
				),
				$"Subtype 'nope' in pivot.types.breaking-change.subtypes is not a valid subtype. Valid subtypes: {string.Join(", ", ChangelogConfiguration.DefaultSubtypes)}"
			),
			new MessageCase(
				"pivot-subtypes-valid",
				c => WithPivot(c, p => p with { Subtypes = new() { ["nope"] = null } }),
				$"Subtype 'nope' in pivot.subtypes is not a valid subtype. Valid subtypes: {string.Join(", ", ChangelogConfiguration.DefaultSubtypes)}"
			),
			new MessageCase(
				"pivot-products-known",
				c => WithPivot(c, p => p with { Products = new() { ["unknown_product 9.0"] = "label" } }),
				"Product 'unknown_product' in pivot.products is not in the list of available products from config/products.yml. Available products: elasticsearch, kibana"
			),
			new MessageCase(
				"available-products-known",
				c => c with { ProductsConfiguration = new ProductsConfig { Available = ["nope"] } },
				"Product 'nope' in changelog.yml is not in the list of available products from config/products.yml. Available products: elasticsearch, kibana"
			),
			new MessageCase(
				"default-product-has-id",
				c => c with { ProductsConfiguration = new ProductsConfig { Default = [new DefaultProduct { Product = "" }] } },
				"Default product in products_config.default must have a product ID"
			),
			new MessageCase(
				"default-product-known",
				c => c with { ProductsConfiguration = new ProductsConfig { Default = [new DefaultProduct { Product = "nope" }] } },
				"Product 'nope' in products_config.default is not in the list of available products from config/products.yml. Available products: elasticsearch, kibana"
			),
#pragma warning disable CS0618 // the rules check the obsolete fields
			new MessageCase(
				"bundle-repo-single",
				c => WithBundle(c, b => b with { Repo = "elastic/a+elastic/b" }),
				"bundle.repo must name a single GitHub repository. Remove '+' merged-repo syntax from bundle.repo."
			),
			new MessageCase(
				"profile-repo-single",
				c => WithProfile(c, new BundleProfile { Product = "elasticsearch", Repo = "a+b" }),
				"bundle.profiles.release.repo must name a single GitHub repository. Remove '+' merged-repo syntax."
			),
			new MessageCase(
				"link-allow-repos-owner-repo",
				c => WithBundle(c, b => b with { LinkAllowRepos = ["elastic"] }),
				"bundle.link_allow_repos: each entry must be exactly 'owner/repo' (one slash). Invalid: 'elastic'."
			),
#pragma warning restore CS0618
			new MessageCase(
				"profile-product-known",
				c => WithProfile(c, new BundleProfile { Product = "nope" }),
				"bundle.profiles.release.product: 'nope' is not in products.yml. Available products: elasticsearch, kibana"
			),
			new MessageCase(
				"release-tag-profile-defined",
				c => WithBundle(
					c,
					b => b with { Releases = b.Releases! with { Github = new Dictionary<string, string> { ["v*"] = "missing" } } }
				),
				"bundle.releases.github['v*']: profile 'missing' is not defined in bundle.profiles."
			),
			new MessageCase(
				"release-product-profile-defined",
				c => WithBundle(
					c,
					b => b with { Releases = b.Releases! with { Products = new Dictionary<string, string> { ["nope"] = "release" } } }
				),
				"bundle.releases.products key 'nope' is not in products.yml. Available products: elasticsearch, kibana"
			),
			new MessageCase(
				"bundle-rules-products-exclusive",
				c => WithBundleRules(c, BundleRules(PerProductRule, includeProducts: ["elasticsearch"], excludeProducts: ["kibana"])),
				"rules.bundle: cannot have both 'exclude_products' and 'include_products'. Use one or the other."
			),
			new MessageCase(
				"bundle-rules-products-known",
				c => WithBundleRules(c, BundleRules(PerProductRule, includeProducts: ["nope"])),
				"rules.bundle.include_products: 'nope' is not in the list of available products. Available products: elasticsearch, kibana"
			),
			new MessageCase(
				"per-product-products-exclusive",
				c => WithBundleRules(
					c,
					BundleRules(PerProductRule with { ExcludeProducts = ["kibana"] }, includeProducts: ["elasticsearch"])
				),
				"rules.bundle.products.elasticsearch: cannot have both 'exclude_products' and 'include_products'. Use one or the other."
			),
			new MessageCase(
				"create-rules-products-known",
				c =>
					c with
					{
						Rules = new RulesConfiguration
						{
							Create = new CreateRules { ByProduct = new Dictionary<string, CreateRules> { ["nope"] = new() } }
						}
					},
				"rules.create.products: 'nope' not in available products. Available: elasticsearch, kibana"
			),
			new MessageCase(
				"filename-keyed-by-pr",
				c => c with { Filename = FilenameStrategy.Timestamp },
				"filename: 'timestamp' is no longer supported. Changelog entries are keyed by PR number; for items with no PR use 'changelog note'."
			)
		}.Select(m => (Func<MessageCase>)(() => m));

	/// <summary>One rule, how to break it, and the one message it reports.</summary>
	public sealed record MessageCase(string Name, Func<ChangelogConfiguration, ChangelogConfiguration> Break, string Expected)
	{
		public override string ToString() => Name;
	}

	private static ChangelogConfigurationInCatalog Valid() => new(ValidConfiguration(), Products);

	private static IReadOnlyDictionary<string, Func<ChangelogConfigurationInCatalog, ChangelogConfigurationInCatalog>> CounterExamples =>
		ConfigurationCounterExamples.ToDictionary(
			kv => kv.Key,
			kv =>
				(Func<ChangelogConfigurationInCatalog, ChangelogConfigurationInCatalog>)(m =>
					m with { Configuration = kv.Value(m.Configuration) })
		);
}
