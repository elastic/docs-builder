// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text;

namespace Elastic.Documentation.Site;

/// <summary>Default HTMX provider for isolated and assembler builds.</summary>
public class DefaultHtmxAttributeProvider(string rootPath) : IHtmxAttributeProvider
{
	public const string Preload = "mousedown";

	public string GetRootPath() => rootPath;

	public virtual string GetHxSelectOob(bool hasSameTopLevelGroup) =>
		hasSameTopLevelGroup
			? "#content-container,#toc-nav"
			: "#content-container,#toc-nav,#pages-nav";

	public string GetHxAttributes(
		bool hasSameTopLevelGroup = false,
		string? preload = Preload,
		string? hxSwapOob = null
	)
	{
		var attributes = new StringBuilder();
		// Unquoted attribute values: Razor HTML-encodes @Model.Htmx.* output, so quotes
		// become &quot; and break htmx. hx-swap=none is required with hx-select-oob —
		// otherwise body hx-boost also swaps the whole body and leaves stale content.
		_ = attributes.Append($" hx-select-oob={hxSwapOob ?? GetHxSelectOob(hasSameTopLevelGroup)}");
		_ = attributes.Append(" hx-swap=none");
		if (!string.IsNullOrEmpty(preload))
			_ = attributes.Append($" preload={preload}");
		return attributes.ToString();
	}

	public string GetNavHxAttributes(bool hasSameTopLevelGroup = false, string? preload = Preload)
	{
		var attributes = new StringBuilder();
		_ = attributes.Append($" hx-select-oob={GetHxSelectOob(hasSameTopLevelGroup)}");
		_ = attributes.Append(" hx-swap=none");
		if (!string.IsNullOrEmpty(preload))
			_ = attributes.Append($" preload={preload}");
		return attributes.ToString();
	}
}

/// <summary>Static facade for backward compatibility. Prefer injecting IHtmxAttributeProvider.</summary>
public static class Htmx
{
	private static readonly IHtmxAttributeProvider Default = new DefaultHtmxAttributeProvider("/");

	public static string GetHxSelectOob(bool hasSameTopLevelGroup) =>
		Default.GetHxSelectOob(hasSameTopLevelGroup);

	public const string Preload = DefaultHtmxAttributeProvider.Preload;

	public static string GetHxAttributes(
		bool hasSameTopLevelGroup = false,
		string? preload = Preload,
		string? hxSwapOob = null
	) =>
		Default.GetHxAttributes(hasSameTopLevelGroup, preload, hxSwapOob);

	public static string GetNavHxAttributes(bool hasSameTopLevelGroup = false, string? preload = Preload) =>
		Default.GetNavHxAttributes(hasSameTopLevelGroup, preload);
}

/// <summary>HTMX provider for codex builds. Includes #codex-breadcrumbs in swap targets so the sub-header updates on navigation.</summary>
public class CodexHtmxAttributeProvider(string rootPath) : DefaultHtmxAttributeProvider(rootPath)
{
	public override string GetHxSelectOob(bool hasSameTopLevelGroup) =>
		$"{base.GetHxSelectOob(hasSameTopLevelGroup)},#codex-breadcrumbs";
}
