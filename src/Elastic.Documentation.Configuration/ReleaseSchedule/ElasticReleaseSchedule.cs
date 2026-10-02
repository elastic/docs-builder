// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Serialization;

namespace Elastic.Documentation.Configuration.ReleaseSchedule;

/// <summary>
/// Types for the two public Elastic release schedule endpoints:
///   https://elastic-release-api.s3.us-west-2.amazonaws.com/public/future-releases.json
///   https://elastic-release-api.s3.us-west-2.amazonaws.com/public/past-releases.json
/// </summary>
public sealed record FutureReleasesResponse
{
	[JsonPropertyName("releases")]
	public required FutureRelease[] Releases { get; init; }
}

public sealed record FutureRelease
{
	[JsonPropertyName("version")]
	public required string Version { get; init; }

	[JsonPropertyName("feature_freeze_date")]
	public string? FeatureFreezeDate { get; init; }

	[JsonPropertyName("build_candidates_schedule")]
	public BcScheduleEntry[]? BuildCandidatesSchedule { get; init; }

	/// <summary>
	/// Populated when a BC has been cut. Key = build-id (e.g. "9.5.5-40327eb4"),
	/// value contains manifest_url and completed_at.
	/// </summary>
	[JsonPropertyName("build_candidates")]
	public Dictionary<string, BuildCandidateEntry>? BuildCandidates { get; init; }

	public bool HasBuildCandidate => BuildCandidates is { Count: > 0 };

	/// <summary>Latest BC, or null when none has been cut yet.</summary>
	public BuildCandidateEntry? LatestBuildCandidate => BuildCandidates is { Count: > 0 } ? BuildCandidates.Values.Last() : null;
}

public sealed record BcScheduleEntry
{
	[JsonPropertyName("bc_date")]
	public string? BcDate { get; init; }
}

public sealed record BuildCandidateEntry
{
	[JsonPropertyName("manifest_url")]
	public required string ManifestUrl { get; init; }

	[JsonPropertyName("completed_at")]
	public string? CompletedAt { get; init; }

	[JsonPropertyName("date_removed")]
	public string? DateRemoved { get; init; }
}

public sealed record PastReleasesResponse
{
	[JsonPropertyName("releases")]
	public required PastRelease[] Releases { get; init; }
}

public sealed record PastRelease
{
	[JsonPropertyName("version")]
	public required string Version { get; init; }

	[JsonPropertyName("public_release_date")]
	public string? PublicReleaseDate { get; init; }

	[JsonPropertyName("manifest")]
	public string? Manifest { get; init; }
}

/// <summary>
/// Staging manifest shape. Both SNAPSHOT and BC manifests share this structure.
/// https://staging.elastic.co/{build_id}/manifest-{version}.json
/// https://snapshots.elastic.co/{build_id}/manifest-{version}-SNAPSHOT.json
/// </summary>
public sealed record ElasticBuildManifest
{
	[JsonPropertyName("version")]
	public required string Version { get; init; }

	[JsonPropertyName("build_id")]
	public required string BuildId { get; init; }

	[JsonPropertyName("projects")]
	public required Dictionary<string, ManifestProject> Projects { get; init; }
}

public sealed record ManifestProject
{
	[JsonPropertyName("branch")]
	public string? Branch { get; init; }

	[JsonPropertyName("commit_hash")]
	public string? CommitHash { get; init; }

	[JsonPropertyName("commit_url")]
	public string? CommitUrl { get; init; }
}

/// <summary>
/// Latest-build pointer returned by:
///   https://snapshots.elastic.co/latest/{branch}.json
///   https://staging.elastic.co/latest/{branch}.json
/// </summary>
public sealed record LatestBuildPointer
{
	[JsonPropertyName("version")]
	public required string Version { get; init; }

	[JsonPropertyName("build_id")]
	public required string BuildId { get; init; }

	[JsonPropertyName("manifest_url")]
	public required string ManifestUrl { get; init; }
}
