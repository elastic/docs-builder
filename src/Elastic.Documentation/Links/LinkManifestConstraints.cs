// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Nullean.Entities;

namespace Elastic.Documentation.Links;

/// <summary>
/// Every rule a published <see cref="LinkManifest"/> (<c>links.json</c>) must satisfy, in the order they are reported.
/// Reading one has no schema validation of its own, so these say what a well-formed manifest is.
/// </summary>
[Constraints]
public static partial class LinkManifestConstraints
{
	public static Constraint<LinkManifest> LinkManifestPagePathsAreMarkdown { get; } = Constraints<LinkManifest>.Each(
		m => m.Links.Keys,
		p => p.EndsWith(".md", StringComparison.Ordinal),
		p => $"Page path '{p}' does not end in '.md'."
	).Describe("N34: Every key of links is a Markdown page path (ends in '.md'); reading a links.json does not validate its schema.");

	public static Constraint<LinkManifest> LinkManifestAnchorsPresent { get; } = Constraints<LinkManifest>.Each(
		m => m.Links,
		l => l.Value?.Anchors is not null,
		l => $"Page '{l.Key}' has no anchors list."
	).Describe("N34: Every page of links has an anchors list (possibly empty); reading a links.json does not validate its schema.");

	public static Constraint<LinkManifest> LinkManifestOriginNamed { get; } = Constraints<LinkManifest>.Always(
		m => m.Origin is { IsAvailable: true, RepositoryName.Length: > 0 },
		"The origin of the links.json names no repository."
	).Describe("N34: The origin names the repository that published the manifest; reading a links.json does not validate its schema.");

	public static Constraint<LinkManifest> UrlPathPrefixNormalized { get; } = Constraints<LinkManifest>
		.Always(
			m => m.UrlPathPrefix,
			IsNormalizedPrefix,
			describe: prefix => $"url_path_prefix '{prefix}' is not normalized: it is either empty or '/x' with no trailing slash."
		)
		.WithSeverity(Severity.Warning)
		.Describe("P42: url_path_prefix is normalized to '/x' or empty; the build coerces any other value silently.");

	public static Constraint<LinkManifest> LinkManifestPageUrlsUnique { get; } = Constraints<LinkManifest>
		.Unique(m => m.Links.Keys, PageUrl)
		.WithSeverity(Severity.Warning)
		.Describe(
			"N1: Two pages produce one URL (for example 'foo.md' and 'foo/index.md'); cross-links to either resolve to the same address."
		);

	public static Constraint<LinkManifest> PagePathSingleMdExtension { get; } = Constraints<LinkManifest>
		.Each(
			m => m.Links.Keys,
			p => !(p.EndsWith(".md", StringComparison.Ordinal) ? p[..^3] : p).Contains(".md", StringComparison.Ordinal),
			p =>
				$"Page path '{p}' contains '.md' besides its extension; cross-link resolution removes every '.md', so its URL would be '{PageUrl(p)}'."
		)
		.WithSeverity(Severity.Warning)
		.Describe("N21: A page path contains '.md' only as its extension, because URL resolution removes every '.md'.");

	public static Constraint<LinkManifest> CrossLinkSchemeAllowed { get; } = Constraints<LinkManifest>
		.Each<string>(
			m => m.CrossLinks,
			link => CrossLinkValidator.IsValidCrossLink(link, out var error) ? null : error ?? $"'{link}' is not a cross-link."
		)
		.WithSeverity(Severity.Warning)
		.Describe(
			"N19: Every cross_links entry is an absolute URI with a repository scheme; http, https, ftp, file, tel, jdbc and mailto are never cross-links."
		);

	public static Constraint<LinkManifest> PageAnchorsUnique { get; } = Constraints<LinkManifest>.EachCheck(
		m => m.Links,
		(_, l) => (l.Value?.DuplicateAnchors ?? []).Select(
			a => Violation.Warning($"Page '{l.Key}' declares the anchor '{a}' more than once; the duplicates overwrite each other.")
		)
	).Describe("M37: A page declares each anchor once (ignoring case); a repeated slug overwrites the first.");

