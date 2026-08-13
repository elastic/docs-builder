// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using System.Text.Json.Serialization;
using Elastic.Changelog.Reconciliation;

namespace Elastic.Documentation.Lambda.ChangelogScrubber;

/// <summary>
/// Emits the per-invocation reconcile counters as a CloudWatch Embedded Metric Format line
/// (elastic/docs-eng-team#688 Phase 0 observability: these numbers gate any later SQS/Lambda
/// tuning). The counter names are static, so the payload is a fixed source-generated contract;
/// it is written to stdout unwrapped, which is what the EMF parser requires.
/// </summary>
internal static class EmfMetricsEmitter
{
	private const string Namespace = "docs-changelog-scrubber";

	private static readonly IReadOnlyList<EmfMetricDefinition> MetricDefinitions =
	[
		new() { Name = "ObjectReconciles" },
		new() { Name = "ObjectReconcileRetries" },
		new() { Name = "GroupReconciles" },
		new() { Name = "RegistryWrites" },
		new() { Name = "RegistryDeletes" },
		new() { Name = "RegistryUnchanged" },
		new() { Name = "WriteConflicts" },
		new() { Name = "ObjectsListed" },
		new() { Name = "EntriesRecomputed" },
		new() { Name = "ShallowRegistryWrites" },
		new() { Name = "ShallowRegistryUnchanged" },
		new() { Name = "FailedMessages" }
	];

	public static void Emit(ReconcileMetrics metrics)
	{
		var payload = new EmfPayload
		{
			Aws = new EmfEnvelope
			{
				Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
				CloudWatchMetrics =
				[
					new EmfMetricDirective
					{
						Namespace = Namespace,
						Dimensions = [[]],
						Metrics = MetricDefinitions
					}
				]
			},
			ObjectReconciles = metrics.ObjectReconciles,
			ObjectReconcileRetries = metrics.ObjectReconcileRetries,
			GroupReconciles = metrics.GroupReconciles,
			RegistryWrites = metrics.RegistryWrites,
			RegistryDeletes = metrics.RegistryDeletes,
			RegistryUnchanged = metrics.RegistryUnchanged,
			WriteConflicts = metrics.WriteConflicts,
			ObjectsListed = metrics.ObjectsListed,
			EntriesRecomputed = metrics.EntriesRecomputed,
			ShallowRegistryWrites = metrics.ShallowRegistryWrites,
			ShallowRegistryUnchanged = metrics.ShallowRegistryUnchanged,
			FailedMessages = metrics.FailedMessages
		};

		Console.WriteLine(JsonSerializer.Serialize(payload, EmfJsonContext.Default.EmfPayload));
	}
}

/// <summary>One EMF log line: the <c>_aws</c> envelope plus the metric values as top-level members.</summary>
internal sealed record EmfPayload
{
	[JsonPropertyName("_aws")]
	public required EmfEnvelope Aws { get; init; }

	public required int ObjectReconciles { get; init; }
	public required int ObjectReconcileRetries { get; init; }
	public required int GroupReconciles { get; init; }
	public required int RegistryWrites { get; init; }
	public required int RegistryDeletes { get; init; }
	public required int RegistryUnchanged { get; init; }
	public required int WriteConflicts { get; init; }
	public required int ObjectsListed { get; init; }
	public required int EntriesRecomputed { get; init; }
	public required int ShallowRegistryWrites { get; init; }
	public required int ShallowRegistryUnchanged { get; init; }
	public required int FailedMessages { get; init; }
}

internal sealed record EmfEnvelope
{
	public required long Timestamp { get; init; }
	public required IReadOnlyList<EmfMetricDirective> CloudWatchMetrics { get; init; }
}

internal sealed record EmfMetricDirective
{
	public required string Namespace { get; init; }
	public required IReadOnlyList<IReadOnlyList<string>> Dimensions { get; init; }
	public required IReadOnlyList<EmfMetricDefinition> Metrics { get; init; }
}

internal sealed record EmfMetricDefinition
{
	public required string Name { get; init; }
	public string Unit { get; init; } = "Count";
}

[JsonSerializable(typeof(EmfPayload))]
internal sealed partial class EmfJsonContext : JsonSerializerContext;
