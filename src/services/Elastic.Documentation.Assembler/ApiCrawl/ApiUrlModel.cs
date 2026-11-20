// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.ApiExplorer.Infrastructure;
using Elastic.Documentation.Links;

namespace Elastic.Documentation.Assembler.ApiCrawl;

/// <summary>One rendered API tree: its URL root and the root of the versionless tree it names as canonical.</summary>
public sealed record ApiTree(string Key, string Root, string VersionlessRoot, ApiPageType Type);

/// <summary>A page the site must serve, with the canonical URL its page type gives it.</summary>
public sealed record ExpectedApiPage(string Path, ApiPageType Type, string CanonicalUrl);

/// <summary>An old <c>/docs/api/doc/</c> URL and the path it must redirect to.</summary>
public sealed record RedirectCheck(string OldPath, string Target);

/// <summary>
/// Every API URL a site serves, read from the floating sections of a published <c>links.json</c>. Paths start at the host:
/// <c>/docs/api/elasticsearch/v9</c>.
/// </summary>
public sealed class ApiUrlModel
{
	/// <summary>The host every API page names in its canonical URL, whatever host serves it.</summary>
	public const string CanonicalHost = "https://www.elastic.co";

	private const string SectionPrefix = "api/";

	private readonly ApiTree[] _treesLongestFirst;
	private readonly ApiTree _hub;

	private ApiUrlModel(ApiTree hub, ApiTree[] trees, IReadOnlyList<ExpectedApiPage> pages)
	{
		_hub = hub;
		_treesLongestFirst = [.. trees.OrderByDescending(t => t.Root.Length).ThenBy(t => t.Root, StringComparer.Ordinal)];
		Pages = pages;
	}

	/// <summary>Every page in <c>links.json</c>, under its root and its mirror root, plus the API hub.</summary>
	public IReadOnlyList<ExpectedApiPage> Pages { get; }

	/// <param name="prefix">The path the docs live under, with a leading slash and no trailing one, such as <c>/docs</c>.</param>
	public static ApiUrlModel Create(RepositoryLinks links, string prefix = "/docs")
	{
		var hub = new ApiTree(string.Empty, $"{prefix}/api", $"{prefix}/api", ApiPageType.Versionless);
		var trees = new List<ApiTree>();
		var pages = new List<ExpectedApiPage> { Expect(hub.Root, hub) };

		foreach (var (sectionKey, section) in (links.Floating ?? []).OrderBy(s => s.Key, StringComparer.Ordinal))
		{
			if (!sectionKey.StartsWith(SectionPrefix, StringComparison.Ordinal))
				continue;
			var key = sectionKey[SectionPrefix.Length..];
			var routes = new ApiVersionRoutes(key, section.Versions.FirstOrDefault(v => v.Current)?.Name);
			for (var bit = 0; bit < section.Versions.Length && bit < FloatingLinkSection.MaxVersions; bit++)
			{
				var version = section.Versions[bit];
				var versionless = $"{prefix}/{version.Root}";
				foreach (var (root, mirror) in RootsOf(version))
				{
					var tree = new ApiTree(key, $"{prefix}/{root}", versionless, ApiPageTypes.Of(routes, version.Name, isMirror: mirror));
					trees.Add(tree);
					pages.AddRange(
						section
							.Pages
							.Where(p => (p.Value & (1UL << bit)) != 0)
							.Select(p => Expect(p.Key.Length == 0 ? tree.Root : $"{tree.Root}/{p.Key}", tree))
					);
				}
			}
		}

		return new ApiUrlModel(hub, [.. trees], pages);
	}

	/// <summary>The tree that serves <paramref name="path"/>, matching the longest root. The hub matches only itself.</summary>
	public bool TryClassify(string path, out ApiTree tree)
	{
		foreach (var candidate in _treesLongestFirst)
		{
			if (path.Equals(candidate.Root, StringComparison.Ordinal) || path.StartsWith(candidate.Root + "/", StringComparison.Ordinal))
			{
				tree = candidate;
				return true;
			}
		}

		tree = _hub;
		return path.Equals(_hub.Root, StringComparison.Ordinal);
	}

	/// <summary>The page at <paramref name="path"/> in <paramref name="tree"/>, with the canonical URL from <see cref="ApiPageTypes.CanonicalPath"/>.</summary>
	public static ExpectedApiPage Expect(string path, ApiTree tree) =>
		new(path, tree.Type, CanonicalHost + ApiPageTypes.CanonicalPath(tree.Type, path, tree.Root, tree.VersionlessRoot));

	/// <summary>
	/// A stable set of old URLs to check: every table redirect, the root of every tree, then every k-th page up to
	/// <paramref name="pageSample"/> pages. <see cref="ApiPageType.Unreleased"/> and <see cref="ApiPageType.NewestMajorCopy"/>
	/// trees are left out: their <c>/next</c> and <c>/vN</c> URLs never existed under <c>/docs/api/doc/</c>.
	/// </summary>
	public IReadOnlyList<RedirectCheck> RedirectSample(int pageSample)
	{
		var roots = _treesLongestFirst.Where(t => HadLegacyUrl(t.Type)).Select(t => t.Root).Order(StringComparer.Ordinal);
		var candidates = Pages
			.Where(p => HadLegacyUrl(p.Type) && p.Path != _hub.Root && !_treesLongestFirst.Any(t => t.Root == p.Path))
			.Select(p => p.Path)
			.ToArray();
		var step = Math.Max(1, candidates.Length / Math.Max(1, pageSample));
		var sampled = candidates.Where((_, i) => i % step == 0).Take(Math.Max(0, pageSample));

		return [
			.. ApiLegacyRedirects
				.RedirectedPaths
				.Order(StringComparer.Ordinal)
				.Select(old => new RedirectCheck(old, ApiLegacyRedirects.Resolve(old))),
			.. roots.Concat(sampled).Select(ToLegacy).Select(old => new RedirectCheck(old, ApiLegacyRedirects.Resolve(old)))
		];
	}

	private static bool HadLegacyUrl(ApiPageType type) => type is ApiPageType.Versionless or ApiPageType.OlderMajor;

	private string ToLegacy(string path) => string.Concat(_hub.Root, "/doc", path.AsSpan(_hub.Root.Length));

	private static IEnumerable<(string Root, bool Mirror)> RootsOf(FloatingLinkVersion version)
	{
		yield return (version.Root, false);
		if (version.MirrorRoot is { } mirror)
			yield return (mirror, true);
	}
}
