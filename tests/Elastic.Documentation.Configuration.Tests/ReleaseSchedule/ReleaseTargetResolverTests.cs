// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Configuration.ReleaseSchedule;

namespace Elastic.Documentation.Configuration.Tests.ReleaseSchedule;

public class ReleaseTargetResolverTests
{
	// ── ResolveCurrentGa ────────────────────────────────────────────────

	[Test]
	public void ResolveCurrentGa_ReturnsHighestVersionWithManifest()
	{
		var past = new PastReleasesResponse
		{
			Releases =
			[
				new PastRelease { Version = "9.1.0", Manifest = "https://staging.elastic.co/9.1.0/manifest.json" },
				new PastRelease { Version = "9.2.4", Manifest = "https://staging.elastic.co/9.2.4/manifest.json" },
				new PastRelease { Version = "9.2.3", Manifest = "https://staging.elastic.co/9.2.3/manifest.json" },
			],
		};

		var result = ReleaseTargetResolver.ResolveCurrentGa(past);

		result.Should().NotBeNull();
		result!.Version.Should().Be("9.2.4");
		result.Source.Should().Be(ReleaseTargetSource.CurrentGa);
	}

	[Test]
	public void ResolveCurrentGa_IgnoresReleasesWithoutManifest()
	{
		var past = new PastReleasesResponse
		{
			Releases =
			[
				new PastRelease { Version = "9.2.4", Manifest = null },
				new PastRelease { Version = "9.1.0", Manifest = "https://staging.elastic.co/9.1.0/manifest.json" },
			],
		};

		var result = ReleaseTargetResolver.ResolveCurrentGa(past);

		result!.Version.Should().Be("9.1.0");
	}

	[Test]
	public void ResolveCurrentGa_OrdersByVersionNotString_SoV9Point10BeatsV9Point9()
	{
		var past = new PastReleasesResponse
		{
			Releases =
			[
				new PastRelease { Version = "9.9.0", Manifest = "https://staging.elastic.co/9.9.0/manifest.json" },
				new PastRelease { Version = "9.10.0", Manifest = "https://staging.elastic.co/9.10.0/manifest.json" },
			],
		};

		var result = ReleaseTargetResolver.ResolveCurrentGa(past);

		result!.Version.Should().Be("9.10.0", "numeric comparison must put 9.10.0 above 9.9.0");
	}

	[Test]
	public void ResolveCurrentGa_WhenNoPastReleases_ReturnsNull()
	{
		var result = ReleaseTargetResolver.ResolveCurrentGa(null);
		result.Should().BeNull();
	}

	// ── ResolvePreviewTargets ────────────────────────────────────────────

	private static FutureRelease FutureWithBc(string version, string manifestUrl) =>
		new()
		{
			Version = version,
			BuildCandidates = new Dictionary<string, BuildCandidateEntry>
			{
				["bc-id"] = new BuildCandidateEntry { ManifestUrl = manifestUrl },
			},
		};

	private static FutureRelease FutureNoBc(string version) => new() { Version = version };

	private static LatestBuildPointer Snapshot(string version, string manifestUrl) =>
		new() { Version = version, BuildId = "snap", ManifestUrl = manifestUrl };

	[Test]
	public void ResolvePreviewTargets_NoBcs_ReturnsTwoSnapshots()
	{
		var past = new PastReleasesResponse { Releases = [new PastRelease { Version = "9.2.4", Manifest = "x" }], };
		var future = new FutureReleasesResponse { Releases = [FutureNoBc("9.2.5"), FutureNoBc("9.3.0")], };
		var patchSnap = Snapshot("9.2.5-SNAPSHOT", "https://snapshots.elastic.co/patch.json");
		var minorSnap = Snapshot("9.3.0-SNAPSHOT", "https://snapshots.elastic.co/master.json");

		var results = ReleaseTargetResolver.ResolvePreviewTargets(past, future, patchSnap, minorSnap, "9.2.4");

		results.Should().HaveCount(2);
		results[0].Version.Should().Be("9.2.5");
		results[0].Source.Should().Be(ReleaseTargetSource.Snapshot);
		results[1].Version.Should().Be("9.3.0");
		results[1].Source.Should().Be(ReleaseTargetSource.Snapshot);
	}

