// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Search.Contract;

/// <summary>
/// Marker for documents sourced by crawl-based pipelines that need HTTP caching fields
/// for incremental sync (ETag / Last-Modified).
/// </summary>
public interface ICrawlDocument : ISearchDocument
{
	string? HttpEtag { get; set; }
	DateTimeOffset? HttpLastModified { get; set; }
}
