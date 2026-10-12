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
	[MethodDataSource(nameof(Axes))]
	public void EveryDeclaredAxisHolds(AxisCase axis) => axis.Run();

	public static IEnumerable<Func<AxisCase>> Axes() =>
		DomainAutoTest
			.For(ChangelogEntryConstraints.All, WithMarkers(ChangelogEntryExamples.Generated<ChangelogEntry>()))
			.CounterExamples(OnNonMarkers(CounterExamples))
			.Axes();

	// A marker is a valid entry too, and the only kind the marker rules apply to: without some in the sample those rules are never exercised (axis 09 says so).
	private static Gen<ChangelogEntry> WithMarkers(Gen<ChangelogEntry> entries) =>
		Gen.Frequency((9, entries), (1, Gen.Int[1, 500].Select(pr => ChangelogEntry.Marker(pr).Value!)));

	// A marker is not a real entry (no title, no products), so a counter-example that breaks one entry rule would break the marker rules too: start those from a plain valid entry.
	private static IReadOnlyDictionary<string, Func<ChangelogEntry, ChangelogEntry>> OnNonMarkers(
		IReadOnlyDictionary<string, Func<ChangelogEntry, ChangelogEntry>> cases
	) =>
		cases.ToDictionary(
			c => c.Key,
			c => c.Key.StartsWith("marker", StringComparison.Ordinal)
				? c.Value
				: (Func<ChangelogEntry, ChangelogEntry>)(e => c.Value(e.IsMarker ? Valid() : e))
		);

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
		new Dictionary<string, Func<ChangelogEntry, ChangelogEntry>> { };
}

public class ChangelogEntryInConfigurationConstraintsTests
{
	[Test]
	[MethodDataSource(nameof(Axes))]
	public void EveryDeclaredAxisHolds(AxisCase axis) => axis.Run();

	public static IEnumerable<Func<AxisCase>> Axes() =>
		DomainAutoTest.For(ChangelogEntryInConfigurationConstraints.All, Generated).CounterExamples(CounterExamples).Axes();

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
		new Dictionary<string, Func<ChangelogEntryInConfiguration, ChangelogEntryInConfiguration>> { };
}