	[Test]
	public void ResolvePreviewTargets_PatchBcPlusMinorSnapshot_ReturnsBoth()
	{
		var past = new PastReleasesResponse { Releases = [new PastRelease { Version = "9.2.4", Manifest = "x" }], };
		var future = new FutureReleasesResponse
		{
			Releases = [FutureWithBc("9.2.5", "https://staging.elastic.co/bc-9.2.5.json"), FutureNoBc("9.3.0")],
		};
		var minorSnap = Snapshot("9.3.0-SNAPSHOT", "https://snapshots.elastic.co/master.json");

		var results = ReleaseTargetResolver.ResolvePreviewTargets(past, future, null, minorSnap, "9.2.4");

		results.Should().HaveCount(2);
		results[0].Version.Should().Be("9.2.5");
		results[0].Source.Should().Be(ReleaseTargetSource.BuildCandidate);
		results[0].ManifestUrl.Should().Be("https://staging.elastic.co/bc-9.2.5.json");
		results[1].Version.Should().Be("9.3.0");
		results[1].Source.Should().Be(ReleaseTargetSource.Snapshot);
	}

	[Test]
	public void ResolvePreviewTargets_BothBcs_ReturnsBothAsBuildCandidates()
	{
		var past = new PastReleasesResponse { Releases = [new PastRelease { Version = "9.2.4", Manifest = "x" }], };
		var future = new FutureReleasesResponse
		{
			Releases =
			[
				FutureWithBc("9.2.5", "https://staging.elastic.co/bc-9.2.5.json"),
				FutureWithBc("9.3.0", "https://staging.elastic.co/bc-9.3.0.json"),
			],
		};

		var results = ReleaseTargetResolver.ResolvePreviewTargets(past, future, null, null, "9.2.4");

		results.Should().HaveCount(2);
		results[0].Source.Should().Be(ReleaseTargetSource.BuildCandidate);
		results[1].Source.Should().Be(ReleaseTargetSource.BuildCandidate);
	}

	[Test]
	public void ResolvePreviewTargets_AlreadyGaVersionSkipped()
	{
		// 9.2.5 is already in past-releases, so the patch target falls back to the SNAPSHOT pointer.
		// The SNAPSHOT version after stripping -SNAPSHOT is also 9.2.5, which is GA — so it is skipped.
		var past = new PastReleasesResponse
		{
			Releases = [new PastRelease { Version = "9.2.4", Manifest = "x" }, new PastRelease { Version = "9.2.5", Manifest = "x" },],
		};
		var future = new FutureReleasesResponse { Releases = [FutureNoBc("9.3.0")], };
		var patchSnap = Snapshot("9.2.5-SNAPSHOT", "https://snapshots.elastic.co/patch.json");
		var minorSnap = Snapshot("9.3.0-SNAPSHOT", "https://snapshots.elastic.co/master.json");

		var results = ReleaseTargetResolver.ResolvePreviewTargets(past, future, patchSnap, minorSnap, "9.2.5");

		results.Should().HaveCount(1);
		results[0].Version.Should().Be("9.3.0");
	}

	[Test]
	public void ResolvePreviewTargets_SemverOrderingPicksEarliestMinor()
	{
		// future-releases has 9.10.0 and 9.3.0; 9.3.0 is the earliest next minor after 9.2.4.
		var past = new PastReleasesResponse { Releases = [new PastRelease { Version = "9.2.4", Manifest = "x" }], };
		var future = new FutureReleasesResponse { Releases = [FutureNoBc("9.10.0"), FutureNoBc("9.3.0")], };
		var minorSnap = Snapshot("9.3.0-SNAPSHOT", "https://snapshots.elastic.co/master.json");

		var results = ReleaseTargetResolver.ResolvePreviewTargets(past, future, null, minorSnap, "9.2.4");

		// Only the minor target is resolved (no patch BC and no patch snapshot given).
		results.Should().HaveCount(1);
		results[0].Version.Should().Be("9.3.0");
	}

