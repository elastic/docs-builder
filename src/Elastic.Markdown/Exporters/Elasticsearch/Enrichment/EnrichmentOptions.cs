// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Markdown.Exporters.Elasticsearch.Enrichment;

/// <summary>
/// Configuration options for document enrichment.
/// </summary>
public sealed record EnrichmentOptions
{
	/// <summary>
	/// Whether enrichment is enabled.
	/// </summary>
	public bool Enabled { get; init; }

	/// <summary>
	/// Maximum new enrichments per run. Limits LLM calls to prevent long deployments.
	/// </summary>
	public int MaxNewEnrichmentsPerRun { get; init; } = 100;

	/// <summary>
	/// Maximum concurrent LLM calls.
	/// </summary>
	public int MaxConcurrentLlmCalls { get; init; } = 4;

	/// <summary>
	/// Creates options with enrichment disabled.
	/// </summary>
	public static EnrichmentOptions Disabled => new() { Enabled = false };
}
