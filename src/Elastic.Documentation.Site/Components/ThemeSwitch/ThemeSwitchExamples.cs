// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Site.Components.ThemeSwitch;

public static class ThemeSwitchExamples
{
	public static ComponentSection Switches { get; } = new(
		"Theme switch",
		"Light and dark in one click, and a cog for the rest: the theme, including true black, the contrast and the motion, each with a match-the-device choice. It sits at the right of the docs bar, or in the header where there is no bar.",
		"theme-switch",
		[ComponentExample.Of("Theme switch", _ThemeSwitch.Create, new ThemeSwitchModel { Id = "example-theme" })]
	);
}
