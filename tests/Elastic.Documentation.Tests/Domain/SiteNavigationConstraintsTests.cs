// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using CsCheck;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Publishing;
using Elastic.Documentation.Testing;
using Nullean.Entities;
using Nullean.Entities.CsCheck;
using Nullean.Snapshots;

namespace Elastic.Documentation.Tests.Domain;

/// <summary>
/// The operations on a <see cref="SiteNavigationDefinition"/> (dropping the mounts of some repositories, mounting more) against every rule it declares,
/// and the rules themselves.
/// </summary>
public class SiteNavigationConstraintsTests
{
	private static readonly string[] Schemes = ["open-a", "closed-b", "open-c", "closed-d"];

	private static SiteTableOfContentsRef Mount(string scheme, string name, string prefix, params SiteTableOfContentsRef[] children) =>
		new(new Uri($"{scheme}://{name}/"), prefix, children);

	/// <summary>
	/// A navigation that satisfies every rule: mounts under distinct prefixes, in several repositories, nested and in a section, with phantoms.
	/// The shape (how many mounts, how deep, whether a section groups some) is what varies.
	/// </summary>
	private static readonly Gen<SiteNavigationDefinition> Navigations = Gen.Int[1, 4].SelectMany(topCount => Gen.Int[0, 3].SelectMany(childCount => Gen.Int[0, 2].SelectMany(grandCount => Gen.Bool.Select(Gen.Int[0, 3], (
		sectioned,
		phantoms
	) =>
	{
		var next = 0;

		string Prefix() => $"area{next++}";

		SiteTableOfContentsRef Make(int depth)
		{
			var index = next;
			var children = depth switch
			{
				0 => Enumerable.Range(0, childCount).Select(_ => Make(1)),
				1 => Enumerable.Range(0, grandCount).Select(_ => Make(2)),
				_ => []
			};
			return Mount(Schemes[index % Schemes.Length], $"m{index}", Prefix(), [.. children]);
		}

		var mounts = Enumerable.Range(0, topCount).Select(_ => Make(0)).ToList();
		var entries = sectioned
			? [new SiteNavigationEntry(new SiteSectionRef("Section", null, mounts, [])), new SiteNavigationEntry(Make(0))]
			: mounts.Select(m => new SiteNavigationEntry(m)).ToList();
		return new SiteNavigationDefinition
		{
			TableOfContents = entries,
			Phantoms = [.. Enumerable.Range(0, phantoms).Select(i => new PhantomRegistration { Source = $"{Schemes[i]}://ph{i}" })]
		};
	}))));

	// the repositories to drop: a subset of the schemes, so some mounts go and some stay
	private static Gen<IReadOnlySet<string>> RepositorySets(SiteNavigationDefinition navigation) =>
		Gen.Int[0, (1 << Schemes.Length) - 1].Select(
			mask => (IReadOnlySet<string>)Schemes.Where((_, i) => (mask & (1 << i)) != 0).ToHashSet(StringComparer.Ordinal)
		);

	// entries to mount after the rest: under prefixes no generated mount uses
	private static Gen<IReadOnlyList<SiteNavigationEntry>> Appended(SiteNavigationDefinition navigation) =>
		Gen.Int[0, 3].Select(
			count =>
				(IReadOnlyList<SiteNavigationEntry>)[
					.. Enumerable.Range(0, count).Select(i => new SiteNavigationEntry(Mount("local", $"l{i}", $"local{i}")))
				]
		);

	public static IEnumerable<Func<AxisCase>> Axes() =>
		AutoTest
			.For(SiteNavigationConstraints.All, Navigations)
			.Action("WithoutRepositories", (n, repositories) => n.WithoutRepositories(repositories), RepositorySets)
			.Action("WithAppended", (n, entries) => n.WithAppended(entries), Appended)
			.Seed(RunSeed.Value)
			.CounterExamples(CounterExamples)
			.AsDataSource();

	// each case adds mounts, under names no generated one has, that break one rule
	private static IReadOnlyDictionary<string, Func<SiteNavigationDefinition, SiteNavigationDefinition>> CounterExamples =>
		new Dictionary<string, Func<SiteNavigationDefinition, SiteNavigationDefinition>>
		{
			["mount-has-path-prefix"] = n => n.WithAppended([new SiteNavigationEntry(Mount("no-prefix", "x", ""))]),
			["path-prefixes-unique"] =
				n => n.WithAppended([
					new SiteNavigationEntry(Mount("dup-a", "x", "dupe")),
					new SiteNavigationEntry(Mount("dup-b", "y", "dupe"))
				])
		};

	[Test]
	[MethodDataSource(nameof(Axes))]
	public Task EveryDeclaredAxisHolds(AxisCase axis) => axis.RunAsync();

	private static string Report(SiteNavigationDefinition navigation) =>
		SiteNavigationDefinition.Constraints.Report(navigation).ToDiagnostics("navigation.yml").ToSnapshotText();

