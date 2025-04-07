// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text;

namespace Elastic.Markdown.Helpers;

public static class Htmx
{
	public static string GetHxSelectOob(bool hasSameTopLevelGroup)
	{
		var selectTargets = "#content-container,#toc-nav";
		if (!hasSameTopLevelGroup)
			selectTargets += ",#pages-nav";
		return selectTargets;
	}

	public const string Preload = "mousedown";
	public const string HxSwap = "none";
	public const string HxPushUrl = "true";
	public const string HxIndicator = "#htmx-indicator";

	public static string GetHxAttributes(
		string targetUrl,
		bool hasSameTopLevelGroup = false,
		string? preload = Preload,
		string? hxSwapOob = null,
		string? hxSwap = HxSwap,
		string? hxPushUrl = HxPushUrl,
		string? hxIndicator = HxIndicator
	)
	{
		var attributes = new StringBuilder();
		if (!string.IsNullOrEmpty(targetUrl))
			_ = attributes.Append($" hx-get={targetUrl}");
		_ = attributes.Append($" hx-select-oob={hxSwapOob ?? GetHxSelectOob(hasSameTopLevelGroup)}");
		_ = attributes.Append($" hx-swap={hxSwap}");
		_ = attributes.Append($" hx-push-url={hxPushUrl}");
		_ = attributes.Append($" hx-indicator={hxIndicator}");
		_ = attributes.Append($" preload={preload}");
		return attributes.ToString();
	}

	public static string GetHxAttributes(
		bool hasSameTopLevelGroup = false,
		string? preload = Preload,
		string? hxSwapOob = null,
		string? hxSwap = HxSwap,
		string? hxPushUrl = HxPushUrl,
		string? hxIndicator = HxIndicator
	) => GetHxAttributes(string.Empty, hasSameTopLevelGroup, preload, hxSwapOob, hxSwap, hxPushUrl, hxIndicator);
}
