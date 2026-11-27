// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using AwesomeAssertions;
using Elastic.Documentation.Catalog;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Testing;
using Nullean.Entities.CsCheck;
using Nullean.Snapshots;

namespace Elastic.Documentation.Tests.Domain;

public class ProductCatalogConstraintsAutoTests
{
	public static IEnumerable<Func<AxisCase>> Axes() =>
		AutoTest
			.For(ProductCatalogConstraints.Instance, CatalogGen.LawfulProductCatalogs)
			.Seed(RunSeed.Value)
			.CounterExamples(CounterExamples)
			.AsDataSource();

	private static readonly IReadOnlyDictionary<string, Func<ProductCatalog, ProductCatalog>> CounterExamples = new Dictionary<string, Func<ProductCatalog, ProductCatalog>>
	{
		["public-reference-product-has-versioning"] =
			catalog => new ProductCatalog
			{
				Products = catalog
					.Products
					.Append(KeyValuePair.Create("unversioned", new Product { Id = "unversioned", DisplayName = "Unversioned" }))
					.ToFrozenDictionary(p => p.Key, p => p.Value)
			}
	};

	/// <summary>
	/// Law: every rule of a product catalog holds for every value the generator builds (hostile text, undefined enum values, keys that differ only in case or
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
		CatalogLaw.ReportsWithoutThrowing(ProductCatalog.Constraints, CatalogGen.HostileProductCatalogs, "products.yml");

	/// <summary>
	/// Law: the same holds for the nulls the type annotations forbid (a null entry in a collection or dictionary, a null nested record, a missing
	/// collection): a loader never produces them, a hand-built value can.
	/// </summary>
	[Test]
	public void Constraints_NullsTheAnnotationsForbid_ReportWithoutThrowing() =>
		CatalogLaw.ReportsWithoutThrowing(ProductCatalog.Constraints, CatalogGen.NullProductCatalogs, "products.yml");

	[Test]
	public void Operations_AreNone() => CatalogLaw.OperationsOf<ProductCatalog>().Should().BeEmpty();

	// Found by generated data: the rule read a null product and a product without features, and threw.
	[Test]
	public void PublicReferenceProductHasVersioning_NullProduct_ReportsNothing() =>
		ProductCatalog.Constraints.Report(Catalog(null!)).Violations.Should().BeEmpty();

	[Test]
	public void PublicReferenceProductHasVersioning_ProductWithoutFeatures_ReportsNothing() =>
		ProductCatalog
			.Constraints
			.Report(Catalog(new Product { Id = "a", DisplayName = "A", Features = null! }))
			.Violations
			.Should()
			.BeEmpty();

	[Test]
	public void PublicReferenceProductHasVersioning_NoProductDictionary_ReportsTheMissingMember() =>
		ProductCatalog
			.Constraints
			.Report(new ProductCatalog { Products = null! })
			.ToDiagnostics("products.yml")
			.ToSnapshotText()
			.Should()
			.MatchInlineSnapshot("error products.yml The Products field is required.");

	private static ProductCatalog Catalog(Product product) =>
		new() { Products = new Dictionary<string, Product> { ["a"] = product }.ToFrozenDictionary() };
}
