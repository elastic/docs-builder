// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using CsCheck;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.DocumentationSets;
using Nullean.Entities.CsCheck;
using Nullean.Snapshots;

namespace Elastic.Documentation.Tests.Domain;

public class DocumentationSetInCatalogConstraintsAutoTests
{
	public static IEnumerable<Func<AxisCase>> Axes() =>
		AutoTest
			.For(DocumentationSetInCatalogConstraints.Instance, DocumentationSetGen.LawfulInCatalog)
			.Seed(RunSeed.Value)
			.CounterExamples(CounterExamples)
			.AsDataSource();

	private static readonly IReadOnlyDictionary<string, Func<DocumentationSetInCatalog, DocumentationSetInCatalog>> CounterExamples = new Dictionary<string, Func<DocumentationSetInCatalog, DocumentationSetInCatalog>>
	{
		["cross-link-well-formed"] = s => s with { Docset = s.Docset with { CrossLinks = [.. s.Docset.CrossLinks ?? [], "foo://x"] } },
		["release-notes-product-known"] =
			s =>
				s with
				{
					Docset = s.Docset with
					{
						CrossLinks = s.Docset.CrossLinks ?? [],
						ReleaseNotes = [new ReleaseNotesDeclaration { Product = "nope" }, .. s.Docset.ReleaseNotes ?? []]
					}
				}
	};

	/// <summary>
	/// Law: every rule of this stage holds for every value the lawful generator builds (padded, blank and valueless entries, keys that differ only in case,
	/// undefined enum values, an overlong depth, and after the first entry with no value anything at all), and every rule can be broken by some value.
	/// Consequence if broken: a rule that cannot fire, or data that never reaches it, reports green while checking nothing. The stage is a record with no
	/// operations, so the idempotency, monotonicity, conservation and confluence axes are not applicable.
	/// </summary>
	[Test]
	[MethodDataSource(nameof(Axes))]
	public Task EveryDeclaredAxisHolds(AxisCase axis) => axis.RunAsync();

	/// <summary>
	/// Law: the rule set reports its findings for every value a docset.yml can put the stage in, the ones that break rules included, and writing them as
	/// diagnostics does not throw. This is about the constraint set only: the resolver still fails, afterwards, on an entry written with no value, as it always has.
	/// Consequence if broken: a docset.yml with an empty key turns the findings before it into an exception, or hides them.
	/// </summary>
	[Test]
	public void Constraints_AnyValueADocsetCanProduce_ReportWithoutThrowing() =>
		DocumentationSetLaw.ReportsWithoutThrowing(DocumentationSetInCatalog.Constraints, DocumentationSetGen.HostileInCatalog);

	[Test]
	public void Operations_AreNone() => DocumentationSetLaw.OperationsOf<DocumentationSetInCatalog>().Should().BeEmpty();

	/// <summary>Law: the findings of the stage are the cross-link findings, then the release notes findings, whatever the entries are.</summary>
	[Test]
	public void Findings_AreTheCrossLinksThenTheReleaseNotes() =>
		DocumentationSetLaw.ReportsPartsInOrder(
			DocumentationSetInCatalog.Constraints,
			DocumentationSetGen.HostileInCatalog,
			s => s.Docset.CrossLinks is not null,
			s => s with { Docset = s.Docset with { ReleaseNotes = [] } },
			s => s with { Docset = s.Docset with { CrossLinks = [] } }
		);

	/// <summary>Law: with cross_links written with no value the step that reads it fails first, so the stage reports nothing at all.</summary>
	[Test]
	public void Findings_WithCrossLinksWrittenWithNoValue_AreEmpty() =>
		DocumentationSetGen
			.HostileInCatalog
			.Select(s => s with { Docset = s.Docset with { CrossLinks = null! } })
			.Sample(
				s => DocumentationSetLaw.Findings(DocumentationSetInCatalog.Constraints, s).Should().BeEmpty(),
				seed: RunSeed.Value,
				iter: 200
			);

	/// <summary>Law: release notes after the first entry written with no value were never reported, as the step that fails on that entry came first.</summary>
	[Test]
	public void ReleaseNotes_AfterTheFirstEntryWithNoValue_AreNotReported() =>
		DocumentationSetLaw.StopsAtTheFirstEntryWithNoValue(
			DocumentationSetInCatalog.Constraints,
			DocumentationSetGen.HostileInCatalog,
			s => s.Docset.ReleaseNotes,
			(s, entries) => s with { Docset = s.Docset with { ReleaseNotes = entries } }
		);

	private static DocumentationSetInCatalog Stage(string[]? crossLinks, params ReleaseNotesDeclaration?[] releaseNotes) =>
		new(DocumentationSetGen.Base with { CrossLinks = crossLinks!, ReleaseNotes = releaseNotes }, DocumentationSetGen.Standard);

	// Found by generated data: a cross_links key written with no value made the rule throw ArgumentNullException before the stage reported anything.
	// The step that reads it fails first on main too, so the stage reports nothing, not even the release notes that come after it.
	[Test]
	public void CrossLinks_WrittenWithNoValue_ReportNothingOfTheStage() =>
		DocumentationSetLaw
			.Report(DocumentationSetInCatalog.Constraints, Stage(crossLinks: null, new ReleaseNotesDeclaration { Product = "nope" }))
			.Should()
			.MatchInlineSnapshot("");

	[Test]
	public void CrossLinks_WithNullEntries_AreSkippedAndTheReleaseNotesStillReported() =>
		DocumentationSetLaw
			.Report(
				DocumentationSetInCatalog.Constraints,
				Stage(crossLinks: [null!, "foo://x"], new ReleaseNotesDeclaration { Product = "nope" })
			)
			.Should()
			.MatchInlineSnapshot(
				"""
				Error cross-link-well-formed Cross-link 'foo://x' uses unknown registry 'foo'. Use 'public' or 'internal'.
				Error release-notes-product-known Unknown 'release_notes' product 'nope'. It must be a product id defined in products.yml.
				"""
			);
}
