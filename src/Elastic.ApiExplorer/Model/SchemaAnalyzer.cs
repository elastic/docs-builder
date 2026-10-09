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
/// <param name="currentPageSchemaId">The schema id of the type page being built, which never links to itself.</param>
/// <param name="resolveCache">
/// Optional cross-page cache for <c>$ref</c> resolutions.  When supplied (keyed by <c>refId</c>, value is the
/// resolved concrete schema or <c>null</c> when unresolvable) each unique component schema is looked up in
/// <see cref="OpenApiDocument.Components"/> only once across all pages that share the cache rather than on
/// every proxy property access.
/// </param>
public class SchemaAnalyzer(
	OpenApiDocument document,
	string? currentPageSchemaId = null,
	ConcurrentDictionary<string, IOpenApiSchema?>? resolveCache = null
)
{
	// Per-unit schema resolve cache; shared (by reference) across all pages that use the same ApiRenderContext.
	// Falls back to a fresh per-instance dict when no external cache is provided.
	private readonly ConcurrentDictionary<string, IOpenApiSchema?> _cache = resolveCache ?? new();

	// Referenced allOf unions this instance is classifying; a variant that refers back to one of them stays a plain named type.
	// Each page builds its own analyzer, so the set never crosses threads.
	private readonly HashSet<string> _expandingAllOfRefs = [with(StringComparer.Ordinal)];

	// Each schema's allOf folds once per page.
	private readonly Dictionary<IOpenApiSchema, EffectiveSchema> _flattened = [with(ReferenceEqualityComparer.Instance)];

	/// <summary>
	/// Checks if a type should link to its container page, considering the current page.
	/// </summary>
	/// <summary>The schema id when it has a page of its own, other than the page being built.</summary>
	private string? LinkedSchemaId(string? schemaId) => TypePages.Links(schemaId, currentPageSchemaId) ? schemaId : null;

	/// <summary>
	/// Whether a schema declares properties itself rather than only through an <c>allOf</c>. A union that does lists them
	/// above its variants; one whose properties come only from an <c>allOf</c> lists just its variants, which carry them.
	/// </summary>
	public bool DeclaresProperties(IOpenApiSchema schema) => (ResolveSchema(schema) ?? schema).Properties is { Count: > 0 };

	/// <summary>
	/// The properties a union lists above its variants. An <c>allOf</c> union already merges its other <c>allOf</c> members
	/// into every variant, so it lists only what it declares itself; a direct <c>oneOf</c>/<c>anyOf</c> lists everything.
	/// </summary>
	public IOpenApiSchema SharedProperties(IOpenApiSchema union)
	{
		var resolved = ResolveSchema(union) ?? union;
		if (UnionSchemas.IsUnion(resolved) || resolved.AllOf is not { Count: > 0 })
			return union;

		return new OpenApiSchema
		{
			Type = JsonSchemaType.Object,
			Properties = resolved.Properties,
			Required = new HashSet<string>(Flatten(union).Required)
		};
	}

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

	/// <summary>The properties of a schema and its <c>allOf</c> members; null when there are none.</summary>
	public IDictionary<string, IOpenApiSchema>? GetSchemaProperties(IOpenApiSchema? schema) =>
		Flatten(schema).Properties is { Count: > 0 } properties ? properties : null;

	/// <summary>The schema's <c>allOf</c> members folded into one view; see <see cref="EffectiveSchema"/>.</summary>
	public EffectiveSchema Flatten(IOpenApiSchema? schema)
	{
		var resolved = ResolveSchema(schema);
		if (resolved is null)
			return EffectiveSchema.Empty;
		if (_flattened.TryGetValue(resolved, out var cached))
			return cached;

		var flattened = Flatten(resolved, [with(ReferenceEqualityComparer.Instance)]);
		_flattened[resolved] = flattened;
		return flattened;
	}

	private EffectiveSchema Flatten(IOpenApiSchema resolved, HashSet<IOpenApiSchema> visited)
	{
		if (!visited.Add(resolved))
			return EffectiveSchema.Empty;
		if (resolved.AllOf is not { Count: > 0 } allOf)
		{
			return new EffectiveSchema(
				resolved.Properties ?? new Dictionary<string, IOpenApiSchema>(),
				resolved.Required ?? new HashSet<string>(),
				resolved.AdditionalProperties,
				resolved.Discriminator
			);
		}

		var properties = new Dictionary<string, IOpenApiSchema>(resolved.Properties ?? new Dictionary<string, IOpenApiSchema>());
		var required = new HashSet<string>(resolved.Required ?? new HashSet<string>());
		var mapValue = resolved.AdditionalProperties;
		var discriminator = resolved.Discriminator;
		foreach (var member in allOf.Select(m => ResolveSchema(m) ?? m))
		{
			var folded = Flatten(member, visited);
			foreach (var (name, property) in folded.Properties)
				_ = properties.TryAdd(name, property);
			required.UnionWith(folded.Required);
			mapValue ??= folded.MapValue;
			// Only a union member's discriminator selects between variants; a plain base's describes the base's own subtypes.
			discriminator ??= UnionSchemas.IsUnion(member) ? member.Discriminator : null;
		}

		return new EffectiveSchema(properties, required, mapValue, discriminator);
	}

	/// <summary>
	/// Gets the union options from a schema's oneOf/anyOf, if present.
	/// </summary>
	public List<UnionOption> GetNestedUnionOptions(IOpenApiSchema? schema)
	{
		// Resolve references to avoid proxy reads (each proxy access calls ResolveReference internally)
		var target = ResolveSchema(schema) ?? schema;
		return UnionSchemas.TryGet(target, out _, out var members) ? GetUnionOptions(members) : [];
	}

	/// <summary>
	/// The options of a union, as every option list (badges, variant tabs, body and schema-page variants) shows them.
	/// Literal members surface through <see cref="TypeInfo.EnumValues"/>, not as options.
	/// </summary>
	public List<UnionOption> GetUnionOptions(IEnumerable<IOpenApiSchema> members) =>
		LabelInlineObjects([
			.. FlattenInlineUnions(members)
				.Where(static m => m is OpenApiSchemaReference || m.Enum is not { Count: > 0 })
				.Select(ClassifyOption)
		]);

	/// <summary>
	/// One union member, classified like any other type, except that a <c>$ref</c> (or an array of one) keeps its schema's
	/// name: an option list reads <c>NodeId | NodeId[]</c>, not <c>string | string[]</c>. A referenced member is not
	/// classified further, so a union that refers back to itself cannot recurse.
	/// </summary>
	private UnionOption ClassifyOption(IOpenApiSchema member)
	{
		if (member is OpenApiSchemaReference { Reference.Id.Length: > 0 } reference)
			return ReferencedOption(reference, member, isArray: false);
		if (member.Type?.HasFlag(JsonSchemaType.Array) == true && member.Items is OpenApiSchemaReference { Reference.Id.Length: > 0 } items)
			return ReferencedOption(items, member, isArray: true);

		var info = ClassifyType(member);
		var titled = info is { IsObject: true, IsArray: false, SchemaRef: null or "" } && !string.IsNullOrWhiteSpace(member.Title);
		return new UnionOption(titled ? member.Title! : info.TypeName, info.SchemaRef, info.IsObject, member, info.IsArray);
	}

	/// <summary>An option named after the schema a <c>$ref</c> points to; aliases of a primitive or an enum are not objects.</summary>
	private UnionOption ReferencedOption(OpenApiSchemaReference reference, IOpenApiSchema member, bool isArray)
	{
		var refId = reference.Reference.Id!;
		var name = SchemaHelpers.FormatSchemaName(refId);
		var target = ResolveSchema(reference) ?? reference;
		var named = ClassifyNamedSchema(name, target);
		var isObject = !named.IsValueType && !named.IsPrimitiveAlias && target.Enum is not { Count: > 0 };
		return PrimitiveSpelling(name, named, target) is { } spelled
			? new UnionOption(spelled.Name, refId, false, member, isArray || spelled.IsArray)
			: new UnionOption(name, refId, isObject, member, isArray);
	}

	/// <summary>
	/// The primitive types a referenced schema stands for, when they read better than its name: a schema of several
	/// primitive types (<c>Stringifieddouble</c> is <c>number | string</c>), or a codegen name with no readable part
	/// (<c>Cases_string</c>, <c>Cases_string_array</c>). Null when the name should show.
	/// </summary>
	private (string Name, bool IsArray)? PrimitiveSpelling(string name, NamedSchemaKind named, IOpenApiSchema target)
	{
		if (named.IsValueType)
			return null;
		if (MultiPrimitiveType(target) is { } types)
			return (types, false);
		if (!SchemaHelpers.IsInternalSchemaName(SchemaHelpers.ReadableSchemaName(name)))
			return null;
		if (named.IsPrimitiveAlias)
			return (named.TypeName, false);
		return target.Type?.HasFlag(JsonSchemaType.Array) == true && PrimitiveAliasItem(target.Items) is { } item ? (item, true) : null;
	}

	private static string? MultiPrimitiveType(IOpenApiSchema target)
	{
		if (
			target.Type is not { } type
			|| type.HasFlag(JsonSchemaType.Object)
			|| type.HasFlag(JsonSchemaType.Array)
			|| UnionSchemas.IsUnion(target)
		)
			return null;
		var names = SchemaHelpers.GetPrimitiveTypeName(type);
		return names.Contains(" | ", StringComparison.Ordinal) ? names : null;
	}

	/// <summary>
	/// The primitive an array's items are: inline, or a <c>$ref</c> to an alias of one. Nothing is classified, so a
	/// referenced item cannot lead back to the union the option belongs to.
	/// </summary>
	private string? PrimitiveAliasItem(IOpenApiSchema? items)
	{
		if (items is OpenApiSchemaReference { Reference.Id: { } itemId } itemRef)
		{
			var alias = ClassifyNamedSchema(SchemaHelpers.FormatSchemaName(itemId), ResolveSchema(itemRef) ?? itemRef);
			return alias.IsPrimitiveAlias ? alias.TypeName : null;
		}
		var primitive = SchemaHelpers.GetPrimitiveTypeName(items?.Type);
		return string.IsNullOrEmpty(primitive) || primitive == "object" ? null : primitive;
	}

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

	/// <summary>
	/// Whether a union option lists anything when expanded: properties of its own (an array option's come from its items),
	/// a map value with properties, or a nested union option that does.
	/// </summary>
	public bool UnionOptionHasProperties(UnionOption option) =>
		UnionOptionHasProperties(option, [with(ReferenceEqualityComparer.Instance)]);

	private bool UnionOptionHasProperties(UnionOption option, HashSet<IOpenApiSchema> visited)
	{
		var target = ResolveSchema(option is { IsArray: true, Schema.Items: { } items } ? items : option.Schema);
		// A union that refers back to itself through an option has nothing new to list the second time.
		if (target is null || !visited.Add(target))
			return false;

		if (GetExpandableDictionaryValue(option.Schema) is not null)
			return true;
		if (option.IsObject && GetSchemaProperties(target) is not null)
			return true;

		return GetNestedUnionOptions(target).Any(nested => UnionOptionHasProperties(nested, visited));
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
	/// Every enum literal a value of <paramref name="schema"/> can take, looking through <c>$ref</c>, <c>allOf</c> members,
	/// union members and array <c>items</c>. The single source of enum values for all views.
	/// </summary>
	/// <remarks>
	/// Union members come from <see cref="UnionSchemas.TryGet"/>, so the walk agrees with every other view on what a union
	/// is. <c>allOf</c> members are walked here rather than through <see cref="Flatten"/>, which folds fields, not values.
	/// </remarks>
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

		var unionMembers = UnionSchemas.TryGet(resolved, out _, out var members) ? members : [];
		foreach (var member in (resolved.AllOf ?? []).Concat(unionMembers))
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
			LinkedSchemaId = LinkedSchemaId(refId),
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
			var built = GetUnionOptions(members);
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
			LinkedSchemaId = LinkedSchemaId(refId),
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
			LinkedSchemaId = itemInfo.LinkedSchemaId,
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
				LinkedSchemaId = valueInfo.LinkedSchemaId,
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
		var flattened = FlattenInlineUnions(members).ToArray();

		// A union of inline literal sets is just a bigger enum.
		if (flattened.All(m => m is not OpenApiSchemaReference && ClassifyType(m) is { IsEnum: true, IsArray: false, SchemaRef: null }))
			return new TypeInfo { TypeName = "enum", IsEnum = true };

		// The type name counts every member; the options, as everywhere, leave the literals to EnumValues.
		var classified = flattened.Select(ClassifyOption).ToArray();
		var options = GetUnionOptions(flattened);

		// Multiple object options render as tabs.
		if (classified.Length > 1 && classified.Any(o => o.IsObject))
			return new TypeInfo { TypeName = keyword.ToSchemaKeyword(), IsObject = true, UnionOptions = options, UnionKeyword = keyword };

		return new TypeInfo
		{
			TypeName = string.Join(" | ", classified.Select(o => SchemaHelpers.ReadableSchemaName(o.Name)).Distinct()),
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
				composed.Add(new ComposedType(name, LinkedSchemaId(reference.Reference.Id)));
		}
		return composed.Count > 0 ? composed : null;
	}

	/// <summary>The discriminator of a union; see <see cref="EffectiveSchema.Discriminator"/>.</summary>
	public OpenApiDiscriminator? GetUnionDiscriminator(IOpenApiSchema? schema) => Flatten(schema).Discriminator;

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
