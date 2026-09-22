// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

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
	Dictionary<string, IOpenApiSchema?>? resolveCache = null
)
{
	// Per-unit schema resolve cache; shared (by reference) across all pages that use the same ApiRenderContext.
	// Falls back to a fresh per-instance dict when no external cache is provided.
	private readonly Dictionary<string, IOpenApiSchema?> _cache = resolveCache ?? [];

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
		var result = new List<UnionOption>();
		if (schema == null)
			return result;

		IList<IOpenApiSchema>? unionSchemas = null;

		// Resolve references to avoid proxy reads (each proxy access calls ResolveReference internally)
		var target = ResolveSchema(schema) ?? schema;
		if (target.OneOf is { Count: > 0 })
			unionSchemas = target.OneOf;
		else if (target.AnyOf is { Count: > 0 })
			unionSchemas = target.AnyOf;

		if (unionSchemas == null)
			return result;

		foreach (var s in unionSchemas)
		{
			if (s is OpenApiSchemaReference unionRef)
			{
				var typeName = SchemaHelpers.FormatSchemaName(unionRef.Reference?.Id ?? "unknown");
				result.Add(new UnionOption(typeName, unionRef.Reference?.Id, !SchemaHelpers.IsValueType(typeName), s));
			}
			else if (s.Type?.HasFlag(JsonSchemaType.Array) == true && s.Items != null)
			{
				var itemInfo = GetTypeInfo(s.Items);
				result.Add(new UnionOption($"{itemInfo.TypeName}[]", itemInfo.SchemaRef, itemInfo.IsObject, s));
			}
			else
			{
				// Could be an inline schema or wrapped reference - try to get type info
				var info = GetTypeInfo(s);
				if (!string.IsNullOrEmpty(info.SchemaRef))
					result.Add(new UnionOption(info.TypeName, info.SchemaRef, info.IsObject, s));
				else
				{
					var primName = SchemaHelpers.GetPrimitiveTypeName(s.Type);
					if (string.IsNullOrEmpty(primName))
						primName = "unknown";
					result.Add(new UnionOption(primName, null, false, s));
				}
			}
		}

		return result;
	}

	/// <summary>
	/// Checks if a union option has properties, using fallback resolution if needed.
	/// Also recursively checks nested unions.
	/// </summary>
	public bool UnionOptionHasProperties(UnionOption option)
	{
		if (option.Schema == null)
			return false;

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

		// Try finding by name pattern (e.g., "SourceFilter" -> look for schemas ending with ".SourceFilter")
		if (document.Components?.Schemas != null)
		{
			var baseName = option.Name.EndsWith("[]") ? option.Name[..^2] : option.Name;
			var matchingSchema = document.Components.Schemas.FirstOrDefault(kvp => kvp.Key.EndsWith("." + baseName) || kvp.Key == baseName);
			if (matchingSchema.Value != null)
			{
				props = GetSchemaProperties(matchingSchema.Value);
				if (props?.Count > 0)
					return true;

				// Check if the matched schema is itself a union
				var nestedOptions = GetNestedUnionOptions(matchingSchema.Value);
				if (nestedOptions.Any(UnionOptionHasProperties))
					return true;
			}
		}

		return false;
	}

	/// <summary>
	/// Flattens nested unions to get all leaf options (options with direct properties, not union wrappers).
	/// </summary>
	public List<UnionOption> FlattenUnionOptions(List<UnionOption> options)
	{
		var result = new List<UnionOption>();

		foreach (var option in options.Where(o => o.Schema != null))
		{
			var baseName = option.Name.EndsWith("[]") ? option.Name[..^2] : option.Name;
			var isArray = option.Name.EndsWith("[]");
			var schema = option.Schema!; // Schema is guaranteed non-null by Where filter

			// For array types, we need to look at the Items schema
			var schemaToCheck = schema;
			if (schema.Type?.HasFlag(JsonSchemaType.Array) == true && schema.Items != null)
				schemaToCheck = schema.Items;

			// Check if this option has direct properties
			var hasDirectProps = false;
			var resolvedSchema = schemaToCheck;

			var props = GetSchemaProperties(schemaToCheck);
			if (props?.Count > 0)
				hasDirectProps = true;
			else if (schemaToCheck is OpenApiSchemaReference schemaRef)
			{
				var refId = schemaRef.Reference?.Id;
				if (!string.IsNullOrEmpty(refId) && document.Components?.Schemas?.TryGetValue(refId, out var resolved) == true)
				{
					resolvedSchema = resolved;
					props = GetSchemaProperties(resolved);
					if (props?.Count > 0)
						hasDirectProps = true;
				}
			}

			if (!hasDirectProps && document.Components?.Schemas != null)
			{
				var matchingSchema = document
					.Components
					.Schemas
					.FirstOrDefault(kvp => kvp.Key.EndsWith("." + baseName) || kvp.Key == baseName);
				if (matchingSchema.Value != null)
				{
					resolvedSchema = matchingSchema.Value;
					props = GetSchemaProperties(matchingSchema.Value);
					if (props?.Count > 0)
						hasDirectProps = true;
				}
			}

			if (hasDirectProps)
			{
				// This option has properties, add it to results
				// For arrays, keep the original schema so we render the right type
				result.Add(new UnionOption(option.Name, option.Ref, option.IsObject, resolvedSchema));
			}
			else if (resolvedSchema != null)
			{
				// Check if this is a nested union that we should expand
				// Try the original schema first (OpenApiSchemaReference proxies OneOf/AnyOf correctly)
				var nestedOptions = GetNestedUnionOptions(schemaToCheck);
				if (nestedOptions.Count == 0)
				{
					// Fallback to resolved schema
					nestedOptions = GetNestedUnionOptions(resolvedSchema);
				}
				if (nestedOptions.Count > 0)
				{
					// Recursively flatten nested union, carrying the array suffix if needed
					var flattenedNested = FlattenUnionOptions(nestedOptions);
					foreach (var nested in flattenedNested)
					{
						// If the parent was an array and the nested option isn't, add array suffix
						var nestedName = nested.Name;
						if (isArray && !nestedName.EndsWith("[]"))
							nestedName = $"{nestedName}[]";
						result.Add(new UnionOption(nestedName, nested.Ref, nested.IsObject, nested.Schema));
					}
				}
			}
		}

		return result;
	}

	/// <summary>
	/// Gets comprehensive type information for a schema.
	/// </summary>
	public TypeInfo GetTypeInfo(IOpenApiSchema? schema)
	{
		if (schema is null)
			return new TypeInfo("unknown", null, false, false, false, null, false, null);

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

				// Check if this is a value type - either from the known list or by detecting it's a primitive alias
				var isValueType = SchemaHelpers.IsValueType(typeName);
				var primitiveAliasType = !isValueType ? SchemaHelpers.GetPrimitiveAliasType(resolvedTarget) : null;
				if (!string.IsNullOrEmpty(primitiveAliasType))
					isValueType = true;

				var valueTypeBase = isValueType ? (primitiveAliasType ?? SchemaHelpers.GetValueTypeBase(resolvedTarget) ?? "string") : null;
				var hasLink = IsLinkedType(typeName);

				// Check if the schema reference is an enum or union — read from the resolved target
				var isEnum = resolvedTarget.Enum is { Count: > 0 };
				var isUnion = !isEnum && (resolvedTarget.OneOf is { Count: > 0 } || resolvedTarget.AnyOf is { Count: > 0 });
				var enumValues = isEnum ? resolvedTarget.Enum?.Select(e => e.ToString()).ToArray() : null;

				// Check if the referenced type is an array of primitives
				string? arrayItemType = null;
				if (isArray)
				{
					var itemSchema = resolvedTarget.Items;
					if (itemSchema is not null)
					{
						var itemInfo = GetTypeInfo(itemSchema);
						// If the item is not an object, not a linked type, and has no schema reference, it's a primitive array
						if (itemInfo is { IsObject: false, HasLink: false } && string.IsNullOrEmpty(itemInfo.SchemaRef))
							arrayItemType = itemInfo.TypeName;
					}
				}

				// Get union options from oneOf/anyOf
				string[]? unionOptions = null;
				List<UnionOption>? anyOfOptions = null;
				if (isUnion)
				{
					var unionSchemas = resolvedTarget.OneOf is { Count: > 0 } ? resolvedTarget.OneOf : resolvedTarget.AnyOf;
					var options = new List<string>();
					var anyOfList = new List<UnionOption>();
					foreach (var s in unionSchemas ?? [])
					{
						if (s is OpenApiSchemaReference unionRef)
						{
							var unionTypeName = SchemaHelpers.FormatSchemaName(unionRef.Reference?.Id ?? "unknown");
							options.Add(unionTypeName);
							// Also add to anyOfOptions for potential expansion
							anyOfList.Add(
								new UnionOption(unionTypeName, unionRef.Reference?.Id, !SchemaHelpers.IsValueType(unionTypeName), s)
							);
						}
						else if (s.Enum is { Count: > 0 } inlineEnum)
						{
							// String literal union - add enum values
							foreach (var enumVal in inlineEnum)
								options.Add(enumVal.ToString());
						}
						else if (s.Type?.HasFlag(JsonSchemaType.Array) == true && s.Items != null)
						{
							// Array type - get the item type and add [] suffix
							var itemInfo = GetTypeInfo(s.Items);
							var arrayTypeName = $"{itemInfo.TypeName}[]";
							options.Add(arrayTypeName);
							// Arrays of objects are expandable
							anyOfList.Add(new UnionOption(arrayTypeName, itemInfo.SchemaRef, itemInfo.IsObject, s));
						}
						else
						{
							var primName = SchemaHelpers.GetPrimitiveTypeName(s.Type);
							if (string.IsNullOrEmpty(primName))
								primName = "unknown";
							options.Add(primName);
							// Primitives are not objects
							anyOfList.Add(new UnionOption(primName, null, false, s));
						}
					}
					unionOptions = options.ToArray();
					anyOfOptions = anyOfList.Count > 0 ? anyOfList : null;
				}

				return new TypeInfo(
					typeName,
					refId,
					isArray,
					!isValueType && !isEnum,
					isValueType,
					valueTypeBase,
					hasLink,
					anyOfOptions,
					false,
					null,
					isEnum,
					isUnion,
					enumValues,
					unionOptions,
					arrayItemType
				);
			}
		}

		// Check for oneOf/anyOf which often indicate union types
		if (schema.OneOf is { Count: > 0 } oneOf)
		{
			var options = oneOf.Select(s =>
			{
				var info = GetTypeInfo(s);
				// Include [] suffix for array types
				var displayName = info.IsArray ? $"{info.TypeName}[]" : info.TypeName;
				return new UnionOption(displayName, info.SchemaRef, info.IsObject, s);
			}).ToList();

			var hasObjectOptions = options.Any(o => o.IsObject);
			if (hasObjectOptions && options.Count > 1)
			{
				// Return anyOf options for potential tab rendering
				return new TypeInfo("oneOf", null, false, true, false, null, false, options, IsUnion: true);
			}

			var typeNames = options.Select(o => o.Name).Distinct().ToArray();
			return new TypeInfo(string.Join(" | ", typeNames), null, false, false, false, null, false, options, IsUnion: true);
		}

		if (schema.AnyOf is { Count: > 0 } anyOf)
		{
			var options = anyOf.Select(s =>
			{
				var info = GetTypeInfo(s);
				// Include [] suffix for array types
				var displayName = info.IsArray ? $"{info.TypeName}[]" : info.TypeName;
				return new UnionOption(displayName, info.SchemaRef, info.IsObject, s);
			}).ToList();

			var hasObjectOptions = options.Any(o => o.IsObject);
			if (hasObjectOptions && options.Count > 1)
			{
				// Return anyOf options for potential tab rendering
				return new TypeInfo("anyOf", null, false, true, false, null, false, options, IsUnion: true);
			}

			var typeNames = options.Select(o => o.Name).Distinct().ToArray();
			return new TypeInfo(string.Join(" | ", typeNames), null, false, false, false, null, false, options, IsUnion: true);
		}

		// Check for allOf (usually inheritance/composition)
		if (schema.AllOf is { Count: > 0 } allOf)
		{
			var refSchemas = allOf.OfType<OpenApiSchemaReference>().ToArray();
			if (refSchemas.Length > 0)
			{
				var refId = refSchemas[0].Reference.Id;
				if (!string.IsNullOrEmpty(refId))
				{
					var typeName = SchemaHelpers.FormatSchemaName(refId);
					var isValueType = SchemaHelpers.IsValueType(typeName);
					var primitiveAliasType = !isValueType ? SchemaHelpers.GetPrimitiveAliasType(refSchemas[0]) : null;
					if (!string.IsNullOrEmpty(primitiveAliasType))
						isValueType = true;
					var valueTypeBase = isValueType
						? (primitiveAliasType ?? SchemaHelpers.GetValueTypeBase(refSchemas[0]) ?? "string")
						: null;
					var hasLink = IsLinkedType(typeName);
					return new TypeInfo(typeName, refId, false, !isValueType, isValueType, valueTypeBase, hasLink, null);
				}
			}
		}

		// Check for array items
		if (schema.Type?.HasFlag(JsonSchemaType.Array) ?? false)
		{
			if (schema.Items is not null)
			{
				var itemInfo = GetTypeInfo(schema.Items);
				// If the item is not an object and not a linked type, it's a primitive array
				var isPrimitiveArray = itemInfo is not { IsObject: false, HasLink: false } || !string.IsNullOrEmpty(itemInfo.SchemaRef);
				var arrayItemType = isPrimitiveArray ? itemInfo.TypeName : null;
				return new TypeInfo(
					itemInfo.TypeName,
					itemInfo.SchemaRef,
					true,
					itemInfo.IsObject,
					itemInfo.IsValueType,
					itemInfo.ValueTypeBase,
					itemInfo.HasLink,
					null,
					ArrayItemType: arrayItemType
				);
			}
			return new TypeInfo("unknown", null, true, false, false, null, false, null, ArrayItemType: "unknown");
		}

		// Check for enum
		if (schema.Enum is { Count: > 0 })
		{
			var enumValues = schema.Enum.Select(e => e.ToString()).Take(5).ToArray();
			return new TypeInfo("enum", null, false, false, false, null, false, null, false, null, true, false, enumValues);
		}

		// Check for additionalProperties (dictionary-like objects)
		if (schema.AdditionalProperties is { } addProps)
		{
			var valueInfo = GetTypeInfo(addProps);
			// Pass valueInfo.HasLink so we know if the dictionary value type has a dedicated page
			return new TypeInfo(
				$"string to {valueInfo.TypeName}",
				valueInfo.SchemaRef,
				false,
				true,
				false,
				null,
				valueInfo.HasLink,
				null,
				true,
				addProps
			);
		}

		// Check if it has properties (inline object)
		if (schema.Properties is { Count: > 0 })
			return new TypeInfo("object", null, false, true, false, null, false, null);

		// Primitive type
		var primitiveName = SchemaHelpers.GetPrimitiveTypeName(schema.Type);
		if (!string.IsNullOrEmpty(primitiveName))
			return new TypeInfo(primitiveName, null, false, primitiveName == "object", false, null, false, null);

		return new TypeInfo("object", null, false, true, false, null, false, null);
	}
}
