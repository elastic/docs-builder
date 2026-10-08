// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.ApiExplorer.Model;
using Elastic.ApiExplorer.Operations;
using Microsoft.AspNetCore.Html;

namespace Elastic.ApiExplorer.Components.PropertyTree;

/// <summary>A single expanded union variant; the array/non-array pairing and children are precomputed.</summary>
public record ApiUnionVariant
{
	/// <summary>Display name without the <c>[]</c> suffix when the array icon is already shown.</summary>
	public required string DisplayName { get; init; }

	/// <summary>The type's own page when it has one; the variant links there instead of listing its fields.</summary>
	public string? PageUrl { get; init; }
	public required bool IsArrayVariant { get; init; }
	public required bool IsObjectType { get; init; }
	public required string AnchorId { get; init; }
	public required bool ShowProperties { get; init; }
	public required bool IsCollapsible { get; init; }
	public required bool DefaultExpanded { get; init; }
	public required int NestedCount { get; init; }
	public required bool UseHidden { get; init; }
	public ApiPropertyList? Properties { get; init; }

	/// <summary>The first paragraph of the variant schema's description; empty when it has none.</summary>
	public HtmlString DescriptionHtml { get; init; } = HtmlString.Empty;
	public string? DescriptionMarkdown { get; init; }

	/// <summary>The name a chip or menu entry shows; an array variant keeps its <c>[]</c> so it stays apart from the plain one.</summary>
	public string ChipName => IsArrayVariant ? $"{DisplayName}[]" : DisplayName;

	/// <summary>The discriminator property and the value that selects this variant, e.g. <c>type: eql</c>.</summary>
	public string? DiscriminatorLabel { get; init; }
}

/// <summary>The expanded variants of a union; the model for <c>_UnionOptions</c>.</summary>
public record ApiUnionVariants
{
	/// <summary>Renders nothing; used where the original template early-returned but its wrapper still rendered.</summary>
	public static readonly ApiUnionVariants Empty = new() { Variants = [], ContainerId = "" };

	public required IReadOnlyList<ApiUnionVariant> Variants { get; init; }

	/// <summary>From this many variants, a list shows one at a time behind a row of chips.</summary>
	public const int ChipThreshold = 3;

	/// <summary>Three or more variants show one at a time behind a row of chips, so a long list stays short to scan.</summary>
	public bool UseChips => Variants.Count >= ChipThreshold;

	/// <summary>Eleven or more variants also get a filter in the chips' "more" menu.</summary>
	public bool HasFilter => Variants.Count >= 11;

	public required string ContainerId { get; init; }

	/// <summary>"Any of:" or "One of:" above a body-level list that has no property row to carry it.</summary>
	public string? Label { get; init; }
}
