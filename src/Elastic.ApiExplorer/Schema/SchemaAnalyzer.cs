// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Schema;

/// <summary>
/// Analyzes OpenAPI schemas and provides type information.
/// Requires access to the OpenApiDocument for resolving schema references.
/// </summary>
/// <remarks>
/// Creates a new SchemaAnalyzer.
/// </remarks>
/// <param name="document">The OpenAPI document for resolving schema references.</param>
/// <param name="currentPageType">Optional current page type to prevent self-linking on schema pages.</param>
public class SchemaAnalyzer(OpenApiDocument document, string? currentPageType = null)
{
	private readonly OpenApiDocument _document = document;
	private readonly string? _currentPageType = currentPageType;

	/// <summary>
	/// Checks if a type should link to its container page, considering the current page.
	/// </summary>
	public bool IsLinkedType(string typeName) =>
		SchemaHelpers.ShouldLinkToContainerPage(typeName, _currentPageType);

	/// <summary>
	/// Resolves a schema reference to its target schema.
	/// </summary>
	public IOpenApiSchema? ResolveSchema(IOpenApiSchema? schema)
	{
		if (schema is null)
			return null;

		// If it's a reference, resolve from Components.Schemas
		if (schema is OpenApiSchemaReference schemaRef)
		{
			var refId = schemaRef.Reference?.Id;
			if (!string.IsNullOrEmpty(refId) &&
				_document.Components?.Schemas?.TryGetValue(refId, out var resolved) == true)
			{
				return resolved;
			}
			return schemaRef;
		}

		// For allOf, return as-is (properties will be merged by GetSchemaProperties)
		if (schema.AllOf is { Count: > 0 })
			return schema;

		return schema;
	}

