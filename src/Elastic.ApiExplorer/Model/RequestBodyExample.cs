// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Model;

/// <summary>
/// Builds a request body from the request schema when the spec has no example, so a sample for an operation that takes
/// a body is not a bare <c>POST</c>. The body holds the required properties only, which is the smallest request the
/// operation accepts. Each value is the first one the spec states (<c>examples</c>, <c>default</c>, <c>const</c>, the first
/// <c>enum</c> value), else a placeholder for its type. A union takes its first variant.
/// </summary>
public static class RequestBodyExample
{
	/// <summary>Required properties of required properties stop here; deeper values are <c>null</c>.</summary>
	private const int MaxDepth = 6;

	private static readonly JsonSerializerOptions PrettyJson = new() { WriteIndented = true };

	/// <summary>The body as indented JSON. Null unless the request is JSON and has a schema.</summary>
	public static string? Build(SchemaAnalyzer analyzer, string? mediaType, IOpenApiSchema? schema)
	{
		if (schema is null || !IsJson(mediaType))
			return null;

		return Value(analyzer, schema, 0)?.ToJsonString(PrettyJson);
	}

	/// <summary>One JSON document: <c>application/json</c> and <c>+json</c> types, but not newline-delimited JSON.</summary>
	internal static bool IsJson(string? mediaType) =>
		mediaType is not null
			&& (mediaType.Equals("application/json", StringComparison.OrdinalIgnoreCase)
				|| mediaType.EndsWith("+json", StringComparison.OrdinalIgnoreCase));

	private static JsonNode? Value(SchemaAnalyzer analyzer, IOpenApiSchema? schema, int depth)
	{
		var resolved = analyzer.ResolveSchema(schema);
		if (resolved is null || depth > MaxDepth)
			return null;

		if (Stated(resolved) is { } stated)
			return stated.DeepClone();

		if (UnionSchemas.TryGet(resolved, out _, out var members))
			return Value(analyzer, members[0], depth + 1);

		var effective = analyzer.Flatten(resolved);
		var type = resolved.Type ?? JsonSchemaType.Null;
		if (type.HasFlag(JsonSchemaType.Array))
			return Items(analyzer, resolved, depth);
		if (effective.Properties.Count > 0 || effective.MapValue is not null || type.HasFlag(JsonSchemaType.Object))
			return Object(analyzer, effective, depth);

		return Placeholder(type);
	}

	/// <summary>The first value the schema states itself, whether an example, a default, a constant or an enum member.</summary>
	private static JsonNode? Stated(IOpenApiSchema schema) =>
		SchemaHelpers.FirstExample(schema)
			?? schema.Default
			?? (string.IsNullOrEmpty(schema.Const) ? null : JsonValue.Create(schema.Const))
			?? schema.Enum?.FirstOrDefault(static e => e is not null);

	private static JsonArray Items(SchemaAnalyzer analyzer, IOpenApiSchema array, int depth) =>
		Value(analyzer, array.Items, depth + 1) is { } item ? [item] : [];

	/// <summary>The required properties, without the ones the server sets (<c>readOnly</c>).</summary>
	private static JsonObject Object(SchemaAnalyzer analyzer, EffectiveSchema effective, int depth)
	{
		var body = new JsonObject();
		foreach (var (name, property) in effective.Properties)
		{
			if (!effective.Required.Contains(name) || analyzer.ResolveSchema(property)?.ReadOnly == true)
				continue;
			body[name] = Value(analyzer, property, depth + 1);
		}

		return body;
	}

	private static JsonNode? Placeholder(JsonSchemaType type) => type switch
	{
		_ when type.HasFlag(JsonSchemaType.String) => JsonValue.Create("string"),
		_ when type.HasFlag(JsonSchemaType.Integer) || type.HasFlag(JsonSchemaType.Number) => JsonValue.Create(0),
		_ when type.HasFlag(JsonSchemaType.Boolean) => JsonValue.Create(false),
		_ => null
	};
}
