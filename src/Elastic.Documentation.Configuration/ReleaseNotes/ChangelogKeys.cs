// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;

namespace Elastic.Documentation.Configuration.ReleaseNotes;

/// <summary>
/// The single source of truth for the changelog artifact-root S3/CDN key layout:
/// bundles live under <c>bundle/{product}/{file}</c>, individual changelog entries under
/// <c>changelog/{org}/{repo}/{branch}/{file}</c>, and each grouping has a <c>registry.json</c>
/// manifest at its root. Centralizes key construction, group extraction, and per-segment
/// validation so the producer (<c>ChangelogUploadService</c>), the scrubber Lambda gate,
/// the registry builder, and the CDN fetchers cannot drift apart.
/// </summary>
/// <remarks>
/// Segment character classes, from strictest to loosest: org is a GitHub login
/// (ASCII alphanumerics and hyphens), product additionally allows underscores, and
/// repo/branch segments additionally allow dots (repos like <c>apm-agent-dotnet</c> and
/// branches like <c>8.x</c>). Empty segments and the traversal segments <c>.</c> / <c>..</c>
/// are always rejected. Branches are stored verbatim, so a branch's own <c>/</c> become
/// real key segments.
/// </remarks>
public static class ChangelogKeys
{
	public const string BundlePrefix = "bundle/";
	public const string ChangelogPrefix = "changelog/";
	public const string RegistryFileName = "registry.json";

	private const string RegistrySuffix = "/" + RegistryFileName;

	/// <summary>The bundle segment kinds, used to pick the allowed character class.</summary>
	private enum SegmentKind
	{
		/// <summary>Product class: ASCII alphanumerics, <c>_</c> and <c>-</c>.</summary>
		Product,

		/// <summary>GitHub login class: ASCII alphanumerics and <c>-</c> only.</summary>
		Org,

		/// <summary>Repo / branch-part class: the product class plus <c>.</c>.</summary>
		RepoOrBranch
	}

	/// <summary>True when <paramref name="product"/> is a valid bundle product segment (<c>[a-zA-Z0-9_-]+</c>).</summary>
	public static bool IsValidProduct([NotNullWhen(true)] string? product) =>
		IsValidSegment(product, SegmentKind.Product);

	/// <summary>True when <paramref name="org"/> is a valid GitHub owner segment (<c>[a-zA-Z0-9-]+</c>).</summary>
	public static bool IsValidOrg([NotNullWhen(true)] string? org) =>
		IsValidSegment(org, SegmentKind.Org);

	/// <summary>True when <paramref name="repo"/> is a valid repository segment (<c>[a-zA-Z0-9._-]+</c>, not <c>.</c>/<c>..</c>).</summary>
	public static bool IsValidRepo([NotNullWhen(true)] string? repo) =>
		IsValidSegment(repo, SegmentKind.RepoOrBranch);

	/// <summary>
	/// True when every <c>/</c>-delimited part of <paramref name="branch"/> is a valid repo-class segment.
	/// Branches are stored verbatim, so each part becomes a real key segment and is validated on its own.
	/// </summary>
	public static bool IsValidBranch([NotNullWhen(true)] string? branch)
	{
		if (string.IsNullOrEmpty(branch))
			return false;

		foreach (var part in branch.Split('/'))
		{
			if (!IsValidSegment(part, SegmentKind.RepoOrBranch))
				return false;
		}
		return true;
	}

	/// <summary>
	/// True when <paramref name="fileName"/> is a safe single path segment: non-empty, not <c>.</c>/<c>..</c>,
	/// and free of path separators. Guards against traversal or nested keys sneaking in via a registry.
	/// </summary>
	public static bool IsSafeFileName([NotNullWhen(true)] string? fileName) =>
		!string.IsNullOrWhiteSpace(fileName)
		&& fileName is not ("." or "..")
		&& !fileName.Contains('/', StringComparison.Ordinal)
		&& !fileName.Contains('\\', StringComparison.Ordinal);

	/// <summary>The artifact-root key of an uploaded bundle file: <c>bundle/{product}/{file}</c>.</summary>
	public static string BundleFileKey(string product, string fileName) =>
		$"{BundlePrefix}{product}/{fileName}";

	/// <summary>The artifact-root key of an uploaded changelog entry: <c>changelog/{org}/{repo}/{branch}/{file}</c>.</summary>
	public static string ChangelogFileKey(string org, string repo, string branch, string fileName) =>
		$"{ChangelogPrefix}{org}/{repo}/{branch}/{fileName}";

	/// <summary>The bundle-index manifest key for a product group: <c>bundle/{product}/registry.json</c>.</summary>
	public static string BundleRegistryKey(string productGroup) =>
		$"{BundlePrefix}{productGroup}/{RegistryFileName}";

	/// <summary>The changelog-entry-index manifest key for an <c>{org}/{repo}/{branch}</c> group: <c>changelog/{group}/registry.json</c>.</summary>
	public static string ChangelogRegistryKey(string poolGroup) =>
		$"{ChangelogPrefix}{poolGroup}/{RegistryFileName}";

	/// <summary>
	/// Extracts the product group from a <c>bundle/{product}/{file}</c> key, or null when
	/// <paramref name="s3Key"/> is not a bundle key with a product segment ahead of the file name.
	/// </summary>
	public static string? ExtractBundleGroup(string s3Key)
	{
		if (!s3Key.StartsWith(BundlePrefix, StringComparison.Ordinal))
			return null;

		var rest = s3Key.AsSpan(BundlePrefix.Length);
		var slash = rest.IndexOf('/');
		return slash <= 0 ? null : rest[..slash].ToString();
	}

