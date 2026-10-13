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
			AppendArrayKeywordSpans(spans, typeInfo, hasActualProperties);
			AppendDisplayedTypeName(spans, typeInfo, hasActualProperties);
			AppendArraySuffix(spans);
			return new TypeAnnotation(spans);
		}

		AppendScalarKeywordSpans(spans, typeInfo, hasActualProperties);

		// A union and a multi-type schema (`type: [number, string]`) both read as a formula of their parts.
		if (typeInfo.Alternatives is { } alternatives)
		{
			AppendUnionFormulaSpans(spans, alternatives);
			return new TypeAnnotation(spans);
		}

		if (typeInfo.HasLink)
			AppendObjectIcon(spans);
		AppendDisplayedTypeName(spans, typeInfo, hasActualProperties);
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

	private static void AppendArrayKeywordSpans(List<TypeSpan> spans, TypeInfo typeInfo, bool hasActualProperties)
	{
		if (typeInfo.IsValueType && !string.IsNullOrEmpty(typeInfo.ValueTypeBase))
		{
			spans.Add(new TypeSpan(typeInfo.ValueTypeBase, SchemaHelpers.ValueKeywordCssClass));
			spans.Add(new TypeSpan(" ", Bare: true));
		}
		else if (typeInfo.IsEnum)
			AppendWrapperKeyword(spans, "enum", SchemaHelpers.WrapperEnumCssClass);
		else if (typeInfo.IsUnion)
			AppendWrapperKeyword(spans, "union", SchemaHelpers.WrapperUnionCssClass);
		else if (typeInfo.IsObject && !string.IsNullOrEmpty(typeInfo.SchemaRef) && (hasActualProperties || typeInfo.HasLink))
			AppendObjectIcon(spans);
	}

	private static void AppendScalarKeywordSpans(List<TypeSpan> spans, TypeInfo typeInfo, bool hasActualProperties)
	{
		if (typeInfo.IsEnum)
			AppendWrapperKeyword(spans, "enum", SchemaHelpers.WrapperEnumCssClass);
		else if (typeInfo.IsUnion)
			AppendWrapperKeyword(spans, "union", SchemaHelpers.WrapperUnionCssClass);
		else if (typeInfo.IsValueType && !string.IsNullOrEmpty(typeInfo.ValueTypeBase))
		{
			spans.Add(new TypeSpan(typeInfo.ValueTypeBase, SchemaHelpers.ValueKeywordCssClass));
			spans.Add(new TypeSpan(" ", Bare: true));
		}
		else if (typeInfo.IsObject && !string.IsNullOrEmpty(typeInfo.SchemaRef) && !typeInfo.HasLink && hasActualProperties)
			AppendObjectIcon(spans);
	}

	private static void AppendDictionarySpans(List<TypeSpan> spans, TypeInfo typeInfo, bool hasActualProperties)
	{
		var valueType = typeInfo.DictValueType;
		var valueTypeName = string.IsNullOrEmpty(valueType?.TypeName) ? "unknown" : valueType.TypeName;

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
		var typeName = typeInfo.TypeName ?? "unknown";
		if (!SchemaHelpers.IsInternalSchemaName(typeName))
		{
			// "enum" is a keyword marker already appended — inline enums have no distinct type name to show.
			if (typeInfo.IsEnum && typeName == "enum")
				return;
			var named = NamedTypeSpan(typeName, typeInfo.SchemaRef, typeInfo.IsValueType);
			spans.Add(named with { Title = named.SchemaId });
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
		for (var i = 0; i < alternatives.Count; i++)
		{
			if (i > 0)
				spans.Add(new TypeSpan(" | ", Bare: true));
			var alternative = alternatives[i];
			AppendInternalOrNamed(spans, alternative.Name, alternative.SchemaRef, labeledAlready: false);
			if (alternative.IsArray)
				AppendArraySuffix(spans);
		}
	}

	private static TypeSpan NamedTypeSpan(string typeName, string? schemaId = null, bool isValueType = false)
	{
		var css = isValueType ? SchemaHelpers.ValueCssClass : SchemaHelpers.TypeAtomCssClassOrNull(typeName);
		return new(typeName, CssClass: css, SchemaId: string.IsNullOrEmpty(schemaId) ? null : schemaId);
	}
}
