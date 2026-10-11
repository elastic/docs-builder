// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Microsoft.AspNetCore.Html;

namespace Elastic.Documentation.Site.Components.ThemeSwitch;

/// <summary>
/// The inline head script that applies the saved display preferences before first paint, so a page never flashes the wrong theme.
/// Every preference is resolved here, the theme too, because the surface art keys off the resolved theme; "system" is the choice of
/// following the device, so nothing is stored for it.
/// </summary>
public static class PreferencesScript
{
	public static HtmlString Inline { get; } = new(
		"<script>try{var d=document.documentElement,s=localStorage,t=s.getItem(\"docs-theme\");" +
			"d.dataset.theme=t===\"light\"||t===\"dark\"||t===\"black\"?t:(matchMedia(\"(prefers-color-scheme: dark)\").matches?\"dark\":\"light\");" +
			"var c=s.getItem(\"docs-contrast\");d.dataset.contrast=c===\"more\"||c===\"normal\"?c:(matchMedia(\"(prefers-contrast: more)\").matches?\"more\":\"normal\");" +
			"var m=s.getItem(\"docs-motion\");d.dataset.motion=m===\"full\"||m===\"reduce\"?m:(matchMedia(\"(prefers-reduced-motion: reduce)\").matches?\"reduce\":\"full\")" +
			"}catch(e){document.documentElement.dataset.motion=\"reduce\"}</script>"
	);
}
