// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.AppliesTo;
using Elastic.Documentation.Configuration.Versions;
using Elastic.Markdown.Helpers;
using Elastic.Markdown.Myst.Components;
using Elastic.Markdown.Myst.Roles.AppliesTo;
using RazorSlices;

namespace Elastic.Markdown.Myst.Directives.Settings;

public class SettingsViewModel
{
	public required YamlSettings SettingsCollection { get; init; }

	public required Func<string, string> RenderMarkdown { get; init; }

	public required VersionsConfiguration VersionsConfig { get; init; }

	/// <summary>Markdown heading level for each group section (1–6).</summary>
	public required int GroupHeadingLevel { get; init; }

	public string RenderAppliesToInline(ApplicableTo? appliesTo) =>
		RenderAppliesToPlacement(appliesTo, ApplicabilityBadgePlacement.Combined);

	public string RenderStackRowBadges(ApplicableTo? appliesTo) =>
		RenderAppliesToPlacement(appliesTo, ApplicabilityBadgePlacement.StackRow);

	public string RenderSupportedOnBadges(ApplicableTo? appliesTo) =>
		RenderAppliesToPlacement(appliesTo, ApplicabilityBadgePlacement.SupportedOnRow);

	private string RenderAppliesToPlacement(ApplicableTo? appliesTo, ApplicabilityBadgePlacement placement)
	{
		if (appliesTo is null || appliesTo == ApplicableTo.All)
			return string.Empty;

		var viewModel = new ApplicableToViewModel
		{
			AppliesTo = appliesTo,
			Inline = true,
			VersionsConfig = VersionsConfig,
			BadgePlacement = placement
		};

		return ApplicableToRole.Create(viewModel).RenderAsync().AsTask().GetAwaiter().GetResult();
	}

	/// <summary>Stable HTML id / in-page TOC slug for a settings YAML group heading.</summary>
	public static string GroupHeadingSlug(SettingsGrouping group) =>
		string.IsNullOrWhiteSpace(group.Id)
			? (group.Name ?? string.Empty).Slugify()
			: group.Id;

	public static string ComposeSettingName(string? parentName, string? settingName)
	{
		if (string.IsNullOrWhiteSpace(settingName))
			return parentName ?? string.Empty;
		if (string.IsNullOrWhiteSpace(parentName))
			return settingName;
		if (settingName.StartsWith(parentName, StringComparison.Ordinal))
			return settingName;
		if (settingName.StartsWith('[') || settingName.StartsWith('.'))
			return parentName + settingName;
		return $"{parentName}.{settingName}";
	}

	/// <summary>Stable HTML fragment for a setting: YAML <c>id</c> when present, otherwise slugified composed name.</summary>
	public static string SettingFragmentId(Setting setting, string composedDisplayName) =>
		string.IsNullOrWhiteSpace(setting.Id)
			? composedDisplayName.Replace('.', '-').Slugify()
			: setting.Id;
}
