// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Site;

/// <summary>Default HTMX provider for isolated and assembler builds.</summary>
public class DefaultHtmxAttributeProvider(string rootPath) : IHtmxAttributeProvider
{
	public const string Preload = "mousedown";

	public string GetRootPath() => rootPath;

	public virtual string GetHxSelectOob(bool hasSameTopLevelGroup) =>
		hasSameTopLevelGroup
			? "#content-container,#toc-nav"
			: "#content-container,#toc-nav,#nav-tree,#nav-dropdown";

	// PoC: boosted links now use htmx's default whole-body swap with hx-preserve islands,
	// so links no longer carry hx-select-oob. The preload extension still needs the
	// attribute on each link itself (it ignores ancestors). Call sites left intact;
	// delete this provider plumbing entirely if the PoC lands.
	public string GetHxAttributes(
		bool hasSameTopLevelGroup = false,
		string? preload = Preload,
		string? hxSwapOob = null
	) => $" preload={preload}";

	public string GetNavHxAttributes(bool hasSameTopLevelGroup = false, string? preload = Preload) => $" preload={preload}";
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
