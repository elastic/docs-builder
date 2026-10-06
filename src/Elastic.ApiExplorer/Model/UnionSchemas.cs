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
	/// <summary><c>oneOf</c> wins when a schema carries both keywords.</summary>
	public static bool TryGet(IOpenApiSchema? schema, out UnionKeyword keyword, out IList<IOpenApiSchema> members)
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

	public static bool IsUnion(IOpenApiSchema? schema) => TryGet(schema, out _, out _);

	public static string ToSchemaKeyword(this UnionKeyword keyword) => keyword == UnionKeyword.AnyOf ? "anyOf" : "oneOf";
}
