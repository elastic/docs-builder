// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using CsCheck;
using Elastic.Documentation.ReleaseNotes;
using Nullean.Entities;
using Nullean.Entities.CsCheck;

namespace Elastic.Documentation.Tests.Domain;

/// <summary>
/// The two stages of a bundle that carry rules today: the bundle <c>changelog bundle</c> assembles (<see cref="AuthoredBundle"/>) and the one
/// <c>changelog render</c> is handed (<see cref="BundleToRender"/>). A bundle a loader reads is neither: it is checked by the loader that skips what it cannot render.
/// </summary>
public class BundleStageConstraintsTests
{
	private static readonly string[] ProductIds = ["elasticsearch", "kibana", "logstash"];

	private static BundledEntry Inlined(string file, string title = "A title") =>
		new()
		{
			File = new BundledFile { Name = file, Checksum = "abc" },
			Title = title,
			Type = ChangelogEntryType.Feature,
			Products = [new ProductReference { ProductId = "elasticsearch" }]
		};

	private static List<Violation> AuthoredViolations(params BundledProduct[] products) =>
		[.. AuthoredBundle.Constraints.Report(AuthoredBundle.Empty.WithProducts(products)).Violations];

	private static List<Violation> RenderViolations(params BundledEntry[] entries) =>
		[.. BundleToRender.Constraints.Report(new BundleToRender(new Bundle { Entries = entries })).Violations];

	public static IEnumerable<Func<AxisCase>> AuthoredAxes() =>
		AutoTest.For(AuthoredBundleConstraints.Instance, AuthoredBundles).Seed(RunSeed.Value).AsDataSource();

	public static IEnumerable<Func<AxisCase>> RenderAxes() =>
		AutoTest.For(BundleToRenderConstraints.Instance, BundlesToRender).Seed(RunSeed.Value).AsDataSource();

	/// <summary>A bundle that lists each product once, in any number of products and with or without a target.</summary>
	private static Gen<AuthoredBundle> AuthoredBundles { get; } = Gen.Int[0, (1 << ProductIds.Length) - 1].Select(
		mask => AuthoredBundle.Empty.WithProducts([
			.. ProductIds.Where((_, i) => (mask & (1 << i)) != 0).Select(id => new BundledProduct(id, "9.0.0"))
		])
	);

	/// <summary>A bundle whose entries are all inlined, with products, and whose file names are distinct.</summary>
	private static Gen<BundleToRender> BundlesToRender { get; } = Gen.Int[0, 5].Select(
		count => new BundleToRender(new Bundle { Entries = [.. Enumerable.Range(0, count).Select(i => Inlined($"{i}.yaml"))] })
	);

	/// <summary>
	/// Law: an assembled bundle lists a product with one target, and an operation that sets its products cannot open a state the rule forbids without the rule saying so.
	/// Consequence if broken: the bundle command writes a bundle whose release version is ambiguous without a warning.
	/// </summary>
	[Test]
	[MethodDataSource(nameof(AuthoredAxes))]
	public Task Authored_EveryDeclaredAxisHolds(AxisCase axis) => axis.RunAsync();

	/// <summary>
	/// Law: a bundle handed to render has every entry inlined, and the stage operation is checked by the same rule.
	/// Consequence if broken: render reads an entry it cannot show as a blank line.
	/// </summary>
	[Test]
	[MethodDataSource(nameof(RenderAxes))]
	public Task Render_EveryDeclaredAxisHolds(AxisCase axis) => axis.RunAsync();

	[Test]
	public void OneTargetPerProduct_BreaksNothing() =>
		AuthoredViolations(new("elasticsearch", "9.0.0"), new("kibana", "9.0.0")).Should().BeEmpty();

	[Test]
	public void AProductWithTwoTargets_IsAWarningNamingBoth() =>
		AuthoredViolations(new("elasticsearch", "9.0.0"), new("Elasticsearch", null, Lifecycle.Beta))
			.Should()
			.ContainSingle()
			.Which
			.Should()
			.Match<Violation>(
				v => v.Severity == Severity.Warning && v.Message ==
					"Product 'elasticsearch' has multiple targets in bundle: 9.0.0, (no target) beta"
			);

	[Test]
	public void InlinedEntries_BreakNothing() => RenderViolations(Inlined("a.yaml"), Inlined("b.yaml")).Should().BeEmpty();

	[Test]
	public void AFileNamedTwice_IsAWarning() =>
		RenderViolations(Inlined("a.yaml"), Inlined("A.yaml"))
			.Should()
			.ContainSingle()
			.Which
			.Should()
			.Match<Violation>(
				v => v.Severity == Severity.Warning && v.Message == "Changelog file 'A.yaml' appears multiple times in the same bundle"
			);

	[Test]
	public void AnEntryWithoutATitleOrType_IsAnErrorNamedByItsFile() =>
		RenderViolations(Inlined("a.yaml") with { Type = null }, Inlined("b.yaml", title: " "))
			.Select(v => (v.Severity, v.Message))
			.Should()
			.Equal(
				(Severity.Error, "Entry 'a.yaml' in bundle has no inline content: title and type are required. Re-create the bundle with 'changelog bundle'."),
				(Severity.Error, "Entry 'b.yaml' in bundle has no inline content: title and type are required. Re-create the bundle with 'changelog bundle'.")
			);

	[Test]
	public void AnEntryWithoutProducts_IsAnErrorNamedByItsTitle() =>
		RenderViolations(Inlined("a.yaml") with { Products = [] })
			.Should()
			.ContainSingle()
			.Which
			.Message
			.Should()
			.Be("Entry 'A title' in bundle is missing required field: products");

	/// <summary>The warning for a repeated file comes before the entry's own error: the command reports entry by entry.</summary>
	[Test]
	public void ReportsEntryByEntry()
	{
		var broken = Inlined("a.yaml") with { Title = null };
		RenderViolations(Inlined("a.yaml"), broken, Inlined("c.yaml") with { Products = null })
			.Select(v => v.Severity)
			.Should()
			.Equal(Severity.Warning, Severity.Error, Severity.Error);
	}
}
