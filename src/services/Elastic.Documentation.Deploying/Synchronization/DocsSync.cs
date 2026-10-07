// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using System.Text.Json.Serialization;
using Elastic.Documentation.Diagnostics;

namespace Elastic.Documentation.Deploying.Synchronization;

public interface IDocsSyncPlanStrategy
{
	Task<Result<SyncPlan>> Plan(float? deleteThreshold, string[] excludePatterns, Cancel ctx = default);
}

public record PlanValidationResult(bool Valid, float DeleteRatio, float DeleteThreshold);

public interface IDocsSyncApplyStrategy
{
	Task Apply(SyncPlan plan, Cancel ctx = default);
}

public record SyncRequest;

public record DeleteRequest : SyncRequest
{
	[JsonPropertyName("destination_path")]
	public required string DestinationPath { get; init; }
}

public record UploadRequest : SyncRequest
{
	[JsonPropertyName("local_path")]
	public required string LocalPath { get; init; }

	[JsonPropertyName("destination_path")]
	public required string DestinationPath { get; init; }
}

public record AddRequest : UploadRequest;

public record UpdateRequest : UploadRequest;

public record SkipRequest : SyncRequest
{
	[JsonPropertyName("local_path")]
	public required string LocalPath { get; init; }

	[JsonPropertyName("destination_path")]
	public required string DestinationPath { get; init; }
}

/// <summary>What an incremental deploy will do: the add, update, delete and skip requests the planner worked out, with the counts and the thresholds it used.</summary>
/// <remarks>
/// Constructed with <c>SyncPlanLoader.Planned</c> or <c>SyncPlanLoader.Load</c>.
/// </remarks>
public sealed record SyncPlan
{
	/// The user-specified delete threshold
	[JsonPropertyName("deletion_threshold_default")]
	public required float? DeleteThresholdDefault { get; init; }

	/// Glob patterns excluded from both local upload and remote deletion
	[JsonPropertyName("exclude_patterns")]
	public required string[] ExcludePatterns { get; init; }

	/// The user-specified delete threshold
	[JsonPropertyName("remote_listing_completed")]
	public required bool RemoteListingCompleted { get; init; }

	/// The total number of source files that were located in the build output
	[JsonPropertyName("total_source_files")]
	public required int TotalSourceFiles { get; init; }

	/// The total number of remote files that were located in the remote location
	[JsonPropertyName("total_remote_files")]
	public required int TotalRemoteFiles { get; init; }

	/// The total number of sync requests that were generated (sum of <see cref="AddRequests"/>, <see cref="UpdateRequests"/>, <see cref="DeleteRequests"/>)
	[JsonPropertyName("total_sync_requests")]
	public required int TotalSyncRequests { get; init; }

	[JsonPropertyName("delete")]
	public required IReadOnlyList<DeleteRequest> DeleteRequests { get; init; }

	[JsonPropertyName("add")]
	public required IReadOnlyList<AddRequest> AddRequests { get; init; }

	[JsonPropertyName("update")]
	public required IReadOnlyList<UpdateRequest> UpdateRequests { get; init; }

	[JsonPropertyName("skip")]
	public required IReadOnlyList<SkipRequest> SkipRequests { get; init; }

	/// <summary>The delete threshold when none was given: no more than a fifth of the remote files may be deleted.</summary>
	public const float DefaultDeleteThreshold = 0.2f;

	/// <summary>
	/// The fraction of the remote files this plan may delete: <see cref="DeleteThresholdDefault"/> (or 0.2), raised to 0.8 for at most 100 remote
	/// files and to 0.5 for at most 1,000 so a young bucket can change quickly, and 0 for an empty bucket, which has nothing to delete.
	/// </summary>
	[JsonIgnore]
	public float DeleteThreshold => EffectiveDeleteThreshold(out _);

	/// <summary>The fraction of the remote files this plan deletes.</summary>
	[JsonIgnore]
	public float DeleteRatio => (float)DeleteRequests.Count / TotalRemoteFiles;

	/// <summary><see cref="DeleteThreshold"/>, with the reason it was changed from the default, or null when it was not.</summary>
	public float EffectiveDeleteThreshold(out string? reason)
	{
		var threshold = DeleteThresholdDefault ?? DefaultDeleteThreshold;
		reason = null;
		if (TotalRemoteFiles == 0)
		{
			reason =
				"No files discovered in S3, assuming a clean bucket resetting delete threshold to `0.0' as our plan should not have ANY deletions";
			return 0.0f;
		}
		// a plan against 100 remote files or fewer is in flux, so it may delete up to 0.8
		if (TotalRemoteFiles <= 100)
		{
			reason = "Plan has less than 100 total remote files ensuring delete threshold is at minimum 0.8";
			return Math.Max(threshold, 0.8f);
		}
		if (TotalRemoteFiles <= 1000)
		{
			reason = "Plan has less than 1000 but more than a 100 total remote files ensuring delete threshold is at minimum 0.5";
			return Math.Max(threshold, 0.5f);
		}
		return threshold;
	}

	public static string Serialize(SyncPlan plan) => JsonSerializer.Serialize(plan, SyncSerializerContext.Default.SyncPlan);

	/// <summary>The raw reader of the plan's wire format; internal: <see cref="SyncPlanLoader.Load"/> checks what it reads.</summary>
	internal static SyncPlan Deserialize(string json) =>
		JsonSerializer.Deserialize(json, SyncSerializerContext.Default.SyncPlan)
			?? throw new JsonException("Failed to deserialize SyncPlan from JSON");
}

[JsonSourceGenerationOptions(WriteIndented = true, UseStringEnumConverter = true)]
[JsonSerializable(typeof(SyncPlan))]
[JsonSerializable(typeof(AddRequest))]
[JsonSerializable(typeof(UpdateRequest))]
[JsonSerializable(typeof(DeleteRequest))]
[JsonSerializable(typeof(SkipRequest))]
public sealed partial class SyncSerializerContext : JsonSerializerContext;
