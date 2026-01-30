// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Serialization;

namespace Elastic.Documentation.Search;

/// <summary>
/// JSON-serializable product record for Elasticsearch indexing.
/// Only contains id and repository - name/version are looked up dynamically by product id.
/// </summary>
public record IndexedProduct
{
	/// <summary>
	/// The product ID from products.yml (e.g., "elasticsearch", "kibana", "apm-agent-java")
	/// </summary>
	[JsonPropertyName("id")]
	public string? Id { get; init; }

	/// <summary>
	/// The repository name (e.g., "elasticsearch", "docs-content", "elastic-otel-java")
	/// </summary>
	[JsonPropertyName("repository")]
	public string? Repository { get; init; }
}
