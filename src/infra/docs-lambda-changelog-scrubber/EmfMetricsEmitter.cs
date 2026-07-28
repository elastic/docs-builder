// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text;
using System.Text.Json;
using Elastic.Changelog.Reconciliation;

namespace Elastic.Documentation.Lambda.ChangelogScrubber;

/// <summary>
/// Emits the per-invocation reconcile counters as a CloudWatch Embedded Metric Format line
/// (elastic/docs-eng-team#688 Phase 0 observability: these numbers gate any later SQS/Lambda
/// tuning). Written with <see cref="Utf8JsonWriter"/> directly — no serializer registration, so
/// nothing for AOT trimming to miss — and to stdout unwrapped, which is what the EMF parser
/// requires.
/// </summary>
internal static class EmfMetricsEmitter
{
	private const string Namespace = "docs-changelog-scrubber";

	private static readonly (string Name, Func<ReconcileMetrics, int> Value)[] Counters =
	[
		("ObjectReconciles", m => m.ObjectReconciles),
		("ObjectReconcileRetries", m => m.ObjectReconcileRetries),
		("GroupReconciles", m => m.GroupReconciles),
		("RegistryWrites", m => m.RegistryWrites),
		("RegistryDeletes", m => m.RegistryDeletes),
		("RegistryUnchanged", m => m.RegistryUnchanged),
		("WriteConflicts", m => m.WriteConflicts),
		("ObjectsListed", m => m.ObjectsListed),
		("EntriesRecomputed", m => m.EntriesRecomputed),
		("FailedMessages", m => m.FailedMessages)
	];

	public static void Emit(ReconcileMetrics metrics)
	{
		using var buffer = new MemoryStream();
		using (var writer = new Utf8JsonWriter(buffer))
		{
			writer.WriteStartObject();

			writer.WritePropertyName("_aws");
			writer.WriteStartObject();
			writer.WriteNumber("Timestamp", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
			writer.WritePropertyName("CloudWatchMetrics");
			writer.WriteStartArray();
			writer.WriteStartObject();
			writer.WriteString("Namespace", Namespace);
			writer.WritePropertyName("Dimensions");
			writer.WriteStartArray();
			writer.WriteStartArray();
			writer.WriteEndArray();
			writer.WriteEndArray();
			writer.WritePropertyName("Metrics");
			writer.WriteStartArray();
			foreach (var (name, _) in Counters)
			{
				writer.WriteStartObject();
				writer.WriteString("Name", name);
				writer.WriteString("Unit", "Count");
				writer.WriteEndObject();
			}
			writer.WriteEndArray();
			writer.WriteEndObject();
			writer.WriteEndArray();
			writer.WriteEndObject();

			foreach (var (name, value) in Counters)
				writer.WriteNumber(name, value(metrics));

			writer.WriteEndObject();
		}

		Console.WriteLine(Encoding.UTF8.GetString(buffer.ToArray()));
	}
}
