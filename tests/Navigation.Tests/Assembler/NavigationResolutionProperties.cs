// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using CsCheck;
using Elastic.Documentation.Assembler.Links;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Publishing;
using Elastic.Documentation.Tests.Domain;

namespace Elastic.Documentation.Navigation.Tests.Assembler;

/// <summary>
/// Service-algorithm laws of the navigation: how a cross-link resolves through the mounts (<see cref="PublishEnvironmentUriResolver"/>, fed by
/// <see cref="SiteNavigationLoader.GetTocMappings"/>) and what the path prefix rules of <see cref="SiteNavigationDefinition"/> promise. A fixed seed, 200 iterations.
/// </summary>
/// <remarks>
/// The segment pool holds <c>apm</c> / <c>apm-agents</c> and <c>a</c> / <c>ab</c> on purpose: the N14 ambiguity is a source that is a text prefix of another
/// without a '/' boundary.
/// </remarks>
public class NavigationResolutionProperties
{
	private static readonly string[] Repositories = ["docs-content", "elasticsearch", "kibana"];

	private static Gen<string[]> Segments(int min, int max) =>
		Gen.OneOfConst("apm", "apm-agents", "apm-x", "ref", "a", "ab", "reference").Array[min, max];

	private sealed record Mount(string Repository, string[] Path, string Prefix)
	{
		public string Source => string.Join('/', Path);
		public string Uri => $"{Repository}://{Source}/";
	}

	/// <summary>Mounts with distinct sources and one prefix each (<c>docs/m0</c>, <c>docs/m1</c>, ...), so a prefix names one mount.</summary>
	private static Gen<Mount[]> Mounts { get; } = Gen
		.Select(Gen.OneOfConst(Repositories), Segments(1, 3))
		.Array[0, 6]
		.Select(
			raw => raw
				.Select(x => (Repository: x.Item1, Path: x.Item2))
				.DistinctBy(x => $"{x.Repository}://{string.Join('/', x.Path)}")
				.Select((x, i) => new Mount(x.Repository, x.Path, $"docs/m{i}"))
				.ToArray()
		);

	private static Gen<(string Repository, string[] Path, bool Markdown)> Links { get; } = Gen.Select(
		Gen.OneOfConst(Repositories),
		Segments(1, 4),
		Gen.Bool,
		(r, p, md) => (r, p, md)
	);

	private static Elastic.Documentation.Assembler.Links.PublishEnvironmentUriResolver ResolverFor(IEnumerable<Mount> mounts)
	{
		var navigation = new SiteNavigationDefinition
		{
			TableOfContents = [.. mounts.Select(m => new SiteNavigationEntry(new SiteTableOfContentsRef(new Uri(m.Uri), m.Prefix, [])))]
		};
		var file = new MockFileSystem().FileInfo.New("/navigation.yml");
		var mappings = SiteNavigationLoader.GetTocMappings(navigation, file, new DiagnosticsCollector([]));
		return new PublishEnvironmentUriResolver(mappings, new PublishEnvironment { Uri = "https://www.elastic.co/" });
	}

	private static bool InsideSegmentwise(Mount m, string repository, string path) =>
		m.Repository == repository && (path == m.Source || path.StartsWith(m.Source + "/", StringComparison.Ordinal));

	private static bool InsideTextwise(Mount m, string repository, string path) =>
		m.Repository == repository && path.StartsWith(m.Source, StringComparison.Ordinal);

	private static string Expected(Mount[] mounts, string repository, string path)
	{
		var best = mounts.Where(m => InsideSegmentwise(m, repository, path)).OrderByDescending(m => m.Source.Length).FirstOrDefault();
		if (best is null)
			return "/" + path;
		var rest = path.Length > best.Source.Length ? path[(best.Source.Length + 1)..] : string.Empty;
		return rest.Length == 0 ? "/" + best.Prefix : $"/{best.Prefix}/{rest}";
	}

	/// <summary>
	/// Law: a link resolves to the longest mount it is inside, where inside means the mount's source is the link's path or a whole-segment prefix of it
	/// (the N14 case: <c>reference/apm</c> is not a mount of <c>reference/apm-agents/x</c>); a link inside no mount resolves as it is.
	/// Consequence if broken: a link to one repository's page is published under another repository's path prefix.
	/// </summary>
	[Test]
	public void Resolve_PicksTheLongestMountBySegment() =>
		Gen.Select(Mounts, Links).Check(x =>
		{
			var (mounts, link) = x;
			var path = string.Join('/', link.Path);

			var resolved = ResolverFor(mounts).Resolve(new Uri($"{link.Repository}://{path}"), path);

			resolved.AbsolutePath.Should().Be(Expected(mounts, link.Repository, path));
		});

