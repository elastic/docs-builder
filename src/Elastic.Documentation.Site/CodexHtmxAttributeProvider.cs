// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text;

namespace Elastic.Documentation.Site;

/// <summary>HTMX provider for codex builds. Root path points to codex root, not doc set root.</summary>
public class CodexHtmxAttributeProvider(string rootPath) : IHtmxAttributeProvider
{
	public string GetRootPath() => rootPath;

	public string GetHxSelectOob(bool hasSameTopLevelGroup) =>
		hasSameTopLevelGroup
			? "#content-container,#toc-nav"   // same group or same docset
			: "#main-container";              // different group or root

	public string GetHxAttributes(
		bool hasSameTopLevelGroup = false,
		string? preload = DefaultHtmxAttributeProvider.Preload,
		string? hxSwapOob = null
	)
	{
		var attributes = new StringBuilder();
		_ = attributes.Append($" hx-select-oob={hxSwapOob ?? GetHxSelectOob(hasSameTopLevelGroup)}");
		_ = attributes.Append($" preload={preload}");
		return attributes.ToString();
	}

	public string GetNavHxAttributes(bool hasSameTopLevelGroup = false, string? preload = DefaultHtmxAttributeProvider.Preload)
	{
		var attributes = new StringBuilder();
		_ = attributes.Append($" hx-select-oob={GetHxSelectOob(hasSameTopLevelGroup)}");
		_ = attributes.Append($" preload={preload}");
		return attributes.ToString();
	}
}
