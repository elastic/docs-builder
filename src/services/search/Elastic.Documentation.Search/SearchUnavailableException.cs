// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Transport;
using Elastic.Transport.Products.Elasticsearch;

namespace Elastic.Documentation.Search;

/// <summary>
/// Thrown by the search layer when the underlying Elasticsearch cluster is temporarily
/// unavailable — e.g. a cold serverless endpoint hasn't finished scaling, a request
/// exceeded <see cref="Elastic.Transport.TransportConfiguration.RequestTimeout"/>,
/// or the cluster returned HTTP 429 (too many requests).
/// <para>
/// This is transient: callers should surface it to users / MCP clients as a retryable
/// error rather than a permanent failure.
/// </para>
/// </summary>
public class SearchUnavailableException(string message, Exception? innerException = null) : Exception(message, innerException)
{
	public static SearchUnavailableException Transient(object? reason, Exception? innerException = null) =>
		new($"Search backend is temporarily unavailable ({reason}). Transient — retry in a few seconds.", innerException);

	/// <summary>A timeout, exhausted retries, or an overloaded cluster: worth retrying in a few seconds.</summary>
	public static bool IsTransient(TransportException ex) =>
		ex.FailureReason is PipelineFailure.MaxTimeoutReached or PipelineFailure.MaxRetriesReached
			|| (ex.FailureReason is PipelineFailure.BadResponse && IsOverloaded(ex.ApiCallDetails?.HttpStatusCode));

	/// <summary>The same test for an invalid response the client returned instead of throwing.</summary>
	public static bool IsTransient(ElasticsearchResponse response) =>
		IsOverloaded(response.ApiCallDetails?.HttpStatusCode)
			|| (response.ApiCallDetails?.OriginalException is TransportException ex && IsTransient(ex));

	private static bool IsOverloaded(int? statusCode) => statusCode is 429 or 503;
}