	/// <summary>
	/// The law that does hold: when the text-prefix mounts and the whole-segment mounts of a link are the same set (no mount ends in the middle of the
	/// link's segment), the link resolves to the longest of them.
	/// Consequence if broken: even an unambiguous link is published under the wrong prefix.
	/// </summary>
	[Test]
	public void Resolve_PicksTheLongestMount_WhenNoMountEndsMidSegment() =>
		Gen.Select(Mounts, Links).Check(x =>
		{
			var (mounts, link) = x;
			var path = string.Join('/', link.Path);
			var unambiguous = mounts.All(m => InsideTextwise(m, link.Repository, path) == InsideSegmentwise(m, link.Repository, path));

			var resolved = ResolverFor(mounts).Resolve(new Uri($"{link.Repository}://{path}"), path);

			if (unambiguous)
				resolved.AbsolutePath.Should().Be(Expected(mounts, link.Repository, path));
		});

	/// <summary>
	/// Law: resolution does not depend on the order the mounts were declared in, for the URL and for the sub-paths the prefix checker compares.
	/// Consequence if broken: moving a line in navigation.yml moves pages.
	/// </summary>
	[Test]
	public void Resolution_IsStableUnderReorderingOfMounts() =>
		Gen.Select(Mounts, Links, Gen.Int).Check(x =>
		{
			var (mounts, link, salt) = x;
			var path = string.Join('/', link.Path) + (link.Markdown ? ".md" : "");
			var uri = new Uri($"{link.Repository}://{path.TrimEnd('/')}");
			var shuffled = mounts
				.OrderBy(m => (m.Source.GetHashCode() ^ salt) & 0xFFFF)
				.ThenBy(m => m.Source, StringComparer.Ordinal)
				.ToArray();

			var a = ResolverFor(mounts);
			var b = ResolverFor(shuffled);

			b.Resolve(uri, path).Should().Be(a.Resolve(uri, path));
			b.ResolveToSubPaths(uri, path).Should().Equal(a.ResolveToSubPaths(uri, path));
		});

	/// <summary>
	/// Law: the sub-paths of a link are all under the mount's path prefix, end in '/', include the prefix itself, are prefix-closed (every parent
	/// directory of a sub-path, down to the prefix, is one too) and are empty exactly when the link is inside no mount.
	/// Consequence if broken: the prefix checker compares paths that are not folders, and misses two repositories claiming the same one.
	/// </summary>
	[Test]
	public void ResolveToSubPaths_AreFoldersUnderTheMountPrefix() =>
		Gen.Select(Mounts, Links).Check(x =>
		{
			var (mounts, link) = x;
			var path = string.Join('/', link.Path) + (link.Markdown ? ".md" : "");
			var uri = new Uri($"{link.Repository}://{path.TrimEnd('/')}");

			var subPaths = ResolverFor(mounts).ResolveToSubPaths(uri, path);

			if (subPaths.Length == 0)
			{
				mounts.Any(m => InsideSegmentwise(m, link.Repository, path)).Should().BeFalse("a link inside a mount has sub-paths");
				return;
			}
			var prefix = subPaths.Single(p => p.Count(c => c == '/') == subPaths.Min(q => q.Count(c => c == '/')));
			subPaths.Should().OnlyContain(p => p.EndsWith('/') && p.StartsWith(prefix, StringComparison.Ordinal));
			foreach (var p in subPaths)
			{
				var parent = p.TrimEnd('/');
				parent = parent[..(parent.LastIndexOf('/') + 1)];
				if (parent.Length >= prefix.Length)
					subPaths.Should().Contain(parent);
			}
		});

	// ---- the path prefix rules of the site navigation ----

	private sealed record Entry(string Repository, string Source, string Prefix);

	private static Gen<Entry[]> Entries { get; } = Gen
		.Select(Gen.OneOfConst(Repositories), Segments(1, 2), Gen.OneOfConst("docs/a", "docs/b", "ref/apm", "ref/apm-agents", "x"))
		.Array[0, 7]
		.Select(
			raw => raw
				.Select(x => new Entry(x.Item1, string.Join('/', x.Item2), x.Item3))
				.DistinctBy(e => $"{e.Repository}://{e.Source}")
				.ToArray()
		);

	private static SiteNavigationDefinition NavigationOf(IReadOnlyList<Entry> entries, int sectionAt)
	{
		SiteTableOfContentsRef Toc(Entry e) => new(new Uri($"{e.Repository}://{e.Source}/"), e.Prefix, []);

		var inSection = entries.Take(sectionAt).Select(Toc).ToArray();
		var top = entries.Skip(sectionAt).Select(e => new SiteNavigationEntry(Toc(e)));
		var section = inSection.Length == 0 ? [] : new[] { new SiteNavigationEntry(new SiteSectionRef("Section", null, inSection, [])) };
		return new SiteNavigationDefinition { TableOfContents = [.. section, .. top] };
	}

