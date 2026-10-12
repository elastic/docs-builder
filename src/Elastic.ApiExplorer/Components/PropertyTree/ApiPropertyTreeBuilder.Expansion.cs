// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.ApiExplorer.Model;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Components.PropertyTree;

public partial class ApiPropertyTreeBuilder
{
	/// <summary>What a property row lists below it. Chosen once, so the count, the toggle and the children always agree.</summary>
	private abstract record ChildPlan(int Count)
	{
		public sealed record None() : ChildPlan(0);

		/// <summary>The properties of the dictionary's value type, under a <c>&lt;string&gt;</c> key row.</summary>
		public sealed record Dictionary(int Count) : ChildPlan(Count);

		/// <summary>The schema's properties, then the variants of a union that declares properties as well.</summary>
		public sealed record Properties(IOpenApiSchema Schema, int Count, List<UnionOption>? UnionVariants = null) : ChildPlan(Count);

		/// <summary>
		/// A union's variants. <paramref name="Inner"/> is <c>X</c> of an <c>X | X[]</c> row: the row hides its own options
		/// line, so the list names <c>X</c> and reads its discriminator from it.
		/// </summary>
		public sealed record Variants(List<UnionOption> Options, int Count, (string Name, IOpenApiSchema Schema)? Inner = null) : ChildPlan(
			Count
		);
	}

	/// <summary>An <c>X | X[]</c> union: the name of <c>X</c>, and whether <c>X</c> lists fields of its own.</summary>
	private sealed record ArrayUnion(string BaseName, bool Expands);

	/// <summary>
	/// How a row expands. <see cref="HasUnionOptions"/> and <see cref="ArrayUnion"/> describe the union whatever the plan
	/// picked, since the union row reads them even when the row lists properties instead.
	/// </summary>
	private sealed record Expansion(ChildPlan Plan, bool HasUnionOptions, ArrayUnion? ArrayUnion, bool IsCollapsible, bool DefaultExpanded)
	{
		public int NestedCount => Plan.Count;
	}

	private Expansion ComputeExpansion(IOpenApiSchema propSchema, TypeInfo typeInfo, int depth, bool isRecursive)
	{
		var withinDepth = depth < options.MaxDepth;
		var arrayUnionBase = DetectSimpleArrayUnion(typeInfo);
		var arrayUnionPlan = arrayUnionBase is not null && withinDepth ? PlanArrayUnion(typeInfo, arrayUnionBase) : new ChildPlan.None();
		var hasUnionOptions = withinDepth
			&& arrayUnionBase is null
			&& typeInfo is { IsUnion: true, UnionOptions: not null }
			&& typeInfo.UnionOptions.Any(_analyzer.UnionOptionHasProperties);

		var plan = withinDepth ? PlanChildren(propSchema, typeInfo, hasUnionOptions) ?? arrayUnionPlan : new ChildPlan.None();
		var isCollapsible = !isRecursive && plan is ChildPlan.Properties or ChildPlan.Variants && plan.Count > 0 && !hasUnionOptions;
		return new Expansion(
			plan,
			hasUnionOptions,
			arrayUnionBase is null ? null : new ArrayUnion(arrayUnionBase, arrayUnionPlan is not ChildPlan.None),
			isCollapsible,
			ComputeDefaultExpanded()
		);
	}

	/// <summary>
	/// The first of: the dictionary's value properties, the row's own properties, the array item's properties, the union's
	/// variants. Null when none applies.
	/// </summary>
	private ChildPlan? PlanChildren(IOpenApiSchema propSchema, TypeInfo typeInfo, bool hasUnionOptions)
	{
		if (
			typeInfo is { IsDictionary: true, HasLink: false, DictValueSchema: { } value } && PropertyCount(value) is > 0 and var valueCount
		)
			return new ChildPlan.Dictionary(valueCount);

		var variantCount = hasUnionOptions ? typeInfo.UnionOptions!.Count(_analyzer.UnionOptionHasProperties) : 0;
		var listsOwnProperties = typeInfo is { IsObject: true, HasLink: false }
			&& (!typeInfo.IsUnion || _analyzer.DeclaresProperties(propSchema));
		var listed = typeInfo.IsUnion ? _analyzer.SharedProperties(propSchema) : propSchema;
		if (listsOwnProperties && PropertyCount(listed) is > 0 and var ownCount)
			return new ChildPlan.Properties(listed, ownCount + variantCount, hasUnionOptions ? typeInfo.UnionOptions : null);

		if (typeInfo is { IsArray: true, HasLink: false } && propSchema.Items is { } items && PropertyCount(items) is > 0 and var itemCount)
			return new ChildPlan.Properties(items, itemCount);

		return hasUnionOptions ? new ChildPlan.Variants(typeInfo.UnionOptions!, variantCount) : null;
	}

