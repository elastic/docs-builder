// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Catalog;
using Nullean.Entities.CsCheck;

namespace Elastic.Documentation.Tests.Domain;

public class LegacyUrlMappingConstraintsAutoTests
{
	public static IEnumerable<Func<AxisCase>> Axes() =>
		AutoTest.For(LegacyUrlMappingConstraints.Instance, CatalogGen.HostileLegacyMappings).Seed(RunSeed.Value).AsDataSource();

	/// <summary>
	/// Law: every rule of a legacy url mapping catalog holds for every value the generator builds (hostile text, undefined enum values, keys that differ only in case or
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
		CatalogLaw.ReportsWithoutThrowing(LegacyUrlMappingCatalog.Constraints, CatalogGen.HostileLegacyMappings, "legacy-url-mappings.yml");

	/// <summary>
	/// Law: the same holds for the nulls the type annotations forbid (a null entry in a collection or dictionary, a null nested record, a missing
	/// collection): a loader never produces them, a hand-built value can.
	/// </summary>
	[Test]
	public void Constraints_NullsTheAnnotationsForbid_ReportWithoutThrowing() =>
		CatalogLaw.ReportsWithoutThrowing(LegacyUrlMappingCatalog.Constraints, CatalogGen.NullLegacyMappings, "legacy-url-mappings.yml");

	[Test]
	public void Operations_AreNone() => CatalogLaw.OperationsOf<LegacyUrlMappingCatalog>().Should().BeEmpty();

	/// <summary>
	/// The set declares no rule of its own: the only rules are the "required" members the generator derives. Blank urls, duplicate base urls and
	/// empty or blank version lists are read without a finding today.
	/// </summary>
	[Test]
	public void BlankAndDuplicateMappings_AreReadWithoutFinding()
	{
		var product = new Product { Id = "a", DisplayName = "A" };
		var catalog = new LegacyUrlMappingCatalog
		{
			Mappings =
			[
				new LegacyUrlMapping { BaseUrl = " ", Product = product, LegacyVersions = [] },
				new LegacyUrlMapping { BaseUrl = " ", Product = product, LegacyVersions = [" ", ""] }
			]
		};

		LegacyUrlMappingCatalog.Constraints.Report(catalog).Violations.Should().BeEmpty();
	}
}