	private static Dictionary<string, int> Findings(SiteNavigationDefinition navigation) =>
		SiteNavigationConstraints.All.Validate(navigation).GroupBy(v => v.ConstraintId).ToDictionary(g => g.Key, g => g.Count());

	/// <summary>
	/// Law: the declared path prefixes and every finding of the navigation rules are the same whatever order the mounts are declared in and whether
	/// they sit in a section or at the top.
	/// Consequence if broken: a navigation.yml verdict depends on where a line was put.
	/// </summary>
	[Test]
	public void PathPrefixes_AndFindings_AreStableUnderReordering() =>
		Gen.Select(Entries, Gen.Int[0, 7]).Check(x =>
		{
			var (entries, sectionAt) = x;
			var a = NavigationOf(entries, 0);
			var b = NavigationOf([.. entries.AsEnumerable().Reverse()], Math.Min(sectionAt, entries.Length));

			SiteNavigationDefinition.GetAllPathPrefixes(b).Should().BeEquivalentTo(SiteNavigationDefinition.GetAllPathPrefixes(a));
			SiteNavigationDefinition.GetAllDeclaredSources(b).Should().BeEquivalentTo(SiteNavigationDefinition.GetAllDeclaredSources(a));
			var (fa, fb) = (Findings(a), Findings(b));
			// section rules (a section that is empty, titles) differ with the grouping; the mount rules do not
			string[] mountRules =
			[
				SiteNavigationConstraints.PathPrefixesUnique.Name,
				SiteNavigationConstraints.RepositoryPathPrefixesUnique.Name,
				SiteNavigationConstraints.MountSourceUnique.Name,
				SiteNavigationConstraints.MountSourcesSegmentPrefixFree.Name,
				SiteNavigationConstraints.PathPrefixIsAUrlPath.Name
			];
			foreach (var rule in mountRules)
				fb.GetValueOrDefault(rule).Should().Be(fa.GetValueOrDefault(rule), rule);
		});

	/// <summary>
	/// Law: no prefix is published twice without a rule saying so: two mounts of one repository with one prefix break
	/// <c>RepositoryPathPrefixesUnique</c>, two repositories with one prefix break <c>PathPrefixesUnique</c> (an error), and a navigation where
	/// every prefix is its own breaks neither.
	/// Consequence if broken: two repositories publish into one folder and the second overwrites the first.
	/// </summary>
	[Test]
	public void PathPrefixes_AreUnique_OrARuleFires() =>
		Entries.Check(entries =>
		{
			var navigation = NavigationOf(entries, 0);
			var findings = Findings(navigation);
			var sameRepository = entries.GroupBy(e => (e.Repository, e.Prefix)).Any(g => g.Count() > 1);
			var acrossRepositories = entries.GroupBy(e => e.Prefix).Any(g => g.Select(e => e.Repository).Distinct().Count() > 1);

			(findings.GetValueOrDefault(SiteNavigationConstraints.RepositoryPathPrefixesUnique.Name) > 0).Should().Be(sameRepository);
			(findings.GetValueOrDefault(SiteNavigationConstraints.PathPrefixesUnique.Name) > 0).Should().Be(acrossRepositories);
		});

	/// <summary>
	/// Law: N14 (<c>MountSourcesSegmentPrefixFree</c>) fires exactly when a mounted source is a text prefix of another without a '/' boundary
	/// (<c>reference/apm</c> and <c>reference/apm-agents</c>), and a source that is a segment prefix (<c>reference/apm</c> and <c>reference/apm/x</c>) is fine.
	/// Consequence if broken: the ambiguity the resolver cannot tell apart is not reported.
	/// </summary>
	[Test]
	public void MountSourcesSegmentPrefixFree_FiresExactlyOnTextOnlyPrefixes() =>
		Entries.Check(entries =>
		{
			var sources = entries.Select(e => $"{e.Repository}://{e.Source}").ToArray();
			var ambiguous = sources.Any(
				outer => sources.Any(
					inner => outer.Length > inner.Length && outer.StartsWith(
						inner,
						StringComparison.OrdinalIgnoreCase
					) && outer[inner.Length] != '/'
				)
			);

			var fired = Findings(NavigationOf(entries, 0)).GetValueOrDefault(SiteNavigationConstraints.MountSourcesSegmentPrefixFree.Name)
				> 0;

			fired.Should().Be(ambiguous);
		});
}
