// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Elastic.Documentation.Search.Contract;
using Elastic.SiteSearch.Cli.Elasticsearch;

namespace Elastic.SiteSearch.Cli.ContentStack;

internal static partial class ContentStackMapper
{
	public static SiteDocument? ToSiteDocument(SyncItem item)
	{
		if (item.Data is not { } data)
			return null;

		var rawUrl = GetString(data, "url");
		if (string.IsNullOrWhiteSpace(rawUrl))
			return null;

		// ContentStack's top-level `locale` field is the entry's fixed master/base locale (almost
		// always "en-us") — it never changes across sync events. The locale actually being
		// published *in this event* lives at `publish_details.locale`. Reading the top-level field
		// here would make every locale-specific publish of the same entry resolve to the identical
		// (unprefixed English) path, so concurrent locale publishes race each other on the same
		// Elasticsearch document id (surfacing as version_conflict_engine_exception).
		//
		// ContentStack's `url` field is not locale-scoped: the same entry gets "published" under
		// several locales while keeping the exact same (English-authored) url. The live site
		// resolves any published locale at /{locale-prefix}{url} (e.g. /es/support/matrix), so
		// namespace non-master-locale variants under their site-served prefix before using the
		// url as our document id — otherwise every locale variant collides on one Elasticsearch
		// document and whichever synced last silently wins.
		var publishLocale = GetNestedString(data, "publish_details", "locale") ?? GetString(data, "locale");
		var url = ResolveUrlForLocale(rawUrl, publishLocale);

		var title = GetString(data, "title") ?? GetString(data, "title_l10n") ?? GetNestedString(data, "main_header", "title_l10n");
		if (string.IsNullOrWhiteSpace(title))
			return null;

		var fullUrl = url.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? url : $"https://www.elastic.co{url}";
		var rawBody = ExtractBody(data);
		var headings = rawBody != null ? ExtractHeadings(rawBody) : [];
		var strippedBody = rawBody != null ? StripHtml(rawBody) : null;
		var description = GetDescription(data);

		// For pages with no body content (overview/listing pages), use the SEO description
		// as body so there's something indexable beyond just the title.
		strippedBody ??= description;

		var abstractText = CreateAbstract(strippedBody, description);

		var navigationSection = GetNavigationSection(url, item.ContentTypeUid);
		var language = GetLanguageFromUrl(url);
		var publishedDate = GetPublishedDate(data);
		var modifiedDate = ParseDate(GetString(data, "updated_at"));

		return new SiteDocument
		{
			Title = title,
			SearchTitle = BuildSearchTitle(title, navigationSection),
			Path = url,
			Hash = ComputeHash(title + strippedBody),
			BatchIndexDate = DateTimeOffset.UtcNow,
			LastUpdated = modifiedDate ?? publishedDate ?? DateTimeOffset.UtcNow,
			Description = description,
			Headings = headings,
			Body = strippedBody,
			Summary = abstractText,
			Section = navigationSection,
			ContentTier = ContentTierClassifier.FromNavigationSection(navigationSection),
			Translated = true,
			// navigation.depth/navigation.table_of_contents are rank features with positive_score_impact:false,
			// designed for hierarchical docs: lower values score higher.
			Navigation = new NavigationMetrics
			{
				Depth = ComputeNavigationDepth(url) + 1,
				TableOfContents = 100
			},
			Locale = language,
			PublishedDate = publishedDate,
			ModifiedDate = modifiedDate,
			Og = new OpenGraphData
			{
				Title = GetSeoString(data, "seo_title_l10n") ?? GetSeoString(data, "seo_title"),
				Description = GetSeoString(data, "seo_description_l10n") ?? GetSeoString(data, "seo_description"),
				Image = GetSeoString(data, "seo_image")
			}
		};
	}

	private static string? ExtractBody(JsonElement data)
	{
		// Strategy 1: flat body_l10n (blog legacy)
		var body = GetString(data, "body_l10n");
		if (!string.IsNullOrWhiteSpace(body))
			return body;

		// Strategy 2: intro_paragraph_l10n + paragraph_l10n (press)
		var intro = GetString(data, "intro_paragraph_l10n");
		var para = GetString(data, "paragraph_l10n");
		if (!string.IsNullOrWhiteSpace(intro))
			return string.IsNullOrWhiteSpace(para) ? intro : $"{intro}\n{para}";

		// Strategy 3: paragraph_l10n alone (agreements, forms, videos, customer_tile)
		if (!string.IsNullOrWhiteSpace(para))
			return para;

		// Strategy 4: modular_blocks (blog_v2, default_detail, product_detail, use_cases)
		var modularBody = ExtractModularBlocks(data);
		if (!string.IsNullOrWhiteSpace(modularBody))
		{
			// Prepend introduction if present (use_cases)
			var introBody = GetNestedString(data, "introduction", "paragraph_l10n");
			var challengeSolution = ExtractChallengeSolution(data);
			var parts = new List<string>();
			if (!string.IsNullOrWhiteSpace(introBody))
				parts.Add(introBody);
			if (!string.IsNullOrWhiteSpace(challengeSolution))
				parts.Add(challengeSolution);
			parts.Add(modularBody);
			return string.Join("\n", parts);
		}

		// Strategy 5: topic[].subtopic[].paragraph_l10n (faq)
		var faqBody = ExtractFaqTopics(data);
		if (!string.IsNullOrWhiteSpace(faqBody))
			return faqBody;

		// Strategy 6: release_notes (product_versions)
		var releaseNotes = GetString(data, "release_notes");
		if (!string.IsNullOrWhiteSpace(releaseNotes))
			return releaseNotes;
		var v5Notes = GetString(data, "v5_release_notes");
		if (!string.IsNullOrWhiteSpace(v5Notes))
			return v5Notes;

		return null;
	}

