// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Api.Core.AskAi;

/// <summary>
/// Transforms raw SSE streams from various AI gateways into canonical AskAiEvent format
/// </summary>
public interface IStreamTransformer
{
	/// <summary>
	/// Transforms a raw SSE stream into a stream of AskAiEvent objects
	/// </summary>
	/// <param name="rawStream">Raw SSE stream from gateway (Agent Builder, LLM Gateway, etc.)</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Stream containing SSE-formatted AskAiEvent objects</returns>
	Task<Stream> TransformAsync(Stream rawStream, CancellationToken cancellationToken = default);
}
