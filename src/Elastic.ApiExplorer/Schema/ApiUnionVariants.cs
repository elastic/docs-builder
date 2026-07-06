// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.ApiExplorer.Schema;

/// <summary>A single expanded union variant; the array/non-array pairing and children are precomputed.</summary>
public record ApiUnionVariant
{
	/// <summary>Display name without the <c>[]</c> suffix when the array icon is already shown.</summary>
	public required string DisplayName { get; init; }
	public required bool IsArrayVariant { get; init; }
	public required bool IsObjectType { get; init; }
	public required string AnchorId { get; init; }
	public required bool ShowProperties { get; init; }
	public required bool IsCollapsible { get; init; }
	public required bool DefaultExpanded { get; init; }
	public required int NestedCount { get; init; }
	public required bool UseHidden { get; init; }
	public ApiPropertyList? Properties { get; init; }
}

/// <summary>The expanded variants of a union; the model for <c>_UnionOptions</c>.</summary>
public record ApiUnionVariants
{
	public required IReadOnlyList<ApiUnionVariant> Variants { get; init; }
	public required bool ShouldCollapse { get; init; }
	public required string ContainerId { get; init; }
	public required bool UseHiddenUntilFound { get; init; }
}
