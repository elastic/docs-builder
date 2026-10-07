// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.ApiExplorer.Model;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Components.PropertyTree;

/// <summary>What a request or response body lists: its properties, or the variants of a body that is itself a union.</summary>
internal static class ApiBodyContent
{
	/// <summary>
	/// The property list or the variant list of a request or response body. A union whose properties come only from an
	/// <c>allOf</c> lists its variants, which already carry those shared properties. A union that declares properties
	/// itself keeps listing them, as property rows do.
	/// </summary>
	public static (ApiPropertyList? Properties, ApiUnionVariants? UnionVariants) Build(
		IOpenApiSchema schema,
		PropertyTreeScope scope,
		SchemaAnalyzer analyzer,
		ApiPropertyTreeBuilder builder
	)
	{
		var declaresProperties = (analyzer.ResolveSchema(schema) ?? schema).Properties is { Count: > 0 };
		var variants = analyzer.GetTypeInfo(schema).IsUnion && !declaresProperties
			? BuildUnionVariants(schema, scope, analyzer, builder)
			: null;
		return variants is not null ? (null, variants) : (builder.BuildPropertyList(schema, scope), null);
	}

	/// <summary>The variants of a body that is itself a union, under a label that says what they describe.</summary>
	public static ApiUnionVariants? BuildUnionVariants(
		IOpenApiSchema bodySchema,
		PropertyTreeScope scope,
		SchemaAnalyzer analyzer,
		ApiPropertyTreeBuilder builder
	)
	{
		var typeInfo = analyzer.GetTypeInfo(bodySchema);
		if (typeInfo is not { IsUnion: true, UnionOptions.Count: > 0 })
			return null;

		var options = typeInfo.UnionOptions.Where(static o => o.Schema is not null).Select(o => DescribeMember(o, analyzer)).ToList();
		var variants = options.Count == 0 ? null : builder.BuildUnionVariants(options, scope, analyzer.GetUnionDiscriminator(bodySchema));
		return variants is null ? null : variants with { Label = UnionLabel(typeInfo) };
	}

	/// <summary>
	/// The label above a body's variants. An array whose items are a union says so, since the variants then describe each
	/// item rather than the body.
	/// </summary>
	private static string UnionLabel(TypeInfo typeInfo)
	{
		var label = SchemaHelpers.UnionLabel(typeInfo.UnionKeyword);
		return typeInfo.IsArray ? $"An array; each item is {label.ToLowerInvariant()}" : label;
	}

	/// <summary>
	/// Classifies a union member the way a body-level list always has. A merged <c>allOf</c> variant has no <c>$ref</c> of its own,
	/// so it keeps the name and <c>$ref</c> of the option it came from; discriminator mappings match on that <c>$ref</c>.
	/// </summary>
	private static UnionOption DescribeMember(UnionOption option, SchemaAnalyzer analyzer)
	{
		if (option.Schema is MergedVariantSchema)
			return option;

		// Keep the inline member's own name (its title) and label; take the classification from the type.
		var classified = analyzer.ClassifyOption(option.Schema!);
		var keepsName = string.IsNullOrEmpty(option.Ref) && option is { IsObject: true, IsArray: false };
		return (keepsName ? classified with { BaseName = option.BaseName } : classified) with { Label = option.Label };
	}

	/// <summary>Names a collapsed section header lists; an array variant keeps its <c>[]</c> so it stays apart from the plain one.</summary>
	public static IEnumerable<string> VariantNames(ApiUnionVariants? variants) =>
		(variants?.Variants ?? []).Select(static v => v.IsArrayVariant ? $"{v.DisplayName}[]" : v.DisplayName);
}
