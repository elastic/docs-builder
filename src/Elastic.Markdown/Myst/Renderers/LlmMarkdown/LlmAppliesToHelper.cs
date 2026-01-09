// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text;
using Elastic.Documentation;
using Elastic.Documentation.AppliesTo;
using Elastic.Documentation.Configuration;
using Elastic.Markdown.Myst.Components;

namespace Elastic.Markdown.Myst.Renderers.LlmMarkdown;

/// <summary>
/// Helper class to render ApplicableTo information in LLM-friendly text format
/// </summary>
public static class LlmAppliesToHelper
{
	/// <summary>
	/// Converts ApplicableTo to a readable text format for LLM consumption
	/// </summary>
	public static string RenderApplicableTo(ApplicableTo? appliesTo, IDocumentationConfigurationContext buildContext)
	{
		if (appliesTo is null || appliesTo == ApplicableTo.All)
			return string.Empty;

		var viewModel = new ApplicableToViewModel
		{
			AppliesTo = appliesTo,
			Inline = false,
			ShowTooltip = false,
			VersionsConfig = buildContext.VersionsConfiguration
		};

		var items = viewModel.GetApplicabilityItems();
		if (items.Count == 0)
			return string.Empty;

		var itemList = new List<string>();

		foreach (var item in items)
		{
			var text = BuildApplicabilityText(item);
			if (!string.IsNullOrEmpty(text))
				itemList.Add(text);
		}

		if (itemList.Count == 0)
			return string.Empty;

		return string.Join(", ", itemList);
	}

	private static string BuildApplicabilityText(ApplicabilityItem item)
	{
		// For LLM output, use the shorter Key name for better readability
		var parts = new List<string> { item.Key };

		// For LLM output, show the actual applicability information directly
		var applicability = item.Applicability;

		// Add lifecycle if it's not GA
		if (applicability.Lifecycle != ProductLifecycle.GenerallyAvailable)
			parts.Add(applicability.GetLifeCycleName());

		// Add version information if present
		if (applicability.Version is not null and not AllVersionsSpec)
		{
			var versionText = FormatVersion(applicability.Version);
			if (!string.IsNullOrEmpty(versionText))
				parts.Add(versionText);
		}

		return string.Join(" ", parts);
	}

	private static string FormatVersion(VersionSpec versionSpec)
	{
		var min = versionSpec.Min;
		var max = versionSpec.Max;
		var showMinPatch = versionSpec.ShowMinPatch;
		var showMaxPatch = versionSpec.ShowMaxPatch;

		static string FormatSemVersion(SemVersion v, bool showPatch) =>
			showPatch ? $"{v.Major}.{v.Minor}.{v.Patch}" : $"{v.Major}.{v.Minor}";

		return versionSpec.Kind switch
		{
			VersionSpecKind.GreaterThanOrEqual => $"{FormatSemVersion(min, showMinPatch)}+",
			VersionSpecKind.Range when max is not null => $"{FormatSemVersion(min, showMinPatch)}-{FormatSemVersion(max, showMaxPatch)}",
			VersionSpecKind.Exact => FormatSemVersion(min, showMinPatch),
			_ => string.Empty
		};
	}
}
