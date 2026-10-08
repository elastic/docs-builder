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
	/// The property list and the variant list of a request or response body, as property rows list them: a union that
	/// declares properties lists them above its variants (see <see cref="SchemaAnalyzer.DeclaresProperties"/>).
	/// </summary>
	public static (ApiPropertyList? Properties, ApiUnionVariants? UnionVariants) Build(
		IOpenApiSchema schema,
		PropertyTreeScope scope,
		SchemaAnalyzer analyzer,
		ApiPropertyTreeBuilder builder
	)
	{
		var variants = analyzer.GetTypeInfo(schema).IsUnion ? BuildUnionVariants(schema, scope, analyzer, builder) : null;
		if (variants is not null && !analyzer.DeclaresProperties(schema))
			return (null, variants);

		return (builder.BuildPropertyList(schema, scope), variants);
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

		var options = typeInfo.UnionOptions.Where(static o => o.Schema is not null).ToList();
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

	/// <summary>Names a collapsed section header lists; an array variant keeps its <c>[]</c> so it stays apart from the plain one.</summary>
	public static IEnumerable<string> VariantNames(ApiUnionVariants? variants) =>
		(variants?.Variants ?? []).Select(static v => v.IsArrayVariant ? $"{v.DisplayName}[]" : v.DisplayName);
}
