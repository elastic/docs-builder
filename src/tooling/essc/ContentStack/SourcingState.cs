// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Elastic.SiteSearch.Cli.ContentStack;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(ContentTypesState))]
[JsonSerializable(typeof(SyncCursorState))]
[JsonSerializable(typeof(SyncCursorMap))]
internal sealed partial class StateJsonContext : JsonSerializerContext;

internal sealed class ContentTypesState
{
	[JsonPropertyName("content_types")]
	public Dictionary<string, ContentTypeEntry> ContentTypes { get; set; } = [];

	[JsonPropertyName("pagination_token")]
	public string? PaginationToken { get; set; }

	[JsonPropertyName("completed")]
	public bool Completed { get; set; }

	[JsonPropertyName("total_items_seen")]
	public int TotalItemsSeen { get; set; }
}

internal sealed class ContentTypeEntry
{
	[JsonPropertyName("uid")]
	public string Uid { get; set; } = "";

	[JsonPropertyName("total")]
	public int Total { get; set; }

	[JsonPropertyName("with_url")]
	public int WithUrl { get; set; }

	[JsonPropertyName("sample_urls")]
	public List<string> SampleUrls { get; set; } = [];

	public void Ingest(SyncItem item)
	{
		Total++;

		if (item.Data is not { } data)
			return;

		if (!data.TryGetProperty("url", out var urlProp)
			|| urlProp.ValueKind != JsonValueKind.String
			|| string.IsNullOrWhiteSpace(urlProp.GetString()))
			return;

		WithUrl++;
		if (SampleUrls.Count < 3)
			SampleUrls.Add(urlProp.GetString()!);
	}
}

internal sealed class SyncCursorState
{
	[JsonPropertyName("sync_token")]
	public string? SyncToken { get; set; }

	[JsonPropertyName("pagination_token")]
	public string? PaginationToken { get; set; }

	[JsonPropertyName("items_processed")]
	public int ItemsProcessed { get; set; }
}

/// <summary>
/// Multi-cursor state for parallel per-content-type sync.
/// Maps content_type_uid to its individual cursor.
/// </summary>
internal sealed class SyncCursorMap
{
	[JsonPropertyName("cursors")]
	public Dictionary<string, SyncCursorState> Cursors { get; set; } = [];
}

internal static class PageContentTypes
{
	public static readonly string[] All =
	[
		"product_versions",
		"videos",
		"blog",
		"blog_v2",
		"customer_tile",
		"product_detail",
		"use_cases",
		"forms",
		"default_detail",
		"faq",
		"agreements",
		"press",
		"account_based_marketing",
		"product_icons",
		"blog_category_detail",
		"demo_gallery_detail",
		"downloads_redesign",
		"features_overview",
		"contact_redesign",
		"videos_overview",
		"site_navigation",
		"press_overview",
		"elasticon_videos_overview",
		"integrations",
		"blog_overview",
		"cloud_regions",
		"support_matrix",
		"pricing_redesign",
		"subscriptions_redesign",
		"subscriptions_cloud",
		"customers_overview",
		"search",
		"pricing_calculator",
		"about_leadership_and_board",
		"past_releases",
		"about_our_source_code",
		"demo_gallery_overview",
		"timeline",
		"events_overview",
		"blog_archive_overview"
	];
}
