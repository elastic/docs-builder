// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Model;
using Elastic.ApiExplorer.Operations;
using Microsoft.AspNetCore.Html;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Components.PropertyTree;

public partial class ApiPropertyTreeBuilder
{
	private static TypeAnnotation BuildAnnotation(TypeInfo typeInfo, bool hasActualProperties)
	{
		var spans = new List<TypeSpan>();

		if (typeInfo.IsDictionary)
		{
			AppendDictionarySpans(spans, typeInfo, hasActualProperties);
			return new TypeAnnotation(spans);
		}

		if (typeInfo.IsArray)
		{
			AppendArrayKeywordSpans(spans, typeInfo);
			AppendDisplayedTypeName(spans, typeInfo, hasActualProperties);
			AppendArraySuffix(spans);
			return new TypeAnnotation(spans);
		}

		AppendScalarKeywordSpans(spans, typeInfo);

		// A union and a multi-type schema (`type: [number, string]`) both read as a formula of their parts.
		if (typeInfo.Alternatives is { } alternatives)
		{
			AppendUnionFormulaSpans(spans, alternatives);
			return new TypeAnnotation(spans);
		}

		if (typeInfo.HasLink)
			AppendObjectIcon(spans);
		AppendDisplayedTypeName(spans, typeInfo, hasActualProperties);
		// A keyword with no name after it (enum) leaves a trailing space, which constraints would follow as a double space.
		if (spans is [.., { Bare: true, Text: " " }])
			spans.RemoveAt(spans.Count - 1);
		return new TypeAnnotation(spans);
	}

	// A keyword with no name after it (an inline enum) leaves a trailing space; "enum[]" reads better than "enum []".
	private static void AppendArraySuffix(List<TypeSpan> spans)
	{
		if (spans is [.., { Bare: true, Text: " " }])
			spans.RemoveAt(spans.Count - 1);
		spans.Add(new TypeSpan("[]", SchemaHelpers.WrapperArrayIconCssClass));
	}

	private static void AppendObjectIcon(List<TypeSpan> spans)
	{
		spans.Add(new TypeSpan("{}", SchemaHelpers.WrapperObjectIconCssClass));
		spans.Add(new TypeSpan(" ", Bare: true));
	}

	// A value type (Field, IndexName) reads as its primitive, which its shown name already is.
	private static void AppendArrayKeywordSpans(List<TypeSpan> spans, TypeInfo typeInfo)
	{
		if (typeInfo.IsEnum)
			AppendWrapperKeyword(spans, "enum", SchemaHelpers.WrapperEnumCssClass);
		else if (typeInfo.IsUnion)
			AppendWrapperKeyword(spans, "union", SchemaHelpers.WrapperUnionCssClass);
		else if (typeInfo is { IsObject: true, HasLink: true })
			AppendObjectIcon(spans);
	}

	private static void AppendScalarKeywordSpans(List<TypeSpan> spans, TypeInfo typeInfo)
	{
		if (typeInfo.IsEnum)
			AppendWrapperKeyword(spans, "enum", SchemaHelpers.WrapperEnumCssClass);
		else if (typeInfo.IsUnion)
			AppendWrapperKeyword(spans, "union", SchemaHelpers.WrapperUnionCssClass);
	}

	private static void AppendDictionarySpans(List<TypeSpan> spans, TypeInfo typeInfo, bool hasActualProperties)
	{
		var valueType = typeInfo.DictValueType;
		var valueTypeName = string.IsNullOrEmpty(valueType?.ShownName) ? "unknown" : valueType.ShownName;

		spans.Add(new TypeSpan("map", SchemaHelpers.WrapperMapKeywordCssClass));
		spans.Add(new TypeSpan(" ", Bare: true));
		spans.Add(NamedTypeSpan("string"));
		spans.Add(new TypeSpan(" to ", Bare: true));
		if (typeInfo.HasLink || hasActualProperties)
			AppendObjectIcon(spans);
		AppendInternalOrNamed(spans, valueTypeName, valueType?.SchemaRef, typeInfo.HasLink || hasActualProperties);
	}

	private static void AppendDisplayedTypeName(List<TypeSpan> spans, TypeInfo typeInfo, bool hasActualProperties)
	{
		var typeName = typeInfo.ShownName;
		if (!SchemaHelpers.IsInternalSchemaName(typeName))
		{
			// "enum" is a keyword marker already appended; an enum has no other name to show.
			if (typeInfo.IsEnum && typeName == "enum")
				return;
			// An allOf around an array schema reads as that array (number[]), though the type is not marked as one.
			if (!typeInfo.IsArray && typeName.EndsWith("[]", StringComparison.Ordinal))
			{
				spans.Add(NamedTypeSpan(typeName[..^2], typeInfo.SchemaRef));
				AppendArraySuffix(spans);
				return;
			}
			spans.Add(NamedTypeSpan(typeName, typeInfo.SchemaRef));
			return;
		}

		if (typeInfo.IsEnum || typeInfo.IsUnion || typeInfo.IsValueType || typeInfo.HasLink)
			return;
		if (typeInfo.IsObject && hasActualProperties)
			return;

		spans.Add(new TypeSpan("object", SchemaHelpers.PrimitiveCssClass));
	}

	private static void AppendInternalOrNamed(List<TypeSpan> spans, string typeName, string? schemaId, bool labeledAlready)
	{
		if (!SchemaHelpers.IsInternalSchemaName(typeName))
		{
			spans.Add(NamedTypeSpan(typeName, schemaId));
			return;
		}

		if (!labeledAlready)
			spans.Add(new TypeSpan("object", SchemaHelpers.PrimitiveCssClass));
	}

	private static void AppendWrapperKeyword(List<TypeSpan> spans, string keyword, string cssClass)
	{
		spans.Add(new TypeSpan(keyword, cssClass));
		spans.Add(new TypeSpan(" ", Bare: true));
	}

	private static void AppendUnionFormulaSpans(List<TypeSpan> spans, IReadOnlyList<TypeAlternative> alternatives)
	{
		// Types the spec tells apart can read the same once shown (Field and Id are both a string).
		var shown = alternatives.DistinctBy(static a => a.Text).ToArray();
		for (var i = 0; i < shown.Length; i++)
		{
			if (i > 0)
				spans.Add(new TypeSpan(" | ", Bare: true));
			var alternative = shown[i];
			AppendInternalOrNamed(spans, alternative.Shown, alternative.SchemaRef, labeledAlready: false);
			if (alternative.IsArray)
				AppendArraySuffix(spans);
		}
	}

	private static TypeSpan NamedTypeSpan(string typeName, string? schemaId = null) =>
		new(typeName, CssClass: SchemaHelpers.TypeAtomCssClassOrNull(typeName), SchemaId: string.IsNullOrEmpty(schemaId) ? null : schemaId);
}
