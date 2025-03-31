// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Serialization;
using Elastic.Markdown.Exporters;
using Elastic.Markdown.IO.Discovery;

namespace Elastic.Markdown.IO.State;

public record GenerationState
{
	[JsonPropertyName("last_seen_changes")]
	public required DateTimeOffset LastSeenChanges { get; init; }

	[JsonPropertyName("invalid_files")]
	public required string[] InvalidFiles { get; init; } = [];

	[JsonPropertyName("exporter")]
	public string Exporter { get; init; } = nameof(DocumentationFileExporter);

	[JsonPropertyName("git")]
	public required GitCheckoutInformation Git { get; init; }
}
