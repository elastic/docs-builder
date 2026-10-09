// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Site.Components.AppliesPopover;
using Microsoft.AspNetCore.Html;

namespace Elastic.Markdown.Myst.Components;

/// <summary>Maps the applicability data the legacy element receives as JSON onto the design-system badge.</summary>
internal static class DesignSystemApplicability
{
	public static AppliesPopoverModel ToModel(ApplicabilityItem item, ApplicableToViewModel view)
	{
		var data = item.RenderData;
		var popover = view.ShowTooltip ? data.PopoverData : null;
		return new AppliesPopoverModel
		{
			Product = item.Key,
			LifecycleClass = data.LifecycleClass,
			LifecycleName = data.LifecycleName,
			Version = data.Version,
			LifecycleText = data.BadgeLifecycleText,
			ShowLifecycleName = data.ShowLifecycleName,
			ShowVersion = data.ShowVersion,
			HasMultipleLifecycles = data.HasMultipleLifecycles,
			Block = !view.Inline,
			Popover = popover is null
				? null
				: new AppliesPopoverContent
				{
					ProductDescription = string.IsNullOrEmpty(popover.ProductDescription)
						? null
						: new HtmlString(popover.ProductDescription),
					Availability = [.. popover.AvailabilityItems.Select(a => new AvailabilityItem(a.Text, a.LifecycleDescription))],
					AdditionalInfo = string.IsNullOrEmpty(popover.AdditionalInfo) ? null : popover.AdditionalInfo,
					VersionNote = popover.ShowVersionNote && !string.IsNullOrEmpty(popover.VersionNote)
						? new HtmlString(popover.VersionNote)
						: null
				}
		};
	}
}
