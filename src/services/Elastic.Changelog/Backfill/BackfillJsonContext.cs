// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Serialization;

namespace Elastic.Changelog.Backfill;

/// <summary>
/// Source-generated JSON serialization for the backfill documents (required for AOT:
/// every serialized type must be registered here or serialization fails at runtime once
/// the binary is trimmed). Property names are snake_case, enums serialize as their
/// kebab-case names, and null properties are omitted — the same convention the rest of
/// the changelog pipeline uses on the wire.
/// </summary>
[JsonSourceGenerationOptions(
	WriteIndented = true,
	PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
	UseStringEnumConverter = true,
	DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
	RespectNullableAnnotations = true
)]
[JsonSerializable(typeof(BackfillEnvelope<InventoryDocument>))]
[JsonSerializable(typeof(BackfillEnvelope<OverridesDocument>))]
[JsonSerializable(typeof(BackfillEnvelope<SemanticModelDocument>))]
[JsonSerializable(typeof(BackfillEnvelope<PlanDocument>))]
[JsonSerializable(typeof(BackfillEnvelope<ProvenanceDocument>))]
[JsonSerializable(typeof(BackfillEnvelope<LedgerDocument>))]
public sealed partial class BackfillJsonContext : JsonSerializerContext;