	public static Constraint<LinkManifest> RedirectSourceIsNotAPage { get; } = Constraints<LinkManifest>
		.Disjoint(
			m => Redirects(m).Select(r => r.Key),
			m => m.Links.Keys,
			StringComparer.OrdinalIgnoreCase,
			describe: source => $"Redirect '{source}' shadows an existing page of the same path; the redirect wins."
		)
		.WithSeverity(Severity.Warning)
		.Describe(
			"N25: A redirect source is not also a page, because a redirect is checked before the page and wins even when the page still exists."
		);

	public static Constraint<LinkManifest> RedirectCatchAllAfterExact { get; } = Constraints<LinkManifest>
		.Each(Redirects, ExactRulesAfterCatchAll)
		.WithSeverity(Severity.Warning)
		.Describe("N26: In a redirect's 'many' list no exact-anchor rule follows a catch-all ('!') rule, because the first catch-all wins.");

	public static Constraint<LinkManifest> RedirectEmptyAnchorsKeepsAnchor { get; } = Constraints<LinkManifest>
		.Each(
			Redirects,
			r => r.Value.Anchors is not { Count: 0 },
			r => $"Redirect '{r.Key}' has empty anchors ('{{}}'), which keeps the anchor; write '!' to drop it."
		)
		.WithSeverity(Severity.Warning)
		.Describe("N27: A redirect's empty anchors map ('{}') keeps the anchor; it is not a catch-all, which is written '!'.");

	public static Constraint<LinkManifest> RedirectSamePageNeedsAnchors { get; } = Constraints<LinkManifest>
		.Each(
			Redirects,
			r => !string.IsNullOrEmpty(r.Value.To) || r.Value.Many is { Length: > 0 } || r.Value.Anchors is { Count: > 0 },
			r => $"Redirect '{r.Key}' has no 'to' and no anchors, so it redirects the page onto itself."
		)
		.WithSeverity(Severity.Hint)
		.Describe("N28: A redirect without a 'to' remaps anchors on the same page, so it declares anchors or 'many' rules.");

	public static Constraint<LinkManifest> RedirectNoChains { get; } = Constraints<LinkManifest>
		.NoChains(LocalRedirects, r => r.Source, r => r.Target, StringComparer.OrdinalIgnoreCase)
		.WithSeverity(Severity.Warning)
		.Describe("N30: A local redirect target does not itself redirect onwards; redirect chains are not followed.");

	public static Constraint<LinkManifest> RedirectTargetAnchorsExist { get; } = Constraints<LinkManifest>.EachCheck(
		Redirects,
		RedirectAnchorsThatTargetsLack
	).Describe("N30: An anchor a redirect maps to exists on the local page it targets; target anchors are not validated.");

	public static Constraint<LinkManifest> RedirectTargetInNavigation { get; } = Constraints<LinkManifest>
		.Each(
			RedirectTargetsOutsideNavigation,
			r => $"Redirect '{r.Source}' targets '{r.Target}', which is not a page in the manifest (not in the navigation)."
		)
		.WithSeverity(Severity.Warning)
		.Describe(
			"N31: A local redirect target is a published page of the manifest (a target that is itself a redirect is a chain, reported by N30); a target need not be in the navigation."
		);

	/// <summary>The URL a page path is published at: every '.md' removed, then a trailing '/index', exactly as cross-link resolution does.</summary>
	public static string PageUrl(string path)
	{
		var url = path.Replace(".md", "", StringComparison.Ordinal);
		if (url.EndsWith("/index", StringComparison.OrdinalIgnoreCase))
			url = url[..^6];
		return url == "index" ? string.Empty : url;
	}

	/// <summary>The page path a cross-link looks up in the target's manifest: its host and path, without the scheme or fragment.</summary>
	public static string LookupPath(Uri uri)
	{
		var path = (uri.Host + '/' + uri.AbsolutePath.TrimStart('/')).Trim('/');
		return path.Length == 0 && uri.Host.EndsWith(".md", StringComparison.OrdinalIgnoreCase) ? uri.Host : path;
	}

