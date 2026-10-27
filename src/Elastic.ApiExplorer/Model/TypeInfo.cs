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
	/// <summary>
	/// The option's identity: <see cref="BaseName"/>, with <c>[]</c> when the member is an array of it. Grouping,
	/// signatures and anchors use it; readers see <see cref="Shown"/>.
	/// </summary>
	public string Name => IsArray ? $"{BaseName}[]" : BaseName;

	/// <summary>
	/// What readers see for <see cref="BaseName"/>: a page title, or the shape of a schema the spec names
	/// (<c>object</c>, <c>enum</c>, <c>string</c>), since the spec's type names are code names.
	/// </summary>
	public string ShownBase { get => field ?? BaseName; init; }

	/// <summary><see cref="ShownBase"/>, with <c>[]</c> when the member is an array of it: <c>(string | object)[]</c> for a formula.</summary>
	public string Shown => IsArray ? SchemaHelpers.ArrayOf(ShownBase) : ShownBase;

	/// <summary>
	/// What an object member reads as, since many look alike once their type names are gone: its title, a field with a
	/// constant value (<c>type: relative</c>), or the property names no sibling has (<c>{ and }</c>). Display only.
	/// </summary>
	public string? Label { get; init; }
}

/// <summary>One type a formula names (<c>number</c> in <c>number | string</c>), with the <c>$ref</c> it stands for.</summary>
/// <param name="Shown">What readers see for <see cref="Name"/>; see <see cref="UnionOption.ShownBase"/>.</param>
public record TypeAlternative(string Name, string? SchemaRef, bool IsArray, string Shown)
{
	/// <summary>How the formula spells it: <see cref="Shown"/>, with <c>[]</c> for an array of it.</summary>
	public string Text => IsArray ? $"{Shown}[]" : Shown;
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
	/// <summary>The spec's name for the type, or its shape for an unnamed one. Identity only; readers see <see cref="ShownName"/>.</summary>
	public required string TypeName { get; init; }

	/// <summary>
	/// What readers see in place of <see cref="TypeName"/>: a page title, or the shape of a schema the spec names
	/// (<c>object</c>, <c>enum</c>, <c>string</c>), since the spec's type names are code names.
	/// </summary>
	public string ShownName { get => field ?? TypeName; init; }

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

	/// <summary>The type of a dictionary's values; <see cref="TypeName"/> spells it <c>string to X</c>.</summary>
	public TypeInfo? DictValueType { get; init; }

	/// <summary>
	/// The types a value can be, for a type that reads as a formula (<c>number | string</c>, <c>NodeId | NodeId[]</c>);
	/// null for any other. <see cref="TypeName"/> spells the same formula.
	/// </summary>
	public IReadOnlyList<TypeAlternative>? Alternatives { get; init; }

	public bool IsFormula => Alternatives is not null;

	public bool IsEnum { get; init; }

	/// <summary>Every literal the value can take, from <see cref="SchemaAnalyzer.GetEnumValues"/>; set for unions and arrays of enums too.</summary>
	public string[]? EnumValues { get; init; }

	/// <summary>The primitive item type for arrays of primitives.</summary>
	public string? ArrayItemType { get; init; }

	/// <summary>Further named schemas an <c>allOf</c> merges in after the first <c>$ref</c>, which names the type.</summary>
	public List<ComposedType>? AlsoIncludes { get; init; }
}
