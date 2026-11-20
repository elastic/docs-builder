// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.DocumentationSets;
using Nullean.Entities.CsCheck;
using Nullean.Snapshots;

namespace Elastic.Documentation.Tests.Domain;

public class DocumentationSetPresentationConstraintsAutoTests
{
	public static IEnumerable<Func<AxisCase>> Axes() =>
		AutoTest
			.For(DocumentationSetPresentationConstraints.Instance, DocumentationSetGen.LawfulPresentation)
			.Seed(RunSeed.Value)
			.CounterExamples(CounterExamples)
			.AsDataSource();

	private static readonly IReadOnlyDictionary<string, Func<DocumentationSetPresentation, DocumentationSetPresentation>> CounterExamples = new Dictionary<string, Func<DocumentationSetPresentation, DocumentationSetPresentation>>
	{
		["branding-images-valid"] = s => s with { Docset = s.Docset with { Branding = new BrandingDeclaration { Icon = "x.exe" } } },
		["cta-template-valid"] =
			s =>
				s with
				{
					Docset = s.Docset with
					{
						Ctas = [new CtaDeclaration { Name = "bad", Label = null, Url = null }, .. s.Docset.Ctas ?? []]
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
		DocumentationSetLaw.ReportsWithoutThrowing(DocumentationSetPresentation.Constraints, DocumentationSetGen.HostilePresentation);

	[Test]
	public void Operations_AreNone() => DocumentationSetLaw.OperationsOf<DocumentationSetPresentation>().Should().BeEmpty();

	/// <summary>Law: the findings of the stage are the branding image findings, then the cta template findings, whatever the entries are.</summary>
	[Test]
	public void Findings_AreTheBrandingImagesThenTheCtaTemplates() =>
		DocumentationSetLaw.ReportsPartsInOrder(
			DocumentationSetPresentation.Constraints,
			DocumentationSetGen.HostilePresentation,
			s => !HasUnreadableImage(s),
			s => s with { Docset = s.Docset with { Ctas = [] } },
			s => s with { Docset = s.Docset with { Branding = null } }
		);

	/// <summary>Law: templates after the first one written with no value were never reported, as the step that fails on it came first.</summary>
	[Test]
	public void Ctas_AfterTheFirstEntryWithNoValue_AreNotReported() =>
		DocumentationSetLaw.StopsAtTheFirstEntryWithNoValue(
			DocumentationSetPresentation.Constraints,
			DocumentationSetGen.HostilePresentation,
			s => s.Docset.Ctas,
			(s, entries) => s with { Docset = s.Docset with { Ctas = entries } }
		);

	private static bool HasUnreadableImage(DocumentationSetPresentation s) =>
		s.Docset.Branding is { } b && new[] { b.Icon, b.OgImage, b.Favicon, b.AppleTouchIcon }.Any(i => i?.Contains('\0') == true);

	private static DocumentationSetPresentation Stage(BrandingDeclaration? branding, params CtaDeclaration?[] ctas) =>
		new(
			DocumentationSetGen.Base with { Branding = branding, Ctas = ctas },
			DocumentationSetGen.FileSystem,
			DocumentationSetGen.Base.SourceDirectory
		);

	private static CtaDeclaration NoButton(string name) => new() { Name = name, Label = "L", Url = null };

	private static CtaDeclaration NoBenefits(string name, string? url = "http://example.test") =>
		new() { Name = name, Label = "L", Url = url, Benefits = null! };

	// Found by generated data: a template whose benefits key was written with no value made the rule throw NullReferenceException and lose the
	// findings before it. Main reports what came before and fails on that template, so the stage stops there.
	[Test]
	public void CtaTemplate_BenefitsWithNoValue_ReportsTheTemplatesBeforeItAndStops() =>
		DocumentationSetLaw
			.Report(DocumentationSetPresentation.Constraints, Stage(null, NoButton("first"), NoBenefits("second"), NoButton("third")))
			.Should()
			.MatchInlineSnapshot("Error cta-template-valid 'cta.first' must define both 'button.label' and 'button.url'.");

	[Test]
	public void CtaTemplate_BenefitsWithNoValueAndAnUnusableButton_ReportsTheButton() =>
		DocumentationSetLaw
			.Report(
				DocumentationSetPresentation.Constraints,
				Stage(null, NoBenefits("a", url: null), NoBenefits("b", url: "ftp://example.test"))
			)
			.Should()
			.MatchInlineSnapshot(
				"""
				Error cta-template-valid 'cta.a' must define both 'button.label' and 'button.url'.
				Error cta-template-valid 'cta.b.button.url' must use http/https or a relative URL.
				"""
			);

	// Found by generated data: a branding image whose path has a NUL character made the rule throw ArgumentException and lose the findings of the
	// images before it. Main fails on that image, after those, and before any template.
	[Test]
	public void BrandingImage_PathThatCannotBeRead_ReportsTheImagesBeforeItAndNoTemplate() =>
		DocumentationSetLaw
			.Report(
				DocumentationSetPresentation.Constraints,
				Stage(new BrandingDeclaration { Icon = "x.exe", OgImage = "a\0.svg", Favicon = "y.exe" }, NoButton("template"))
			)
			.Should()
			.MatchInlineSnapshot(
				"Error branding-images-valid 'branding.icon' has unsupported extension '.exe'. Allowed: .svg, .png, .jpg, .jpeg, .gif, .webp, .ico"
			);
}
