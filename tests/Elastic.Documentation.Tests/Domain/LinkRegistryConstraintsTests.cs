// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using CsCheck;
using Elastic.Documentation.Links;
using Nullean.Entities;
using Nullean.Entities.CsCheck;

namespace Elastic.Documentation.Tests.Domain;

public class LinkRegistryConstraintsTests
{
	private static readonly string[] Repositories = ["docs-content", "kibana", "example-repo", "tool-kit"];
	private static readonly string[] Branches = ["main", "8.x", "9.0", "7.17"];

	private static readonly Gen<DateTime> Times = Gen.Int[1, 28].Select(day => new DateTime(2026, 1, day, 0, 0, 0, DateTimeKind.Utc));

	private static Gen<LinkRegistryEntry> EntryOf(string repository, string branch) =>
		Gen.Select(Times, Gen.OneOf("abc123", "def456"), (updatedAt, gitRef) => Entry(repository, branch, updatedAt, gitRef));

	// every repository has a main branch, so every rule holds
	private static readonly Gen<LinkRegistry> Registries = Gen.Select(
		Gen.Int[1, Repositories.Length],
		Gen.Int[0, (1 << (Branches.Length - 1)) - 1],
		Gen.Int[0, 20],
		Build
	);

	// an entry for a repository and branch the registry already holds, older or newer, so the operation sometimes replaces and sometimes does not
	private static Gen<LinkRegistryEntry> KnownEntries(LinkRegistry registry)
	{
		var known = registry.Repositories.SelectMany(r => r.Value.Keys.Select(branch => (Repository: r.Key, Branch: branch))).ToArray();
		return Gen.Int[0, known.Length - 1].SelectMany(i => EntryOf(known[i].Repository, known[i].Branch));
	}

	private static AutoTest<LinkRegistry> Auto() =>
		AutoTest
			.For(LinkRegistryConstraints.Instance, Registries)
			.Seed(RunSeed.Value)
			// the operation updates the branch dictionaries it shares with the registry it was called on, so the monotonicity axis, which compares the
			// start with the result, is shown a copy
			.Action("WithKnownLinkRegistryEntry", (registry, entry) => Copy(registry).WithLinkRegistryEntry(entry), KnownEntries)
			.CounterExamples(CounterExamples);

	public static IEnumerable<Func<AxisCase>> Axes() => Auto().AsDataSource();

	/// <summary>
	/// Law: every rule of the link registry holds for every registry that is read, and stays true after filing an entry: an entry's updated_at never
	/// moves back, and every rule can be broken by some registry.
	/// Consequence if broken: a registry written back by the index publisher breaks what the cross-link fetchers rely on (a main or master branch, a
	/// path that names the repository and branch it is filed under).
	/// </summary>
	[Test]
	[MethodDataSource(nameof(Axes))]
	public Task EveryDeclaredAxisHolds(AxisCase axis) => axis.RunAsync();

	[Test]
	public void WithLinkRegistryEntry_IsTheOneOperationOfTheRegistry() =>
		LinkRegistryConstraints.Actions.Select(a => a.Name).Should().BeEquivalentTo(["WithLinkRegistryEntry"]);

	[Test]
	public void ValidRegistry_BreaksNoRule() => LinkRegistry.Constraints.Report(Valid()).Violations.Should().BeEmpty();

	[Test]
	public void GitRefKnown_FiresExactlyForUnknown()
	{
		ConstraintReport<LinkRegistry> Report(string gitRef) =>
			LinkRegistry.Constraints.Report(Registry(("kibana", "main", Entry("kibana", "main", Published, gitRef))));

		Report("abc123").Violations.Should().BeEmpty();
		Report("Unknown").Violations.Should().BeEmpty();
		Report("unknown")
			.Violations
			.Should()
			.ContainSingle()
			.Which
			.Message
			.Should()
			.Be("Entry kibana/main is a legacy entry: its ref is 'unknown'.");
	}

