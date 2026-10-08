// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;
using AwesomeAssertions;
using CsCheck;
using Elastic.Documentation.Catalog;
using Elastic.Documentation.ReleaseNotes;
using Nullean.Entities.CsCheck;

namespace Elastic.Documentation.Tests.Domain;

public class ChangelogEntryConstraintsTests
{
	[Test]
	public void Laws() => ChangelogEntryConstraints.All.Laws(ChangelogEntryExamples.Generated<ChangelogEntry>(), CounterExamples);

	private static ChangelogEntry Valid() => ChangelogEntryExamples.Valid(new ChangelogEntry { Title = "Add a thing" });

	[Test]
	public void AMarkerNamesEachFieldItShouldNotCarry()
	{
		var marker = new ChangelogEntry { Link = "12", Title = "A title", Issues = ["https://github.com/elastic/elasticsearch/issues/2"] };

		var violations = ChangelogEntryConstraints.All.Validate(marker);

		violations
			.Where(v => v.ConstraintId == "marker-carries-only-link")
			.Select(v => v.Member)
			.Should()
			.BeEquivalentTo("title", "issues");
	}

	[Test]
	public void AFileNamedForItsPr_BreaksNoRule()
	{
		var entry = Valid() with { FileName = "14-my-feature.yaml", Pr = "#14" };
		ChangelogEntryConstraints.All.Validate(entry).Should().BeEmpty();
	}

	private static IReadOnlyDictionary<string, Func<ChangelogEntry, ChangelogEntry>> CounterExamples =>
		new Dictionary<string, Func<ChangelogEntry, ChangelogEntry>>
		{
			["entry-title-required"] = e => e with { Title = " " },
			["entry-title-max-80"] = e => e with { Title = new string('x', 81) },
			["entry-description-max-600"] = e => e with { Description = new string('x', 601) },
			["entry-type-required"] = e => e with { Type = ChangelogEntryType.Invalid },
			["entry-subtype-only-breaking-change"] = e => e with { Type = ChangelogEntryType.Feature, Subtype = ChangelogEntrySubtype.Api },
			["entry-products-required"] = e => e with { Products = [] },
			["products-product-id-required"] = e => e with { Products = [new ProductReference { ProductId = null! }] },
			["entry-product-id-required"] = e => e with { Products = [new ProductReference { ProductId = " " }] },
			["entry-product-target-obsolete"] =
				e =>
					e with
					{
						Products = [new ProductReference { ProductId = "elasticsearch", Lifecycle = Lifecycle.Ga, TargetRaw = "9.1.0" }]
					},
			["entry-file-name-starts-with-pr"] = e => e with { FileName = "my-feature.yaml" },
			["entry-pr-matches-file-name"] = e => e with { FileName = "14-my-feature.yaml", Pr = "15" },
			["marker-link-is-pr-number"] = e => new ChangelogEntry { Link = "abc" },
			["marker-carries-only-link"] =
				e => new ChangelogEntry { Link = "12", Issues = ["https://github.com/elastic/elasticsearch/issues/2"] }
		};
}

public class ChangelogEntryInConfigurationConstraintsTests
{
	[Test]
	public void Laws() => ChangelogEntryInConfigurationConstraints.All.Laws(Generated, CounterExamples);

	private static ProductCatalog CatalogOf(IEnumerable<string> ids) =>
		ProductCatalog.FromPublicProducts(
			ids.Distinct(StringComparer.OrdinalIgnoreCase).Select(id => KeyValuePair.Create(id, new Product { Id = id, DisplayName = id }))
		);

