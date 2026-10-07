// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Links;
using Nullean.Entities.CsCheck;

namespace Elastic.Documentation.Tests.Domain;

public class LinkRegistryConstraintsTests
{
	private static readonly DateTime Published = LinkExamples.Published;

	[Test]
	public void Laws() =>
		LinkRegistryConstraints.All.Laws(
			DomainGen.Of<LinkRegistry>(o => o.Normalize<LinkRegistry>(r => LinkExamples.Registry(r))),
			CounterExamples
		);

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
	public void AddingANewerEntry_IsMonotonic()
	{
		var before = Valid();
		var after = before.WithLinkRegistryEntry(Entry("kibana", "main") with { UpdatedAt = Published.AddDays(1) }).OrThrowIfMissing();
		LinkRegistryConstraints.All.Check(before, after).Should().BeEmpty();
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
		["registry-entry-has-ref-and-timestamp"] =
			r => With(r, "tools", "main", Entry("tools", "main") with { UpdatedAt = DateTime.MinValue }),
		["registry-entry-git-ref-known"] = r => WithFirst(r, e => e with { GitReference = "unknown" }),
		["repository-has-main-or-master"] = r => With(r, "tools", "dev", Entry("tools", "dev")),
		["registry-entry-path-matches-key"] = r => WithFirst(r, e => e with { Path = "elastic/elsewhere/links.json" }),
		["registry-repository-name-single-segment"] = r => With(r, "../evil", "main", Entry("../evil", "main"))
	};
}
