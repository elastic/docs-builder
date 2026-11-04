// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Elastic.SiteSearch.Cli.ContentStack;

[JsonSerializable(typeof(SyncResponse))]
internal sealed partial class SyncJsonContext : JsonSerializerContext;

internal sealed class SyncResponse
{
	[JsonPropertyName("items")]
	public List<SyncItem> Items { get; set; } = [];

	[JsonPropertyName("skip")]
	public int Skip { get; set; }

	[JsonPropertyName("limit")]
	public int Limit { get; set; }

	[JsonPropertyName("total_count")]
	public int TotalCount { get; set; }

	[JsonPropertyName("pagination_token")]
	public string? PaginationToken { get; set; }

	[JsonPropertyName("sync_token")]
	public string? SyncToken { get; set; }
}

internal sealed class SyncItem
{
	[JsonPropertyName("type")]
	public string Type { get; set; } = string.Empty;

	[JsonPropertyName("event_at")]
	public string? EventAt { get; set; }

	[JsonPropertyName("content_type_uid")]
	public string? ContentTypeUid { get; set; }

	[JsonPropertyName("data")]
	public JsonElement? Data { get; set; }
}
