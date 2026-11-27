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

public class RelatedLearningConstraintsAutoTests
{
	public static IEnumerable<Func<AxisCase>> Axes() =>
		AutoTest
			.For(RelatedLearningConstraints.Instance, CatalogGen.LawfulRelatedLearning)
			.Seed(RunSeed.Value)
			.CounterExamples(CounterExamples)
			.AsDataSource();

	private static readonly IReadOnlyDictionary<string, Func<RelatedLearningCatalog, RelatedLearningCatalog>> CounterExamples = new Dictionary<string, Func<RelatedLearningCatalog, RelatedLearningCatalog>>
	{
		["link-has-title"] = catalog => With(catalog, new RelatedLearningLink { Id = "bad", Title = " ", Url = "https://example.test" }),
		["link-has-url"] = catalog => With(catalog, new RelatedLearningLink { Id = "bad", Title = "Bad", Url = " " }),
		["link-url-absolute-http"] = catalog => With(catalog, new RelatedLearningLink { Id = "bad", Title = "Bad", Url = "/relative" })
	};

	private static RelatedLearningCatalog With(RelatedLearningCatalog catalog, RelatedLearningLink link) =>
		new() { Links = catalog.Links.Append(KeyValuePair.Create("bad", link)).ToFrozenDictionary(p => p.Key, p => p.Value) };

	/// <summary>
	/// Law: every rule of a related-learning catalog holds for every value the generator builds (hostile text, undefined enum values, keys that differ only in case or
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
		CatalogLaw.ReportsWithoutThrowing(RelatedLearningCatalog.Constraints, CatalogGen.HostileRelatedLearning, "related-learning.yml");

	/// <summary>
	/// Law: the same holds for the nulls the type annotations forbid (a null entry in a collection or dictionary, a null nested record, a missing
	/// collection): a loader never produces them, a hand-built value can.
	/// </summary>
	[Test]
	public void Constraints_NullsTheAnnotationsForbid_ReportWithoutThrowing() =>
		CatalogLaw.ReportsWithoutThrowing(RelatedLearningCatalog.Constraints, CatalogGen.NullRelatedLearning, "related-learning.yml");

	[Test]
	public void Operations_AreNone() => CatalogLaw.OperationsOf<RelatedLearningCatalog>().Should().BeEmpty();

	// Found by generated data: the three rules read a null link, and threw.
	[Test]
	public void Rules_NullLink_ReportNothing() =>
		RelatedLearningCatalog
			.Constraints
			.Report(new RelatedLearningCatalog
			{
				Links = new Dictionary<string, RelatedLearningLink> { ["demo"] = null! }.ToFrozenDictionary()
			})
			.Violations
			.Should()
			.BeEmpty();

	[Test]
	public void Rules_NoLinkDictionary_ReportTheMissingMember() =>
		RelatedLearningCatalog
			.Constraints
			.Report(new RelatedLearningCatalog { Links = null! })
			.ToDiagnostics("related-learning.yml")
			.ToSnapshotText()
			.Should()
			.MatchInlineSnapshot("error related-learning.yml The Links field is required.");

	[Test]
	public void LinkUrlAbsoluteHttp_UrlsAUriCannotParse_AreReportedNotThrown()
	{
		foreach (var url in new[] { "http://[", "http:///", "http://a b", "\0", "https://" })
		{
			var catalog = new RelatedLearningCatalog
			{
				Links = new Dictionary<string, RelatedLearningLink>
				{
					["demo"] = new() { Id = "demo", Title = "Demo", Url = url }
				}.ToFrozenDictionary()
			};

			var rules = RelatedLearningCatalog.Constraints.Report(catalog).ToDiagnostics("related-learning.yml").Select(d => d.RuleId);

			rules.Should().Equal("link-url-absolute-http");
		}
	}
}
