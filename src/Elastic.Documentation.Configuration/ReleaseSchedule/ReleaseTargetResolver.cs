// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Versions;

namespace Elastic.Documentation.Configuration.ReleaseSchedule;

/// <summary>
/// How a release target's manifest was resolved.
/// </summary>
public enum ReleaseTargetSource
{
	CurrentGa,
	BuildCandidate,
	Snapshot,
}

/// <summary>
/// A resolved release target: a version, its manifest URL, and how it was resolved.
/// </summary>
public sealed record ReleaseTarget(string Version, string ManifestUrl, ReleaseTargetSource Source);

/// <summary>
/// Pure static resolver for DRA bundle targets. Takes in-memory API responses and snapshot
/// pointers and produces an ordered list of <see cref="ReleaseTarget"/> values.
/// No HTTP calls — all inputs are pre-fetched by the caller, keeping this unit-testable.
/// </summary>
public static class ReleaseTargetResolver
{
	/// <summary>
	/// Resolves the current GA release from <paramref name="past"/>.
	/// Returns <c>null</c> when no released version with a manifest exists.
	/// </summary>
	public static ReleaseTarget? ResolveCurrentGa(PastReleasesResponse? past)
	{
		if (past is null)
			return null;

		var latest = past
			.Releases
			.Where(r => !string.IsNullOrEmpty(r.Manifest))
			.Select(r => (release: r, version: TryParseVersion(r.Version)))
			.Where(t => t.version is not null)
			.OrderByDescending(t => t.version)
			.Select(t => t.release)
			.FirstOrDefault();

		return latest is null ? null : new ReleaseTarget(latest.Version, latest.Manifest!, ReleaseTargetSource.CurrentGa);
	}

	/// <summary>
	/// Resolves preview targets: the next patch release and the next minor release.
	/// Each uses its BC manifest if one has been cut; otherwise uses the supplied SNAPSHOT pointer.
	/// A target is omitted when it is already GA (appears in <paramref name="past"/>).
	/// </summary>
	/// <param name="past">Past releases used to detect already-GA versions.</param>
	/// <param name="future">Future releases listing upcoming versions and their BCs.</param>
	/// <param name="patchSnapshotPointer">
	/// SNAPSHOT pointer for the current patch branch (e.g. <c>9.2.json</c>). <c>null</c> skips the
	/// patch target when no BC exists.
	/// </param>
	/// <param name="minorSnapshotPointer">
	/// SNAPSHOT pointer for the next minor branch (<c>master.json</c>). <c>null</c> skips the minor
	/// target when no BC exists.
	/// </param>
	/// <param name="currentGaVersion">
	/// The current GA version string (e.g. <c>"9.2.4"</c>), used to distinguish the patch and minor
	/// release lines. Pass <c>null</c> to skip patch-target resolution entirely.
	/// </param>
	public static IReadOnlyList<ReleaseTarget> ResolvePreviewTargets(
		PastReleasesResponse? past,
		FutureReleasesResponse? future,
		LatestBuildPointer? patchSnapshotPointer,
		LatestBuildPointer? minorSnapshotPointer,
		string? currentGaVersion
	)
	{
		var results = new List<ReleaseTarget>(2);
		var gaVersions = BuildGaSet(past);

		var currentGa = currentGaVersion is not null ? TryParseVersion(currentGaVersion) : null;

		// Next patch — same major.minor, higher patch.
		if (currentGa is not null)
		{
			var nextPatch = ResolveFutureTarget(
				future,
				gaVersions,
				r =>
				{
					var v = TryParseVersion(r.Version);
					return v is not null && v.Major == currentGa.Major && v.Minor == currentGa.Minor && v > currentGa;
				},
				patchSnapshotPointer
			);
			if (nextPatch is not null)
				results.Add(nextPatch);
		}

		// Next minor — higher minor than the current GA.
		{
			var nextMinor = ResolveFutureTarget(
				future,
				gaVersions,
				r =>
				{
					var v = TryParseVersion(r.Version);
					return v is not null
						&& (currentGa is null || v.Major > currentGa.Major || (v.Major == currentGa.Major && v.Minor > currentGa.Minor));
				},
				minorSnapshotPointer
			);
			if (nextMinor is not null)
				results.Add(nextMinor);
		}

		return results;
	}

	private static ReleaseTarget? ResolveFutureTarget(
		FutureReleasesResponse? future,
		HashSet<string> gaVersions,
		Func<FutureRelease, bool> filter,
		LatestBuildPointer? snapshotPointer
	)
	{
		if (future is not null)
		{
			var candidate = future
				.Releases
				.Where(r => filter(r) && !gaVersions.Contains(r.Version))
				.Select(r => (release: r, version: TryParseVersion(r.Version)))
				.Where(t => t.version is not null)
				.OrderBy(t => t.version)
				.Select(t => t.release)
				.FirstOrDefault();

			if (candidate is not null && candidate.HasBuildCandidate && candidate.LatestBuildCandidate is { } bc)
				return new ReleaseTarget(candidate.Version, bc.ManifestUrl, ReleaseTargetSource.BuildCandidate);
		}

		if (snapshotPointer is not null && TryParseVersion(snapshotPointer.Version) is { } sv)
		{
			var version = $"{sv.Major}.{sv.Minor}.{sv.Patch}";
			if (!gaVersions.Contains(version))
				return new ReleaseTarget(version, snapshotPointer.ManifestUrl, ReleaseTargetSource.Snapshot);
		}

		return null;
	}

	private static HashSet<string> BuildGaSet(PastReleasesResponse? past) =>
		past is null ? [] : new HashSet<string>(past.Releases.Select(r => r.Version), StringComparer.OrdinalIgnoreCase);

	/// <summary>
	/// Parses a semver string (including prerelease suffixes like <c>-SNAPSHOT</c>) into a
	/// <see cref="SemVersion"/> for numeric comparison. Returns <c>null</c> for malformed strings.
	/// </summary>
	public static SemVersion? TryParseVersion(string? versionString) =>
		!string.IsNullOrEmpty(versionString) && SemVersion.TryParse(versionString, out var v) ? v : null;
}
