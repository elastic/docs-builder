// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Links;
using Nullean.Entities.CsCheck;

namespace Elastic.Documentation.Tests.Domain;

/// <summary>Generators for the link aggregates: what a loaded registry, manifest and graph promise that a generator cannot guess.</summary>
internal static class LinkExamples
{
	public static readonly DateTime Published = new(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

	public static LinkRegistryEntry Entry(string repository, string branch) =>
		new()
		{
			Repository = repository,
			Branch = branch,
			Path = $"elastic/{repository}/{branch}/links.json",
			ETag = "etag",
			GitReference = "abc123",
			UpdatedAt = Published
		};

	private static string Segment(string name) => name.Replace('/', '-').Replace('\\', '-').Replace("..", "--", StringComparison.Ordinal);

	/// <summary>
	/// A registry as the S3 listing builds it: every repository named as one path segment with a main branch, every entry filed under its own repository and branch,
	/// with the path that implies, a git ref and a timestamp. <paramref name="also"/> names repositories that must be in it (the schemes of the cross-links that refer to them).
	/// </summary>
	public static LinkRegistry Registry(LinkRegistry r, IEnumerable<string>? also = null)
	{
		var repositories = new Dictionary<string, Dictionary<string, LinkRegistryEntry>>(StringComparer.OrdinalIgnoreCase);
		foreach (var (key, branches) in r.Repositories)
		{
			var name = Segment(key);
			if (name.Length == 0 || repositories.ContainsKey(name))
				continue;
			var entries = branches
				.Keys
				.Select(Segment)
				.Where(b => b.Length > 0)
				.Distinct(StringComparer.Ordinal)
				.Append("main")
				.Distinct()
				.ToList();
			repositories[name] = entries.ToDictionary(b => b, b => Entry(name, b));
		}
		foreach (var name in (also ?? []).Append("docs-content"))
		{
			if (!repositories.ContainsKey(name))
				repositories[name] = new Dictionary<string, LinkRegistryEntry> { ["main"] = Entry(name, "main") };
		}
		return r with
		{
			Repositories = new Dictionary<string, Dictionary<string, LinkRegistryEntry>>(repositories, StringComparer.Ordinal)
		};
	}

	/// <summary>
	/// A links.json as the generator writes it: pages named like files with their own anchors (each once), a url prefix that is empty or '/x', cross-links with a repository
	/// scheme, and redirects that each land on a page of the manifest, none of them a page itself or another redirect.
	/// </summary>
	public static LinkManifest Manifest(LinkManifest m)
	{
		var pages = m
			.Links
			.Values
			.Select((l, i) => (Path: $"page{i}.md", Anchors: (l.Anchors ?? []).Select(a => a.ToLowerInvariant()).Distinct().ToArray()))
			.ToList();
		return m with
		{
			Origin = m.Origin.RepositoryName.Length > 0 && m.Origin.IsAvailable
				? m.Origin
				: m.Origin with { RepositoryName = "docs-content" },
			UrlPathPrefix = "/docs",
			Links = pages.ToDictionary(p => p.Path, p => new LinkMetadata { Anchors = p.Anchors, Hidden = false, DuplicateAnchors = [] }),
			CrossLinks = [.. m.CrossLinks.Select((_, i) => $"repo{i}://reference/page.md#top")],
			Redirects = pages.Count == 0
				? null
				: (m.Redirects ?? new Dictionary<string, LinkRedirect>())
					.Values
					.Select((_, i) => i)
					.ToDictionary(i => $"old{i}.md", i => new LinkRedirect { To = pages[i % pages.Count].Path })
		};
	}
}

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
		["registry-entry-has-ref-and-timestamp"] = r => WithFirst(r, e => e with { GitReference = "unknown" }),
		["repository-has-main-or-master"] = r => With(r, "tools", "dev", Entry("tools", "dev")),
		["registry-entry-path-matches-key"] = r => WithFirst(r, e => e with { Path = "elastic/elsewhere/links.json" }),
		["registry-repository-name-single-segment"] = r => With(r, "../evil", "main", Entry("../evil", "main"))
	};
}
