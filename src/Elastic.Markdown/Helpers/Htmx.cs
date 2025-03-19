// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text;
using Elastic.Markdown.IO.Configuration;

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

	public static string GetPreload() => "true";

	private static string GetHxSwap() => "none";
	private static string GetHxPushUrl() => "true";
	private static string GetHxIndicator() => "#htmx-indicator";

	public static string GetHxAttributes(string targetUrl, bool hasSameTopLevelGroup)
	{
		var attributes = new StringBuilder();
		_ = attributes.Append($" hx-get={targetUrl}");
		_ = attributes.Append($" hx-select-oob={GetHxSelectOob(hasSameTopLevelGroup)}");
		_ = attributes.Append($" hx-swap={GetHxSwap()}");
		_ = attributes.Append($" hx-push-url={GetHxPushUrl()}");
		_ = attributes.Append($" hx-indicator={GetHxIndicator()}");
		_ = attributes.Append($" preload={GetPreload()}");
		return attributes.ToString();
	}
}
