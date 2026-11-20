// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Catalog;
using Elastic.Documentation.Versions;
using Nullean.Entities.CsCheck;

namespace Elastic.Documentation.Tests.Domain;

public class VersioningSystemConstraintsAutoTests
{
	public static IEnumerable<Func<AxisCase>> Axes() =>
		AutoTest.For(VersioningSystemConstraints.Instance, CatalogGen.HostileVersioningCatalogs).Seed(RunSeed.Value).AsDataSource();

	/// <summary>
	/// Law: every rule of a versioning system catalog holds for every value the generator builds (hostile text, undefined enum values, keys that differ only in case or
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
		CatalogLaw.ReportsWithoutThrowing(VersioningSystemCatalog.Constraints, CatalogGen.HostileVersioningCatalogs, "versions.yml");

	/// <summary>
	/// Law: the same holds for the nulls the type annotations forbid (a null entry in a collection or dictionary, a null nested record, a missing
	/// collection): a loader never produces them, a hand-built value can.
	/// </summary>
	[Test]
	public void Constraints_NullsTheAnnotationsForbid_ReportWithoutThrowing() =>
		CatalogLaw.ReportsWithoutThrowing(VersioningSystemCatalog.Constraints, CatalogGen.NullVersioningCatalogs, "versions.yml");

	[Test]
	public void Operations_AreNone() => CatalogLaw.OperationsOf<VersioningSystemCatalog>().Should().BeEmpty();

	/// <summary>
	/// The set declares no rule of its own: the only rules are the "required" members the generator derives, and a base after a current version is
	/// read without a finding today. The generator is not filtered, so every state (out-of-order versions, undefined ids, a key that is not the
	/// system's id) must satisfy them.
	/// </summary>
	[Test]
	public void BaseAfterCurrent_UndefinedIdAndMismatchedKey_AreReadWithoutFinding()
	{
		var backwards = new VersioningSystem
		{
			Id = (VersioningSystemId)12345,
			Base = new SemVersion(9, 0, 0),
			Current = new SemVersion(1, 0, 0)
		};
		var catalog = new VersioningSystemCatalog
		{
			VersioningSystems = new Dictionary<VersioningSystemId, VersioningSystem> { [VersioningSystemId.Stack] = backwards }
		};

		VersioningSystemCatalog.Constraints.Report(catalog).Violations.Should().BeEmpty();
	}
}
