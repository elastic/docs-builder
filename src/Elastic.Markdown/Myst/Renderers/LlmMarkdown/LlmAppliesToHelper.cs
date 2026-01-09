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
	/// Converts ApplicableTo to a readable text format for LLM consumption (block level - for page or section)
	/// </summary>
	public static string RenderAppliesToBlock(ApplicableTo? appliesTo, IDocumentationConfigurationContext buildContext)
	{
		if (appliesTo is null || appliesTo == ApplicableTo.All)
			return string.Empty;

		var items = GetApplicabilityItems(appliesTo, buildContext);
		if (items.Count == 0)
			return string.Empty;

		var sb = new StringBuilder();
		_ = sb.AppendLine();
		_ = sb.AppendLine("This applies to:");

		foreach (var (productName, availabilityText) in items)
			_ = sb.AppendLine($"- {availabilityText} for {productName}");

		return sb.ToString();
	}

	/// <summary>
	/// Converts ApplicableTo to a readable inline text format for LLM consumption
	/// </summary>
	public static string RenderApplicableTo(ApplicableTo? appliesTo, IDocumentationConfigurationContext buildContext)
	{
		if (appliesTo is null || appliesTo == ApplicableTo.All)
			return string.Empty;

		var items = GetApplicabilityItems(appliesTo, buildContext);
		if (items.Count == 0)
			return string.Empty;

		var itemList = items.Select(item => $"{item.availabilityText} for {item.productName}").ToList();
		return string.Join(", ", itemList);
	}

	private static List<(string productName, string availabilityText)> GetApplicabilityItems(
		ApplicableTo appliesTo,
		IDocumentationConfigurationContext buildContext)
	{
		var viewModel = new ApplicableToViewModel
		{
			AppliesTo = appliesTo,
			Inline = false,
			ShowTooltip = false,
			VersionsConfig = buildContext.VersionsConfiguration
		};

		var applicabilityItems = viewModel.GetApplicabilityItems();
		var results = new List<(string productName, string availabilityText)>();

		foreach (var item in applicabilityItems)
		{
			var renderData = item.RenderData;
			var productName = item.Key;

			// Get the availability text from the popover data
			var availabilityText = GetAvailabilityText(renderData);
			if (!string.IsNullOrEmpty(availabilityText))
				results.Add((productName, availabilityText));
		}

		return results;
	}

	private static string GetAvailabilityText(ApplicabilityRenderer.ApplicabilityRenderData renderData)
	{
		// Use the first availability item's text if available (this is what the popover shows)
		if (renderData.PopoverData?.AvailabilityItems is { Length: > 0 } items)
		{
			// The popover text already includes lifecycle and version info
			// e.g., "Generally available since 9.1", "Preview in 8.0", etc.
			return items[0].Text;
		}

		// Fallback to constructing from badge data
		var parts = new List<string>();

		if (!string.IsNullOrEmpty(renderData.LifecycleName) && renderData.LifecycleName != "Generally available")
			parts.Add(renderData.LifecycleName);

		if (!string.IsNullOrEmpty(renderData.Version))
			parts.Add(renderData.Version);
		else if (!string.IsNullOrEmpty(renderData.BadgeLifecycleText))
			parts.Add(renderData.BadgeLifecycleText);

		return parts.Count > 0 ? string.Join(" ", parts) : "Available";
	}
}
