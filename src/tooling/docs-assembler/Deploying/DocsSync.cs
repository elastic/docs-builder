// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Documentation.Assembler.Deploying;

public interface IDocsSyncPlanStrategy
{
	Task<SyncPlan> Plan(Cancel ctx = default);

	(bool, float) Validate(SyncPlan plan, float deleteThreshold);
}

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
	[JsonPropertyName("total_source_files")]
	public required int TotalSourceFiles { get; init; }

	[JsonPropertyName("total_files_to_sync")]
	public required int TotalFilesToSync { get; init; }

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
