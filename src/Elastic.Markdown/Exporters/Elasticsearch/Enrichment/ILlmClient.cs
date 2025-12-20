// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Markdown.Exporters.Elasticsearch.Enrichment;

/// <summary>
/// Abstraction for LLM inference operations.
/// Enables swapping implementations and testing.
/// </summary>
public interface ILlmClient : IDisposable
{
	/// <summary>
	/// Generates enrichment data for the given document content.
	/// </summary>
	/// <param name="title">The document title.</param>
	/// <param name="body">The document body content.</param>
	/// <param name="ct">Cancellation token.</param>
	/// <returns>The enrichment data if successful, null otherwise.</returns>
	Task<EnrichmentData?> EnrichAsync(string title, string body, CancellationToken ct);
}
