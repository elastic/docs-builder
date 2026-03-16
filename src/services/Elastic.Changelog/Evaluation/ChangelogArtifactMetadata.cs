// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Serialization;
using Elastic.Documentation.Configuration.Changelog;
using Elastic.Documentation.ReleaseNotes;

namespace Elastic.Changelog.Evaluation;

/// <summary>Artifact metadata transferred between the generate and commit CI workflows.</summary>
public record ChangelogArtifactMetadata
{
	public required int PrNumber { get; init; }
	public required string HeadRef { get; init; }
	public required string HeadSha { get; init; }
	public required string Status { get; init; }
	public string? LabelTable { get; init; }
	public string? ConfigFile { get; init; }
	public string? ChangelogDir { get; init; }
	public string? ChangelogFilename { get; init; }
	public CreateRules? CreateRules { get; init; }
}

[JsonSourceGenerationOptions(
	WriteIndented = true,
	UseStringEnumConverter = true,
	PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower
)]
[JsonSerializable(typeof(ChangelogArtifactMetadata))]
[JsonSerializable(typeof(CreateRules))]
[JsonSerializable(typeof(FieldMode))]
[JsonSerializable(typeof(MatchMode))]
public sealed partial class ChangelogArtifactMetadataJsonContext : JsonSerializerContext;