	/// <summary>
	/// The configuration an entry is checked against allows what the generated entry uses: its type and subtype, its products' lifecycles, its areas,
	/// and its products are in products.yml and in products.available. The configuration is derived from the entry, so each rule holds by construction
	/// and the counter-examples are the ways to leave one thing out.
	/// </summary>
	private static ChangelogEntryInConfiguration InConfiguration(ChangelogEntryInConfiguration m)
	{
		var e = m.Entry;
		var ids = (e.Products ?? []).Select(p => p.ProductId).ToList();
		var configuration = ChangelogConfiguration.Default with
		{
			Types = [.. ChangelogConfiguration.DefaultTypes.Append(e.Type.ToStringFast(true)).Distinct()],
			SubTypes =
			[
				.. ChangelogConfiguration.DefaultSubtypes.Append(e.Subtype?.ToStringFast(true) ?? "").Where(x => x != "").Distinct()
			],
			Lifecycles =
			[
				.. ChangelogConfiguration
					.DefaultLifecycles
					.Concat((e.Products ?? []).Select(p => p.Lifecycle).OfType<Lifecycle>())
					.Distinct()
			],
			Areas = e.Areas is { Count: > 0 } ? [.. e.Areas.Distinct()] : null,
			ProductsConfiguration = new ProductsConfig { Available = ids }
		};
		return new ChangelogEntryInConfiguration(e, new ChangelogConfigurationInCatalog(configuration, CatalogOf(ids)));
	}

	private static Gen<ChangelogEntryInConfiguration> Generated =>
		ChangelogEntryExamples.Generated<ChangelogEntryInConfiguration>(
			o => o.Use(Gen.Const(new ChangelogConfigurationInCatalog(
				ChangelogConfiguration.Default,
				CatalogOf(["elasticsearch"])
			))).Normalize<ChangelogEntryInConfiguration>(InConfiguration)
		);

	private static ChangelogEntryInConfiguration WithEntry(ChangelogEntryInConfiguration m, Func<ChangelogEntry, ChangelogEntry> change) =>
		m with { Entry = change(m.Entry) };

	private static ChangelogEntryInConfiguration WithConfiguration(
		ChangelogEntryInConfiguration m,
		Func<ChangelogConfiguration, ChangelogConfiguration> change
	) => m with { Configuration = m.Configuration with { Configuration = change(m.Configuration.Configuration) } };

	private static IReadOnlyDictionary<string, Func<ChangelogEntryInConfiguration, ChangelogEntryInConfiguration>> CounterExamples =>
		new Dictionary<string, Func<ChangelogEntryInConfiguration, ChangelogEntryInConfiguration>>
		{
			// widening the available list is not what breaks, so the entry names a product products.yml lacks
			["entry-products-known"] =
				m => WithConfiguration(
					WithEntry(m, e => e with { Products = [new ProductReference { ProductId = "nope" }] }),
					c => c with { ProductsConfiguration = null }
				),
			// kibana is in products.yml (the catalog gains it) but not in products.available
			["entry-products-available"] =
				m => WithEntry(
					m with
					{
						Configuration = m.Configuration with { Products = CatalogOf([.. m.Configuration.Products.Products.Keys, "kibana"]) }
					},
					e => e with { Products = [new ProductReference { ProductId = "kibana" }] }
				),
			["entry-type-allowed"] =
				m => WithConfiguration(
					WithEntry(m, e => e with { Type = ChangelogEntryType.Feature }),
					c =>
						c with
						{
							Types = [.. ChangelogConfiguration.DefaultTypes.Where(t => t != ChangelogEntryType.Feature.ToStringFast(true))]
						}
				),
			["entry-subtype-allowed"] =
				m => WithConfiguration(
					WithEntry(m, e => e with { Type = ChangelogEntryType.BreakingChange, Subtype = ChangelogEntrySubtype.Api }),
					c => c with { SubTypes = [ChangelogEntrySubtype.Behavioral.ToStringFast(true)] }
				),
			["entry-lifecycle-allowed"] =
				m => WithConfiguration(
					WithEntry(m, e => e with { Products = [.. e.Products!.Select(p => p with { Lifecycle = Lifecycle.Ga })] }),
					c => c with { Lifecycles = [Lifecycle.Beta] }
				),
			["entry-areas-allowed"] =
				m => WithConfiguration(WithEntry(m, e => e with { Areas = ["search"] }), c => c with { Areas = ["Search"] })
		};
}
