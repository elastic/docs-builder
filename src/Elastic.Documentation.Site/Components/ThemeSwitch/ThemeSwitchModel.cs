// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Site.Components.ThemeSwitch;

/// <summary>
/// One control for the reader's display preferences: a light and dark quick pick, and a cog that opens the theme (including true black),
/// contrast and motion options. It is radios and a checkbox, so it opens and reads without script; script stores the choice.
/// </summary>
public sealed record ThemeSwitchModel
{
	/// <summary>Prefix for the ids that tie each label to its radio. Unique per control on the page.</summary>
	public string Id { get; init; } = "theme";

	/// <summary>Where the menu opens from, so it stays inside the window: <c>right</c> (default) or <c>left</c>.</summary>
	public bool OpensLeft { get; init; }
}
