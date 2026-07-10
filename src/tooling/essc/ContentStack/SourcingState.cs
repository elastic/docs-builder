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

	/// <summary>
	/// Content types that exist in Contentstack but must never be synced or indexed — e.g. content
	/// types still being authored that aren't ready to appear in search yet.
	/// </summary>
	public static readonly string[] Blocked =
	[
		"blog_landing",
		"example_link",
		"glossary",
		"examples_landing",
		"integrations_landing",
		"labs_integration",
		"labs_homepage",
		"notebook",
		"series",
		"tutorials_landing",
		"tutorial",
		"tutorial_page",
		"tutorial_chapter",
		"blog_v3"
	];

	/// <summary>
	/// Content types that are never expected to represent a standalone page — reusable components,
	/// taxonomy/tags, navigation config, redirects, etc. Unlike <see cref="Blocked"/>, these aren't
	/// candidates for future syncing.
	/// </summary>
	public static readonly string[] KnownNonPages =
	[
		"Sub_Navigation",
		"accordion_faq",
		"accordion_table",
		"alert_bar",
		"auto_linking_settings",
		"auto_linking_term",
		"banner",
		"blog_categories",
		"blog_disclaimer",
		"boilerplate",
		"callout",
		"card",
		"card_carousel",
		"carousel",
		"category_cta",
		"cloud_region_locations",
		"cloud_regions_service_provider",
		"code_reference",
		"column_listing",
		"contact_languages",
		"contact_regions",
		"contact_worldwide_offices",
		"content_gallery",
		"content_promos",
		"contributors",
		"customer_content_type",
		"customer_industry",
		"customer_use_case",
		"date_field",
		"featured_split_listing",
		"features",
		"footer",
		"footer_cta",
		"gdpr_text",
		"glossary_term",
		"hero",
		"hero_grid",
		"image_alternative_text",
		"image_reference",
		"image_video",
		"integration_category",
		"integration_detail",
		"integration_solution",
		"listing_with_sidebar",
		"logo_bar",
		"marketo",
		"marketo_form_split",
		"meetup_events",
		"press_contact",
		"product_names",
		"quotes",
		"quotes_carousel",
		"redirects",
		"serve_pdfs",
		"showcase_carousel",
		"site_navigation_reference",
		"sitemap_management",
		"tab_navigation_reference",
		"table",
		"tags_audience",
		"tags_campaigns",
		"tags_content_type",
		"tags_culture",
		"tags_demo_features",
		"tags_demo_solutions",
		"tags_demo_type",
		"tags_elastic_stack",
		"tags_event_delivery",
		"tags_event_type",
		"tags_industry",
		"tags_language",
		"tags_meta",
		"tags_partner",
		"tags_region",
		"tags_role",
		"tags_stage",
		"tags_teams",
		"tags_technical_level",
		"tags_topic",
		"tags_use_case",
		"text_image_video",
		"title_text_reference",
		"translate_content",
		"translate_content_redesign",
		"video_reference",
		"video_type",
		"vidyard_reference"
	];
}
