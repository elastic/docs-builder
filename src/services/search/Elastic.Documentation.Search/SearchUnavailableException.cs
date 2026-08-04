// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

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
public class SearchUnavailableException(string message, Exception? innerException = null)
	: Exception(message, innerException);
