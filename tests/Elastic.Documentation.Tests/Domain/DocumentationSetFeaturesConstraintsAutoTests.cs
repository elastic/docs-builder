// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.DocumentationSets;
using Nullean.Entities.CsCheck;
using Nullean.Snapshots;

namespace Elastic.Documentation.Tests.Domain;

public class DocumentationSetFeaturesConstraintsAutoTests
{
	public static IEnumerable<Func<AxisCase>> Axes() =>
		AutoTest
			.For(DocumentationSetFeaturesConstraints.Instance, DocumentationSetGen.LawfulFeaturesStage)
			.Seed(RunSeed.Value)
			.CounterExamples(CounterExamples)
			.AsDataSource();

	private static readonly IReadOnlyDictionary<string, Func<DocumentationSetFeatures, DocumentationSetFeatures>> CounterExamples = new Dictionary<string, Func<DocumentationSetFeatures, DocumentationSetFeatures>>
	{
		["default-cta-known"] =
			s =>
				s with
				{
					Docset = s.Docset with
					{
						TocDefaultCtas = new Dictionary<string, string>(s.Docset.TocDefaultCtas)
						{
							["unknown-page.md"] = "no-such-template"
						}
					}
				},
		["primary-nav-without-branding"] =
			s =>
				s with
				{
					Docset = s.Docset with { Branding = new BrandingDeclaration(), Features = new DeclaredFeatures { PrimaryNav = true } }
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
		DocumentationSetLaw.ReportsWithoutThrowing(DocumentationSetFeatures.Constraints, DocumentationSetGen.HostileFeaturesStage);

	[Test]
	public void Operations_AreNone() => DocumentationSetLaw.OperationsOf<DocumentationSetFeatures>().Should().BeEmpty();

	/// <summary>Law: the findings of the stage are the default cta findings, then the primary-nav finding, whatever the entries are.</summary>
	[Test]
	public void Findings_AreTheDefaultCtasThenThePrimaryNav() =>
		DocumentationSetLaw.ReportsPartsInOrder(
			DocumentationSetFeatures.Constraints,
			DocumentationSetGen.HostileFeaturesStage,
			_ => true,
			s => s with { Docset = s.Docset with { Features = null! } },
			s => s with { Docset = s.Docset with { TocDefaultCtas = new Dictionary<string, string>() } }
		);

	private static DocumentationSetFeatures Stage(params CtaDeclaration?[]? ctas) =>
		new(DocumentationSetGen.Base with
		{
			Ctas = ctas!,
			TocDefaultCtas = new Dictionary<string, string> { ["index.md"] = "mp", ["a.md"] = "trial" }
		});

	private static readonly CtaDeclaration Mp = new() { Name = "mp", Label = "Go", Url = "http://example.test", Benefits = [] };

	// Found by generated data: a template with no value in the list, or no list at all, made the rule throw. Resolution fails on such a
	// template before this stage is reached, so here it is only not usable.
	[Test]
	public void DefaultCta_TemplateWithNoValue_IsNotUsableAndDoesNotThrow() =>
		DocumentationSetLaw.Report(DocumentationSetFeatures.Constraints, Stage(null, Mp)).Should().MatchInlineSnapshot("");

	[Test]
	public void DefaultCta_TemplateWithBenefitsWithNoValue_IsNotUsable() =>
		DocumentationSetLaw
			.Report(
				DocumentationSetFeatures.Constraints,
				Stage(new CtaDeclaration { Name = "mp", Label = "Go", Url = "http://example.test", Benefits = null! })
			)
			.Should()
			.MatchInlineSnapshot(
				"Error default-cta-known 'default_cta: mp' on page 'index.md' does not match any 'cta' template in docset.yml."
			);

	[Test]
	public void DefaultCta_NoTemplates_OnlyTheBuiltInOneIsUsable() =>
		DocumentationSetLaw
			.Report(DocumentationSetFeatures.Constraints, Stage(ctas: null))
			.Should()
			.MatchInlineSnapshot(
				"Error default-cta-known 'default_cta: mp' on page 'index.md' does not match any 'cta' template in docset.yml."
			);
}
