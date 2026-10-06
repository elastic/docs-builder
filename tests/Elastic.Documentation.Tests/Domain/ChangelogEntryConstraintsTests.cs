// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Catalog;
using Elastic.Documentation.ReleaseNotes;
using Nullean.Entities.CsCheck;

namespace Elastic.Documentation.Tests.Domain;

public class ChangelogEntryConstraintsTests
{
	[Test]
	public void Laws() => ChangelogEntryConstraints.All.Laws(Valid(), CounterExamples);

	private static ChangelogEntry Valid() =>
		new()
		{
			Type = ChangelogEntryType.Feature,
			Title = "Add a thing",
			Products = [new ProductReference { ProductId = "elasticsearch", Lifecycle = Lifecycle.Ga }],
			Prs = ["https://github.com/elastic/elasticsearch/pull/1"]
		};

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
			["entry-subtype-only-breaking-change"] = e => e with { Subtype = ChangelogEntrySubtype.Api },
			["entry-products-required"] = e => e with { Products = [] },
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
	public void Laws() => ChangelogEntryInConfigurationConstraints.All.Laws(Valid(), CounterExamples);

	private static ProductCatalog Products =>
		ProductCatalog.FromPublicProducts(new Dictionary<string, Product>
		{
			["elasticsearch"] = new() { Id = "elasticsearch", DisplayName = "Elasticsearch" },
			["kibana"] = new() { Id = "kibana", DisplayName = "Kibana" }
		});

	private static ChangelogEntryInConfiguration Valid() =>
		new(
			new ChangelogEntry
			{
				Type = ChangelogEntryType.Feature,
				Title = "Add a thing",
				Subtype = null,
				Areas = ["Search"],
				Products = [new ProductReference { ProductId = "elasticsearch", Lifecycle = Lifecycle.Ga }]
			},
			new ChangelogConfigurationInCatalog(
				ChangelogConfiguration.Default with
				{
					Areas = ["Search"],
					ProductsConfiguration = new ProductsConfig { Available = ["elasticsearch"] }
				},
				Products
			)
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
			// kibana is in products.yml; widening the available list is not what breaks, so the entry names a product products.yml lacks
			["entry-products-known"] =
				m => WithConfiguration(
					WithEntry(m, e => e with { Products = [new ProductReference { ProductId = "nope" }] }),
					c => c with { ProductsConfiguration = null }
				),
			["entry-products-available"] = m => WithEntry(m, e => e with { Products = [new ProductReference { ProductId = "kibana" }] }),
			["entry-type-allowed"] =
				m => WithConfiguration(
					m,
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
			["entry-lifecycle-allowed"] = m => WithConfiguration(m, c => c with { Lifecycles = [Lifecycle.Beta] }),
			["entry-areas-allowed"] = m => WithEntry(m, e => e with { Areas = ["search"] })
		};
}
