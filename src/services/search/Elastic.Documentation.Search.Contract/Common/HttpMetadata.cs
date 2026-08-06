// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Serialization;
using Elastic.Mapping;

namespace Elastic.Documentation.Search.Contract;

/// <summary>
/// HTTP caching metadata used to drive incremental crawl sync. Nested under
/// <see cref="ICrawlDocument.Http"/> (JSON <c>http.*</c>).
/// </summary>
public record HttpMetadata
{
	[Keyword(Index = false)]
	[JsonPropertyName("etag")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Etag { get; set; }

	[JsonPropertyName("last_modified")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public DateTimeOffset? LastModified { get; set; }
}
