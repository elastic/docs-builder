// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Documentation.Assembler.Deploying;

public interface IDocsSyncPlanStrategy
{
	Task<SyncPlan> Plan(float? deleteThreshold, Cancel ctx = default);

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

public record SyncPlan
{
	/// The user-specified delete threshold
	[JsonPropertyName("deletion_threshold_default")]
	public required float? DeleteThresholdDefault { get; init; }

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

	public static string Serialize(SyncPlan plan) => JsonSerializer.Serialize(plan, SyncSerializerContext.Default.SyncPlan);

	public static SyncPlan Deserialize(string json) =>
		JsonSerializer.Deserialize(json, SyncSerializerContext.Default.SyncPlan) ??
		throw new JsonException("Failed to deserialize SyncPlan from JSON");
}

[JsonSourceGenerationOptions(WriteIndented = true, UseStringEnumConverter = true)]
[JsonSerializable(typeof(SyncPlan))]
[JsonSerializable(typeof(AddRequest))]
[JsonSerializable(typeof(UpdateRequest))]
[JsonSerializable(typeof(DeleteRequest))]
[JsonSerializable(typeof(SkipRequest))]
public sealed partial class SyncSerializerContext : JsonSerializerContext;