	private static string? ExtractModularBlocks(JsonElement data)
	{
		if (!data.TryGetProperty("modular_blocks", out var blocks) || blocks.ValueKind != JsonValueKind.Array)
			return null;

		var parts = new List<string>();
		foreach (var block in blocks.EnumerateArray())
		{
			// title_text block (most common)
			if (block.TryGetProperty("title_text", out var titleText))
			{
				// blog_v2 style: title_text.title_text[] (nested array)
				if (titleText.TryGetProperty("title_text", out var innerArray) && innerArray.ValueKind == JsonValueKind.Array)
				{
					foreach (var inner in innerArray.EnumerateArray())
					{
						var p = GetString(inner, "paragraph_l10n");
						if (!string.IsNullOrWhiteSpace(p))
							parts.Add(p);
					}
				}
				else
				{
					// default_detail style: title_text.paragraph_l10n (flat)
					var p = GetString(titleText, "paragraph_l10n");
					if (!string.IsNullOrWhiteSpace(p))
						parts.Add(p);
				}
			}

			// quote block (use_cases)
			if (block.TryGetProperty("quote", out var quote))
			{
				var q = GetString(quote, "quote_l10n");
				if (!string.IsNullOrWhiteSpace(q))
					parts.Add(q);
			}
		}

		return parts.Count > 0 ? string.Join("\n", parts) : null;
	}

	private static string? ExtractChallengeSolution(JsonElement data)
	{
		if (!data.TryGetProperty("challenge_solution", out var cs) || cs.ValueKind != JsonValueKind.Array)
			return null;

		var parts = new List<string>();
		foreach (var item in cs.EnumerateArray())
		{
			var p = GetString(item, "paragraph_l10n");
			if (!string.IsNullOrWhiteSpace(p))
				parts.Add(p);
		}
		return parts.Count > 0 ? string.Join("\n", parts) : null;
	}

	private static string? ExtractFaqTopics(JsonElement data)
	{
		if (!data.TryGetProperty("topic", out var topics) || topics.ValueKind != JsonValueKind.Array)
			return null;

		var parts = new List<string>();
		foreach (var topic in topics.EnumerateArray())
		{
			if (!topic.TryGetProperty("subtopic", out var subtopics) || subtopics.ValueKind != JsonValueKind.Array)
				continue;

			foreach (var sub in subtopics.EnumerateArray())
			{
				var p = GetString(sub, "paragraph_l10n");
				if (!string.IsNullOrWhiteSpace(p))
					parts.Add(p);
			}
		}
		return parts.Count > 0 ? string.Join("\n", parts) : null;
	}

	private static string? GetDescription(JsonElement data)
	{
		// abstract_l10n (blog, blog_v2)
		var abs = GetString(data, "abstract_l10n");
		if (!string.IsNullOrWhiteSpace(abs))
			return abs;

		// introduction.paragraph_l10n (use_cases)
		var intro = GetNestedString(data, "introduction", "paragraph_l10n");
		if (!string.IsNullOrWhiteSpace(intro))
			return StripHtml(intro);

		// SEO description fallback
		return GetSeoString(data, "seo_description_l10n") ?? GetSeoString(data, "seo_description");
	}

	private static DateTimeOffset? GetPublishedDate(JsonElement data)
	{
		// publish_date (blog, blog_v2)
		var pd = ParseDate(GetString(data, "publish_date"));
		if (pd != null)
			return pd;

		// date (press, product_versions)
		var d = ParseDate(GetString(data, "date"));
		if (d != null)
			return d;

		// presentation_date (videos)
		return ParseDate(GetString(data, "presentation_date"));
	}

