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
/// <para><b>Built by:</b> <c>SyncPlanLoader.Planned</c> (the planner, <c>IDocsSyncPlanStrategy.Plan</c>) and <c>SyncPlanLoader.Load</c> (a plan read back from JSON), both in <c>Elastic.Documentation.Deploying</c>, checked once through <c>SyncPlanLoader.Check</c>.</para>
/// <para><b>Why it cannot be built from outside:</b> Its members are <c>internal init</c>: only <c>Elastic.Documentation.Deploying</c> and its test project (<c>Elastic.Documentation.Deploying.Tests</c>, through <c>InternalsVisibleTo</c>) can set them (the domain's grant does not reach this assembly), so a record that reaches a service has been through a loader or a checked operation.</para>
/// <para><b>Members that must always be set</b> (they were <c>required</c> before the type was sealed; C# forbids <c>required</c> on a member whose setter is less visible than the type, CS9032): <c>DeleteThresholdDefault</c>, <c>ExcludePatterns</c>, <c>RemoteListingCompleted</c>, <c>TotalSourceFiles</c>, <c>TotalRemoteFiles</c>, <c>TotalSyncRequests</c>, <c>DeleteRequests</c>, <c>AddRequests</c>, <c>UpdateRequests</c>, <c>SkipRequests</c>. A loader or test that leaves one out gets the default; the aggregate's constraint set (<c>SyncPlanConstraints</c>) is where a missing member is reported, and the loader's mapping is what normally sets them.</para>
/// <para><b>In a test:</b> build it with an object initializer (<c>new SyncPlan { ... }</c>) with every member above, and <c>with</c> for variations; <c>SyncPlanProperties</c> (tests) generates plans.</para>
/// <para><b>Serialised:</b> <c>[JsonInclude]</c> on the internal setters and <c>[JsonRequired]</c> stand in for the lost <c>required</c>: the serializer cannot reach an internal setter without the first, and a missing key is still a parse error with the second.</para>
/// </remarks>
public sealed record SyncPlan
{
	/// The user-specified delete threshold
	[JsonPropertyName("deletion_threshold_default")]
	[JsonInclude]
	[JsonRequired]
	public float? DeleteThresholdDefault { get; internal init; }

	/// Glob patterns excluded from both local upload and remote deletion
	[JsonPropertyName("exclude_patterns")]
	[JsonInclude]
	[JsonRequired]
	public string[] ExcludePatterns { get; internal init; } = default!;

	/// The user-specified delete threshold
	[JsonPropertyName("remote_listing_completed")]
	[JsonInclude]
	[JsonRequired]
	public bool RemoteListingCompleted { get; internal init; }

	/// The total number of source files that were located in the build output
	[JsonPropertyName("total_source_files")]
	[JsonInclude]
	[JsonRequired]
	public int TotalSourceFiles { get; internal init; }

	/// The total number of remote files that were located in the remote location
	[JsonPropertyName("total_remote_files")]
	[JsonInclude]
	[JsonRequired]
	public int TotalRemoteFiles { get; internal init; }

	/// The total number of sync requests that were generated (sum of <see cref="AddRequests"/>, <see cref="UpdateRequests"/>, <see cref="DeleteRequests"/>)
	[JsonPropertyName("total_sync_requests")]
	[JsonInclude]
	[JsonRequired]
	public int TotalSyncRequests { get; internal init; }

	[JsonPropertyName("delete")]
	[JsonInclude]
	[JsonRequired]
	public IReadOnlyList<DeleteRequest> DeleteRequests { get; internal init; } = default!;

	[JsonPropertyName("add")]
	[JsonInclude]
	[JsonRequired]
	public IReadOnlyList<AddRequest> AddRequests { get; internal init; } = default!;

	[JsonPropertyName("update")]
	[JsonInclude]
	[JsonRequired]
	public IReadOnlyList<UpdateRequest> UpdateRequests { get; internal init; } = default!;

	[JsonPropertyName("skip")]
	[JsonInclude]
	[JsonRequired]
	public IReadOnlyList<SkipRequest> SkipRequests { get; internal init; } = default!;

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
