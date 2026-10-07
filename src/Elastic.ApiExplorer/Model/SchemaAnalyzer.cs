// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Concurrent;
using Elastic.ApiExplorer.Operations;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Model;

/// <summary>
/// Analyzes OpenAPI schemas and provides type information.
/// Requires access to the OpenApiDocument for resolving schema references.
/// </summary>
/// <remarks>
/// Creates a new SchemaAnalyzer.
/// </remarks>
/// <param name="document">The OpenAPI document for resolving schema references.</param>
/// <param name="currentPageType">Optional current page type to prevent self-linking on schema pages.</param>
/// <param name="resolveCache">
/// Optional cross-page cache for <c>$ref</c> resolutions.  When supplied (keyed by <c>refId</c>, value is the
/// resolved concrete schema or <c>null</c> when unresolvable) each unique component schema is looked up in
/// <see cref="OpenApiDocument.Components"/> only once across all pages that share the cache rather than on
/// every proxy property access.
/// </param>
public class SchemaAnalyzer(
	OpenApiDocument document,
	string? currentPageType = null,
	ConcurrentDictionary<string, IOpenApiSchema?>? resolveCache = null
)
{
	// Per-unit schema resolve cache; shared (by reference) across all pages that use the same ApiRenderContext.
	// Falls back to a fresh per-instance dict when no external cache is provided.
	private readonly ConcurrentDictionary<string, IOpenApiSchema?> _cache = resolveCache ?? new();

	// Referenced allOf unions this instance is classifying; a variant that refers back to one of them stays a plain named type.
	// Each page builds its own analyzer, so the set never crosses threads.
	private readonly HashSet<string> _expandingAllOfRefs = [with(StringComparer.Ordinal)];

	/// <summary>
	/// Checks if a type should link to its container page, considering the current page.
	/// </summary>
	private bool IsLinkedType(string typeName) => SchemaHelpers.ShouldLinkToContainerPage(typeName, currentPageType);

	/// <summary>
	/// Resolves a schema reference to its concrete target, using a per-unit cache to avoid repeated
	/// <c>ResolveReference</c> calls through the OpenAPI workspace.
	/// </summary>
	/// <returns>
	/// The concrete <see cref="IOpenApiSchema"/> from <c>Components.Schemas</c> for local refs,
	/// the proxy (<see cref="OpenApiSchemaReference"/>) for external or unresolvable refs,
	/// or <paramref name="schema"/> unchanged when it is not a reference.
	/// </returns>
	public IOpenApiSchema? ResolveSchema(IOpenApiSchema? schema)
	{
		if (schema is null)
			return null;

		if (schema is not OpenApiSchemaReference schemaRef)
			return schema;

		var refId = schemaRef.Reference.Id;
		if (string.IsNullOrEmpty(refId))
			return schemaRef;

		// External $refs (e.g. ../common.yaml#/components/schemas/Error) may share the same Id with
		// a local component schema. Skip the cache for external refs but return the proxy so
		// callers can still traverse through it (Target resolves via the OpenAPI workspace).
		if (!string.IsNullOrEmpty(schemaRef.Reference.ExternalResource))
			return schemaRef;

		if (_cache.TryGetValue(refId, out var cached))
			return cached ?? schemaRef;

		IOpenApiSchema? resolved = null;
		_ = document.Components?.Schemas?.TryGetValue(refId, out resolved);
		_cache[refId] = resolved;
		return resolved ?? schemaRef;
	}

	/// <summary>
	/// Gets the properties from a schema, resolving references and handling allOf composition.
	/// </summary>
	public IDictionary<string, IOpenApiSchema>? GetSchemaProperties(IOpenApiSchema? schema)
	{
		if (schema is null)
			return null;

		// For schema references resolve directly to avoid proxy reads:
		// each proxy property access on OpenApiSchemaReference calls ResolveReference internally,
		// so reading .Properties through the proxy is equivalent to re-resolving the $ref on every call.
		if (schema is OpenApiSchemaReference schemaRef)
		{
			var resolved = ResolveSchema(schemaRef);
			// Only recurse when we have a concrete resolved schema; null means external ref or
			// unresolvable — fall through so the proxy's own property reads are used as a fallback.
			if (resolved is not null && !ReferenceEquals(resolved, schemaRef))
				return GetSchemaProperties(resolved);
		}

		// Direct properties
		if (schema.Properties is { Count: > 0 })
			return schema.Properties;

		// For allOf, collect properties from all schemas
		if (schema.AllOf is { Count: > 0 } allOf)
		{
			var props = new Dictionary<string, IOpenApiSchema>();
			foreach (var subProps in allOf.Select(GetSchemaProperties).Where(p => p is not null))
			{
				foreach (var prop in subProps!)
					_ = props.TryAdd(prop.Key, prop.Value);
			}
			return props.Count > 0 ? props : null;
		}

		return null;
	}

	/// <summary>
	/// Gets the union options from a schema's oneOf/anyOf, if present.
	/// </summary>
	public List<UnionOption> GetNestedUnionOptions(IOpenApiSchema? schema)
	{
		// Resolve references to avoid proxy reads (each proxy access calls ResolveReference internally)
		var target = ResolveSchema(schema) ?? schema;
		return UnionSchemas.TryGet(target, out _, out var members) ? BuildUnionOptions(members) : [];
	}

	/// <summary>
	/// One option per union member. A <c>$ref</c> member stays shallow (its name and id) so cyclic unions cannot recurse;
	/// <see cref="ClassifyUnion"/> classifies inline unions in full instead.
	/// </summary>
	/// <summary>
	/// A union member classified in full, as body-level and schema-page variant lists show it. The options a type keeps
	/// while it is classified (<see cref="BuildUnionOptions"/>) stay shallow for <c>$ref</c> members, so cyclic unions
	/// cannot recurse. The two disagree on a few members, such as named string aliases (<c>CatDfaColumn</c> against
	/// <c>string</c>) and free-form inline objects, so each list keeps its own.
	/// </summary>
	public UnionOption ClassifyOption(IOpenApiSchema member)
	{
		var info = GetTypeInfo(member);
		return new UnionOption(info.TypeName, info.SchemaRef, info.IsObject, member, info.IsArray);
	}

	private List<UnionOption> BuildUnionOptions(IEnumerable<IOpenApiSchema> members) =>
		LabelInlineObjects([.. FlattenInlineUnions(members).Select(BuildUnionOption).OfType<UnionOption>()]);

	/// <summary>Gives each unnamed inline object member a <see cref="UnionOption.Label"/> it can be told apart by.</summary>
	private List<UnionOption> LabelInlineObjects(List<UnionOption> options)
	{
		var inline = options.Select((o, i) => (Option: o, Index: i)).Where(x => IsInlineObject(x.Option)).ToList();
		if (inline.Count == 0)
			return options;

		var propertyNames = inline.ToDictionary(x => x.Index, x => GetSchemaProperties(x.Option.Schema)?.Keys.ToArray() ?? []);
		return [
			.. options.Select(
				(o, i) => propertyNames.TryGetValue(i, out var own)
					? o with { Label = InlineObjectLabel(o.Schema!, own, propertyNames.Where(p => p.Key != i).SelectMany(p => p.Value)) }
					: o
			)
		];
	}

	private static bool IsInlineObject(UnionOption option) =>
		option is { Ref: null or "", IsObject: true, IsArray: false, Schema: not null and not OpenApiSchemaReference };

	private string? InlineObjectLabel(IOpenApiSchema member, IReadOnlyList<string> own, IEnumerable<string> siblingNames)
	{
		if (!string.IsNullOrWhiteSpace(member.Title))
			return member.Title;

		var properties = GetSchemaProperties(member) ?? new Dictionary<string, IOpenApiSchema>();
		foreach (var (name, property) in properties)
		{
			if (GetEnumValues(property) is [var constant])
				return $"{name}: {constant}";
		}

		// The fields no sibling has tell the variant apart; required ones define it, so they come first.
		var others = siblingNames.ToHashSet(StringComparer.Ordinal);
		var distinct = own.Where(n => !others.Contains(n)).ToList();
		var required = member.Required ?? new HashSet<string>();
		var ordered = distinct.Where(required.Contains).Concat(distinct.Where(n => !required.Contains(n))).ToList();
		return ordered.Count == 0 ? null : $"{{ {string.Join(", ", ordered.Take(3))}{(ordered.Count > 3 ? ", …" : "")} }}";
	}

	/// <summary>
	/// A member that is itself an inline <c>oneOf</c>/<c>anyOf</c>, with no properties of its own, lists its members in the
	/// parent: <c>anyOf[anyOf[A, B], C]</c> offers A, B and C. Named unions stay one option, so cyclic references cannot recurse.
	/// </summary>
	private static IEnumerable<IOpenApiSchema> FlattenInlineUnions(IEnumerable<IOpenApiSchema> members) =>
		members.SelectMany(
			m => m is not OpenApiSchemaReference && m.Properties is not { Count: > 0 } && UnionSchemas.TryGet(m, out _, out var inner)
				? FlattenInlineUnions(inner)
				: [m]
		);

	private UnionOption? BuildUnionOption(IOpenApiSchema member)
	{
		if (member is OpenApiSchemaReference reference)
		{
			var name = SchemaHelpers.FormatSchemaName(reference.Reference?.Id ?? "unknown");
			return new UnionOption(name, reference.Reference?.Id, !SchemaHelpers.IsValueType(name), member);
		}

		// Literal members surface through TypeInfo.EnumValues, not as union options.
		if (member.Enum is { Count: > 0 })
			return null;

		if (member.Type?.HasFlag(JsonSchemaType.Array) == true && member.Items != null)
		{
			var item = ClassifyType(member.Items);
			return new UnionOption(item.TypeName, item.SchemaRef, item.IsObject, member, IsArray: true);
		}

		// An inline schema can still wrap a reference (allOf: [$ref]).
		var info = ClassifyType(member);
		if (!string.IsNullOrEmpty(info.SchemaRef))
			return new UnionOption(info.TypeName, info.SchemaRef, info.IsObject, member);

		// An inline object with properties of its own expands like a named one; its title, when set, names it.
		if (info is { IsObject: true, IsUnion: false, IsDictionary: false } && GetSchemaProperties(member)?.Count > 0)
			return new UnionOption(string.IsNullOrWhiteSpace(member.Title) ? info.TypeName : member.Title, null, true, member);

		var primitive = SchemaHelpers.GetPrimitiveTypeName(member.Type);
		return new UnionOption(string.IsNullOrEmpty(primitive) ? "unknown" : primitive, null, false, member);
	}

	/// <summary>
	/// Checks if a union option has properties, resolving its reference if needed.
	/// Also recursively checks nested unions.
	/// </summary>
	public bool UnionOptionHasProperties(UnionOption option)
	{
		if (option.Schema == null)
			return false;

		if (GetExpandableDictionaryValue(option.Schema) is not null)
			return true;

		// For non-object types, check if they're nested unions with object options
		if (!option.IsObject)
		{
			// Check if this is a union type that might contain objects
			var nestedOptions = GetNestedUnionOptions(option.Schema);
			return nestedOptions.Any(UnionOptionHasProperties);
		}

		// Try to get properties directly first
		var props = GetSchemaProperties(option.Schema);
		if (props?.Count > 0)
			return true;

		// For schema references, try resolving via the Ref ID or the schema reference itself
		var refId = option.Ref;
		if (string.IsNullOrEmpty(refId) && option.Schema is OpenApiSchemaReference schemaRef)
			refId = schemaRef.Reference.Id;

		if (!string.IsNullOrEmpty(refId) && document.Components?.Schemas?.TryGetValue(refId, out var resolvedSchema) == true)
		{
			props = GetSchemaProperties(resolvedSchema);
			if (props?.Count > 0)
				return true;

			// Check if the resolved schema is itself a union
			// Try the original schema reference first (OpenApiSchemaReference proxies OneOf/AnyOf)
			var nestedOptions = GetNestedUnionOptions(option.Schema);
			if (nestedOptions.Count == 0)
			{
				// Fallback to resolved schema
				nestedOptions = GetNestedUnionOptions(resolvedSchema);
			}
			if (nestedOptions.Any(UnionOptionHasProperties))
				return true;
		}

		return false;
	}

	/// <summary>
	/// The value schema of a map (<c>additionalProperties</c>) when it has properties worth listing and no page of its own.
	/// A union member that is such a map shows its value properties under a <c>&lt;string&gt;</c> key row.
	/// </summary>
	public IOpenApiSchema? GetExpandableDictionaryValue(IOpenApiSchema? schema)
	{
		if (schema is null)
			return null;

		var info = GetTypeInfo(schema);
		return info is { IsDictionary: true, HasLink: false, DictValueSchema: { } value } && GetSchemaProperties(value)?.Count > 0
			? value
			: null;
	}

	/// <summary>
	/// Gets comprehensive type information for a schema.
	/// </summary>
	public TypeInfo GetTypeInfo(IOpenApiSchema? schema)
	{
		var info = ClassifyType(schema);
		var enumValues = GetEnumValues(schema);
		return enumValues.Count > 0 ? info with { EnumValues = [.. enumValues] } : info;
	}

	/// <summary>
	/// Every enum literal a value of <paramref name="schema"/> can take, looking through <c>$ref</c>,
	/// <c>allOf</c>/<c>oneOf</c>/<c>anyOf</c> members and array <c>items</c>. The single source of enum values for all views.
	/// </summary>
	public IReadOnlyList<string> GetEnumValues(IOpenApiSchema? schema)
	{
		var values = new List<string>();
		CollectEnumValues(schema, values, [with(ReferenceEqualityComparer.Instance)]);
		return values.Distinct(StringComparer.Ordinal).ToArray();
	}

	private void CollectEnumValues(IOpenApiSchema? schema, List<string> values, HashSet<IOpenApiSchema> visited)
	{
		var resolved = ResolveSchema(schema);
		if (resolved is null || !visited.Add(resolved))
			return;

		if (resolved.Enum is { Count: > 0 } literals)
		{
			values.AddRange(literals.Select(e => e?.ToString().Trim('"')).Where(e => !string.IsNullOrEmpty(e))!);
			return;
		}

		if (resolved.Type?.HasFlag(JsonSchemaType.Array) == true)
			CollectEnumValues(resolved.Items, values, visited);

		foreach (var member in (resolved.AllOf ?? []).Concat(resolved.OneOf ?? []).Concat(resolved.AnyOf ?? []))
			CollectEnumValues(member, values, visited);
	}

	private TypeInfo ClassifyType(IOpenApiSchema? schema)
	{
		if (schema is null)
			return new TypeInfo { TypeName = "unknown" };
		if (schema is OpenApiSchemaReference { Reference.Id: { Length: > 0 } refId } reference)
			return ClassifyReference(reference, refId);
		if (UnionSchemas.TryGet(schema, out var keyword, out var members))
			return ClassifyUnion(members, keyword);
		if (schema.AllOf is { Count: > 0 } allOf && ClassifyComposition(allOf) is { } composed)
			return composed;
		if (schema.Type?.HasFlag(JsonSchemaType.Array) ?? false)
			return ClassifyArray(schema);
		return ClassifyScalar(schema);
	}

	private TypeInfo ClassifyReference(OpenApiSchemaReference reference, string refId)
	{
		// Resolve the $ref once. Reading any property through OpenApiSchemaReference calls
		// ResolveReference internally on every access, so we resolve here and read structural
		// fields (Type, Enum, OneOf, AnyOf, Items, Properties) off the concrete schema.
		// Description/Title/ReadOnly/WriteOnly intentionally stay on the proxy because
		// OpenAPI 3.1 allows sibling keywords alongside $ref to override those fields.
		var resolvedTarget = ResolveSchema(reference) ?? reference;
		var isArray = resolvedTarget.Type?.HasFlag(JsonSchemaType.Array) ?? false;
		var named = ClassifyNamedSchema(SchemaHelpers.FormatSchemaName(refId), resolvedTarget);
		var isEnum = resolvedTarget.Enum is { Count: > 0 };
		var (unionKeyword, unionOptions) = isEnum ? (null, null) : ReferencedUnion(refId, resolvedTarget);

		return new TypeInfo
		{
			TypeName = named.TypeName,
			SchemaRef = named.IsPrimitiveAlias ? null : refId,
			IsArray = isArray,
			IsObject = !named.IsValueType && !isEnum && !named.IsPrimitiveAlias,
			IsValueType = named.IsValueType,
			ValueTypeBase = named.ValueTypeBase,
			HasLink = IsLinkedType(named.TypeName),
			UnionOptions = unionOptions,
			IsEnum = isEnum,
			UnionKeyword = unionKeyword,
			ArrayItemType = isArray ? PrimitiveItemType(resolvedTarget.Items) : null
		};
	}

	/// <summary>The item type of a referenced array, when the items are a plain primitive rather than a linked or named type.</summary>
	private string? PrimitiveItemType(IOpenApiSchema? itemSchema)
	{
		if (itemSchema is null)
			return null;
		var itemInfo = ClassifyType(itemSchema);
		return itemInfo is { IsObject: false, HasLink: false } && string.IsNullOrEmpty(itemInfo.SchemaRef) ? itemInfo.TypeName : null;
	}

	/// <summary>The union a referenced schema declares, directly or as an <c>allOf</c> around a union member.</summary>
	private (UnionKeyword? Keyword, List<UnionOption>? Options) ReferencedUnion(string refId, IOpenApiSchema resolvedTarget)
	{
		if (UnionSchemas.TryGet(resolvedTarget, out var keyword, out var members))
		{
			var built = BuildUnionOptions(members);
			return (keyword, built.Count > 0 ? built : null);
		}

		return ClassifyReferencedAllOfUnion(refId, resolvedTarget) is { } allOfUnion
			? (allOfUnion.UnionKeyword, allOfUnion.UnionOptions)
			: (null, null);
	}

	/// <summary>
	/// An <c>allOf</c> is a union when it wraps one, otherwise it takes the name of its first <c>$ref</c>. Null when neither
	/// applies, so classification goes on with the schema's other keywords.
	/// </summary>
	private TypeInfo? ClassifyComposition(IList<IOpenApiSchema> allOf)
	{
		if (AllOfUnion.TrySplit(allOf, this, out var allOfUnion))
			return ClassifyAllOfUnion(allOfUnion);

		var refSchemas = allOf.OfType<OpenApiSchemaReference>().ToArray();
		if (refSchemas.Length == 0 || refSchemas[0].Reference.Id is not { Length: > 0 } refId)
			return null;

		var resolvedTarget = ResolveSchema(refSchemas[0]) ?? refSchemas[0];
		var named = ClassifyNamedSchema(SchemaHelpers.FormatSchemaName(refId), resolvedTarget);

		// allOf wrapping a single enum $ref is how OpenAPI 3.1 attaches a description to a $ref.
		if (resolvedTarget.Enum is { Count: > 0 })
			return new TypeInfo { TypeName = named.TypeName, SchemaRef = named.IsPrimitiveAlias ? null : refId, IsEnum = true };

		return new TypeInfo
		{
			TypeName = named.TypeName,
			SchemaRef = named.IsPrimitiveAlias ? null : refId,
			IsObject = !named.IsValueType && !named.IsPrimitiveAlias,
			IsValueType = named.IsValueType,
			ValueTypeBase = named.ValueTypeBase,
			HasLink = IsLinkedType(named.TypeName),
			AlsoIncludes = GetComposedTypes(refSchemas)
		};
	}

	private TypeInfo ClassifyArray(IOpenApiSchema schema)
	{
		if (schema.Items is null)
			return new TypeInfo { TypeName = "unknown", IsArray = true, ArrayItemType = "unknown" };

		var itemInfo = ClassifyType(schema.Items);
		// If the item is not an object and not a linked type, it's a primitive array
		var isPrimitiveArray = itemInfo is not { IsObject: false, HasLink: false } || !string.IsNullOrEmpty(itemInfo.SchemaRef);
		return new TypeInfo
		{
			TypeName = itemInfo.TypeName,
			SchemaRef = itemInfo.SchemaRef,
			IsArray = true,
			IsObject = itemInfo.IsObject,
			IsValueType = itemInfo.IsValueType,
			ValueTypeBase = itemInfo.ValueTypeBase,
			HasLink = itemInfo.HasLink,
			IsEnum = itemInfo.IsEnum,
			ArrayItemType = isPrimitiveArray ? itemInfo.TypeName : null,
			// An array of a union offers the union's variants for each item.
			UnionOptions = itemInfo.UnionOptions,
			UnionKeyword = itemInfo.UnionKeyword
		};
	}

	/// <summary>An inline enum, map, object or primitive.</summary>
	private TypeInfo ClassifyScalar(IOpenApiSchema schema)
	{
		if (schema.Enum is { Count: > 0 })
			return new TypeInfo { TypeName = "enum", IsEnum = true };

		// Check for additionalProperties (dictionary-like objects)
		if (schema.AdditionalProperties is { } addProps)
		{
			var valueInfo = ClassifyType(addProps);
			// Pass valueInfo.HasLink so we know if the dictionary value type has a dedicated page
			return new TypeInfo
			{
				TypeName = $"string to {valueInfo.TypeName}",
				SchemaRef = valueInfo.SchemaRef,
				IsObject = true,
				HasLink = valueInfo.HasLink,
				IsDictionary = true,
				DictValueSchema = addProps
			};
		}

		if (schema.Properties is { Count: > 0 })
			return new TypeInfo { TypeName = "object", IsObject = true };

		var primitiveName = SchemaHelpers.GetPrimitiveTypeName(schema.Type);
		if (!string.IsNullOrEmpty(primitiveName))
			return new TypeInfo { TypeName = primitiveName, IsObject = primitiveName == "object" };

		return new TypeInfo { TypeName = "object", IsObject = true };
	}

	private TypeInfo ClassifyUnion(IList<IOpenApiSchema> members, UnionKeyword keyword)
	{
		var classified = FlattenInlineUnions(members).Select(s => (Schema: s, Info: ClassifyType(s))).ToArray();

		// A union of inline literal sets is just a bigger enum.
		if (classified.All(m => m.Info is { IsEnum: true, IsArray: false, SchemaRef: null }))
			return new TypeInfo { TypeName = "enum", IsEnum = true };

		var options = LabelInlineObjects([
			.. classified.Select(m => new UnionOption(m.Info.TypeName, m.Info.SchemaRef, m.Info.IsObject, m.Schema, m.Info.IsArray))
		]);

		// Multiple object options render as tabs.
		if (options.Count > 1 && options.Any(o => o.IsObject))
			return new TypeInfo { TypeName = keyword.ToSchemaKeyword(), IsObject = true, UnionOptions = options, UnionKeyword = keyword };

		return new TypeInfo
		{
			TypeName = string.Join(" | ", options.Select(o => o.Name).Distinct()),
			UnionOptions = options,
			UnionKeyword = keyword
		};
	}

	/// <summary>The object schemas after the first <c>$ref</c> of an <c>allOf</c>; the first one names the type.</summary>
	private List<ComposedType>? GetComposedTypes(OpenApiSchemaReference[] refSchemas)
	{
		var composed = new List<ComposedType>();
		var primaryName = SchemaHelpers.FormatSchemaName(refSchemas[0].Reference.Id ?? "");
		foreach (var reference in refSchemas.Skip(1))
		{
			var name = SchemaHelpers.FormatSchemaName(reference.Reference.Id ?? "");
			var target = ResolveSchema(reference) ?? reference;
			if (
				string.IsNullOrEmpty(name)
				|| name == primaryName
				|| target.Enum is { Count: > 0 }
				|| ClassifyNamedSchema(name, target).IsPrimitiveAlias
			)
				continue;
			if (composed.All(c => c.Name != name))
				composed.Add(new ComposedType(name, IsLinkedType(name)));
		}
		return composed.Count > 0 ? composed : null;
	}

	/// <summary>The discriminator of a union: its own, or the one declared on the <c>oneOf</c>/<c>anyOf</c> member of an <c>allOf</c>.</summary>
	public OpenApiDiscriminator? GetUnionDiscriminator(IOpenApiSchema? schema)
	{
		var resolved = ResolveSchema(schema) ?? schema;
		if (resolved?.Discriminator is { } own)
			return own;

		return (resolved?.AllOf ?? [])
			.Select(m => ResolveSchema(m) ?? m)
			.Where(m => m.Discriminator is not null && UnionSchemas.IsUnion(m))
			.Select(static m => m.Discriminator)
			.FirstOrDefault();
	}

	/// <summary>
	/// A named schema that is an <c>allOf</c> with a <c>oneOf</c>/<c>anyOf</c> member is a union too, so a <c>$ref</c> to it
	/// expands its variants. The guard stops a variant that refers back to its parent from recursing forever.
	/// </summary>
	/// <remarks>
	/// Inside A's expansion, a variant B that refers back to A sees A as a plain named type, while B classified on its own
	/// sees A as a union. That difference stays inside the nested classification: a union option keeps only its name,
	/// <c>$ref</c> and object flag, which come out the same in any order, and the guard is empty again once a
	/// classification returns.
	/// </remarks>
	private TypeInfo? ClassifyReferencedAllOfUnion(string refId, IOpenApiSchema target)
	{
		if (target.AllOf is not { Count: > 0 } allOf || !AllOfUnion.TrySplit(allOf, this, out var split) || !_expandingAllOfRefs.Add(refId))
			return null;

		try
		{
			var union = ClassifyAllOfUnion(split);
			return union.IsUnion ? union : null;
		}
		finally
		{
			_ = _expandingAllOfRefs.Remove(refId);
		}
	}

	/// <summary>Each object variant carries the shared base members, so it expands to base plus its own properties.</summary>
	private TypeInfo ClassifyAllOfUnion(AllOfUnion split)
	{
		var union = ClassifyUnion(split.Variants, split.Keyword);
		if (union.UnionOptions is null)
			return union;

		var options = union
			.UnionOptions
			.Select(o => o is { IsObject: true, Schema: not null } && !o.IsArray ? o with { Schema = split.MergeInto(o.Schema, this) } : o)
			.ToList();
		return union with { UnionOptions = options };
	}

	/// <summary>
	/// Known domain aliases (<c>Field</c>, <c>Id</c>) keep their name. Codegen wrappers that are
	/// only a primitive (<c>Security_Lists_API_ListDescription</c>) collapse to that primitive.
	/// </summary>
	private static NamedSchemaKind ClassifyNamedSchema(string typeName, IOpenApiSchema schema)
	{
		if (SchemaHelpers.IsValueType(typeName))
			return new NamedSchemaKind(typeName, true, SchemaHelpers.GetValueTypeBase(schema) ?? "string", false);

		var primitiveAlias = SchemaHelpers.GetPrimitiveAliasType(schema);
		if (!string.IsNullOrEmpty(primitiveAlias))
			return new NamedSchemaKind(primitiveAlias, false, null, true);

		return new NamedSchemaKind(typeName, false, null, false);
	}

	private readonly record struct NamedSchemaKind(string TypeName, bool IsValueType, string? ValueTypeBase, bool IsPrimitiveAlias);
}
