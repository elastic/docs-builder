// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Model;

/// <summary>The schema keyword a union came from.</summary>
public enum UnionKeyword
{
	OneOf,
	AnyOf
}

/// <summary>The one place that decides whether a schema is a <c>oneOf</c>/<c>anyOf</c> union and which keyword wins.</summary>
public static class UnionSchemas
{
	/// <summary>
	/// <c>oneOf</c> wins when a schema carries both keywords. Members that only list <c>required</c> fields offer no shapes,
	/// so they make no union; <see cref="TryGetRequiredAlternatives"/> reads them instead.
	/// </summary>
	public static bool TryGet(IOpenApiSchema? schema, out UnionKeyword keyword, out IList<IOpenApiSchema> members) =>
		TryGetMembers(schema, out keyword, out members) && !members.All(IsRequiredOnly);

	/// <summary>
	/// The field sets a <c>oneOf</c>/<c>anyOf</c> of <c>required</c>-only members asks for, e.g. <c>correlation_id</c> or
	/// <c>externalId</c>: the object needs one of them (<c>anyOf</c>: at least one).
	/// </summary>
	public static bool TryGetRequiredAlternatives(
		IOpenApiSchema? schema,
		out UnionKeyword keyword,
		out IReadOnlyList<IReadOnlyList<string>> alternatives
	)
	{
		alternatives = TryGetMembers(schema, out keyword, out var members) && members.All(IsRequiredOnly)
			? members.Select(static m => (IReadOnlyList<string>)[.. m.Required!]).ToArray()
			: [];
		return alternatives.Count > 0;
	}

	private static bool TryGetMembers(IOpenApiSchema? schema, out UnionKeyword keyword, out IList<IOpenApiSchema> members)
	{
		if (schema?.OneOf is { Count: > 0 } oneOf)
		{
			keyword = UnionKeyword.OneOf;
			members = oneOf;
			return true;
		}

		if (schema?.AnyOf is { Count: > 0 } anyOf)
		{
			keyword = UnionKeyword.AnyOf;
			members = anyOf;
			return true;
		}

		keyword = default;
		members = [];
		return false;
	}

	/// <summary>A member like <c>{ "required": ["externalId"] }</c>: a constraint on the parent's fields, not a shape.</summary>
	private static bool IsRequiredOnly(IOpenApiSchema member) =>
		member is OpenApiSchema
		{
			Required.Count: > 0,
			Type: null,
			Properties: null or { Count: 0 },
			Items: null,
			Enum: null or { Count: 0 },
			AllOf: null or { Count: 0 },
			OneOf: null or { Count: 0 },
			AnyOf: null or { Count: 0 },
			AdditionalProperties: null
		};

	public static bool IsUnion(IOpenApiSchema? schema) => TryGet(schema, out _, out _);

	/// <summary>True for the type name an inline union displays (<c>oneOf</c>/<c>anyOf</c>), which names no schema.</summary>
	public static bool IsKeywordName(string? name) => name is "oneOf" or "anyOf";

	public static string ToSchemaKeyword(this UnionKeyword keyword) => keyword == UnionKeyword.AnyOf ? "anyOf" : "oneOf";
}
