// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Microsoft.AspNetCore.Html;

namespace Elastic.Documentation.Site.Components.ThemeSwitch;

/// <summary>
/// The inline head script that applies the saved display preferences before first paint, so a page never flashes the wrong theme.
/// It sets data-theme only for a pinned choice (the stylesheet follows the system otherwise), and resolves contrast and motion.
/// </summary>
public static class PreferencesScript
{
	public static HtmlString Inline { get; } = new(
		"<script>try{var d=document.documentElement,s=localStorage,t=s.getItem(\"docs-theme\");" +
			"if(t===\"light\"||t===\"dark\"||t===\"black\")d.dataset.theme=t;" +
			"var c=s.getItem(\"docs-contrast\");d.dataset.contrast=c===\"more\"||c===\"normal\"?c:(matchMedia(\"(prefers-contrast: more)\").matches?\"more\":\"normal\");" +
			"var m=s.getItem(\"docs-motion\");d.dataset.motion=m===\"full\"||m===\"reduce\"?m:(matchMedia(\"(prefers-reduced-motion: reduce)\").matches?\"reduce\":\"full\")" +
			"}catch(e){document.documentElement.dataset.motion=\"reduce\"}</script>"
	);
}