	/// <summary>
	/// Extracts the <c>{org}/{repo}/{branch}</c> group from a <c>changelog/{org}/{repo}/{branch}/{file}</c>
	/// key, or null when <paramref name="s3Key"/> is not a changelog key with at least three group segments
	/// ahead of the file name. The branch may carry extra slashes, so the group is everything before the
	/// final segment.
	/// </summary>
	public static string? ExtractChangelogGroup(string s3Key)
	{
		if (!s3Key.StartsWith(ChangelogPrefix, StringComparison.Ordinal))
			return null;

		var rest = s3Key.AsSpan(ChangelogPrefix.Length);
		var lastSlash = rest.LastIndexOf('/');
		if (lastSlash <= 0)
			return null;

		var group = rest[..lastSlash];
		return CountSegments(group) >= 3 ? group.ToString() : null;
	}

	/// <summary>The CDN path segments of a product's bundle pool (<c>["bundle", product]</c>), for per-segment URI escaping.</summary>
	public static IReadOnlyList<string> BundleSegments(string product) =>
		["bundle", product];

	/// <summary>
	/// The CDN path segments of an org/repo/branch changelog pool — <c>changelog</c>, org, repo, then each
	/// <c>/</c>-delimited part of the branch — so a branch's slashes stay real key separators rather than
	/// being percent-encoded into a single segment.
	/// </summary>
	public static IReadOnlyList<string> PoolSegments(string org, string repo, string branch) =>
		["changelog", org, repo, .. branch.Split('/')];

	/// <summary>
	/// Returns true when <paramref name="key"/> is a manifest of either artifact-root form
	/// <c>bundle/{product}/registry.json</c> (exactly one product segment) or
	/// <c>changelog/{org}/{repo}/{branch}/registry.json</c> (at least three segments, validated
	/// per position with the same rules the producer enforces on upload).
	/// </summary>
	/// <remarks>
	/// Used by the changelog scrubber Lambda to decide whether to pass an incoming
	/// <c>*.json</c> object through to the public bucket. Anything else (a bare
	/// <c>{x}/registry.json</c>, a changelog manifest shallower than <c>org/repo/branch</c>, or an unknown
	/// top-level prefix) is rejected, which keeps arbitrary JSON out of the public surface.
	/// </remarks>
	public static bool IsRegistry(string key)
	{
		if (string.IsNullOrEmpty(key))
			return false;

		return IsBundleRegistry(key) || IsChangelogRegistry(key);
	}

	private static bool IsBundleRegistry(string key) =>
		TryGetRegistryGroup(key, BundlePrefix, out var group)
		&& group.IndexOf('/') < 0
		&& IsValidSegment(group, SegmentKind.Product);

	private static bool IsChangelogRegistry(string key)
	{
		if (!TryGetRegistryGroup(key, ChangelogPrefix, out var group))
			return false;

		var segments = 0;
		var start = 0;
		for (var i = 0; i <= group.Length; i++)
		{
			if (i != group.Length && group[i] != '/')
				continue;

			// Per-position rules: org, then repo, then one-or-more branch parts.
			var kind = segments switch
			{
				0 => SegmentKind.Org,
				_ => SegmentKind.RepoOrBranch
			};
			if (!IsValidSegment(group[start..i], kind))
				return false;

			segments++;
			start = i + 1;
		}

		return segments >= 3;
	}

	/// <summary>Slices the middle group out of a <c>{prefix}{group}/registry.json</c> key.</summary>
	private static bool TryGetRegistryGroup(string key, string prefix, out ReadOnlySpan<char> group)
	{
		group = default;
		if (!key.StartsWith(prefix, StringComparison.Ordinal) || !key.EndsWith(RegistrySuffix, StringComparison.Ordinal))
			return false;

		var middleLength = key.Length - prefix.Length - RegistrySuffix.Length;
		if (middleLength <= 0)
			return false;

		group = key.AsSpan(prefix.Length, middleLength);
		return true;
	}

	private static bool IsValidSegment(string? segment, SegmentKind kind) =>
		segment is not null && IsValidSegment(segment.AsSpan(), kind);

	private static bool IsValidSegment(ReadOnlySpan<char> segment, SegmentKind kind)
	{
		if (segment.IsEmpty || segment is "." or "..")
			return false;

		foreach (var c in segment)
		{
			if (!IsValidSegmentChar(c, kind))
				return false;
		}
		return true;
	}

	private static bool IsValidSegmentChar(char c, SegmentKind kind)
	{
		if (char.IsAsciiLetterOrDigit(c) || c == '-')
			return true;

		return kind switch
		{
			SegmentKind.Product => c == '_',
			SegmentKind.RepoOrBranch => c is '_' or '.',
			_ => false
		};
	}

	/// <summary>Counts the <c>/</c>-delimited segments in a joined path span (empty span → 0).</summary>
	private static int CountSegments(ReadOnlySpan<char> path)
	{
		if (path.IsEmpty)
			return 0;

		var count = 1;
		foreach (var c in path)
		{
			if (c == '/')
				count++;
		}
		return count;
	}
}
