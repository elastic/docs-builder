// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.ApiExplorer.Operations;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Model;

/// <summary>
/// Represents a union option with full schema information.
/// </summary>
public record UnionOption(string BaseName, string? Ref, bool IsObject, IOpenApiSchema? Schema, bool IsArray = false)
{
	/// <summary>The display name: <see cref="BaseName"/>, with <c>[]</c> when the member is an array of it.</summary>
	public string Name => IsArray ? $"{BaseName}[]" : BaseName;

	/// <summary>
	/// What an unnamed inline object member reads as, since many share the name <c>object</c>: its title, a field with a
	/// constant value (<c>type: relative</c>), or the property names no sibling has (<c>{ and }</c>). Display only;
	/// grouping, signatures and anchors keep using <see cref="Name"/>.
	/// </summary>
	public string? Label { get; init; }
}

/// <summary>A named schema merged into a type through <c>allOf</c>, beyond the one that names the type.</summary>
/// <param name="LinkedSchemaId">The schema id of its page, when it has one (see <see cref="TypePages"/>).</param>
public record ComposedType(string Name, string? LinkedSchemaId);

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

	/// <summary>
	/// The schema id of the type's own page, when it has one (see <see cref="TypePages"/>); an array or a map takes its
	/// item's or value's.
	/// </summary>
	public string? LinkedSchemaId { get; init; }

	/// <summary>Whether this type has a dedicated page to link to.</summary>
	public bool HasLink => LinkedSchemaId is not null;

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
