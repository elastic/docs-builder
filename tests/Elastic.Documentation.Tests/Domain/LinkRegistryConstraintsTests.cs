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
	private static readonly DateTime Published = LinkExamples.Published;

	private static AutoTest<LinkRegistry> Auto() =>
		DomainAutoTest
			.For(LinkRegistryConstraints.All, DomainGen.Of<LinkRegistry>(o => o.Normalize<LinkRegistry>(r => LinkExamples.Registry(r))))
			.CounterExamples(CounterExamples)
			.Action<LinkRegistryEntry>("WithLinkRegistryEntry", (r, e) => r.WithLinkRegistryEntry(e).OrThrowIfMissing());

	public static IEnumerable<Func<AxisCase>> Axes() => Auto().Axes();

	/// <summary>
	/// Law: each axis the link registry declares a rule on holds (see <see cref="AxisCase.Law"/>): the invariants, an entry's updated_at never moving back, and every rule being reachable.
	/// Consequence if broken: see <see cref="AxisCase.Consequence"/>; a failure message names both, the rule, a shrunk case and a seed.
	/// </summary>
	[Test]
	[MethodDataSource(nameof(Axes))]
	public void EveryDeclaredAxisHolds(AxisCase axis) => axis.Run();

	[Test]
	public void The_registry_declares_exactly_the_invariant_monotonicity_and_vacancy_axes() =>
		Auto().Cases.Select(c => c.Axis).Should().Equal(Axis.Invariant, Axis.Monotonicity, Axis.Vacancy);

	[Test]
	public void ValidRegistry_BreaksNoRule() => LinkRegistryConstraints.All.Validate(Valid()).Should().BeEmpty();

	[Test]
	public void GitRefKnown_FiresExactlyForUnknown()
	{
		var rule = LinkRegistryConstraints.RegistryEntryGitRefKnown;

		static IReadOnlyList<Nullean.Entities.Violation> Check(Nullean.Entities.Constraint<LinkRegistry> c, string gitRef)
		{
			var r = Registry(("kibana", "main", Entry("kibana", "main") with { GitReference = gitRef }));
			return c.Check(r, r).ToList();
		}

		Check(rule, "abc123").Should().BeEmpty();
		Check(rule, "unknown").Should().ContainSingle().Which.Message.Should().Contain("kibana/main").And.Contain("'unknown'");
		Check(rule, null!).Should().ContainSingle();
		Check(rule, "Unknown").Should().BeEmpty();
		Check(LinkRegistryConstraints.RegistryEntryHasRefAndTimestamp, "unknown").Should().BeEmpty();
	}

	[Test]
	public void AnOlderEntry_IsNamedWithItsRepositoryAndBranch()
	{
		var before = Valid();
		var after = CounterExamples["registry-entry-updated-at-monotonic"](before);
		LinkRegistryConstraints.All.Check(before, after).Should().ContainSingle().Which.Message.Should().Contain("docs-content/main");
	}

	private static LinkRegistryEntry Entry(string repository, string branch) =>
		new()
		{
			Repository = repository,
			Branch = branch,
			Path = $"elastic/{repository}/{branch}/links.json",
			ETag = "etag",
			GitReference = "abc123",
			UpdatedAt = Published
		};

	private static LinkRegistry Registry(params (string Repository, string Branch, LinkRegistryEntry Entry)[] entries) =>
		new()
		{
			Repositories = entries.GroupBy(e => e.Repository).ToDictionary(g => g.Key, g => g.ToDictionary(e => e.Branch, e => e.Entry))
		};

	private static LinkRegistry Valid() =>
		Registry(
			("docs-content", "main", Entry("docs-content", "main")),
			("kibana", "main", Entry("kibana", "main")),
			("kibana", "8.x", Entry("kibana", "8.x"))
		);

	private static LinkRegistry With(LinkRegistry r, string repository, string branch, LinkRegistryEntry entry)
	{
		var copy = r.Repositories.ToDictionary(x => x.Key, x => new Dictionary<string, LinkRegistryEntry>(x.Value));
		if (!copy.TryGetValue(repository, out var branches))
			copy[repository] = branches = [];
		branches[branch] = entry;
		return r with { Repositories = copy };
	}

	// the entry cases change the first entry the generated registry holds (it always has one, the narrative repository's main); the others add a repository the generator never names
	private static LinkRegistry WithFirst(LinkRegistry r, Func<LinkRegistryEntry, LinkRegistryEntry> change)
	{
		var (repository, branches) = r.Repositories.First();
		var (branch, entry) = branches.First();
		return With(r, repository, branch, change(entry));
	}

	private static IReadOnlyDictionary<string, Func<LinkRegistry, LinkRegistry>> CounterExamples
	{
		get;
	} = new Dictionary<string, Func<LinkRegistry, LinkRegistry>>
	{
		["registry-entry-updated-at-monotonic"] = r => WithFirst(r, e => e with { UpdatedAt = e.UpdatedAt.AddDays(-1) }),
		["repository-has-main-or-master"] = r => With(r, "tools", "dev", Entry("tools", "dev")),
		["registry-repository-name-single-segment"] = r => With(r, "../evil", "main", Entry("../evil", "main"))
	};
}