	[Test]
	public void ALegacyEntry_IsReportedForItsMissingTimestamp()
	{
		var legacy = Entry("kibana", "main", DateTime.MinValue, "abc123");

		LinkRegistry
			.Constraints
			.Report(Registry(("kibana", "main", legacy)))
			.Violations
			.Should()
			.ContainSingle()
			.Which
			.Message
			.Should()
			.Be("Entry kibana/main is a legacy entry: its updated_at is unset.");
	}

	[Test]
	public void ARepositoryWithoutMainOrMaster_NamesItsBranches() =>
		LinkRegistry
			.Constraints
			.Report(
				Registry(
					("tools", "dev", Entry("tools", "dev", Published, "abc123")),
					("tools", "9.0", Entry("tools", "9.0", Published, "abc123"))
				)
			)
			.Violations
			.Should()
			.ContainSingle()
			.Which
			.Message
			.Should()
			.Be("Repository 'tools' has neither a 'main' nor a 'master' entry in the link registry (it has: dev, 9.0).");

	[Test]
	public void ACodexRegistryEntry_FiledUnderItsEnvironment_BreaksNoRule()
	{
		var codex = Entry("kibana", "main", Published, "abc123") with { Path = "internal/elastic/kibana/links.json" };

		LinkRegistry.Constraints.Report(Registry(("kibana", "main", codex))).Violations.Should().BeEmpty();
	}

	[Test]
	[Arguments("internal/elastic/other/links.json")]
	[Arguments("elastic/kibana/links.json")]
	[Arguments("/elastic/kibana/links.json")]
	[Arguments("a/b/elastic/kibana/links.json")]
	[Arguments("elastic/kibana/9.0/links.json")]
	[Arguments("../elastic/kibana/links.json")]
	[Arguments("C:internal/elastic/kibana/links.json")]
	[Arguments("./elastic/kibana/links.json")]
	[Arguments(" /elastic/kibana/links.json")]
	[Arguments("a\\b/elastic/kibana/links.json")]
	public void AnEntryPathThatNamesNeitherLayout_IsReported(string path)
	{
		var entry = Entry("kibana", "main", Published, "abc123") with { Path = path };

		LinkRegistry
			.Constraints
			.Report(Registry(("kibana", "main", entry)))
			.Violations
			.Should()
			.ContainSingle(v => v.Message.StartsWith("Entry kibana/main has path", StringComparison.Ordinal));
	}

	[Test]
	[Arguments("../evil")]
	[Arguments("a/../b")]
	[Arguments("/rooted")]
	public void ARepositoryNameThatEscapesTheCheckoutFolder_IsAnError(string name) =>
		LinkRegistry
			.Constraints
			.Report(Registry((name, "main", Entry(name, "main", Published, "abc123"))))
			.Violations
			.Should()
			.ContainSingle(v => v.Message.StartsWith($"Repository name '{name}'", StringComparison.Ordinal));

	[Test]
	public void WithLinkRegistryEntry_AnOlderEntryIsIgnored_ANewerOneReplaces_BothKeepTheOriginalRepositoryDictionary()
	{
		var registry = Valid();
		var newer = Entry("kibana", "main", Published.AddDays(1), "def456");

		registry
			.WithLinkRegistryEntry(Entry("kibana", "main", Published.AddDays(-1), "older"))
			.Repositories["kibana"]["main"]
			.GitReference
			.Should()
			.Be("abc123");
		var updated = registry.WithLinkRegistryEntry(newer);

		updated.Repositories["kibana"]["main"].Should().Be(newer);
		updated.Repositories.Should().NotBeSameAs(registry.Repositories);
	}

	// Recorded as it is today: the branch dictionaries are shared between the registry and its copy, so an update shows through the original.
	[Test]
	public void WithLinkRegistryEntry_UpdatingAKnownRepository_ShowsThroughTheOriginalRegistry()
	{
		var registry = Valid();
		var newer = Entry("kibana", "main", Published.AddDays(1), "def456");

		_ = registry.WithLinkRegistryEntry(newer);

		registry.Repositories["kibana"]["main"].Should().Be(newer);
	}

	[Test]
	public void WithLinkRegistryEntry_ANewRepository_IsOnlyInTheCopy()
	{
		var registry = Valid();

		var updated = registry.WithLinkRegistryEntry(Entry("fresh", "main", Published, "abc123"));

		updated.Repositories.Keys.Should().Contain("fresh");
		registry.Repositories.Keys.Should().NotContain("fresh");
	}

