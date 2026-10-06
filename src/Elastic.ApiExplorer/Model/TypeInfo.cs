// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.ApiExplorer.Operations;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Model;

/// <summary>
/// Represents a union option with full schema information.
/// </summary>
public record UnionOption(string Name, string? Ref, bool IsObject, IOpenApiSchema? Schema);

/// <summary>A named schema merged into a type through <c>allOf</c>, beyond the one that names the type.</summary>
public record ComposedType(string Name, bool HasLink);

/// <summary>
/// Unified type information record used by both OperationView and SchemaView.
/// Contains all metadata needed for rendering schema types.
/// </summary>
public record TypeInfo
{
	/// <summary>The display name of the type.</summary>
	public required string TypeName { get; init; }

	/// <summary>The schema reference ID, if applicable.</summary>
	public string? SchemaRef { get; init; }

	public bool IsArray { get; init; }

	/// <summary>Whether this is an object type (has properties).</summary>
	public bool IsObject { get; init; }

	/// <summary>Whether this is a known value type (resolves to primitive).</summary>
	public bool IsValueType { get; init; }

	/// <summary>The primitive base type for value types.</summary>
	public string? ValueTypeBase { get; init; }

	/// <summary>Whether this type has a dedicated page to link to.</summary>
	public bool HasLink { get; init; }

	/// <summary>The options of a <c>oneOf</c>/<c>anyOf</c> union, with schema references for potential expansion.</summary>
	public List<UnionOption>? UnionOptions { get; init; }

	/// <summary>The schema keyword a union came from; null for every other type.</summary>
	public UnionKeyword? UnionKeyword { get; init; }

	public bool IsUnion => UnionKeyword is not null;

	/// <summary>Whether this is a dictionary/map type (additionalProperties).</summary>
	public bool IsDictionary { get; init; }

	/// <summary>The schema for dictionary value types.</summary>
	public IOpenApiSchema? DictValueSchema { get; init; }

	public bool IsEnum { get; init; }

	/// <summary>Every literal the value can take, from <see cref="SchemaAnalyzer.GetEnumValues"/>; set for unions and arrays of enums too.</summary>
	public string[]? EnumValues { get; init; }

	/// <summary>The primitive item type for arrays of primitives.</summary>
	public string? ArrayItemType { get; init; }

	/// <summary>Further named schemas an <c>allOf</c> merges in after the first <c>$ref</c>, which names the type.</summary>
	public List<ComposedType>? AlsoIncludes { get; init; }
}