	// ContentStack locale codes -> the site's live URL prefix segment for that locale
	// (https://www.elastic.co/{prefix}/... proxies to the ContentStack entry published under
	// that locale). en-us is the master locale and is never prefixed.
	private static readonly Dictionary<string, string> LocaleUrlPrefixes = new(StringComparer.OrdinalIgnoreCase)
	{
		["de-de"] = "de",
		["fr-fr"] = "fr",
		["ja-jp"] = "jp",
		["ko-kr"] = "kr",
		["zh-cn"] = "cn",
		["zh-tw"] = "tw",
		["es-419"] = "es",
		["es-mx"] = "es",
		["pt-br"] = "pt",
	};

	// ContentStack's `url` field is not locale-scoped — an entry gets "published" under several
	// locales while keeping the same url. Namespace non-master-locale variants under the prefix
	// the live site actually serves them at, so each locale variant gets its own document instead
	// of colliding on one Elasticsearch id (whichever locale synced last would otherwise silently
	// overwrite the others).
	private static string ResolveUrlForLocale(string url, string? locale)
	{
		if (string.IsNullOrWhiteSpace(locale) || locale.StartsWith("en", StringComparison.OrdinalIgnoreCase))
			return url;

		// Already carries a recognized locale prefix (author-managed localized url) — trust it as-is.
		if (TryGetLanguageFromUrlPrefix(url, out _))
			return url;

		// Prefer the short prefix the live site actually serves this locale at. Fall back to the
		// locale's base language subtag (e.g. "xx-yy" -> "xx") for locales we haven't mapped yet —
		// site-served prefixes are always two letters, never the full locale code — so every
		// non-English variant still gets its own document id, or it silently collides with (and
		// can 409 against) the entry for another locale of the same underlying ContentStack url.
		var prefix = LocaleUrlPrefixes.TryGetValue(locale, out var mapped)
			? mapped
			: locale.Split('-')[0].ToLowerInvariant();
		return $"/{prefix}{url}";
	}

	// The URL prefix is the reliable language signal — it always wins; an unprefixed URL is
	// treated as English.
	internal static string GetLanguageFromUrl(string url) => TryGetLanguageFromUrlPrefix(url, out var lang) ? lang : "en";

	private static bool TryGetLanguageFromUrlPrefix(string url, out string language)
	{
		if (url.StartsWith("/de/", StringComparison.OrdinalIgnoreCase))
			language = "de";
		else if (url.StartsWith("/fr/", StringComparison.OrdinalIgnoreCase))
			language = "fr";
		else if (url.StartsWith("/jp/", StringComparison.OrdinalIgnoreCase))
			language = "ja";
		else if (url.StartsWith("/kr/", StringComparison.OrdinalIgnoreCase))
			language = "ko";
		else if (url.StartsWith("/cn/", StringComparison.OrdinalIgnoreCase))
			language = "zh";
		else if (url.StartsWith("/es/", StringComparison.OrdinalIgnoreCase))
			language = "es";
		else if (url.StartsWith("/pt/", StringComparison.OrdinalIgnoreCase))
			language = "pt";
		else if (url.StartsWith("/tw/", StringComparison.OrdinalIgnoreCase))
			language = "zh";
		else
		{
			language = "en";
			return false;
		}
		return true;
	}

	private static int ComputeNavigationDepth(string url)
	{
		var path = url.StartsWith("http", StringComparison.OrdinalIgnoreCase)
			? new Uri(url).AbsolutePath
			: url;
		return path.Split('/', StringSplitOptions.RemoveEmptyEntries).Length;
	}

	/// <summary>
	/// ContentStack's <c>ContentTypeUid</c> (CMS schema/template, e.g. "product_versions", "forms",
	/// "faq") is a finer-grained, orthogonal signal to the URL. It has no dedicated bucket of its own
	/// on <see cref="SiteDocument.Section"/> — instead it fills in a better answer than the generic
	/// "marketing" catch-all for content types the URL heuristic below can't otherwise classify.
	/// URL-matched categories always win where the URL is already specific enough (no behavior change
	/// there); this only improves classification for the entries that would previously all collapse
	/// into "marketing" regardless of what kind of page they actually are.
	/// </summary>
	private static string? GetSectionFromContentType(string? contentTypeUid) => contentTypeUid switch
	{
		"product_versions" or "product_detail" or "default_detail" => "product",
		"blog_v2" or "blog_overview" => "blog",
		"videos" => "demo",
		"forms" or "use_cases" => "marketing",
		"faq" => "reference",
		"agreements" => "legal",
		"customer_tile" => "customer-story",
		_ => null
	};