	/// <summary>
	/// Gets the properties from a schema, resolving references and handling allOf composition.
	/// </summary>
	public IDictionary<string, IOpenApiSchema>? GetSchemaProperties(IOpenApiSchema? schema)
	{
		if (schema is null)
			return null;

		// Handle schema references - resolve to get actual properties
		if (schema is OpenApiSchemaReference schemaRef)
		{
			// OpenApiSchemaReference proxies to the target schema
			// Try direct property access first (proxied)
			if (schemaRef.Properties is { Count: > 0 })
				return schemaRef.Properties;

			// Try resolving via Reference.Id
			var refId = schemaRef.Reference?.Id;
			if (!string.IsNullOrEmpty(refId))
			{
				// Try to get the resolved schema from the document
				if (_document.Components?.Schemas?.TryGetValue(refId, out var resolvedSchema) == true)
				{
					return GetSchemaProperties(resolvedSchema);
				}
			}
			// Fall through to try other schema properties
		}

		// Direct properties
		if (schema.Properties is { Count: > 0 })
			return schema.Properties;

		// For allOf, collect properties from all schemas
		if (schema.AllOf is { Count: > 0 } allOf)
		{
			var props = new Dictionary<string, IOpenApiSchema>();
			foreach (var subSchema in allOf)
			{
				var subProps = GetSchemaProperties(subSchema);
				if (subProps is not null)
				{
					foreach (var prop in subProps)
						_ = props.TryAdd(prop.Key, prop.Value);
				}
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

		// First try the schema directly (OpenApiSchemaReference proxies OneOf/AnyOf)
		if (schema.OneOf is { Count: > 0 })
			unionSchemas = schema.OneOf;
		else if (schema.AnyOf is { Count: > 0 })
			unionSchemas = schema.AnyOf;

		// If not found and it's a reference, resolve and try again
		if (unionSchemas == null && schema is OpenApiSchemaReference schemaRef)
		{
			var refId = schemaRef.Reference?.Id;
			if (!string.IsNullOrEmpty(refId) &&
				_document.Components?.Schemas?.TryGetValue(refId, out var resolved) == true)
			{
				if (resolved.OneOf is { Count: > 0 })
					unionSchemas = resolved.OneOf;
				else if (resolved.AnyOf is { Count: > 0 })
					unionSchemas = resolved.AnyOf;
			}
		}

		if (unionSchemas == null)
			return result;

		foreach (var s in unionSchemas)
		{
			if (s is OpenApiSchemaReference unionRef)
			{
				var typeName = SchemaHelpers.FormatSchemaName(unionRef.Reference?.Id ?? "unknown");
				result.Add(new UnionOption(typeName, unionRef.Reference?.Id, !SchemaHelpers.IsValueType(typeName), s));
			}
			else if (s?.Type?.HasFlag(JsonSchemaType.Array) == true && s.Items != null)
			{
				var itemInfo = GetTypeInfo(s.Items);
				result.Add(new UnionOption($"{itemInfo.TypeName}[]", itemInfo.SchemaRef, itemInfo.IsObject, s));
			}
			else if (s != null)
			{
				// Could be an inline schema or wrapped reference - try to get type info
				var info = GetTypeInfo(s);
				if (!string.IsNullOrEmpty(info.SchemaRef))
				{
					result.Add(new UnionOption(info.TypeName, info.SchemaRef, info.IsObject, s));
				}
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
			refId = schemaRef.Reference?.Id;

		if (!string.IsNullOrEmpty(refId) &&
			_document.Components?.Schemas?.TryGetValue(refId, out var resolvedSchema) == true)
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
		if (_document.Components?.Schemas != null)
		{
			var baseName = option.Name.EndsWith("[]") ? option.Name[..^2] : option.Name;
			var matchingSchema = _document.Components.Schemas
				.FirstOrDefault(kvp => kvp.Key.EndsWith("." + baseName) || kvp.Key == baseName);
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

		foreach (var option in options)
		{
			if (option.Schema == null)
				continue;

			var baseName = option.Name.EndsWith("[]") ? option.Name[..^2] : option.Name;
			var isArray = option.Name.EndsWith("[]");

			// For array types, we need to look at the Items schema
			var schemaToCheck = option.Schema;
			if (option.Schema.Type?.HasFlag(JsonSchemaType.Array) == true && option.Schema.Items != null)
			{
				schemaToCheck = option.Schema.Items;
			}

			// Check if this option has direct properties
			var hasDirectProps = false;
			var resolvedSchema = schemaToCheck;

			var props = GetSchemaProperties(schemaToCheck);
			if (props?.Count > 0)
			{
				hasDirectProps = true;
			}
			else if (schemaToCheck is OpenApiSchemaReference schemaRef)
			{
				var refId = schemaRef.Reference?.Id;
				if (!string.IsNullOrEmpty(refId) &&
					_document.Components?.Schemas?.TryGetValue(refId, out var resolved) == true)
				{
					resolvedSchema = resolved;
					props = GetSchemaProperties(resolved);
					if (props?.Count > 0)
						hasDirectProps = true;
				}
			}

			if (!hasDirectProps && _document.Components?.Schemas != null)
			{
				var matchingSchema = _document.Components.Schemas
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
			var refId = schemaRef.Reference?.Id;
			if (!string.IsNullOrEmpty(refId))
			{
				var typeName = SchemaHelpers.FormatSchemaName(refId);
				var isArray = schema.Type?.HasFlag(JsonSchemaType.Array) ?? false;
				var isValueType = SchemaHelpers.IsValueType(typeName);
				var valueTypeBase = isValueType ? SchemaHelpers.GetValueTypeBase(schemaRef) ?? "string" : null;
				var hasLink = IsLinkedType(typeName);

				// Check if the schema reference is an enum or union
				// OpenApiSchemaReference proxies to resolved schema properties
				var isEnum = schemaRef.Enum is { Count: > 0 };
				var isUnion = !isEnum && (schemaRef.OneOf is { Count: > 0 } || schemaRef.AnyOf is { Count: > 0 });
				var enumValues = isEnum ? schemaRef.Enum?.Select(e => e?.ToString() ?? "").ToArray() : null;

				// Check if the referenced type is an array of primitives
				string? arrayItemType = null;
				if (isArray)
				{
					// Try getting Items from the proxy first
					var itemSchema = schemaRef.Items;

					// If Items is null, try resolving the schema explicitly
					if (itemSchema is null &&
						_document.Components?.Schemas?.TryGetValue(refId, out var resolvedArraySchema) == true)
					{
						itemSchema = resolvedArraySchema.Items;
					}

					if (itemSchema is not null)
					{
						var itemInfo = GetTypeInfo(itemSchema);
						// If the item is not an object and not a linked type, it's a primitive array
						if (!itemInfo.IsObject && !itemInfo.HasLink && string.IsNullOrEmpty(itemInfo.SchemaRef))
						{
							arrayItemType = itemInfo.TypeName;
						}
					}
				}

				// Get union options from oneOf/anyOf
				string[]? unionOptions = null;
				List<UnionOption>? anyOfOptions = null;
				if (isUnion)
				{
					var unionSchemas = schemaRef.OneOf is { Count: > 0 } ? schemaRef.OneOf : schemaRef.AnyOf;
					var options = new List<string>();
					var anyOfList = new List<UnionOption>();
					foreach (var s in unionSchemas ?? [])
					{
						if (s is OpenApiSchemaReference unionRef)
						{
							var unionTypeName = SchemaHelpers.FormatSchemaName(unionRef.Reference?.Id ?? "unknown");
							options.Add(unionTypeName);
							// Also add to anyOfOptions for potential expansion
							anyOfList.Add(new UnionOption(unionTypeName, unionRef.Reference?.Id, !SchemaHelpers.IsValueType(unionTypeName), s));
						}
						else if (s?.Enum is { Count: > 0 } inlineEnum)
						{
							// String literal union - add enum values
							foreach (var enumVal in inlineEnum)
								options.Add(enumVal?.ToString() ?? "");
						}
						else if (s?.Type?.HasFlag(JsonSchemaType.Array) == true && s.Items != null)
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
							var primName = SchemaHelpers.GetPrimitiveTypeName(s?.Type);
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

				return new TypeInfo(typeName, refId, isArray, !isValueType && !isEnum, isValueType, valueTypeBase, hasLink, anyOfOptions, false, null, isEnum, isUnion, enumValues, unionOptions, arrayItemType);
			}
		}

		// Check for oneOf/anyOf which often indicate union types
		if (schema.OneOf is { Count: > 0 } oneOf)
		{
			var options = oneOf.Where(s => s is not null).Select(s =>
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
			var options = anyOf.Where(s => s is not null).Select(s =>
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
				var refId = refSchemas[0].Reference?.Id;
				if (!string.IsNullOrEmpty(refId))
				{
					var typeName = SchemaHelpers.FormatSchemaName(refId);
					var isValueType = SchemaHelpers.IsValueType(typeName);
					var hasLink = IsLinkedType(typeName);
					return new TypeInfo(typeName, refId, false, !isValueType, isValueType, null, hasLink, null);
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
				var isPrimitiveArray = !itemInfo.IsObject && !itemInfo.HasLink && string.IsNullOrEmpty(itemInfo.SchemaRef);
				var arrayItemType = isPrimitiveArray ? itemInfo.TypeName : null;
				return new TypeInfo(itemInfo.TypeName, itemInfo.SchemaRef, true, itemInfo.IsObject, itemInfo.IsValueType, itemInfo.ValueTypeBase, itemInfo.HasLink, null, ArrayItemType: arrayItemType);
			}
			return new TypeInfo("unknown", null, true, false, false, null, false, null, ArrayItemType: "unknown");
		}

		// Check for enum
		if (schema.Enum is { Count: > 0 })
		{
			var enumValues = schema.Enum.Select(e => e?.ToString() ?? "").Take(5).ToArray();
			return new TypeInfo("enum", null, false, false, false, null, false, null, false, null, true, false, enumValues);
		}

		// Check for additionalProperties (dictionary-like objects)
		if (schema.AdditionalProperties is IOpenApiSchema addProps)
		{
			var valueInfo = GetTypeInfo(addProps);
			// Pass valueInfo.HasLink so we know if the dictionary value type has a dedicated page
			return new TypeInfo($"string to {valueInfo.TypeName}", valueInfo.SchemaRef, false, true, false, null, valueInfo.HasLink, null, true, addProps);
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