	// which of two duplicates a message names first follows the hash order of a set of Uris, which changes with every process: the rule is what is stable
	private static string RuleIds(SiteNavigationDefinition navigation) =>
		string.Join(", ", SiteNavigationDefinition.Constraints.Report(navigation).ToDiagnostics("navigation.yml").Select(d => d.RuleId));

	private static SiteNavigationDefinition Navigation(params SiteTableOfContentsRef[] mounts) =>
		new() { TableOfContents = [.. mounts.Select(m => new SiteNavigationEntry(m))] };

	[Test]
	public void Navigation_MountWithoutAPathPrefix_ReportsItWithTheFallback() =>
		Report(Navigation(Mount("repo", "a", "")))
			.Should()
			.MatchInlineSnapshot(
				"error navigation.yml Path prefix is not defined for: repo://a/, falling back to a/ which may be incorrect"
			);

	[Test]
	public void Navigation_BareTocName_NeedsNoPathPrefix() =>
		Report(Navigation(new SiteTableOfContentsRef(new Uri("docs-content://get-started"), "", [], shorthandPath: "get-started")))
			.Should()
			.MatchInlineSnapshot("");

	[Test]
	public void Navigation_TwoRepositoriesUnderOnePathPrefix_ReportsTheSecondAsADuplicate() =>
		RuleIds(Navigation(Mount("repoa", "a", "shared"), Mount("repob", "b", "shared")))
			.Should()
			.MatchInlineSnapshot("path-prefixes-unique");

	[Test]
	public void Navigation_PathPrefixesDifferingByATrailingSlash_AreDuplicates() =>
		RuleIds(Navigation(Mount("repoa", "a", "shared"), Mount("repob", "b", "shared/")))
			.Should()
			.MatchInlineSnapshot("path-prefixes-unique");

	[Test]
	public void Navigation_SameRepositoryUnderOnePathPrefixTwice_IsOneMount() =>
		Report(Navigation(Mount("repo", "a", "shared"), Mount("repo", "b", "shared/"))).Should().MatchInlineSnapshot("");

	[Test]
	public void WithoutRepositories_ADroppedMount_PromotesItsChildrenInItsPlace()
	{
		var navigation = Navigation(
			Mount("closed", "one", "one", Mount("open", "two", "two", Mount("closed", "inner", "inner")), Mount("open", "three", "three")),
			Mount("open", "four", "four")
		);

		var filtered = navigation.WithoutRepositories(new HashSet<string> { "closed" });

		string
			.Join(", ", SiteNavigationConstraints.AllMounts(filtered).Select(m => m.PathPrefix))
			.Should()
			.MatchInlineSnapshot("two, inner, three, four");
	}

	[Test]
	public void WithoutRepositories_RepositoryNamePrefixOfAnother_DropsOnlyTheNamedOne()
	{
		var navigation = new SiteNavigationDefinition
		{
			TableOfContents =
			[
				new SiteNavigationEntry(Mount("closed-but-longer", "a", "a")),
				new SiteNavigationEntry(Mount("closed", "b", "b"))
			],
			Phantoms =
			[
				new PhantomRegistration { Source = "closed-but-longer://x" },
				new PhantomRegistration { Source = "closed://y" },
				new PhantomRegistration { Source = "bare-path" }
			]
		};

		var filtered = navigation.WithoutRepositories(new HashSet<string> { "closed" });

		string
			.Join(
				", ",
				filtered.Phantoms.Select(p => p.Source).Concat(SiteNavigationConstraints.AllMounts(filtered).Select(m => m.PathPrefix))
			)
			.Should()
			.MatchInlineSnapshot("closed-but-longer://x, bare-path, a");
	}

	[Test]
	public void WithoutRepositories_NothingToDrop_IsTheSameNavigation()
	{
		var navigation = Navigation(Mount("open", "a", "a"));

		navigation.WithoutRepositories(new HashSet<string>()).Should().BeSameAs(navigation);
	}

	[Test]
	public void WithAppended_Entries_AreMountedAfterTheRest()
	{
		var navigation = Navigation(Mount("open", "a", "a"));

		var appended = navigation.WithAppended([new SiteNavigationEntry(Mount("local", "b", "b"))]);

		string.Join(", ", SiteNavigationConstraints.AllMounts(appended).Select(m => m.PathPrefix)).Should().MatchInlineSnapshot("a, b");
		navigation.WithAppended([]).Should().BeSameAs(navigation);
	}

	[Test]
	public void WithoutRepositories_PromotionCreatesACollision_TheCheckedFormRefusesIt()
	{
		var navigation = Navigation(Mount("closed", "one", "one", Mount("open", "two", "same")), Mount("other", "three", "same"));

		var outcome = SiteNavigationDefinition.Constraints.ApplyDetailed(
			navigation,
			n => n.WithoutRepositories(new HashSet<string> { "closed" })
		);

		outcome.Should().BeOfType<ApplyOutcome<SiteNavigationDefinition>.Failure>();
		string
			.Join(", ", outcome.ToDiagnostics("navigation.yml").Select(d => d.RuleId))
			.Should()
			.MatchInlineSnapshot("path-prefixes-unique");
	}
}
