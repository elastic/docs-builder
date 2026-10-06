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
	private List<UnionOption> BuildUnionOptions(IEnumerable<IOpenApiSchema> members) =>
		[.. members.Select(BuildUnionOption).OfType<UnionOption>()];

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

		// Check if this is a schema reference
		if (schema is OpenApiSchemaReference schemaRef)
		{
			var refId = schemaRef.Reference.Id;
			if (!string.IsNullOrEmpty(refId))
			{
				var typeName = SchemaHelpers.FormatSchemaName(refId);

				// Resolve the $ref once. Reading any property through OpenApiSchemaReference calls
				// ResolveReference internally on every access, so we resolve here and read structural
				// fields (Type, Enum, OneOf, AnyOf, Items, Properties) off the concrete schema.
				// Description/Title/ReadOnly/WriteOnly intentionally stay on the proxy because
				// OpenAPI 3.1 allows sibling keywords alongside $ref to override those fields.
				var resolvedTarget = ResolveSchema(schemaRef) ?? schemaRef;
				var isArray = resolvedTarget.Type?.HasFlag(JsonSchemaType.Array) ?? false;
				var named = ClassifyNamedSchema(typeName, resolvedTarget);
				typeName = named.TypeName;
				var isValueType = named.IsValueType;
				var valueTypeBase = named.ValueTypeBase;
				var schemaRefId = named.IsPrimitiveAlias ? null : refId;
				var hasLink = IsLinkedType(typeName);

				// Check if the schema reference is an enum or union — read from the resolved target
				var isEnum = resolvedTarget.Enum is { Count: > 0 };

				// Check if the referenced type is an array of primitives
				string? arrayItemType = null;
				if (isArray)
				{
					var itemSchema = resolvedTarget.Items;
					if (itemSchema is not null)
					{
						var itemInfo = ClassifyType(itemSchema);
						// If the item is not an object, not a linked type, and has no schema reference, it's a primitive array
						if (itemInfo is { IsObject: false, HasLink: false } && string.IsNullOrEmpty(itemInfo.SchemaRef))
							arrayItemType = itemInfo.TypeName;
					}
				}

				UnionKeyword? unionKeyword = null;
				List<UnionOption>? unionOptions = null;
				if (!isEnum && UnionSchemas.TryGet(resolvedTarget, out var keyword, out var members))
				{
					unionKeyword = keyword;
					var built = BuildUnionOptions(members);
					unionOptions = built.Count > 0 ? built : null;
				}

				return new TypeInfo
				{
					TypeName = typeName,
					SchemaRef = schemaRefId,
					IsArray = isArray,
					IsObject = !isValueType && !isEnum && !named.IsPrimitiveAlias,
					IsValueType = isValueType,
					ValueTypeBase = valueTypeBase,
					HasLink = hasLink,
					UnionOptions = unionOptions,
					IsEnum = isEnum,
					UnionKeyword = unionKeyword,
					ArrayItemType = arrayItemType
				};
			}
		}

		// Check for oneOf/anyOf which often indicate union types
		if (UnionSchemas.TryGet(schema, out var inlineKeyword, out var inlineMembers))
			return ClassifyUnion(inlineMembers, inlineKeyword);

		// Check for allOf (usually inheritance/composition)
		if (schema.AllOf is { Count: > 0 } allOf)
		{
			if (TrySplitAllOfUnion(allOf, out var allOfUnion))
				return ClassifyAllOfUnion(allOfUnion);

			var refSchemas = allOf.OfType<OpenApiSchemaReference>().ToArray();
			if (refSchemas.Length > 0)
			{
				var refId = refSchemas[0].Reference.Id;
				if (!string.IsNullOrEmpty(refId))
				{
					var typeName = SchemaHelpers.FormatSchemaName(refId);
					var resolvedTarget = ResolveSchema(refSchemas[0]) ?? refSchemas[0];
					var named = ClassifyNamedSchema(typeName, resolvedTarget);

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
			}
		}

		// Check for array items
		if (schema.Type?.HasFlag(JsonSchemaType.Array) ?? false)
		{
			if (schema.Items is not null)
			{
				var itemInfo = ClassifyType(schema.Items);
				// If the item is not an object and not a linked type, it's a primitive array
				var isPrimitiveArray = itemInfo is not { IsObject: false, HasLink: false } || !string.IsNullOrEmpty(itemInfo.SchemaRef);
				var arrayItemType = isPrimitiveArray ? itemInfo.TypeName : null;
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
					ArrayItemType = arrayItemType
				};
			}
			return new TypeInfo { TypeName = "unknown", IsArray = true, ArrayItemType = "unknown" };
		}

		// Check for enum
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

		// Check if it has properties (inline object)
		if (schema.Properties is { Count: > 0 })
			return new TypeInfo { TypeName = "object", IsObject = true };

		// Primitive type
		var primitiveName = SchemaHelpers.GetPrimitiveTypeName(schema.Type);
		if (!string.IsNullOrEmpty(primitiveName))
			return new TypeInfo { TypeName = primitiveName, IsObject = primitiveName == "object" };

		return new TypeInfo { TypeName = "object", IsObject = true };
	}

	private TypeInfo ClassifyUnion(IList<IOpenApiSchema> members, UnionKeyword keyword)
	{
		var classified = members.Select(s => (Schema: s, Info: ClassifyType(s))).ToArray();

		// A union of inline literal sets is just a bigger enum.
		if (classified.All(m => m.Info is { IsEnum: true, IsArray: false, SchemaRef: null }))
			return new TypeInfo { TypeName = "enum", IsEnum = true };

		var options = classified.Select(
			m => new UnionOption(m.Info.TypeName, m.Info.SchemaRef, m.Info.IsObject, m.Schema, m.Info.IsArray)
		).ToList();

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

	/// <summary>
	/// <c>allOf: [Base, { oneOf: [A, B] }]</c> means "Base plus one of A or B". The base members only count when
	/// they contribute properties; an <c>allOf</c> that merely wraps a union <c>$ref</c> stays a plain named type.
	/// </summary>
	private bool TrySplitAllOfUnion(IList<IOpenApiSchema> allOf, out AllOfUnion split)
	{
		split = default;
		foreach (var member in allOf)
		{
			var resolved = ResolveSchema(member);
			if (resolved is null || resolved.Enum is { Count: > 0 })
				continue;

			if (!UnionSchemas.TryGet(resolved, out var keyword, out var variants))
				continue;

			var bases = allOf.Where(m => !ReferenceEquals(m, member)).ToArray();
			if (!bases.Any(b => GetSchemaProperties(b)?.Count > 0))
				continue;

			split = new AllOfUnion(bases, variants, keyword);
			return true;
		}

		return false;
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

	/// <summary>Each object variant carries the shared base members, so it expands to base plus its own properties.</summary>
	private TypeInfo ClassifyAllOfUnion(AllOfUnion split)
	{
		var union = ClassifyUnion(split.Variants, split.Keyword);
		if (union.UnionOptions is null)
			return union;

		var options = union
			.UnionOptions
			.Select(
				o => o is { IsObject: true, Schema: not null } && !o.IsArray ? o with { Schema = MergeBasesInto(split.Bases, o.Schema) } : o
			)
			.ToList();
		return union with { UnionOptions = options };
	}

	/// <summary>A synthetic <c>allOf</c> has no <c>required</c> list of its own, so it collects the lists of every member.</summary>
	/// <summary>
	/// Folds the shared base members and one variant into a single object schema. The variant's own properties win over a base
	/// property of the same name, and the <c>required</c> lists of every member carry over.
	/// </summary>
	private OpenApiSchema MergeBasesInto(IReadOnlyList<IOpenApiSchema> bases, IOpenApiSchema variant)
	{
		var properties = new Dictionary<string, IOpenApiSchema>();
		foreach (var member in bases)
		{
			foreach (var (name, property) in GetSchemaProperties(member) ?? new Dictionary<string, IOpenApiSchema>())
				_ = properties.TryAdd(name, property);
		}

		foreach (var (name, property) in GetSchemaProperties(variant) ?? new Dictionary<string, IOpenApiSchema>())
			properties[name] = property;

		var required = new HashSet<string>();
		CollectRequired([.. bases, variant], required, [with(ReferenceEqualityComparer.Instance)]);
		return new OpenApiSchema
		{
			Type = JsonSchemaType.Object,
			Properties = properties,
			Required = required,
			Description = variant.Description
		};
	}

	private void CollectRequired(IEnumerable<IOpenApiSchema> schemas, HashSet<string> required, HashSet<IOpenApiSchema> visited)
	{
		foreach (var schema in schemas)
		{
			var resolved = ResolveSchema(schema) ?? schema;
			if (!visited.Add(resolved))
				continue;

			if (resolved.Required is { } declared)
				required.UnionWith(declared);
			CollectRequired(resolved.AllOf ?? [], required, visited);
		}
	}

	private readonly record struct AllOfUnion(IReadOnlyList<IOpenApiSchema> Bases, IList<IOpenApiSchema> Variants, UnionKeyword Keyword);

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