	internal static string GetNavigationSection(string url, string? contentTypeUid = null)
	{
		if (url.Contains("/blog/", StringComparison.OrdinalIgnoreCase))
			return "blog";
		if (url.Contains("/what-is/", StringComparison.OrdinalIgnoreCase))
			return "concept";
		if (url.Contains("/webinars/", StringComparison.OrdinalIgnoreCase))
			return "webinar";
		if (url.Contains("/virtual-events/", StringComparison.OrdinalIgnoreCase))
			return "event";
		if (url.Contains("/elasticon/", StringComparison.OrdinalIgnoreCase))
			return "event";
		if (url.Contains("/events/", StringComparison.OrdinalIgnoreCase))
			return "event";
		if (url.Contains("/customers/", StringComparison.OrdinalIgnoreCase))
			return "customer-story";
		if (url.Contains("/downloads/", StringComparison.OrdinalIgnoreCase))
			return "download";
		if (url.Contains("/demo-gallery/", StringComparison.OrdinalIgnoreCase))
			return "demo";
		if (url.Contains("/about/press", StringComparison.OrdinalIgnoreCase))
			return "press";
		if (url.Contains("/about/", StringComparison.OrdinalIgnoreCase))
			return "about";
		if (url.Contains("/agreements/", StringComparison.OrdinalIgnoreCase))
			return "legal";
		if (url.Contains("/subscriptions", StringComparison.OrdinalIgnoreCase))
			return "pricing";
		if (url.Contains("/pricing", StringComparison.OrdinalIgnoreCase))
			return "pricing";
		if (url.Contains("/support/matrix", StringComparison.OrdinalIgnoreCase))
			return "product";
		if (url.Contains("/support/", StringComparison.OrdinalIgnoreCase))
			return "reference";
		if (url.Contains("/security", StringComparison.OrdinalIgnoreCase))
			return "product";
		if (url.Contains("/elasticsearch", StringComparison.OrdinalIgnoreCase))
			return "product";
		if (url.Contains("/kibana", StringComparison.OrdinalIgnoreCase))
			return "product";
		if (url.Contains("/observability", StringComparison.OrdinalIgnoreCase))
			return "product";
		return GetSectionFromContentType(contentTypeUid) ?? "marketing";
	}

	private static string BuildSearchTitle(string title, string navigationSection)
	{
		var prefix = navigationSection switch
		{
			"blog" => "Blog",
			"webinar" => "Webinar",
			"event" => "Event",
			"customer-story" => "Customer Story",
			"product" => "Product",
			"concept" => "What is",
			"download" => "Download",
			"press" => "Press",
			"demo" => "Demo",
			"legal" => "Legal",
			"pricing" => "Pricing",
			_ => null
		};

		return prefix is not null ? $"{prefix}: {title}" : title;
	}

	private static string? CreateAbstract(string? strippedBody, string? description)
	{
		var source = description ?? strippedBody;
		if (string.IsNullOrWhiteSpace(source))
			return null;

		var clean = StripHtml(source);
		return clean.Length > 500 ? string.Concat(clean.AsSpan(0, 497), "...") : clean;
	}

	internal static string StripHtml(string html)
	{
		var text = HtmlTagRegex().Replace(html, " ");
		text = HtmlEntityRegex().Replace(text, " ");
		text = WhitespaceRegex().Replace(text, " ");
		return text.Trim();
	}

	internal static string[] ExtractHeadings(string html)
	{
		var matches = HeadingRegex().Matches(html);
		return matches
			.Select(m => StripHtml(m.Groups[1].Value))
			.Where(h => !string.IsNullOrWhiteSpace(h))
			.ToArray();
	}

	private static string ComputeHash(string content)
	{
		var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(content));
		return Convert.ToHexString(bytes)[..16].ToLowerInvariant();
	}

	private static string? GetString(JsonElement el, string prop)
	{
		if (el.TryGetProperty(prop, out var val) && val.ValueKind == JsonValueKind.String)
			return val.GetString();
		return null;
	}

	private static string? GetNestedString(JsonElement el, string parent, string child)
	{
		if (el.TryGetProperty(parent, out var p) && p.ValueKind == JsonValueKind.Object)
			return GetString(p, child);
		return null;
	}

	private static string? GetSeoString(JsonElement data, string field)
	{
		if (data.TryGetProperty("seo", out var seo) && seo.ValueKind == JsonValueKind.Object)
			return GetString(seo, field);
		return null;
	}

	private static DateTimeOffset? ParseDate(string? value)
	{
		if (string.IsNullOrWhiteSpace(value))
			return null;
		return DateTimeOffset.TryParse(value, out var dto) ? dto : null;
	}

	[GeneratedRegex("<[^>]+>")]
	private static partial Regex HtmlTagRegex();

	[GeneratedRegex("&[a-zA-Z0-9#]+;")]
	private static partial Regex HtmlEntityRegex();

	[GeneratedRegex(@"\s+")]
	private static partial Regex WhitespaceRegex();

	[GeneratedRegex(@"<h[1-6][^>]*>(.*?)</h[1-6]>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
	private static partial Regex HeadingRegex();
}
