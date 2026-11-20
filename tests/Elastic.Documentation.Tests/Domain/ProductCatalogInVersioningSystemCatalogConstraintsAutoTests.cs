// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using AwesomeAssertions;
using Elastic.Documentation.Catalog;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Testing;
using Elastic.Documentation.Versions;
using Nullean.Entities.CsCheck;
using Nullean.Snapshots;

namespace Elastic.Documentation.Tests.Domain;

public class ProductCatalogInVersioningSystemCatalogConstraintsAutoTests
{
	public static IEnumerable<Func<AxisCase>> Axes() =>
		AutoTest
			.For(ProductCatalogInVersioningSystemCatalogConstraints.Instance, CatalogGen.LawfulProductsInVersioning)
			.Seed(RunSeed.Value)
			.CounterExamples(CounterExamples)
			.AsDataSource();

	private static readonly IReadOnlyDictionary<string, Func<ProductCatalogInVersioningSystemCatalog, ProductCatalogInVersioningSystemCatalog>> CounterExamples = new Dictionary<string, Func<ProductCatalogInVersioningSystemCatalog, ProductCatalogInVersioningSystemCatalog>>
	{
		["product-versioning-system-defined"] =
			pair =>
				pair with
				{
					VersioningSystems = new VersioningSystemCatalog
					{
						VersioningSystems = new Dictionary<VersioningSystemId, VersioningSystem>()
					},
					Products = new ProductCatalog
					{
						Products = new Dictionary<string, Product>
						{
							["a"] = new()
							{
								Id = "a",
								DisplayName = "A",
								VersioningSystem = new VersioningSystem
								{
									Id = VersioningSystemId.Stack,
									Base = new SemVersion(1, 0, 0),
									Current = new SemVersion(2, 0, 0)
								}
							}
						}.ToFrozenDictionary()
					}
				}
	};

	/// <summary>
	/// Law: every rule of the products of a catalog and the versioning systems they point at holds for every value the generator builds (hostile text, undefined enum values, keys that differ only in case or
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
			ProductCatalogInVersioningSystemCatalog.Constraints,
			CatalogGen.HostileProductsInVersioning,
			"products.yml"
		);

	/// <summary>
	/// Law: the same holds for the nulls the type annotations forbid (a null entry in a collection or dictionary, a null nested record, a missing
	/// collection): a loader never produces them, a hand-built value can.
	/// </summary>
	[Test]
	public void Constraints_NullsTheAnnotationsForbid_ReportWithoutThrowing() =>
		CatalogLaw.ReportsWithoutThrowing(
			ProductCatalogInVersioningSystemCatalog.Constraints,
			CatalogGen.NullProductsInVersioning,
			"products.yml"
		);

	[Test]
	public void Operations_AreNone() => CatalogLaw.OperationsOf<ProductCatalogInVersioningSystemCatalog>().Should().BeEmpty();

	// Found by generated data: the rule read a missing catalog, a null product and a null dictionary, and threw.
	[Test]
	public void ProductVersioningSystemDefined_MissingCatalogs_ReportNothing() =>
		ProductCatalogInVersioningSystemCatalog
			.Constraints
			.Report(new ProductCatalogInVersioningSystemCatalog(null!, null!))
			.Violations
			.Should()
			.BeEmpty();

	[Test]
	public void ProductVersioningSystemDefined_NullProduct_ReportsNothing() =>
		ProductCatalogInVersioningSystemCatalog
			.Constraints
			.Report(new ProductCatalogInVersioningSystemCatalog(
				new ProductCatalog { Products = new Dictionary<string, Product> { ["a"] = null! }.ToFrozenDictionary() },
				new VersioningSystemCatalog { VersioningSystems = new Dictionary<VersioningSystemId, VersioningSystem>() }
			))
			.Violations
			.Should()
			.BeEmpty();

	[Test]
	public void ProductVersioningSystemDefined_UndefinedEnumValue_IsNamedByItsNumber() =>
		ProductCatalogInVersioningSystemCatalog
			.Constraints
			.Report(Pair(new VersioningSystem
			{
				Id = (VersioningSystemId)12345,
				Base = new SemVersion(1, 0, 0),
				Current = new SemVersion(1, 0, 0)
			}))
			.ToDiagnostics("products.yml")
			.ToSnapshotText()
			.Should()
			.MatchInlineSnapshot("error products.yml Product 'a' uses versioning system '12345', which is not defined in versions.yml.");

	private static ProductCatalogInVersioningSystemCatalog Pair(VersioningSystem used) =>
		new(
			new ProductCatalog
			{
				Products = new Dictionary<string, Product>
				{
					["a"] = new() { Id = "a", DisplayName = "A", VersioningSystem = used }
				}.ToFrozenDictionary()
			},
			new VersioningSystemCatalog { VersioningSystems = new Dictionary<VersioningSystemId, VersioningSystem>() }
		);
}
