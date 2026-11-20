// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.DocumentationSets;
using Nullean.Entities;
using Nullean.Entities.CsCheck;
using Nullean.Snapshots;

namespace Elastic.Documentation.Tests.Domain;

public class DocumentationSetApisConstraintsAutoTests
{
	public static IEnumerable<Func<AxisCase>> Axes() =>
		AutoTest
			.For(DocumentationSetApisConstraints.Instance, DocumentationSetGen.LawfulApis)
			.Seed(RunSeed.Value)
			.CounterExamples(CounterExamples)
			.AsDataSource();

	private static readonly IReadOnlyDictionary<string, Func<DocumentationSetApis, DocumentationSetApis>> CounterExamples = new Dictionary<string, Func<DocumentationSetApis, DocumentationSetApis>>
	{
		["api-entry-valid"] =
			s => s with { Resolutions = [.. s.Resolutions, new ApiRejected([Violation.Error("an api entry that cannot be used")])] },
		["storybook-variables-allowed"] =
			s => s with { Docset = s.Docset with { Api = s.Docset.Api ?? [], HasStorybook = true, StorybookRegistry = "${NOT_ALLOWED}" } }
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
		DocumentationSetLaw.ReportsWithoutThrowing(DocumentationSetApis.Constraints, DocumentationSetGen.HostileApis);

	[Test]
	public void Operations_AreNone() => DocumentationSetLaw.OperationsOf<DocumentationSetApis>().Should().BeEmpty();

	/// <summary>Law: the findings of the stage are the api findings, then the storybook findings, whatever the entries are.</summary>
	[Test]
	public void Findings_AreTheApiEntriesThenTheStorybook() =>
		DocumentationSetLaw.ReportsPartsInOrder(
			DocumentationSetApis.Constraints,
			DocumentationSetGen.HostileApis,
			s => !s.Resolutions.Any(r => r.Value is ApiFailed),
			s => s with { Docset = s.Docset with { HasStorybook = false } },
			s => s with { Resolutions = [] }
		);

	private static ApiDeclaration Declared(string key, string? product, string? spec) =>
		new() { ProductKey = key, Entries = [new ApiDeclarationEntry { Product = product, Spec = spec }] };

	// Found by generated data: an api entry whose spec path has a NUL character threw out of the resolution, so the findings of the entries before
	// it were lost. Main reports those, then fails on that entry, and never reaches storybook or the entries after it.
	[Test]
	public void ApiEntry_ThatThrows_ReportsTheEntriesBeforeItAndNoStorybook()
	{
		var docset = DocumentationSetGen.Base with
		{
			Api =
			[
				Declared("first", product: null, spec: "spec.json"),
				Declared("second", "widgets", "a\0.json"),
				Declared("third", product: null, spec: "spec.json")
			],
			HasStorybook = true,
			StorybookRegistry = "${NOT_ALLOWED}"
		};
		var resolutions = DocumentationSetRules.ResolveApis(
			docset.Api,
			DocumentationSetGen.Standard,
			DocumentationSetGen.FileSystem,
			docset.SourceDirectory
		);

		DocumentationSetLaw
			.Report(DocumentationSetApis.Constraints, new DocumentationSetApis(docset, resolutions))
			.Should()
			.MatchInlineSnapshot(
				"Error api-entry-valid API 'first' is missing required 'product:'. It must match a product id defined in products.yml."
			);
		resolutions.Select(r => r.Value.GetType()).Should().Equal(typeof(ApiRejected), typeof(ApiFailed));
		resolutions[1].Value.Should().BeOfType<ApiFailed>().Which.Failure.SourceException.Should().BeOfType<ArgumentException>();
	}
}
