// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Catalog;
using CsCheck;
using Nullean.Entities.CsCheck;
using Elastic.Documentation.ReleaseNotes;

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

#pragma warning disable CS0618 // the rules still check the obsolete fields
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
#pragma warning disable CS0618 // the rules still check the obsolete fields
			["link-allow-repos-owner-repo"] = c => WithBundle(c, b => b with { LinkAllowRepos = ["elastic"] }),
#pragma warning restore CS0618
			["bundle-rules-products-exclusive"] =
				c => WithBundleRules(c, BundleRules(PerProductRule, includeProducts: ["elasticsearch"], excludeProducts: ["kibana"])),
			["per-product-products-exclusive"] =
				c => WithBundleRules(
					c,
					BundleRules(PerProductRule with { ExcludeProducts = ["kibana"] }, includeProducts: ["elasticsearch"])
				),
			["per-product-include-selective"] =
				c => WithBundleRules(
					c,
					BundleRules(PerProductRule with { MatchProducts = MatchMode.Any }, includeProducts: ["elasticsearch"])
				),
			["per-product-include-not-disjoint"] =
				c => WithBundleRules(
					c,
					BundleRules(PerProductRule with { IncludeProducts = ["elasticsearch", "kibana"] }, includeProducts: ["elasticsearch"])
				),
			["pivot-label-unique"] =
				c => WithPivot(c, p => p with { Features = new() { ["feature:a"] = "shared", ["feature:b"] = "shared" } }),
			["bundle-rule-types-areas-valid"] =
				c => WithBundleRules(
					c,
					BundleRules(PerProductRule, includeProducts: ["elasticsearch"]) with
					{
						Blocker = new PublishBlocker { Types = ["nope"] }
					}
				),
			["output-directory-not-file"] = c => WithBundle(c, b => b with { OutputDirectory = "docs/releases/bundle.yml" }),
#pragma warning disable CS0618 // the rules still check the obsolete fields
#pragma warning restore CS0618
			["profile-primary-product-unique"] =
				c => WithBundle(
					c,
					b =>
						b with
						{
							Profiles = new Dictionary<string, BundleProfile>
							{
								["release"] = new BundleProfile { Product = "elasticsearch" },
								["other"] = new BundleProfile { Product = "elasticsearch" }
							}
						}
				)
		};

	// Stays hand-written (5a: not generated). Generated configurations (GenAuto at depth 6, so the sections are not empty) break 18 of the 31 rules at once, because the rules are referential
	// across Pivot, Bundle, Rules and ProductsConfiguration (a profile names a product of the catalog, a release names a profile, a rule names a product): a valid configuration is a
	// consistent web of ids, and a normalizer that builds one is the loader's resolution logic written again. A generator for it would be model-first (a chosen product subset, then the
	// sections built around it), not a repair of generated members. (At the default depth the sections generate empty, so those rules would only ever be checked against nothing.)
	[Test]
	[MethodDataSource(nameof(Axes))]
	public void EveryDeclaredAxisHolds(AxisCase axis) => axis.Run();

	public static IEnumerable<Func<AxisCase>> Axes() =>
		DomainAutoTest.For(ChangelogConfigurationConstraints.All, Gen.Const(0).Select(_ => Valid())).CounterExamples(CounterExamples).Axes();

	private static ChangelogConfigurationInCatalog Valid() => new(ValidConfiguration(), Products);

	private static IReadOnlyDictionary<string, Func<ChangelogConfigurationInCatalog, ChangelogConfigurationInCatalog>> CounterExamples =>
		ConfigurationCounterExamples.ToDictionary(
			kv => kv.Key,
			kv =>
				(Func<ChangelogConfigurationInCatalog, ChangelogConfigurationInCatalog>)(m =>
					m with { Configuration = kv.Value(m.Configuration) })
		);
}
