// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.ApiExplorer.Model;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Components.PropertyTree;

/// <summary>What a request or response body lists: its properties, the variants of a union body, or both.</summary>
/// <param name="Requires">Which of the body's fields it needs, when its <c>oneOf</c>/<c>anyOf</c> only lists <c>required</c> sets.</param>
internal sealed record ApiBodyContent(ApiPropertyList? Properties, ApiUnionVariants? UnionVariants, RequiredAlternatives? Requires)
{
	public static readonly ApiBodyContent Empty = new(null, null, null);

	/// <summary>
	/// The content of a request or response body, as property rows list it: a union that declares properties lists them
	/// above its variants (see <see cref="SchemaAnalyzer.DeclaresProperties"/>).
	/// </summary>
	public static ApiBodyContent Build(
		IOpenApiSchema schema,
		PropertyTreeScope scope,
		SchemaAnalyzer analyzer,
		ApiPropertyTreeBuilder builder
	)
	{
		var variants = analyzer.GetTypeInfo(schema).IsUnion ? BuildUnionVariants(schema, scope, analyzer, builder) : null;
		var properties = variants is not null && !analyzer.DeclaresProperties(schema) ? null : builder.BuildPropertyList(schema, scope);
		return new ApiBodyContent(properties, variants, builder.DescribeRequiredAlternatives(schema));
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