	private int PropertyCount(IOpenApiSchema? schema) => _analyzer.GetSchemaProperties(schema)?.Count ?? 0;

	/// <summary>The base name of an <c>X | X[]</c> union, or null for any other type.</summary>
	private static string? DetectSimpleArrayUnion(TypeInfo typeInfo)
	{
		if (typeInfo is not { IsUnion: true, UnionOptions.Count: > 0 })
			return null;

		var distinctOptions = typeInfo.UnionOptions.DistinctBy(o => o.Name).ToArray();
		if (distinctOptions.Length != 2)
			return null;

		var baseNames = distinctOptions.Select(o => o.BaseName).Distinct().ToArray();
		return baseNames is [{ Length: > 0 } baseName] ? baseName : null;
	}

	/// <summary>
	/// What an <c>X | X[]</c> union lists: the fields of <c>X</c>, or its variants when <c>X</c> is itself a union.
	/// Nothing when <c>X</c> has its own page or no fields.
	/// </summary>
	private ChildPlan PlanArrayUnion(TypeInfo typeInfo, string baseName)
	{
		var baseOption = typeInfo.UnionOptions!.FirstOrDefault(o => o.Name == baseName);
		if (
			baseOption?.Schema is not { } schema || _analyzer.GetTypeInfo(schema).HasLink || !_analyzer.UnionOptionHasProperties(baseOption)
		)
			return new ChildPlan.None();

		if (PropertyCount(schema) is > 0 and var count)
			return new ChildPlan.Properties(schema, count);

		var nested = _analyzer.GetNestedUnionOptions(schema);
		return nested.Count > 0
			? new ChildPlan.Variants(nested, nested.Count(_analyzer.UnionOptionHasProperties), (baseName, schema))
			: new ChildPlan.Properties(schema, 0);
	}

	private ApiPropertyChildren BuildChildren(PropertyRow row, PropertyTreeScope scope, Expansion expansion)
	{
		var childScope = scope with
		{
			Prefix = row.AnchorId,
			Depth = scope.Depth + 1,
			AncestorRefs = AugmentAncestors(row.TypeInfo, scope.AncestorRefs),
			RequiredProperties = null,
			Owner = row.Name
		};
		var useHidden = options.UseHiddenUntilFound && expansion.IsCollapsible && !expansion.DefaultExpanded;

		return expansion.Plan switch
		{
			ChildPlan.Dictionary => BuildDictionaryChildren(row, childScope, expansion),
			ChildPlan.Properties properties =>
				new ApiPropertyChildren
				{
					Kind = ChildKind.PropertyList,
					UseHidden = useHidden,
					Properties = BuildPropertyList(properties.Schema, childScope) ?? new ApiPropertyList([]),
					Variants = properties.UnionVariants is { } variants ? BuildLabelledVariants(row, childScope, variants) : null
				},
			ChildPlan.Variants variants =>
				new ApiPropertyChildren
				{
					Kind = ChildKind.UnionVariants,
					UseHidden = useHidden,
					Variants = BuildVariantChildren(row, childScope, variants)
				},
			_ => ApiPropertyChildren.None
		};
	}

	/// <summary>Variants listed below the union's own properties, labelled so they read apart from those properties.</summary>
	private ApiUnionVariants? BuildLabelledVariants(PropertyRow row, PropertyTreeScope childScope, List<UnionOption> variants) =>
		BuildUnionVariants(variants, childScope, _analyzer.GetUnionDiscriminator(row.Schema)) is { } built
			? built with { Label = SchemaHelpers.UnionLabel(row.TypeInfo.UnionKeyword) }
			: null;

	private ApiUnionVariants BuildVariantChildren(PropertyRow row, PropertyTreeScope childScope, ChildPlan.Variants plan)
	{
		var union = plan.Inner?.Schema ?? row.Schema;
		var variants = BuildUnionVariants(plan.Options, childScope, _analyzer.GetUnionDiscriminator(union));
		if (variants is null || plan.Inner is not { Name: var name })
			return variants ?? ApiUnionVariants.Empty;

		// The row's type does not always name X (an inline X | X[] reads "union oneOf"), so the label says both shapes.
		var keyword = SchemaHelpers.UnionLabel(_analyzer.GetTypeInfo(union).UnionKeyword).ToLowerInvariant();
		var readable = SchemaHelpers.ReadableSchemaName(name);
		return variants with { Label = $"{readable} or {readable}[]; each {readable} is {keyword}" };
	}
}
