// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Markdown.Myst.Directives.Diagram;

/// <summary>
/// Shared HttpClient for diagram downloads to avoid resource exhaustion
/// </summary>
public static class DiagramHttpClient
{
	private static readonly Lazy<HttpClient> LazyHttpClient = new(() => new HttpClient
	{
		Timeout = TimeSpan.FromSeconds(30)
	});

	/// <summary>
	/// Shared HttpClient instance for diagram downloads
	/// </summary>
	public static HttpClient Instance => LazyHttpClient.Value;
}
