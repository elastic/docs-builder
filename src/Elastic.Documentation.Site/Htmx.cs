// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Reflection;
using System.Text;
using Elastic.Documentation.Extensions;

namespace Elastic.Documentation.Site;

public static class Htmx
{
	private static readonly string Version =
		Assembly.GetExecutingAssembly().GetCustomAttributes<AssemblyInformationalVersionAttribute>()
			.FirstOrDefault()?.InformationalVersion ?? "0.0.0";
	public static readonly string VersionHash = ShortId.Create(Version);

	public static string GetHxSelectOob(bool hasSameTopLevelGroup) => hasSameTopLevelGroup ? "#content-container,#toc-nav" : "#main-container";
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
		targetUrl = targetUrl.Contains('?') ? $"{targetUrl}&v={VersionHash}" : $"{targetUrl}?v={VersionHash}";
		var attributes = new StringBuilder();
		_ = attributes.Append($" hx-get={targetUrl}");
		_ = attributes.Append($" hx-select-oob={hxSwapOob ?? GetHxSelectOob(hasSameTopLevelGroup)}");
		_ = attributes.Append($" hx-swap={hxSwap}");
		_ = attributes.Append($" hx-push-url={hxPushUrl}");
		_ = attributes.Append($" hx-indicator={hxIndicator}");
		_ = attributes.Append($" preload={preload}");
		return attributes.ToString();
	}

	public static string GetNavHxAttributes(bool hasSameTopLevelGroup = false, string? preload = Preload)
	{
		var attributes = new StringBuilder();
		_ = attributes.Append($" hx-select-oob={GetHxSelectOob(hasSameTopLevelGroup)}");
		_ = attributes.Append($" preload={preload}");
		return attributes.ToString();
	}
}
