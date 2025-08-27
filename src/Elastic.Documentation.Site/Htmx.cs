// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text;

namespace Elastic.Documentation.Site;

public static class Htmx
{
	public static string GetHxSelectOob(bool hasSameTopLevelGroup) => hasSameTopLevelGroup ? "#content-container,#toc-nav" : "#main-container";
	public const string Preload = "mousedown";

	public static string GetHxAttributes(
		bool hasSameTopLevelGroup = false,
		string? preload = Preload,
		string? hxSwapOob = null
	)
	{
		var attributes = new StringBuilder();
		_ = attributes.Append($" hx-select-oob={hxSwapOob ?? GetHxSelectOob(hasSameTopLevelGroup)}");
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
