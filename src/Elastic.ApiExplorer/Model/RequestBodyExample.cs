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
	/// <summary>Required objects nested in required objects stop here; a deeper compound value is left out. A scalar still gets its value at any depth.</summary>
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

		if (SchemaHelpers.StatedValue(resolved) is { } stated)
			return CloneWithoutSentinels(stated);

		var type = resolved.Type ?? JsonSchemaType.Null;
		if (depth >= MaxDepth)
			return Placeholder(type);

		if (UnionSchemas.TryGet(resolved, out _, out var members))
		{
			// The first variant that is a value; `null` is listed as a variant only to allow leaving the field out.
			var variant = members.FirstOrDefault(m => (analyzer.ResolveSchema(m) ?? m).Type != JsonSchemaType.Null);
			var value = variant is null ? null : Value(analyzer, variant, depth + 1);
			return value is JsonObject variantBody ? WithSharedProperties(analyzer, resolved, variantBody, depth) : value;
		}

		if (type.HasFlag(JsonSchemaType.Array))
			return Value(analyzer, resolved.Items, depth + 1) is { } item ? new JsonArray(item) : [];

		var effective = analyzer.Flatten(resolved);
		if (effective.Properties.Count > 0 || effective.MapValue is not null || type.HasFlag(JsonSchemaType.Object))
			return Object(analyzer, resolved, effective, depth);

		// A property written as `allOf: [{ $ref }]` plus a description has no type of its own; the member it wraps does.
		if (resolved.Type is null && resolved.AllOf is { Count: > 0 } wrapped)
			return wrapped.Select(member => Value(analyzer, member, depth + 1)).FirstOrDefault(static v => v is not null);

		return Placeholder(type);
	}

	/// <summary>The required properties a union schema declares beside its variants belong in the body with the variant's own.</summary>
	private static JsonObject WithSharedProperties(SchemaAnalyzer analyzer, IOpenApiSchema union, JsonObject variantBody, int depth)
	{
		var effective = analyzer.Flatten(union);
		if (effective.Properties.Count == 0)
			return variantBody;

		var shared = Object(analyzer, union, effective, depth);
		foreach (var name in shared.Select(static p => p.Key).ToArray())
		{
			if (variantBody.ContainsKey(name))
				continue;
			var node = shared[name];
			_ = shared.Remove(name);
			variantBody[name] = node;
		}

		return variantBody;
	}

	/// <summary>A copy of a stated value. A literal <c>null</c> inside it is stored as a sentinel string by the reader; the copy gets the <c>null</c> back.</summary>
	private static JsonNode? CloneWithoutSentinels(JsonNode? node) => node switch
	{
		JsonObject obj => new JsonObject(obj.Select(static p => KeyValuePair.Create(p.Key, CloneWithoutSentinels(p.Value)))),
		JsonArray array => new JsonArray([.. array.Select(CloneWithoutSentinels)]),
		_ => SchemaHelpers.IsValue(node) ? node!.DeepClone() : null
	};

	/// <summary>
	/// The required properties, without the ones the server sets (<c>readOnly</c>). When the schema requires one of several
	/// field sets through a <c>oneOf</c>/<c>anyOf</c> of <c>required</c> lists, the first set counts as required too, so the body
	/// satisfies the schema. A property with nothing to show is left out rather than written as <c>null</c>, which a schema
	/// such as <c>not: {}</c> would reject.
	/// </summary>
	private static JsonObject Object(SchemaAnalyzer analyzer, IOpenApiSchema schema, EffectiveSchema effective, int depth)
	{
		var required = effective.Required;
		if (UnionSchemas.TryGetRequiredAlternatives(schema, out _, out var alternatives))
			required = new HashSet<string>(required.Concat(alternatives[0]), StringComparer.Ordinal);

		var body = new JsonObject();
		foreach (var (name, property) in effective.Properties)
		{
			// readOnly may sit on the reference itself (OpenAPI 3.1) or on the schema it points to.
			if (!required.Contains(name) || property.ReadOnly || analyzer.ResolveSchema(property)?.ReadOnly == true)
				continue;
			// A property whose only type is null is sent as null; that is its value, not a value we lack.
			if ((analyzer.ResolveSchema(property) ?? property).Type == JsonSchemaType.Null)
				body[name] = null;
			else if (Value(analyzer, property, depth + 1) is { } value)
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
