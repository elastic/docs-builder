// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Microsoft.AspNetCore.Html;

namespace Elastic.Documentation.Site.Components.AppliesPopover;

public sealed record AvailabilityItem(string Text, string? LifecycleDescription = null);

public sealed record AppliesPopoverContent
{
	/// <summary>Trusted HTML produced by the build, describing the product.</summary>
	public IHtmlContent? ProductDescription { get; init; }

	public IReadOnlyList<AvailabilityItem> Availability { get; init; } = [];
	public string? AdditionalInfo { get; init; }

	/// <summary>Trusted HTML produced by the build, with links to the release notes.</summary>
	public IHtmlContent? VersionNote { get; init; }

	public bool HasContent =>
		ProductDescription is not null || Availability.Count > 0 || AdditionalInfo is not null || VersionNote is not null;
}

public sealed record AppliesPopoverModel
{
	public required string Product { get; init; }

	/// <summary>The lifecycle as a short class name, such as <c>ga</c>, <c>beta</c> or <c>deprecated</c>.</summary>
	public required string LifecycleClass { get; init; }

	public string? LifecycleName { get; init; }
	public string? Version { get; init; }

	/// <summary>Text shown when the version is hidden, for example "Unavailable".</summary>
	public string? LifecycleText { get; init; }

	public bool ShowLifecycleName { get; init; }
	public bool ShowVersion { get; init; }

	/// <summary>The product is available in more than one lifecycle; the badge shows an ellipsis.</summary>
	public bool HasMultipleLifecycles { get; init; }

	/// <summary>The larger form used under a page title instead of inside running text.</summary>
	public bool Block { get; init; }

	public AppliesPopoverContent? Popover { get; init; }

	public bool Interactive => Popover is { HasContent: true };

	public string CssClass =>
		string.Join(
			' ',
			new[] { "applies-badge", $"applies-badge--{LifecycleClass}", Block ? "applies-badge--block" : null }.Where(c => c is not null)
		);
}
