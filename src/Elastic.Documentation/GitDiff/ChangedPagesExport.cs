// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using System.Text.Json.Serialization;
using Elastic.Documentation.Serialization;

namespace Elastic.Documentation.GitDiff;

public static class ChangedPagesExportFile
{
	public const string FileName = "changed-pages.json";

	public static string Serialize(ChangedPagesExport export) =>
		JsonSerializer.Serialize(export, SourceGenerationContext.Default.ChangedPagesExport);
}

public record ChangedPagesExport
{
	[JsonPropertyName("base")]
	public required string Base { get; init; }

	[JsonPropertyName("config_changed")]
	public bool ConfigChanged { get; init; }

	[JsonPropertyName("pages")]
	public required IReadOnlyList<ChangedPageEntry> Pages { get; init; }

	[JsonPropertyName("deleted")]
	public required IReadOnlyList<DeletedPageEntry> Deleted { get; init; }
}

public record ChangedPageEntry
{
	[JsonPropertyName("source_path")]
	public required string SourcePath { get; init; }

	[JsonPropertyName("url")]
	public required string Url { get; init; }

	[JsonPropertyName("title")]
	public required string Title { get; init; }

	[JsonPropertyName("change")]
	public required string Change { get; init; }

	[JsonPropertyName("included_from")]
	public required IReadOnlyList<string> IncludedFrom { get; init; }
}

public record DeletedPageEntry
{
	[JsonPropertyName("source_path")]
	public required string SourcePath { get; init; }
}
