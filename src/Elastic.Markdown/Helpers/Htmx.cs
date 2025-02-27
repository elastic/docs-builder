// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text;
using Elastic.Markdown.IO.Configuration;

namespace Elastic.Markdown.Helpers;

public static class Htmx
{
	public static string GetHxSelectOob(ConfigurationFile configuration, string? pathPrefix, string currentUrl, string targetUrl)
	{
		HashSet<string> selectTargets =
		[
			"#primary-nav", "#secondary-nav", "#markdown-content", "#toc-nav", "#prev-next-nav", "#breadcrumbs"
		];
		if (!HasSameTopLevelGroup(pathPrefix, currentUrl, targetUrl) && configuration.Features.ContainsKey("primary-nav"))
			_ = selectTargets.Add("#pages-nav");
		return string.Join(',', selectTargets);
	}

	public static bool HasSameTopLevelGroup(string? pathPrefix, string currentUrl, string targetUrl)
	{
		var startIndex = pathPrefix?.Length ?? 0;
		var currentSegments = GetSegments(currentUrl[startIndex..].Trim('/'));
		var targetSegments = GetSegments(targetUrl[startIndex..].Trim('/'));
		return currentSegments.Length >= 1 && targetSegments.Length >= 1 && currentSegments[0] == targetSegments[0];
	}

	public static string GetPreload() => "true";

	public static string GetHxSwap() => "none";
	public static string GetHxPushUrl() => "true";
	public static string GetHxIndicator() => "#htmx-indicator";

	private static string[] GetSegments(string url) => url.Split('/');

	public static string GetHxAttributes(ConfigurationFile configuration, string? pathPrefix, string currentUrl, string targetUrl)
	{

		var attributes = new StringBuilder();
		_ = attributes.Append($" hx-get={targetUrl}");
		_ = attributes.Append($" hx-select-oob={GetHxSelectOob(configuration, pathPrefix, currentUrl, targetUrl)}");
		_ = attributes.Append($" hx-swap={GetHxSwap()}");
		_ = attributes.Append($" hx-push-url={GetHxPushUrl()}");
		_ = attributes.Append($" hx-indicator={GetHxIndicator()}");
		_ = attributes.Append($" preload={GetPreload()}");
		return attributes.ToString();
	}
}
