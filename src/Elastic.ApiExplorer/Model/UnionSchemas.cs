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
	/// The union a schema offers: the first of <c>oneOf</c> and <c>anyOf</c> whose members are shapes. Members that only
	/// list <c>required</c> fields offer no shapes; <see cref="TryGetRequiredAlternatives"/> reads them instead, so a schema
	/// can carry a <c>oneOf</c> of required sets next to an <c>anyOf</c> of variants.
	/// </summary>
	public static bool TryGet(IOpenApiSchema? schema, out UnionKeyword keyword, out IList<IOpenApiSchema> members) =>
		TryFind(schema, requiredOnly: false, out keyword, out members);

	/// <summary>
	/// The field sets the first <c>oneOf</c>/<c>anyOf</c> of <c>required</c>-only members asks for, e.g. <c>correlation_id</c>
	/// or <c>externalId</c>: the object needs one of them (<c>anyOf</c>: at least one).
	/// </summary>
	public static bool TryGetRequiredAlternatives(
		IOpenApiSchema? schema,
		out UnionKeyword keyword,
		out IReadOnlyList<IReadOnlyList<string>> alternatives
	)
	{
		alternatives = TryFind(schema, requiredOnly: true, out keyword, out var members)
			? members.Select(static m => (IReadOnlyList<string>)[.. m.Required!]).ToArray()
			: [];
		return alternatives.Count > 0;
	}

	/// <summary><c>oneOf</c> before <c>anyOf</c>: the first keyword whose members are all required-only, or all not.</summary>
	private static bool TryFind(IOpenApiSchema? schema, bool requiredOnly, out UnionKeyword keyword, out IList<IOpenApiSchema> members)
	{
		foreach (var (candidate, list) in new[] { (UnionKeyword.OneOf, schema?.OneOf), (UnionKeyword.AnyOf, schema?.AnyOf) })
		{
			if (list is not { Count: > 0 } || list.All(IsRequiredOnly) != requiredOnly)
				continue;
			keyword = candidate;
			members = list;
			return true;
		}

		keyword = default;
		members = [];
		return false;
	}

	/// <summary>
	/// A member like <c>{ "required": ["externalId"] }</c>, with or without <c>"type": "object"</c>: a constraint on the
	/// parent's fields, not a shape. A <c>$ref</c> member reads through to its target.
	/// </summary>
	private static bool IsRequiredOnly(IOpenApiSchema member) =>
		member is
		{
			Required.Count: > 0,
			Type: null or JsonSchemaType.Object,
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
