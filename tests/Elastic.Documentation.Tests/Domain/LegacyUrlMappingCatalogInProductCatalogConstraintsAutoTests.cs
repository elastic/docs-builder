// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using AwesomeAssertions;
using Elastic.Documentation.Catalog;
using Nullean.Entities.CsCheck;

namespace Elastic.Documentation.Tests.Domain;

public class LegacyUrlMappingCatalogInProductCatalogConstraintsAutoTests
{
	public static IEnumerable<Func<AxisCase>> Axes() =>
		AutoTest
			.For(LegacyUrlMappingCatalogInProductCatalogConstraints.Instance, CatalogGen.LawfulLegacyInProducts)
			.Seed(RunSeed.Value)
			.CounterExamples(CounterExamples)
			.AsDataSource();

	private static readonly IReadOnlyDictionary<string, Func<LegacyUrlMappingCatalogInProductCatalog, LegacyUrlMappingCatalogInProductCatalog>> CounterExamples = new Dictionary<string, Func<LegacyUrlMappingCatalogInProductCatalog, LegacyUrlMappingCatalogInProductCatalog>>
	{
		["legacy-mapping-product-defined"] =
			pair =>
				pair with
				{
					LegacyUrlMappings = new LegacyUrlMappingCatalog
					{
						Mappings =
						[
							.. pair.LegacyUrlMappings.Mappings,
							new LegacyUrlMapping
							{
								BaseUrl = "en/missing/",
								Product = new Product { Id = "missing", DisplayName = "Missing" },
								LegacyVersions = []
							}
						]
					}
				}
	};

	/// <summary>
	/// Law: every rule of the legacy url mappings of a catalog and the products they point at holds for every value the generator builds (hostile text, undefined enum values, keys that differ only in case or
	/// from the value's own id, versions out of order, empty collections), and every rule can be broken by some value.
	/// Consequence if broken: a rule that cannot fire, or data that never reaches it, reports green while checking nothing. The record has no
	/// operations, so the idempotency, monotonicity, conservation and confluence axes are not applicable.
	/// </summary>
	[Test]
	[MethodDataSource(nameof(Axes))]
	public Task EveryDeclaredAxisHolds(AxisCase axis) => axis.RunAsync();

	/// <summary>
	/// Law: the rule set reports a violation list for every value the type allows, the ones that break a rule included, and writing it as
	/// diagnostics does not throw.
	/// Consequence if broken: a hand-built value turns a finding into an exception.
	/// </summary>
	[Test]
	public void Constraints_AnyValueTheTypeAllows_ReportWithoutThrowing() =>
		CatalogLaw.ReportsWithoutThrowing(
			LegacyUrlMappingCatalogInProductCatalog.Constraints,
			CatalogGen.HostileLegacyInProducts,
			"legacy-url-mappings.yml"
		);

	/// <summary>
	/// Law: the same holds for the nulls the type annotations forbid (a null entry in a collection or dictionary, a null nested record, a missing
	/// collection): a loader never produces them, a hand-built value can.
	/// </summary>
	[Test]
	public void Constraints_NullsTheAnnotationsForbid_ReportWithoutThrowing() =>
		CatalogLaw.ReportsWithoutThrowing(
			LegacyUrlMappingCatalogInProductCatalog.Constraints,
			CatalogGen.NullLegacyInProducts,
			"legacy-url-mappings.yml"
		);

	[Test]
	public void Operations_AreNone() => CatalogLaw.OperationsOf<LegacyUrlMappingCatalogInProductCatalog>().Should().BeEmpty();

	// Found by generated data: the rule read a missing catalog, a null mapping and a mapping without a product, and threw.
	[Test]
	public void LegacyMappingProductDefined_MissingCatalogs_ReportNothing() =>
		LegacyUrlMappingCatalogInProductCatalog
			.Constraints
			.Report(new LegacyUrlMappingCatalogInProductCatalog(null!, null!))
			.Violations
			.Should()
			.BeEmpty();

	[Test]
	public void LegacyMappingProductDefined_NullMapping_ReportsNothing() =>
		LegacyUrlMappingCatalogInProductCatalog.Constraints.Report(Pair(null!)).Violations.Should().BeEmpty();

	[Test]
	public void LegacyMappingProductDefined_MappingWithoutProduct_ReportsNothing() =>
		LegacyUrlMappingCatalogInProductCatalog
			.Constraints
			.Report(Pair(new LegacyUrlMapping { BaseUrl = "en/x/", Product = null!, LegacyVersions = [] }))
			.Violations
			.Should()
			.BeEmpty();

	private static LegacyUrlMappingCatalogInProductCatalog Pair(LegacyUrlMapping mapping) =>
		new(new LegacyUrlMappingCatalog { Mappings = [mapping] }, new ProductCatalog { Products = FrozenDictionary<string, Product>.Empty });
}
