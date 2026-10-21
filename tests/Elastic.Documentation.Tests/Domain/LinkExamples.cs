// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

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