	/// <summary>True for a redirect target in another repository (<c>scheme://path</c>); http and https are external, not cross-repository.</summary>
	public static bool IsCrossRepositoryTarget(string target, out Uri uri) =>
		Uri.TryCreate(target, UriKind.Absolute, out uri!) && uri.Scheme is not ("http" or "https");

	/// <summary>Every redirect of the manifest (none when it has none).</summary>
	public static IEnumerable<KeyValuePair<string, LinkRedirect>> Redirects(LinkManifest manifest) =>
		(IEnumerable<KeyValuePair<string, LinkRedirect>>?)manifest.Redirects ?? [];

	/// <summary>Every <c>to</c> of every redirect, top-level and in 'many' rules, with the page it redirects from. Empty targets are left out.</summary>
	public static IEnumerable<(string Source, string Target)> RedirectTargets(LinkManifest manifest) =>
		Redirects(manifest).SelectMany(r => RuleTargets(r.Value).Select(t => (r.Key, t)));

	private static IEnumerable<string> RuleTargets(LinkRedirect redirect) =>
		new[] { redirect.To }.Concat((redirect.Many ?? []).Select(s => s.To)).OfType<string>().Where(t => t.Length > 0);

	private static IEnumerable<(string Source, string Target)> LocalRedirects(LinkManifest manifest) =>
		RedirectTargets(manifest).Where(r => !r.Target.Contains("://", StringComparison.Ordinal));

	private static bool IsNormalizedPrefix(string? prefix) =>
		string.IsNullOrEmpty(prefix) || (prefix.StartsWith('/') && prefix.Length > 1 && !prefix.EndsWith('/') && prefix.Trim() == prefix);

	private static string? ExactRulesAfterCatchAll(KeyValuePair<string, LinkRedirect> entry)
	{
		var (source, redirect) = entry;
		var rules = (redirect.Many ?? []).Where(r => !string.IsNullOrEmpty(r.To) && r.Anchors is { Count: > 0 }).ToList();
		var catchAll = rules.FindIndex(r => r.Anchors!.ContainsKey("!"));
		if (catchAll < 0)
			return null;
		var shadowed = rules.Skip(catchAll + 1).Where(r => !r.Anchors!.ContainsKey("!")).SelectMany(r => r.Anchors!.Keys).ToList();
		return shadowed.Count > 0
			? $"Redirect '{source}' has exact anchor rule(s) ({string.Join(", ", shadowed)}) after a catch-all ('!') rule, which wins first, so they are never used."
			: null;
	}

	private static IEnumerable<Violation> RedirectAnchorsThatTargetsLack(LinkManifest manifest, KeyValuePair<string, LinkRedirect> entry)
	{
		var (source, redirect) = entry;
		LinkSingleRedirect[] rules = [redirect, .. redirect.Many ?? []];
		foreach (var rule in rules)
		{
			if (string.IsNullOrEmpty(rule.To) || rule.To.Contains("://", StringComparison.Ordinal) || rule.Anchors is null)
				continue;
			if (!manifest.Links.TryGetValue(rule.To, out var target) || target.Anchors is null)
				continue;
			foreach (var mapped in rule.Anchors.Values.Where(a => !string.IsNullOrEmpty(a) && a != "!").OfType<string>())
			{
				if (!target.Anchors.Contains(mapped, StringComparer.OrdinalIgnoreCase))
					yield return Violation.Warning($"Redirect '{source}' maps an anchor to '#{mapped}', which '{rule.To}' does not have.");
			}
		}
	}

	private static IEnumerable<(string Source, string Target)> RedirectTargetsOutsideNavigation(LinkManifest manifest) =>
		LocalRedirects(manifest).Where(r => !manifest.Links.ContainsKey(r.Target) && !Redirects(manifest).Any(x => x.Key == r.Target));
}
