// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;
using Elastic.Documentation.AppliesTo;
using Elastic.Documentation.Configuration.Versions;
using Elastic.Markdown.Myst.Components;
using Elastic.Markdown.Myst.Roles.AppliesTo;
using RazorSlices;

namespace Elastic.Markdown.Myst.Directives.Settings;

public class SettingsViewModel
{
	public required YamlSettings SettingsCollection { get; init; }

	public required Func<string, string> RenderMarkdown { get; init; }

	public required VersionsConfiguration VersionsConfig { get; init; }

	[SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly")]
	public string RenderAppliesToInline(ApplicableTo? appliesTo)
	{
		if (appliesTo is null || appliesTo == ApplicableTo.All)
			return string.Empty;

		var viewModel = new ApplicableToViewModel
		{
			AppliesTo = appliesTo,
			Inline = true,
			VersionsConfig = VersionsConfig
		};

		return ApplicableToRole.Create(viewModel).RenderAsync().GetAwaiter().GetResult();
	}

	public static string ComposeSettingName(string? parentName, string? settingName)
	{
		if (string.IsNullOrWhiteSpace(settingName))
			return parentName ?? string.Empty;
		if (string.IsNullOrWhiteSpace(parentName))
			return settingName;
		if (settingName.StartsWith('[') || settingName.StartsWith('.') || settingName.StartsWith(parentName, StringComparison.Ordinal))
			return parentName + settingName;
		return $"{parentName}.{settingName}";
	}
}
