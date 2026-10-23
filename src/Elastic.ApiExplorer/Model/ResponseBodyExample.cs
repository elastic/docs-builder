// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Model;

public static class ResponseBodyExample
{
	public const string Note =
		"Generated from the API spec. Values are representative. Unions use the first variant; deeply nested or large bodies may omit fields.";

	private const int MaxDepth = 6;
	private const int MaxNodes = 200;
	private static readonly JsonSerializerOptions PrettyJson = new() { WriteIndented = true };

	public static string? Build(SchemaAnalyzer analyzer, string mediaType, IOpenApiSchema? schema)
	{
		if (schema is null || !RequestBodyExample.IsJson(mediaType))
			return null;

		return new Builder(analyzer).TryValue(schema, 0, out var value) ? value?.ToJsonString(PrettyJson) ?? "null" : null;
	}

	private sealed class Builder(SchemaAnalyzer analyzer)
	{
		private int _remaining = MaxNodes;

		public bool TryValue(IOpenApiSchema? schema, int depth, out JsonNode? value)
		{
			value = null;
			var resolved = analyzer.ResolveSchema(schema);
			if (resolved is null || _remaining-- <= 0)
				return false;

			if ((StatedValue(schema!) ?? StatedValue(resolved)) is { } stated)
			{
				value = CopyValue(stated);
				return true;
			}

			if (depth < MaxDepth && UnionSchemas.TryGet(resolved, out _, out var members))
				return TryUnion(resolved, members, depth, out value);
			if (depth < MaxDepth && resolved.AllOf is { Count: > 0 } && TryCompound(resolved, depth, out value))
				return true;
			if (TryScalar(resolved.Type, out value))
				return true;
			return depth < MaxDepth && TryCompound(resolved, depth, out value);
		}

		private bool TryUnion(IOpenApiSchema schema, IList<IOpenApiSchema> members, int depth, out JsonNode? value)
		{
			var variant = members.FirstOrDefault(m => (analyzer.ResolveSchema(m) ?? m).Type != JsonSchemaType.Null) ?? members[0];
			if (!TryValue(variant, depth + 1, out value))
				return false;
			if (value is JsonObject body)
				AddProperties(body, analyzer.Flatten(schema), depth);
			return true;
		}

		private bool TryCompound(IOpenApiSchema schema, int depth, out JsonNode? value)
		{
			value = null;
			if (schema.Type?.HasFlag(JsonSchemaType.Array) == true)
			{
				value = TryValue(schema.Items, depth + 1, out var item) ? new JsonArray(item) : [];
				return true;
			}
			var effective = analyzer.Flatten(schema);
			if (effective.Properties.Count > 0 || effective.MapValue is not null || schema.Type?.HasFlag(JsonSchemaType.Object) == true)
			{
				var body = new JsonObject();
				AddProperties(body, effective, depth);
				value = body;
				return true;
			}
			if (schema.AllOf is not { Count: > 0 } wrapped)
				return false;
			foreach (var member in wrapped)
			{
				if (TryValue(member, depth + 1, out value))
					return true;
			}
			return false;
		}

		private void AddProperties(JsonObject body, EffectiveSchema effective, int depth)
		{
			foreach (var (name, property) in effective.Properties)
			{
				if (_remaining <= 0)
					break;
				if (body.ContainsKey(name) || property.WriteOnly || analyzer.ResolveSchema(property)?.WriteOnly == true)
					continue;
				if (TryValue(property, depth + 1, out var value))
					body[name] = value;
			}
			if (body.Count == 0 && effective.MapValue is { } map && TryValue(map, depth + 1, out var mapValue))
				body["key"] = mapValue;
		}
	}

#pragma warning disable CS0618
	private static JsonNode? StatedValue(IOpenApiSchema schema) =>
		schema.Examples?.FirstOrDefault()
			?? schema.Example
			?? schema.Default
			?? (schema.Const is not null ? SchemaHelpers.StatedValue(schema) : schema.Enum?.FirstOrDefault());
#pragma warning restore CS0618

	private static bool TryScalar(JsonSchemaType? type, out JsonNode? value)
	{
		value = type switch
		{
			_ when type?.HasFlag(JsonSchemaType.String) == true => JsonValue.Create("string"),
			_ when type?.HasFlag(JsonSchemaType.Integer) == true || type?.HasFlag(JsonSchemaType.Number) == true => JsonValue.Create(0),
			_ when type?.HasFlag(JsonSchemaType.Boolean) == true => JsonValue.Create(false),
			_ => null
		};
		return value is not null || type == JsonSchemaType.Null;
	}

	internal static JsonNode? CopyValue(JsonNode? node) => node switch
	{
		JsonObject obj => new JsonObject(obj.Select(static p => KeyValuePair.Create(p.Key, CopyValue(p.Value)))),
		JsonArray array => new JsonArray([.. array.Select(CopyValue)]),
		_ => SchemaHelpers.IsValue(node) ? node!.DeepClone() : null
	};
}