	[Test]
	public void ValueEquality_ComparesRepositoriesAndBranchesWhateverTheirOrder()
	{
		var a = Registry(("a", "main", Entry("a", "main", Published, "x")), ("b", "main", Entry("b", "main", Published, "x")));
		var b = Registry(("b", "main", Entry("b", "main", Published, "x")), ("a", "main", Entry("a", "main", Published, "x")));

		a.Should().Be(b);
		a.Should().NotBe(a.WithLinkRegistryEntry(Entry("c", "main", Published, "x")));
		a.Equals(b with { ETag = "other" }).Should().BeFalse("the ETag is part of the registry a publisher writes back");
	}

	private static LinkRegistry Copy(LinkRegistry registry) =>
		registry with
		{
			Repositories = registry.Repositories.ToDictionary(r => r.Key, r => new Dictionary<string, LinkRegistryEntry>(r.Value))
		};

	private static DateTime Published { get; } = new(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc);

	private static LinkRegistryEntry Entry(string repository, string branch, DateTime updatedAt, string gitRef) =>
		new()
		{
			Repository = repository,
			Branch = branch,
			Path = $"elastic/{repository}/{branch}/links.json",
			ETag = "etag",
			GitReference = gitRef,
			UpdatedAt = updatedAt
		};

	private static LinkRegistry Registry(params (string Repository, string Branch, LinkRegistryEntry Entry)[] entries) =>
		new()
		{
			Repositories = entries.GroupBy(e => e.Repository).ToDictionary(g => g.Key, g => g.ToDictionary(e => e.Branch, e => e.Entry))
		};

	private static LinkRegistry Build(int count, int extraBranches, int dayOffset) =>
		new()
		{
			Repositories = Repositories
				.Take(count)
				.Select((repository, i) => (repository, i))
				.ToDictionary(
					r => r.repository,
					r => Branches
						.Select((branch, b) => (branch, b))
						.Where(x => x.b == 0 || (extraBranches & (1 << (x.b - 1))) != 0)
						.ToDictionary(x => x.branch, x => Entry(r.repository, x.branch, Published.AddDays(dayOffset + r.i + x.b), "abc123"))
				)
		};

	private static LinkRegistry Valid() =>
		Registry(
			("docs-content", "main", Entry("docs-content", "main", Published, "abc123")),
			("kibana", "main", Entry("kibana", "main", Published, "abc123")),
			("kibana", "8.x", Entry("kibana", "8.x", Published, "abc123"))
		);

	private static LinkRegistry With(LinkRegistry r, string repository, string branch, LinkRegistryEntry entry)
	{
		var copy = r.Repositories.ToDictionary(x => x.Key, x => new Dictionary<string, LinkRegistryEntry>(x.Value));
		if (!copy.TryGetValue(repository, out var branches))
			copy[repository] = branches = [];
		branches[branch] = entry;
		return r with { Repositories = copy };
	}

	// the cases add a repository the generator never names
	private static IReadOnlyDictionary<string, Func<LinkRegistry, LinkRegistry>> CounterExamples
	{
		get;
	} = new Dictionary<string, Func<LinkRegistry, LinkRegistry>>
	{
		["registry-entry-has-ref-and-timestamp"] = r => With(r, "legacy", "main", Entry("legacy", "main", DateTime.MinValue, "abc123")),
		["registry-entry-git-ref-known"] =
			r => With(r, "legacy", "main", Entry("legacy", "main", Published, LinkRegistryEntry.UnknownGitReference)),
		["registry-entry-path-matches-key"] =
			r => With(r, "moved", "main", Entry("moved", "main", Published, "abc123") with { Path = "elsewhere/links.json" }),
		["repository-has-main-or-master"] = r => With(r, "tools", "dev", Entry("tools", "dev", Published, "abc123")),
		["registry-repository-name-single-segment"] = r => With(r, "../evil", "main", Entry("../evil", "main", Published, "abc123"))
	};
}
