// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Schema;

/// <summary>
/// Represents a union option with full schema information.
/// </summary>
public record UnionOption(
	string Name,
	string? Ref,
	bool IsObject,
	IOpenApiSchema? Schema
);

/// <summary>
/// Unified type information record used by both OperationView and SchemaView.
/// Contains all metadata needed for rendering schema types.
/// </summary>
/// <param name="TypeName">The display name of the type.</param>
/// <param name="SchemaRef">The schema reference ID, if applicable.</param>
/// <param name="IsArray">Whether this is an array type.</param>
/// <param name="IsObject">Whether this is an object type (has properties).</param>
/// <param name="IsValueType">Whether this is a known value type (resolves to primitive).</param>
/// <param name="ValueTypeBase">The primitive base type for value types.</param>
/// <param name="HasLink">Whether this type has a dedicated page to link to.</param>
/// <param name="AnyOfOptions">Union options with schema references for potential expansion.</param>
/// <param name="IsDictionary">Whether this is a dictionary/map type (additionalProperties).</param>
/// <param name="DictValueSchema">The schema for dictionary value types.</param>
/// <param name="IsEnum">Whether this is an enum type.</param>
/// <param name="IsUnion">Whether this is a union type (oneOf/anyOf).</param>
/// <param name="EnumValues">The enum values, if this is an enum type.</param>
/// <param name="UnionOptions">String array of union option names for display.</param>
/// <param name="ArrayItemType">The primitive item type for arrays of primitives.</param>
public record TypeInfo(
	string TypeName,
	string? SchemaRef,
	bool IsArray,
	bool IsObject,
	bool IsValueType,
	string? ValueTypeBase,
	bool HasLink,
	List<UnionOption>? AnyOfOptions,
	bool IsDictionary = false,
	IOpenApiSchema? DictValueSchema = null,
	bool IsEnum = false,
	bool IsUnion = false,
	string[]? EnumValues = null,
	string[]? UnionOptions = null,
	string? ArrayItemType = null
);
