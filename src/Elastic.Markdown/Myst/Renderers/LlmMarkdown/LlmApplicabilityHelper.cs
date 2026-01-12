// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text;
using Elastic.Documentation.AppliesTo;
using Elastic.Documentation.Configuration.Versions;
using Elastic.Markdown.Myst.Components;

namespace Elastic.Markdown.Myst.Renderers.LlmMarkdown;

/// <summary>
/// Helper for rendering applies_to metadata in human-readable format for LLM consumption.
/// Uses the same text generation logic as the web popover component.
/// </summary>
public static class LlmApplicabilityHelper
{
	/// <summary>
	/// Renders an ApplicableTo object as a human-readable string for LLM consumption.
	/// Uses full display names and availability text.
	/// </summary>
	/// <param name="appliesTo">The ApplicableTo object to render</param>
	/// <param name="versionsConfig">The versions configuration for determining release status</param>
	/// <param name="useInlineTag">Whether to wrap in &lt;applies-to&gt; tag (for inline use)</param>
	/// <returns>A formatted string like "&lt;applies-to&gt;Elastic Stack: GA since 9.1&lt;/applies-to&gt;" or plain text</returns>
	public static string RenderForLlm(ApplicableTo? appliesTo, VersionsConfiguration versionsConfig, bool useInlineTag = true)
	{
		if (appliesTo is null || appliesTo == ApplicableTo.All)
			return string.Empty;

		// Use the same view model that generates popover data
		var viewModel = new ApplicableToViewModel
		{
			AppliesTo = appliesTo,
			Inline = true,
			ShowTooltip = true, // Need tooltip to get popover data
			VersionsConfig = versionsConfig
		};

		var items = viewModel.GetApplicabilityItems();
		if (items.Count == 0)
			return string.Empty;

		var result = new StringBuilder();
		foreach (var item in items)
		{
			if (result.Length > 0)
				_ = result.Append(", ");

			// Use the display name from ApplicabilityDefinition, removing HTML entities
			var displayName = GetPlainDisplayName(item.ApplicabilityDefinition.DisplayName);
			_ = result.Append(displayName);
			_ = result.Append(": ");

			// Get the availability text from the popover data
			var availabilityText = GetAvailabilityText(item);
			_ = result.Append(availabilityText);
		}

		// Wrap in <applies-to> tag for inline use
		if (useInlineTag)
			return $"<applies-to>{result}</applies-to>";

		return result.ToString();
	}

	/// <summary>
	/// Gets availability text for a single applicability item.
	/// Uses the short Text from PopoverData (e.g., "Generally available since 7.3").
	/// </summary>
	private static string GetAvailabilityText(ApplicabilityItem item)
	{
		// Get availability items from the render data
		var popoverData = item.RenderData.PopoverData;
		if (popoverData?.AvailabilityItems is { Length: > 0 })
		{
			// Use the short text which includes version info (e.g., "Generally available since 7.3")
			return string.Join(", ", popoverData.AvailabilityItems.Select(a => a.Text));
		}

		// Fallback: construct from badge data
		var parts = new List<string>();

		if (item.RenderData.ShowLifecycleName && !string.IsNullOrEmpty(item.RenderData.LifecycleName))
			parts.Add(item.RenderData.LifecycleName);

		if (!string.IsNullOrEmpty(item.RenderData.BadgeLifecycleText))
			parts.Add(item.RenderData.BadgeLifecycleText);

		if (item.RenderData.ShowVersion && !string.IsNullOrEmpty(item.RenderData.Version))
			parts.Add(item.RenderData.Version);

		return parts.Count > 0 ? string.Join(" ", parts) : "Available";
	}

	/// <summary>
	/// Converts display name from HTML entities to plain text (e.g., "Elastic&amp;nbsp;Stack" -> "Elastic Stack")
	/// </summary>
	private static string GetPlainDisplayName(string displayName) =>
		displayName.Replace("&nbsp;", " ");
}
