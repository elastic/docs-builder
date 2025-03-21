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

	public const string Preload = "mousedown";
	public const string HxSwap = "none";
	public const string HxPushUrl = "true";
	public const string HxIndicator = "#htmx-indicator";

	public static string GetHxAttributes(string targetUrl, bool hasSameTopLevelGroup, string? preload = Preload)
	{
		var attributes = new StringBuilder();
		_ = attributes.Append($" hx-get={targetUrl}");
		_ = attributes.Append($" hx-select-oob={GetHxSelectOob(hasSameTopLevelGroup)}");
		_ = attributes.Append($" hx-swap={HxSwap}");
		_ = attributes.Append($" hx-push-url={HxPushUrl}");
		_ = attributes.Append($" hx-indicator={HxIndicator}");
		_ = attributes.Append($" preload={preload}");
		return attributes.ToString();
	}
}
