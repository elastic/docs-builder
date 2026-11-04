// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Model;
using Elastic.ApiExplorer.Operations;
using Microsoft.AspNetCore.Html;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Components.PropertyTree;

public partial class ApiPropertyTreeBuilder
{
	/// <summary>
	/// Lists a row's children and records them as the page's listing of the row's shape. While they are built the shape
	/// counts as an ancestor, so a recursive union that a spec inlines level after level stops at the first repeat. When
	/// the finished listing matches an earlier one exactly, the row links there instead and the copy is dropped.
	/// </summary>
	private (ApiPropertyChildren Children, RepeatedShape? Repeats) ListChildren(
		PropertyRow row,
		PropertyTreeScope scope,
		Expansion expansion
	)
	{
		if (row.ShapeKey is not { } key)
			return (BuildChildren(row, scope, expansion), null);

		var shape = new RepeatedShape(row.Name, row.AnchorId, row.TypeInfo.IsUnion, scope.Owner);
		var checkpoint = _shapes.Checkpoint;
		_shapes.BeginListing(key, shape);
		var children = BuildChildren(row, scope, expansion);
		_shapes.EndListing(key);

		var content = ListingContent.Of(children, _shapes);
		if (_shapes.Listing(key, content) is { } earlier)
		{
			_shapes.Rollback(checkpoint);
			return (ApiPropertyChildren.None, earlier);
		}

		_shapes.Record(key, shape, content, CountRows(children));
		return (children, null);
	}

	/// <summary>Named object types repeat by <c>$ref</c>; unions, which are often inline, repeat by their option signature.</summary>
	private string? ShapeKey(TypeInfo typeInfo) => typeInfo switch
	{
		{ IsUnion: true, UnionOptions.Count: > 1 } => "union:" + UnionSignature(typeInfo.UnionOptions),
		{ IsObject: true, IsDictionary: false, SchemaRef: { Length: > 0 } reference } => "type:" + reference,
		_ => null
	};

	private static int CountRows(ApiPropertyChildren children) =>
		CountRows(children.Properties)
			+ (children.Variants?.Variants.Sum(v => CountRows(v.Properties)) ?? 0)
			+ CountRows(children.Dictionary?.Properties);

	private static int CountRows(ApiPropertyList? properties) => properties?.Items.Sum(p => 1 + CountRows(p.Children)) ?? 0;

	/// <summary>
	/// What a union offers: each referenced option by its <c>$ref</c>, each inline option by a fingerprint of its fields.
	/// Two unions with the same signature offer the same shapes, so the second can link to the first.
	/// </summary>
	private string UnionSignature(IEnumerable<UnionOption> unionOptions) =>
		string.Join(
			";",
			unionOptions.Select(
				o => !string.IsNullOrEmpty(o.Ref) && o.Schema is not MergedVariantSchema
					? $"ref:{o.Name}@{o.Ref}"
					: $"{o.Name}{{{Fingerprint(o.IsArray ? o.Schema?.Items : o.Schema, depth: 2)}}}"
			)
		);

	/// <summary>
	/// Each field's name, requiredness and type, and the map value of an object that is also a map. A union-typed field
	/// contributes only its type, so a generated spec that repeats a recursive union level after level still matches its parent.
	/// </summary>
	private string Fingerprint(IOpenApiSchema? schema, int depth)
	{
		var resolved = _analyzer.ResolveSchema(schema) ?? schema;
		var required = resolved?.Required ?? new HashSet<string>();
		var fields = (_analyzer.GetSchemaProperties(schema) ?? new Dictionary<string, IOpenApiSchema>()).OrderBy(
			p => p.Key,
			StringComparer.Ordinal
		).Select(p => $"{p.Key}{(required.Contains(p.Key) ? "!" : "")}:{TypeFingerprint(p.Value, depth)}");
		var mapValue = resolved?.AdditionalProperties is { } value
			? [$"{DictionaryKeyName}:{TypeFingerprint(value, depth)}"]
			: Array.Empty<string>();
		return string.Join(",", fields.Concat(mapValue));
	}

	/// <summary>A field's or map value's type and <c>$ref</c>, and one more level for an inline object.</summary>
	private string TypeFingerprint(IOpenApiSchema schema, int depth)
	{
		var type = _analyzer.GetTypeInfo(schema);
		var nested = depth > 1 && type is { IsObject: true, IsUnion: false, SchemaRef: null or "" }
			? $"{{{Fingerprint(type.IsArray ? schema.Items : schema, depth - 1)}}}"
			: "";
		return $"{(type.IsArray ? "[]" : "")}{type.TypeName}@{type.SchemaRef}{nested}";
	}
}