	// ── Latest build candidate ───────────────────────────────────────────

	[Test]
	public void LatestBuildCandidate_PicksNewestCompletedAt_NotLastInObjectOrder()
	{
		var release = new FutureRelease
		{
			Version = "9.5.5",
			BuildCandidates = new Dictionary<string, BuildCandidateEntry>
			{
				["9.5.5-newest"] = new() { ManifestUrl = "newest", CompletedAt = "2026-10-03T10:00:00Z" },
				["9.5.5-oldest"] = new() { ManifestUrl = "oldest", CompletedAt = "2026-10-01T10:00:00Z" },
				["9.5.5-middle"] = new() { ManifestUrl = "middle", CompletedAt = "2026-10-02T10:00:00Z" },
			},
		};

		release.LatestBuildCandidate!.ManifestUrl.Should().Be("newest");
	}

	[Test]
	public void LatestBuildCandidate_WithoutParseableTimestamps_FallsBackToLastEntry()
	{
		var release = new FutureRelease
		{
			Version = "9.5.5",
			BuildCandidates = new Dictionary<string, BuildCandidateEntry>
			{
				["a"] = new() { ManifestUrl = "first", CompletedAt = null },
				["b"] = new() { ManifestUrl = "last", CompletedAt = "not a date" },
			},
		};

		release.LatestBuildCandidate!.ManifestUrl.Should().Be("last");
	}

	[Test]
	public void LatestBuildCandidate_IgnoresEntriesWithoutTimestampWhenOthersHaveOne()
	{
		var release = new FutureRelease
		{
			Version = "9.5.5",
			BuildCandidates = new Dictionary<string, BuildCandidateEntry>
			{
				["dated"] = new() { ManifestUrl = "dated", CompletedAt = "2026-10-01T10:00:00Z" },
				["undated"] = new() { ManifestUrl = "undated", CompletedAt = null },
			},
		};

		release.LatestBuildCandidate!.ManifestUrl.Should().Be("dated");
	}

	[Test]
	public void LatestBuildCandidate_NoCandidates_ReturnsNull() => FutureNoBc("9.5.5").LatestBuildCandidate.Should().BeNull();

	// ── GA detection ─────────────────────────────────────────────────────

	[Test]
	public void ResolvePreviewTargets_PastReleaseWithoutManifestIsNotGa_SoPatchPreviewIsKept()
	{
		var past = new PastReleasesResponse
		{
			Releases =
			[
				new PastRelease { Version = "9.2.4", Manifest = "https://staging.elastic.co/9.2.4/manifest.json" },
				new PastRelease { Version = "9.2.5", Manifest = null },
			],
		};
		var future = new FutureReleasesResponse { Releases = [FutureWithBc("9.2.5", "https://staging.elastic.co/bc-9.2.5.json")], };

		var results = ReleaseTargetResolver.ResolvePreviewTargets(past, future, null, null, "9.2.4");

		results.Should().ContainSingle().Which.Version.Should().Be("9.2.5");
	}

	[Test]
	public void IsGa_RequiresAManifest()
	{
		var past = new PastReleasesResponse
		{
			Releases =
			[
				new PastRelease { Version = "9.2.4", Manifest = "x" },
				new PastRelease { Version = "9.2.5", Manifest = null },
				new PastRelease { Version = "9.2.6", Manifest = "" },
			],
		};

		ReleaseTargetResolver.IsGa(past, "9.2.4").Should().BeTrue();
		ReleaseTargetResolver.IsGa(past, "9.2.5").Should().BeFalse();
		ReleaseTargetResolver.IsGa(past, "9.2.6").Should().BeFalse();
		ReleaseTargetResolver.IsGa(past, "9.9.9").Should().BeFalse();
		ReleaseTargetResolver.IsGa(null, "9.2.4").Should().BeFalse();
	}
}
