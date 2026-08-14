// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation;
using Elastic.Documentation.AppliesTo;
using Elastic.Documentation.Configuration;
using Elastic.Markdown.Myst.Components;

namespace Elastic.Markdown.Myst.Directives.AppliesSwitch;

public class AppliesItemViewModel : DirectiveViewModel
{
	public required int Index { get; init; }
	public required bool Checked { get; init; }
	public required bool IsDropdown { get; init; }
	public required int AppliesSwitchIndex { get; init; }
	public required string? AppliesToDefinition { get; init; }
	public required ApplicableTo? AppliesTo { get; init; }
	public required string? SyncKey { get; init; }
	public required string? AppliesSwitchGroupKey { get; init; }
	public required BuildContext BuildContext { get; init; }

	/// <summary>
	/// Compact text form of the applies_to definition used by the dropdown
	/// appearance, e.g. "Serverless, 9.1+" or "9.0 (preview)". Stack segments
	/// drop the product name, and the lifecycle only shows when it is not GA.
	/// </summary>
	public string ShortLabel()
	{
		if (AppliesTo is null)
			return AppliesToDefinition ?? string.Empty;

		try
		{
			var viewModel = new ApplicableToViewModel
			{
				AppliesTo = AppliesTo,
				Inline = true,
				ShowTooltip = false,
				VersionsConfig = BuildContext.VersionsConfiguration
			};
			var segments = viewModel.GetApplicabilityItems()
				.Select(FormatShortSegment)
				.Where(s => s.Length > 0)
				.ToList();
			return segments.Count > 0 ? string.Join(", ", segments) : AppliesToDefinition ?? string.Empty;
		}
		catch
		{
			// Mirrors the badge view's fallback: an applies_to definition that
			// cannot be resolved against the versions configuration renders as
			// its raw definition text.
			return AppliesToDefinition ?? string.Empty;
		}
	}

	private static string FormatShortSegment(ApplicabilityItem item)
	{
		var version = item.RenderData.ShowVersion ? item.RenderData.Version : string.Empty;
		var name = item.Key == ApplicabilityMappings.Stack.Key && version.Length > 0 ? string.Empty : item.Key;
		var text = name.Length > 0 && version.Length > 0 ? $"{name} {version}" : name + version;
		var lifecycle = ShortLifecycleName(item.Applicability.Lifecycle);
		if (lifecycle is null)
			return text;
		return text.Length > 0 ? $"{text} ({lifecycle})" : $"({lifecycle})";
	}

	private static string? ShortLifecycleName(ProductLifecycle lifecycle) => lifecycle switch
	{
		ProductLifecycle.GenerallyAvailable => null,
		ProductLifecycle.TechnicalPreview => "preview",
		ProductLifecycle.Beta => "beta",
		ProductLifecycle.Experimental => "experimental",
		ProductLifecycle.Deprecated => "deprecated",
		ProductLifecycle.Removed => "removed",
		ProductLifecycle.Unavailable => "unavailable",
		ProductLifecycle.Development => "development",
		ProductLifecycle.Planned => "planned",
		ProductLifecycle.Discontinued => "discontinued",
		_ => null
	};
}
