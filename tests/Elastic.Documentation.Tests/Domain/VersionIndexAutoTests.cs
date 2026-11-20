// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using CsCheck;
using Elastic.Documentation.OpenApi;
using Nullean.Entities;
using Nullean.Entities.CsCheck;

namespace Elastic.Documentation.Tests.Domain;

/// <summary>
/// Generated version indexes against every rule of <see cref="VersionIndex"/>. The aggregate has no operation that returns a new index (the loader
/// and the publisher build it), so the operation axes have nothing to run: the invariant, absence and vacancy axes cover the rules, over two generators.
/// <see cref="Legal"/> is every shape a legal index takes, including the ones that look hostile and are not (blank and duplicate names, case variants,
/// JSON nulls the index may hold); <see cref="Hostile"/> adds the values the rules exist to flag.
/// </summary>
public class VersionIndexAutoTests
{
	private static readonly string[] Names = ["elastic/api", "elastic/api", "ELASTIC/API", "", "  ", "a/b"];
	private static readonly string[] Files = ["openapi.json", "openapi.json", "OpenAPI.json", "", " "];

	// the keys and versions the rules flag: case variants, blanks, signs, padding, overflow, a non-ASCII digit, a missing minor and a null
	private static readonly string?[] BadKeys = ["MAIN", "Main", "007", "-1", "+5", "latest", "", "  ", "9.0", "99999999999", "٩", " 9"];

	private static readonly string?[] BadVersions =
	[
		"MAIN",
		"9.",
		".5",
		"9.x",
		"09.5",
		"9.5.1",
		"9.-1",
		"9.+1",
		"99999999999.1",
		"9.99999999999",
		"",
		" ",
		"9",
		"main.1",
		null
	];

	private static readonly Gen<VersionMoniker> LegalMonikers = Gen.OneOf(
		Gen.Const(() => new VersionMoniker("main", "main")),
		Gen.Const(() => new VersionMoniker("main", "master")),
		Gen.Select(Gen.Int[0, 99999], Gen.Int[0, 99999], (major, minor) => new VersionMoniker($"{major}", $"{major}.{minor}"))
	);

	private static readonly Gen<VersionMoniker> HostileMonikers = Gen.Frequency(
		(3, LegalMonikers),
		(2, Gen.Select(Gen.OneOf(BadKeys), Gen.OneOf("main", "9.5", null), (k, v) => new VersionMoniker(k!, v!))),
		(2, Gen.Select(Gen.OneOf("main", "9", "8", "0"), Gen.OneOf(BadVersions), (k, v) => new VersionMoniker(k, v!))),
		(1, Gen.Select(Gen.OneOf(BadKeys), Gen.OneOf(BadVersions), (k, v) => new VersionMoniker(k!, v!)))
	);

	private static Gen<VersionIndex> IndexOf(Gen<VersionMoniker> monikers)
	{
		// a null element and a null list are what a JSON null in the index becomes
		var element = Gen.Frequency((12, monikers.Select(m => (VersionMoniker?)m)), (1, Gen.Const(() => (VersionMoniker?)null)));
		var spec = Gen.Frequency(
			(8, Gen.Select(Gen.OneOf(Files), element.List[0, 4], (f, m) => new VersionIndexSpec(f, [.. m!]))),
			(1, Gen.OneOf(Files).Select(f => new VersionIndexSpec(f, null!)))
		);
		var repository = Gen.Frequency(
			(8, Gen.Select(Gen.OneOf(Names), spec.List[0, 3], (n, s) => new VersionIndexRepository(n, [.. s]))),
			(1, Gen.OneOf(Names).Select(n => new VersionIndexRepository(n, null!)))
		);
		return repository.List[0, 3].Select(r => new VersionIndex([.. r]));
	}

	private static readonly Gen<VersionIndex> Legal = IndexOf(LegalMonikers);
	private static readonly Gen<VersionIndex> Hostile = IndexOf(HostileMonikers);

	public static IEnumerable<Func<AxisCase>> Axes() =>
		AutoTest.For(VersionIndexConstraints.All, Legal).Seed(RunSeed.Value).CounterExamples(CounterExamples).AsDataSource();

	// a repository of its own, so the case does not depend on what the generator drew
	private static VersionIndex WithMoniker(VersionIndex index, string key, string? version) =>
		new([
			.. index.Repositories ?? [],
			new VersionIndexRepository("elastic/counter", [new VersionIndexSpec("openapi.json", [new VersionMoniker(key, version!)])])
		]);

	private static IReadOnlyDictionary<string, Func<VersionIndex, VersionIndex>> CounterExamples
	{
		get;
	} = new Dictionary<string, Func<VersionIndex, VersionIndex>>
	{
		["version-moniker-main-or-major"] = i => WithMoniker(i, "latest", "9.1"),
		["version-key-matches-version"] = i => WithMoniker(i, "8", "9.1")
	};

	/// <summary>
	/// Law: every rule of the version index holds for every legal index the generator builds, and every rule can be broken by some mutation of one.
	/// Consequence if broken: a rule that cannot fire reports green while checking nothing, and the index the lambda publishes is read unchecked.
	/// </summary>
	[Test]
	[MethodDataSource(nameof(Axes))]
	public Task EveryDeclaredAxisHolds(AxisCase axis) => axis.RunAsync();

	/// <summary>
	/// Law: no rule, equality or hash throws on any index the generator builds, however hostile.
	/// Consequence if broken: a bad index fetched from the CDN crashes the build instead of being reported.
	/// </summary>
	[Test]
	public void HostileIndexes_NeverThrow() =>
		Hostile.Sample(
			index =>
			{
				_ = VersionIndex.Constraints.Report(index);
				_ = index.Equals(index with { });
				_ = index.GetHashCode();
				return true;
			},
			seed: RunSeed.Value,
			iter: 5_000
		);

	[Test]
	public void HostileIndexes_BreakEveryRuleAndSomeBreakNone()
	{
		var broken = new HashSet<string?>();
		var legal = 0;
		Hostile.Sample(
			index =>
			{
				var violations = VersionIndex.Constraints.Report(index).Violations;
				foreach (var v in violations)
					_ = broken.Add(v.ConstraintId);
				if (violations.Count == 0)
					legal++;
				return true;
			},
			seed: RunSeed.Value,
			iter: 5_000
		);

		broken.Should().BeEquivalentTo(["version-moniker-main-or-major", "version-key-matches-version"]);
		legal.Should().BeGreaterThan(0);
	}

	[Test]
	public void AnIndexWithNullRepositories_IsComparedAndWalkedAsAnEmptyOne()
	{
		var none = new VersionIndex(null!);

		none.Should().Be(new VersionIndex([]));
		none.GetHashCode().Should().Be(new VersionIndex([]).GetHashCode());
		none.AllMonikers().Should().BeEmpty();
		VersionIndex.Constraints.Report(none).Violations.Should().BeEmpty();
	}
}
