// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Schema;

/// <summary>
/// Shared static utilities for OpenAPI schema rendering.
/// </summary>
public static class SchemaHelpers
{
	/// <summary>
	/// Maximum depth for recursive property rendering.
	/// </summary>
	public const int MaxDepth = 100;

	/// <summary>
	/// Types that are known to be value types (resolve to primitives like string).
	/// </summary>
	public static readonly HashSet<string> KnownValueTypes = new(StringComparer.OrdinalIgnoreCase)
	{
		"Field", "Fields", "Id", "Ids", "IndexName", "Indices", "Name", "Names",
		"Routing", "VersionNumber", "SequenceNumber", "PropertyName", "RelationName",
		"TaskId", "ScrollId", "SuggestionName", "Duration", "DateMath", "Fuzziness",
		"GeoHashPrecision", "Distance", "TimeOfDay", "MinimumShouldMatch", "Script",
		"ByteSize", "Percentage", "Stringifiedboolean", "ExpandWildcards", "float", "Stringifiedinteger",
		// Numeric value types
		"uint", "ulong", "long", "int", "short", "ushort", "byte", "sbyte", "double", "decimal"
	};

	/// <summary>
	/// Types that have dedicated pages we can link to.
	/// Only container types get their own pages - individual queries/aggregations are rendered inline.
	/// </summary>
	public static readonly HashSet<string> LinkedTypes = new(StringComparer.OrdinalIgnoreCase)
	{
		"QueryContainer", "AggregationContainer", "Aggregate"
	};

	/// <summary>
	/// Gets the URL for a container type's dedicated page.
	/// </summary>
	public static string? GetContainerPageUrl(string typeName) => typeName switch
	{
		"QueryContainer" => "/api/elasticsearch/types/_types-query_dsl-querycontainer",
		"AggregationContainer" => "/api/elasticsearch/types/_types-aggregations-aggregationcontainer",
		"Aggregate" => "/api/elasticsearch/types/_types-aggregations-aggregate",
		_ => null
	};

	/// <summary>
	/// Determines if a type should link to its container page.
	/// </summary>
	/// <param name="typeName">The type name to check.</param>
	/// <param name="currentPageType">Optional current page type to prevent self-linking.</param>
	public static bool ShouldLinkToContainerPage(string typeName, string? currentPageType = null)
	{
		if (!LinkedTypes.Contains(typeName))
			return false;

		// Prevent self-linking on schema pages
		if (!string.IsNullOrEmpty(currentPageType) &&
			typeName.Equals(currentPageType, StringComparison.OrdinalIgnoreCase))
			return false;

		return true;
	}

	/// <summary>
	/// Converts a JsonSchemaType to a human-readable primitive type name.
	/// </summary>
	public static string GetPrimitiveTypeName(JsonSchemaType? type)
	{
		if (type is null)
			return "";

		if (type.Value.HasFlag(JsonSchemaType.Boolean))
			return "boolean";
		if (type.Value.HasFlag(JsonSchemaType.Integer))
			return "integer";
		if (type.Value.HasFlag(JsonSchemaType.String))
			return "string";
		if (type.Value.HasFlag(JsonSchemaType.Number))
			return "number";
		if (type.Value.HasFlag(JsonSchemaType.Null))
			return "null";
		if (type.Value.HasFlag(JsonSchemaType.Object))
			return "object";

		return "";
	}

	/// <summary>
	/// Extracts the display name from a full schema ID (e.g., "_types.query_dsl.QueryContainer" -> "QueryContainer").
	/// </summary>
	public static string FormatSchemaName(string schemaId)
	{
		var parts = schemaId.Split('.');
		return parts.Length > 0 ? parts[^1] : schemaId;
	}

	/// <summary>
	/// Checks if a type name represents a known value type.
	/// </summary>
	public static bool IsValueType(string typeName) => KnownValueTypes.Contains(typeName);

	/// <summary>
	/// Gets the primitive type base for a value type schema.
	/// </summary>
	public static string? GetValueTypeBase(IOpenApiSchema? schema)
	{
		if (schema is null)
			return null;

		var primitiveType = GetPrimitiveTypeName(schema.Type);
		if (!string.IsNullOrEmpty(primitiveType) && primitiveType != "object")
			return primitiveType;

		return null;
	}
}
