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
	/// <summary>Required objects nested in required objects stop here; a deeper compound value is <c>null</c>. A scalar still gets its value at any depth.</summary>
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
		if (resolved is null)
			return null;

		if (Stated(resolved) is { } stated)
			return stated.DeepClone();

		if (UnionSchemas.TryGet(resolved, out _, out var members))
		{
			// The first variant that is a value; `null` is listed as a variant only to allow leaving the field out.
			var variant = members.FirstOrDefault(m => (analyzer.ResolveSchema(m) ?? m).Type != JsonSchemaType.Null);
			return depth < MaxDepth && variant is not null ? Value(analyzer, variant, depth + 1) : null;
		}

		var effective = analyzer.Flatten(resolved);
		var type = resolved.Type ?? JsonSchemaType.Null;
		if (type.HasFlag(JsonSchemaType.Array))
			return depth < MaxDepth ? Items(analyzer, resolved, depth) : null;
		if (effective.Properties.Count > 0 || effective.MapValue is not null || type.HasFlag(JsonSchemaType.Object))
			return depth < MaxDepth ? Object(analyzer, effective, depth) : null;

		// A property written as `allOf: [{ $ref }]` plus a description has no type of its own; the member it wraps does.
		if (resolved.Type is null && resolved.AllOf is { Count: > 0 } wrapped && depth < MaxDepth)
			return wrapped.Select(member => Value(analyzer, member, depth + 1)).FirstOrDefault(static v => v is not null);

		return Placeholder(type);
	}

	/// <summary>The first value the schema states itself, whether an example, a default, a constant or an enum member.</summary>
	private static JsonNode? Stated(IOpenApiSchema schema) =>
		SchemaHelpers.FirstExample(schema)
			?? (SchemaHelpers.IsValue(schema.Default) ? schema.Default : null)
			?? Const(schema)
			?? schema.Enum?.FirstOrDefault(SchemaHelpers.IsValue);

	/// <summary>The reader keeps <c>const</c> as text, so a number or boolean is parsed back unless the schema says it is a string.</summary>
	private static JsonNode? Const(IOpenApiSchema schema)
	{
		if (string.IsNullOrEmpty(schema.Const))
			return null;
		if (schema.Type?.HasFlag(JsonSchemaType.String) != false)
			return JsonValue.Create(schema.Const);
		try
		{
			return JsonNode.Parse(schema.Const);
		}
		catch (JsonException)
		{
			return JsonValue.Create(schema.Const);
		}
	}

	private static JsonArray Items(SchemaAnalyzer analyzer, IOpenApiSchema array, int depth) =>
		Value(analyzer, array.Items, depth + 1) is { } item ? [item] : [];

	/// <summary>
	/// The required properties, without the ones the server sets (<c>readOnly</c>). A property with nothing to show is left
	/// out rather than written as <c>null</c>, which a schema such as <c>not: {}</c> would reject.
	/// </summary>
	private static JsonObject Object(SchemaAnalyzer analyzer, EffectiveSchema effective, int depth)
	{
		var body = new JsonObject();
		foreach (var (name, property) in effective.Properties)
		{
			if (!effective.Required.Contains(name) || analyzer.ResolveSchema(property)?.ReadOnly == true)
				continue;
			if (Value(analyzer, property, depth + 1) is { } value)
				body[name] = value;
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
