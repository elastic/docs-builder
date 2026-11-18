// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using CsCheck;
using Elastic.Documentation.ReleaseNotes;
using Nullean.Entities.CsCheck;

namespace Elastic.Documentation.Tests.Domain;

/// <summary>The rules of a changelog file about to be inlined into a bundle, checked on entries built directly.</summary>
public class EntryToBundleConstraintsTests
{
	private static ChangelogEntry Valid() =>
		new() { Title = "A title", Type = ChangelogEntryType.Feature, Products = [new ProductReference { ProductId = "elasticsearch" }] };

	private static IReadOnlyList<string> Messages(ChangelogEntry entry) =>
		[.. EntryToBundle.Constraints.Report(new EntryToBundle(entry)).Violations.Select(v => v.Message)];

	private static readonly Gen<EntryToBundle> Entries = Gen
		.String
		.AlphaNumeric
		.Where(s => s.Length > 0)
		.Select(title => new EntryToBundle(Valid() with { Title = title }));

	public static IEnumerable<Func<AxisCase>> Axes() =>
		AutoTest.For(EntryToBundleConstraints.Instance, Entries).Seed(RunSeed.Value).AsDataSource();

	/// <summary>
	/// Law: an entry with a title, a type and products with ids breaks no rule, and the rule can fire.
	/// Consequence if broken: a changelog file without a required field is inlined into a bundle that render then refuses.
	/// </summary>
	[Test]
	[MethodDataSource(nameof(Axes))]
	public Task EveryDeclaredAxisHolds(AxisCase axis) => axis.RunAsync();

	[Test]
	public void AValidEntry_BreaksNothing() => Messages(Valid()).Should().BeEmpty();

	[Test]
	public void AMissingTitle_IsReported() =>
		Messages(Valid() with { Title = " " }).Should().Equal("Changelog file is missing required field: title");

	[Test]
	public void AnInvalidType_IsReported() =>
		Messages(Valid() with { Type = ChangelogEntryType.Invalid }).Should().Equal("Changelog file is missing required field: type");

	[Test]
	public void NoProducts_IsReported() =>
		Messages(Valid() with { Products = [] }).Should().Equal("Changelog file is missing required field: products");

	[Test]
	public void AProductWithoutAnId_IsReported() =>
		Messages(Valid() with { Products = [new ProductReference { ProductId = "" }] })
			.Should()
			.Equal("Changelog file has product entry missing required field: product");

	[Test]
	public void OnlyTheFirstProblemIsReported() =>
		Messages(Valid() with { Title = "", Type = ChangelogEntryType.Invalid, Products = null })
			.Should()
			.Equal("Changelog file is missing required field: title");
}
