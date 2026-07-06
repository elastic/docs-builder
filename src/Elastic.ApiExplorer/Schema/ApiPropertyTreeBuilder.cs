// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.ApiExplorer.Operations;
using Microsoft.AspNetCore.Html;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Schema;

/// <summary>
/// Builds the renderable <see cref="ApiProperty"/> tree for a schema, moving every structural
/// decision (recursion, unions, dictionaries, collapse state) out of the views. The tree is built
/// eagerly; recursion detection prunes descent exactly where rendering previously stopped.
/// </summary>
public class ApiPropertyTreeBuilder(OpenApiDocument document, PropertyDisplayOptions options, string? currentPageType = null)
{
	private readonly SchemaAnalyzer _analyzer = new(document, currentPageType);

	/// <summary>Builds the property rows for a schema; null when it has no renderable properties.</summary>
	public ApiPropertyList? BuildPropertyList(
		IOpenApiSchema? schema, string prefix, bool isRequest,
		int depth = 0, IReadOnlySet<string>? ancestors = null, ISet<string>? requiredProperties = null)
	{
		var properties = _analyzer.GetSchemaProperties(schema);
		if (properties is null || properties.Count == 0)
			return null;

		var requiredProps = requiredProperties ?? schema?.Required ?? new HashSet<string>();
		var propArray = properties.ToArray();
		var items = new List<ApiProperty>(propArray.Length);
		for (var i = 0; i < propArray.Length; i++)
		{
			var (name, propSchema) = propArray[i];
			if (propSchema is null)
				continue;

			var typeInfo = _analyzer.GetTypeInfo(propSchema);
			var propId = string.IsNullOrEmpty(prefix) ? name : $"{prefix}-{name}";
			var isRecursive = DetectRecursion(propSchema, typeInfo, ancestors);
			items.Add(BuildProperty(
				name, propSchema, typeInfo, propId,
				isRequired: requiredProps.Contains(name),
				isLast: i == propArray.Length - 1,
				isRecursive, isRequest, depth, ancestors));
		}

		return new ApiPropertyList(items);
	}

	/// <summary>Builds the expanded variants for a top-level oneOf/anyOf union (schema pages).</summary>
	public ApiUnionVariants? BuildUnionVariantsForSchemas(IList<IOpenApiSchema> unionSchemas, string prefix, IReadOnlySet<string>? ancestors)
	{
		var unionOptions = unionSchemas.Where(s => s is not null).Select(s =>
		{
			var info = _analyzer.GetTypeInfo(s);
			var displayName = info.IsArray ? $"{info.TypeName}[]" : info.TypeName;
			return new UnionOption(displayName, info.SchemaRef, info.IsObject, s);
		}).ToList();
		return BuildUnionVariants(unionOptions, prefix, depth: 0, isRequest: false, ancestors);
	}

	/// <summary>The display form (icons, keywords, name) of a schema's type.</summary>
	public TypeAnnotation Describe(IOpenApiSchema? schema)
	{
		var typeInfo = _analyzer.GetTypeInfo(schema);
		return BuildAnnotation(typeInfo, HasActualProperties(schema));
	}

	/// <summary>Validation constraint lines for a schema; empty when it declares none.</summary>
	public static IReadOnlyList<ConstraintDisplay> BuildConstraints(IOpenApiSchema schema)
	{
		var constraints = new List<ConstraintDisplay>();

		var defaultValue = schema.Default?.ToString();
		if (!string.IsNullOrEmpty(defaultValue))
			constraints.Add(new ConstraintDisplay("default: ", defaultValue));

		if (schema.MinLength.HasValue)
			constraints.Add(new ConstraintDisplay($"min length: {schema.MinLength.Value}"));
		if (schema.MaxLength.HasValue)
			constraints.Add(new ConstraintDisplay($"max length: {schema.MaxLength.Value}"));
		if (!string.IsNullOrEmpty(schema.Pattern))
			constraints.Add(new ConstraintDisplay("pattern: ", schema.Pattern));

		if (!string.IsNullOrEmpty(schema.Minimum))
			constraints.Add(new ConstraintDisplay($"min: {schema.Minimum}"));
		if (!string.IsNullOrEmpty(schema.Maximum))
			constraints.Add(new ConstraintDisplay($"max: {schema.Maximum}"));
		if (!string.IsNullOrEmpty(schema.ExclusiveMinimum))
			constraints.Add(new ConstraintDisplay($"exclusive min: {schema.ExclusiveMinimum}"));
		if (!string.IsNullOrEmpty(schema.ExclusiveMaximum))
			constraints.Add(new ConstraintDisplay($"exclusive max: {schema.ExclusiveMaximum}"));
		if (schema.MultipleOf.HasValue)
			constraints.Add(new ConstraintDisplay($"multiple of: {schema.MultipleOf.Value}"));

		if (schema.MinItems.HasValue)
			constraints.Add(new ConstraintDisplay($"min items: {schema.MinItems.Value}"));
		if (schema.MaxItems.HasValue)
			constraints.Add(new ConstraintDisplay($"max items: {schema.MaxItems.Value}"));
		if (schema.UniqueItems == true)
			constraints.Add(new ConstraintDisplay("unique items"));

		return constraints;
	}

	private bool HasActualProperties(IOpenApiSchema? schema) =>
		_analyzer.GetSchemaProperties(schema)?.Count > 0;

	private ApiProperty BuildProperty(
		string name, IOpenApiSchema propSchema, TypeInfo typeInfo, string propId,
		bool isRequired, bool isLast, bool isRecursive, bool isRequest, int depth, IReadOnlySet<string>? ancestors)
	{
		var expansion = ComputeExpansion(propSchema, typeInfo, depth, isRecursive);

		return new ApiProperty
		{
			Name = name,
			Schema = propSchema,
			AnchorId = propId,
			Depth = depth,
			IsRequired = isRequired,
			IsLast = isLast,
			IsRecursive = isRecursive,
			IsRequest = isRequest,
			Type = BuildAnnotation(typeInfo, HasActualProperties(propSchema)),
			DescriptionHtml = string.IsNullOrWhiteSpace(propSchema.Description)
				? HtmlString.Empty
				: options.RenderMarkdown(propSchema.Description),
			ShowDeprecatedBadge = options.ShowDeprecated && propSchema.Deprecated,
			Availability = options.ShowVersionInfo
				? AvailabilityBadgeHelper.FromSchema(propSchema, options.VersionsConfiguration)
				: null,
			ExternalDocs = BuildExternalDocs(propSchema, typeInfo),
			Constraints = BuildConstraints(propSchema),
			EnumValues = typeInfo is { IsEnum: true, EnumValues.Length: > 0 } ? typeInfo.EnumValues : [],
			Union = typeInfo.IsUnion ? BuildUnionDisplay(propSchema, typeInfo, expansion) : null,
			ArrayItemTypeName = string.IsNullOrEmpty(typeInfo.ArrayItemType) ? null : typeInfo.ArrayItemType,
			TypeLink = BuildTypeLink(typeInfo, expansion),
			IsCollapsible = expansion.IsCollapsible,
			DefaultExpanded = expansion.DefaultExpanded,
			NestedCount = expansion.NestedCount,
			Children = isRecursive
				? ApiPropertyChildren.None
				: BuildChildren(propSchema, typeInfo, propId, isRequest, depth, ancestors, expansion)
		};
	}

	/// <summary>Everything the original view's opening code block derived about a property's expansion.</summary>
	private sealed record Expansion(
		bool HasNestedProps, bool HasDictValueProps, IOpenApiSchema? ArrayItemSchema, bool HasArrayItemProps,
		bool IsSimpleArrayUnion, string? SimpleUnionBaseName, bool HasUnionOptions,
		bool SimpleUnionHasExpandableProps, IOpenApiSchema? SimpleUnionSchema, List<UnionOption>? SimpleUnionNestedOptions,
		int NestedCount, bool HasChildren, bool IsCollapsible, bool DefaultExpanded);

	private Expansion ComputeExpansion(IOpenApiSchema propSchema, TypeInfo typeInfo, int depth, bool isRecursive)
	{
		var dictHasLinkedValue = typeInfo is { IsDictionary: true, HasLink: true };
		var hasNestedProps = typeInfo is { IsObject: true, HasLink: false } && depth < options.MaxDepth
			&& HasActualProperties(propSchema);
		var hasDictValueProps = typeInfo is { IsDictionary: true, DictValueSchema: not null }
			&& depth < options.MaxDepth && !dictHasLinkedValue && HasActualProperties(typeInfo.DictValueSchema);
		var arrayItemSchema = typeInfo.IsArray && propSchema.Items is not null ? propSchema.Items : null;
		var hasArrayItemProps = arrayItemSchema is not null && !typeInfo.HasLink && depth < options.MaxDepth
			&& HasActualProperties(arrayItemSchema);

		var (isSimpleArrayUnion, simpleUnionBaseName) = DetectSimpleArrayUnion(typeInfo);

		var hasUnionOptions = typeInfo is { IsUnion: true, AnyOfOptions: not null } && depth < options.MaxDepth
			&& !isSimpleArrayUnion
			&& typeInfo.AnyOfOptions.Any(_analyzer.UnionOptionHasProperties);

		var (simpleUnionHasExpandableProps, simpleUnionSchema, simpleUnionNestedOptions) =
			ResolveSimpleUnionExpansion(typeInfo, isSimpleArrayUnion, simpleUnionBaseName, depth);

		var nestedCount = 0;
		if (hasNestedProps)
			nestedCount = _analyzer.GetSchemaProperties(propSchema)?.Count ?? 0;
		else if (hasDictValueProps)
			nestedCount = _analyzer.GetSchemaProperties(typeInfo.DictValueSchema)?.Count ?? 0;
		else if (hasArrayItemProps)
			nestedCount = _analyzer.GetSchemaProperties(arrayItemSchema)?.Count ?? 0;
		else if (hasUnionOptions)
			nestedCount = typeInfo.AnyOfOptions!.Count(_analyzer.UnionOptionHasProperties);
		else if (simpleUnionHasExpandableProps && simpleUnionNestedOptions is { Count: > 0 })
			nestedCount = simpleUnionNestedOptions.Count(_analyzer.UnionOptionHasProperties);
		else if (simpleUnionHasExpandableProps && simpleUnionSchema is not null)
			nestedCount = _analyzer.GetSchemaProperties(simpleUnionSchema)?.Count ?? 0;

		var hasChildren = (hasNestedProps || hasDictValueProps || hasArrayItemProps || hasUnionOptions || simpleUnionHasExpandableProps) && !isRecursive;
		var isCollapsible = hasChildren && nestedCount > 1 && !hasUnionOptions && !hasDictValueProps;
		var defaultExpanded = ComputeDefaultExpanded(depth, nestedCount);

		return new Expansion(
			hasNestedProps, hasDictValueProps, arrayItemSchema, hasArrayItemProps,
			isSimpleArrayUnion, simpleUnionBaseName, hasUnionOptions,
			simpleUnionHasExpandableProps, simpleUnionSchema, simpleUnionNestedOptions,
			nestedCount, hasChildren, isCollapsible, defaultExpanded);
	}

	private static (bool IsSimpleArrayUnion, string? BaseName) DetectSimpleArrayUnion(TypeInfo typeInfo)
	{
		if (typeInfo is not { IsUnion: true, AnyOfOptions.Count: > 0 })
			return (false, null);

		var unionOptionNames = new List<string>();
		unionOptionNames.AddRange(typeInfo.AnyOfOptions.Select(o => o.Name));
		if (typeInfo.UnionOptions is not null)
			unionOptionNames.AddRange(typeInfo.UnionOptions);
		var distinctNames = unionOptionNames.Distinct().ToArray();
		if (distinctNames.Length != 2)
			return (false, null);

		var baseNames = distinctNames.Select(n => n.EndsWith("[]") ? n[..^2] : n).Distinct().ToArray();
		if (baseNames.Length == 1 && !string.IsNullOrEmpty(baseNames[0]))
			return (true, baseNames[0]);
		return (false, null);
	}

	private (bool Expandable, IOpenApiSchema? Schema, List<UnionOption>? NestedOptions) ResolveSimpleUnionExpansion(
		TypeInfo typeInfo, bool isSimpleArrayUnion, string? simpleUnionBaseName, int depth)
	{
		if (!isSimpleArrayUnion || string.IsNullOrEmpty(simpleUnionBaseName) || depth >= options.MaxDepth)
			return (false, null, null);

		var baseOption = typeInfo.AnyOfOptions!.FirstOrDefault(o => o.Name == simpleUnionBaseName);
		if (baseOption?.Schema is null)
			return (false, null, null);

		var baseTypeInfo = _analyzer.GetTypeInfo(baseOption.Schema);
		if (baseTypeInfo.HasLink || !_analyzer.UnionOptionHasProperties(baseOption))
			return (false, null, null);

		var directProps = _analyzer.GetSchemaProperties(baseOption.Schema);
		var nestedOptions = directProps is null or { Count: 0 }
			? _analyzer.GetNestedUnionOptions(baseOption.Schema)
			: null;
		return (true, baseOption.Schema, nestedOptions);
	}

	private bool ComputeDefaultExpanded(int depth, int nestedCount) =>
		options.CollapseMode == CollapseMode.DepthBased && depth != 0 && nestedCount is > 0 and < 5;

	private ExternalDocLink? BuildExternalDocs(IOpenApiSchema propSchema, TypeInfo typeInfo)
	{
		if (!options.ShowExternalDocs || propSchema.ExternalDocs?.Url is null || typeInfo.HasLink)
			return null;
		var url = propSchema.ExternalDocs.Url.ToString();
		return new ExternalDocLink(url, IsElasticDocsUrl(url));
	}

	internal static bool IsElasticDocsUrl(string url) =>
		url.Contains("www.elastic.co/docs") || url.Contains("elastic.co/guide");

	private TypePageLink? BuildTypeLink(TypeInfo typeInfo, Expansion expansion)
	{
		string? linkedTypeName = null;
		if (typeInfo.HasLink)
		{
			linkedTypeName = typeInfo is { IsDictionary: true, DictValueSchema: not null }
				? _analyzer.GetTypeInfo(typeInfo.DictValueSchema).TypeName
				: typeInfo.TypeName;
		}
		else if (expansion.IsSimpleArrayUnion && !string.IsNullOrEmpty(expansion.SimpleUnionBaseName))
		{
			var baseOption = typeInfo.AnyOfOptions!.FirstOrDefault(o => o.Name == expansion.SimpleUnionBaseName);
			if (baseOption?.Schema is not null && _analyzer.GetTypeInfo(baseOption.Schema).HasLink)
				linkedTypeName = expansion.SimpleUnionBaseName;
		}

		if (string.IsNullOrEmpty(linkedTypeName))
			return null;
		return new TypePageLink(linkedTypeName, SchemaHelpers.GetContainerPageUrl(options.ApiRootUrl, linkedTypeName));
	}

	private UnionDisplay? BuildUnionDisplay(IOpenApiSchema propSchema, TypeInfo typeInfo, Expansion expansion)
	{
		var unionOptionNames = new List<string>();
		if (typeInfo.AnyOfOptions is { Count: > 0 })
			unionOptionNames.AddRange(typeInfo.AnyOfOptions.Select(o => o.Name));
		if (typeInfo.UnionOptions is not null)
			unionOptionNames.AddRange(typeInfo.UnionOptions);
		var sortedOptions = unionOptionNames.Distinct()
			.OrderByDescending(o => o.EndsWith("[]"))
			.ToArray();

		var allEnumLike = sortedOptions.Length > 0 && sortedOptions.All(o =>
			!o.EndsWith("[]") &&
			!string.IsNullOrEmpty(o) &&
			!SchemaHelpers.PrimitiveTypeNames.Contains(o) &&
			(char.IsLower(o[0]) || o.All(c => !char.IsLetter(c) || char.IsLower(c) || c == '_')));

		if (allEnumLike)
			return new UnionDisplay { Kind = UnionDisplayKind.EnumLike, EnumLikeValues = sortedOptions };

		if (expansion.IsSimpleArrayUnion && !string.IsNullOrEmpty(expansion.SimpleUnionBaseName))
			return BuildSimpleArrayUnionDisplay(typeInfo, expansion.SimpleUnionBaseName);

		if (sortedOptions.Length > 0 || expansion.HasUnionOptions)
		{
			var badgeOptions = expansion.HasUnionOptions ? [] : sortedOptions;
			return new UnionDisplay
			{
				Kind = UnionDisplayKind.Badges,
				DiscriminatorProperty = propSchema.Discriminator?.PropertyName,
				Badges = badgeOptions.Select(o => new UnionBadge(o, IsTypeOptionBadge(o))).ToArray()
			};
		}

		return null;
	}

	private UnionDisplay BuildSimpleArrayUnionDisplay(TypeInfo typeInfo, string baseName)
	{
		var baseTypeOption = typeInfo.AnyOfOptions!.FirstOrDefault(o => o.Name == baseName);
		var baseTypeInfo = baseTypeOption?.Schema is not null ? _analyzer.GetTypeInfo(baseTypeOption.Schema) : null;
		var isBaseValueType = baseTypeInfo?.IsValueType ?? false;
		var valueTypePrefix = isBaseValueType && !string.IsNullOrEmpty(baseTypeInfo?.ValueTypeBase)
			? baseTypeInfo.ValueTypeBase + " "
			: "";
		return new UnionDisplay
		{
			Kind = UnionDisplayKind.SimpleArrayUnion,
			SimpleUnionBaseName = baseName,
			SimpleUnionIsObject = baseTypeOption?.IsObject ?? false,
			SimpleUnionValueTypePrefix = valueTypePrefix
		};
	}

	internal static bool IsTypeOptionBadge(string option) =>
		SchemaHelpers.PrimitiveTypeNames.Contains(option) ||
		SchemaHelpers.PrimitiveTypeNames.Contains(option.TrimEnd('[', ']')) ||
		char.IsUpper(option[0]) || option.EndsWith("[]");

	private ApiPropertyChildren BuildChildren(
		IOpenApiSchema propSchema, TypeInfo typeInfo, string propId, bool isRequest,
		int depth, IReadOnlySet<string>? ancestors, Expansion expansion)
	{
		var newAncestors = AugmentAncestors(typeInfo, ancestors);
		var useHidden = options.UseHiddenUntilFound && expansion.IsCollapsible && !expansion.DefaultExpanded;

		if (expansion.HasDictValueProps)
			return BuildDictionaryChildren(typeInfo, propId, isRequest, depth, newAncestors, expansion);

		if (expansion.HasNestedProps)
		{
			return new ApiPropertyChildren
			{
				Kind = ChildKind.PropertyList,
				UseHidden = useHidden,
				Properties = BuildPropertyList(propSchema, propId, isRequest, depth + 1, newAncestors) ?? new ApiPropertyList([])
			};
		}

		if (expansion.HasArrayItemProps)
		{
			return new ApiPropertyChildren
			{
				Kind = ChildKind.PropertyList,
				UseHidden = useHidden,
				Properties = BuildPropertyList(expansion.ArrayItemSchema, propId, isRequest, depth + 1, newAncestors) ?? new ApiPropertyList([])
			};
		}

		if (expansion.HasUnionOptions)
		{
			return new ApiPropertyChildren
			{
				Kind = ChildKind.UnionVariants,
				UseHidden = false,
				Variants = BuildUnionVariants(typeInfo.AnyOfOptions!, propId, depth + 1, isRequest, newAncestors) ?? ApiUnionVariants.Empty
			};
		}

		if (expansion.SimpleUnionHasExpandableProps && expansion.SimpleUnionNestedOptions is { Count: > 0 })
		{
			return new ApiPropertyChildren
			{
				Kind = ChildKind.SimpleUnionVariants,
				UseHidden = useHidden,
				Variants = BuildUnionVariants(expansion.SimpleUnionNestedOptions, propId, depth + 1, isRequest, newAncestors) ?? ApiUnionVariants.Empty
			};
		}

		if (expansion.SimpleUnionHasExpandableProps && expansion.SimpleUnionSchema is not null)
		{
			return new ApiPropertyChildren
			{
				Kind = ChildKind.PropertyList,
				UseHidden = useHidden,
				Properties = BuildPropertyList(expansion.SimpleUnionSchema, propId, isRequest, depth + 1, newAncestors) ?? new ApiPropertyList([])
			};
		}

		return ApiPropertyChildren.None;
	}

	private ApiPropertyChildren BuildDictionaryChildren(
		TypeInfo typeInfo, string propId, bool isRequest, int depth, IReadOnlySet<string> newAncestors, Expansion expansion)
	{
		var keyAnchorId = $"{propId}-string";
		var dictIsCollapsible = expansion.NestedCount > 1;
		var dictDefaultExpanded = ComputeDefaultExpanded(depth + 1, expansion.NestedCount);
		return new ApiPropertyChildren
		{
			Kind = ChildKind.Dictionary,
			UseHidden = false,
			Dictionary = new DictionaryChildDisplay
			{
				KeyAnchorId = keyAnchorId,
				Depth = depth + 1,
				IsCollapsible = dictIsCollapsible,
				DefaultExpanded = dictDefaultExpanded,
				NestedCount = expansion.NestedCount,
				UseHidden = options.UseHiddenUntilFound && dictIsCollapsible && !dictDefaultExpanded,
				ValueType = Describe(typeInfo.DictValueSchema),
				Properties = BuildPropertyList(typeInfo.DictValueSchema, keyAnchorId, isRequest, depth + 2, newAncestors)
					?? new ApiPropertyList([])
			}
		};
	}

	private IReadOnlySet<string> AugmentAncestors(TypeInfo typeInfo, IReadOnlySet<string>? ancestors)
	{
		var newAncestors = ancestors is not null ? new HashSet<string>(ancestors) : [];
		if (string.IsNullOrEmpty(typeInfo.TypeName) || !typeInfo.IsObject)
			return newAncestors;

		if (typeInfo is { IsDictionary: true, DictValueSchema: not null })
		{
			var dictValueType = _analyzer.GetTypeInfo(typeInfo.DictValueSchema);
			if (!string.IsNullOrEmpty(dictValueType.TypeName))
				_ = newAncestors.Add(dictValueType.TypeName);
		}
		else
			_ = newAncestors.Add(typeInfo.TypeName);

		return newAncestors;
	}

	private bool DetectRecursion(IOpenApiSchema propSchema, TypeInfo typeInfo, IReadOnlySet<string>? ancestors)
	{
		if (ancestors is null)
			return false;

		if (IsAncestorType(typeInfo.TypeName, ancestors))
			return true;

		if (typeInfo.IsArray && propSchema.Items is not null
			&& IsAncestorType(_analyzer.GetTypeInfo(propSchema.Items).TypeName, ancestors))
			return true;

		if (typeInfo is { IsDictionary: true, DictValueSchema: not null }
			&& IsAncestorType(_analyzer.GetTypeInfo(typeInfo.DictValueSchema).TypeName, ancestors))
			return true;

		if (typeInfo is { IsUnion: true, AnyOfOptions: not null }
			&& typeInfo.AnyOfOptions
				.Select(option => option.Name.EndsWith("[]") ? option.Name[..^2] : option.Name)
				.Any(baseName => IsAncestorType(baseName, ancestors)))
			return true;

		return DetectDirectUnionRecursion(propSchema, ancestors);
	}

	private bool DetectDirectUnionRecursion(IOpenApiSchema propSchema, IReadOnlySet<string> ancestors)
	{
		var unionSchemas = propSchema.OneOf ?? propSchema.AnyOf;
		if (unionSchemas is not { Count: > 0 })
			return false;

		foreach (var unionSchema in unionSchemas.Where(s => s is not null))
		{
			var unionTypeInfo = _analyzer.GetTypeInfo(unionSchema);
			var typeName = unionTypeInfo.TypeName;
			var baseName = typeName?.EndsWith("[]") == true ? typeName[..^2] : typeName;
			if (IsAncestorType(baseName, ancestors))
				return true;

			if (unionTypeInfo.IsArray && unionSchema.Items is not null
				&& IsAncestorType(_analyzer.GetTypeInfo(unionSchema.Items).TypeName, ancestors))
				return true;
		}

		return false;
	}

	private static bool IsAncestorType(string? typeName, IReadOnlySet<string> ancestors) =>
		!string.IsNullOrEmpty(typeName) && !SchemaHelpers.IsPrimitiveTypeName(typeName) && ancestors.Contains(typeName);

	private ApiUnionVariants? BuildUnionVariants(
		List<UnionOption> unionOptions, string prefix, int depth, bool isRequest, IReadOnlySet<string>? ancestors)
	{
		if (unionOptions.Count == 0 || !unionOptions.Any(o => o.IsObject))
			return null;

		var variantsToRender = CollectVariantsToRender(unionOptions);
		if (variantsToRender.Count == 0)
			return null;

		// Children of union variants always show deprecated/version/external-docs regardless of page settings.
		var childBuilder = new ApiPropertyTreeBuilder(document,
			options with { ShowDeprecated = true, ShowVersionInfo = true, ShowExternalDocs = true },
			currentPageType);

		var variants = new List<ApiUnionVariant>(variantsToRender.Count);
		foreach (var variant in variantsToRender)
		{
			var hasProperties = variant.Props is { Count: > 0 };
			var optionId = $"{prefix}-variant-{variant.Name.ToLowerInvariant().Replace(" ", "-").Replace("[]", "-array")}";
			var hasBothVariants = variantsToRender.Count(v => v.BaseName == variant.BaseName) > 1;
			var showProperties = hasProperties && (!variant.IsArray || !hasBothVariants);

			var newAncestors = ancestors is not null ? new HashSet<string>(ancestors) : [];
			if (!string.IsNullOrEmpty(variant.BaseName))
				_ = newAncestors.Add(variant.BaseName);

			var nestedCount = variant.Props?.Count ?? 0;
			var isCollapsible = showProperties && nestedCount > 1;
			var defaultExpanded = ComputeDefaultExpanded(depth, nestedCount);

			variants.Add(new ApiUnionVariant
			{
				DisplayName = variant.IsArray && variant.Name.EndsWith("[]") ? variant.Name[..^2] : variant.Name,
				IsArrayVariant = variant.IsArray,
				IsObjectType = variant.IsObject,
				AnchorId = optionId,
				ShowProperties = showProperties && variant.Schema is not null,
				IsCollapsible = isCollapsible,
				DefaultExpanded = defaultExpanded,
				NestedCount = nestedCount,
				UseHidden = options.UseHiddenUntilFound && isCollapsible && !defaultExpanded,
				Properties = showProperties && variant.Schema is not null
					? childBuilder.BuildPropertyList(variant.Schema, optionId, isRequest, depth + 1, newAncestors) ?? new ApiPropertyList([])
					: null
			});
		}

		return new ApiUnionVariants
		{
			Variants = variants,
			ShouldCollapse = variantsToRender.Count > 2,
			ContainerId = $"{prefix}-union-options",
			UseHiddenUntilFound = options.UseHiddenUntilFound
		};
	}

	private sealed record VariantCandidate(
		string Name, string BaseName, bool IsArray, bool IsObject,
		IOpenApiSchema? Schema, IDictionary<string, IOpenApiSchema>? Props);

	private List<VariantCandidate> CollectVariantsToRender(List<UnionOption> unionOptions)
	{
		// Sort: array variants first within each base-name group, preserving group order
		var sortedOptions = unionOptions
			.GroupBy(o => o.Name.EndsWith("[]") ? o.Name[..^2] : o.Name)
			.SelectMany(g => g.OrderByDescending(o => o.Name.EndsWith("[]")))
			.ToList();

		var typeGroups = sortedOptions
			.GroupBy(o => o.Name.EndsWith("[]") ? o.Name[..^2] : o.Name)
			.ToDictionary(g => g.Key, g => g.ToList());

		var variantsToRender = new List<VariantCandidate>();
		foreach (var (baseName, variants) in typeGroups)
		{
			var primaryOption = variants.FirstOrDefault(o => !o.Name.EndsWith("[]"));
			if (primaryOption?.Schema is null)
				primaryOption = variants.First();

			var schemaToRender = primaryOption?.Schema;
			var optionProps = primaryOption?.IsObject == true && schemaToRender is not null
				? _analyzer.GetSchemaProperties(schemaToRender)
				: null;

			var isObject = primaryOption?.IsObject ?? false;
			var hasArrayVariant = variants.Any(v => v.Name.EndsWith("[]"));
			var hasNonArrayVariant = variants.Any(v => !v.Name.EndsWith("[]"));

			if (hasArrayVariant && hasNonArrayVariant)
			{
				variantsToRender.Add(new VariantCandidate($"{baseName}[]", baseName, true, isObject, schemaToRender, optionProps));
				variantsToRender.Add(new VariantCandidate(baseName, baseName, false, isObject, schemaToRender, optionProps));
			}
			else if (hasArrayVariant)
				variantsToRender.Add(new VariantCandidate($"{baseName}[]", baseName, true, isObject, schemaToRender, optionProps));
			else
				variantsToRender.Add(new VariantCandidate(baseName, baseName, false, isObject, schemaToRender, optionProps));
		}

		return variantsToRender;
	}

	private static TypeAnnotation BuildAnnotation(TypeInfo typeInfo, bool hasActualProperties)
	{
		var spans = new List<TypeSpan>();
		var typeName = typeInfo.TypeName ?? "unknown";

		if (typeInfo.IsDictionary)
		{
			AppendDictionarySpans(spans, typeInfo, typeName, hasActualProperties);
			return new TypeAnnotation(spans);
		}

		if (typeInfo.IsArray)
		{
			spans.Add(new TypeSpan("[] ", "array-icon"));
			AppendArrayKeywordSpans(spans, typeInfo, hasActualProperties);
		}
		else
			AppendScalarKeywordSpans(spans, typeInfo, hasActualProperties);

		if (typeInfo.HasLink)
			spans.Add(new TypeSpan("{} ", "object-icon"));
		spans.Add(new TypeSpan(typeName, Title: string.IsNullOrEmpty(typeInfo.SchemaRef) ? null : typeInfo.SchemaRef));
		return new TypeAnnotation(spans);
	}

	private static void AppendArrayKeywordSpans(List<TypeSpan> spans, TypeInfo typeInfo, bool hasActualProperties)
	{
		if (typeInfo.IsValueType && !string.IsNullOrEmpty(typeInfo.ValueTypeBase))
		{
			spans.Add(new TypeSpan(typeInfo.ValueTypeBase, "value-type-keyword"));
			spans.Add(new TypeSpan(" ", Bare: true));
		}
		else if (typeInfo.IsEnum)
			spans.Add(new TypeSpan("enum ", "enum-icon"));
		else if (typeInfo.IsUnion)
			spans.Add(new TypeSpan("union ", "union-icon"));
		else if (typeInfo.IsObject && !string.IsNullOrEmpty(typeInfo.SchemaRef) && (hasActualProperties || typeInfo.HasLink))
			spans.Add(new TypeSpan("{} ", "object-icon"));
	}

	private static void AppendScalarKeywordSpans(List<TypeSpan> spans, TypeInfo typeInfo, bool hasActualProperties)
	{
		if (typeInfo.IsEnum)
			spans.Add(new TypeSpan("enum ", "enum-icon"));
		else if (typeInfo.IsUnion)
			spans.Add(new TypeSpan("union ", "union-icon"));
		else if (typeInfo.IsValueType && !string.IsNullOrEmpty(typeInfo.ValueTypeBase))
		{
			spans.Add(new TypeSpan(typeInfo.ValueTypeBase, "value-type-keyword"));
			spans.Add(new TypeSpan(" ", Bare: true));
		}
		else if (typeInfo.IsObject && !string.IsNullOrEmpty(typeInfo.SchemaRef) && !typeInfo.HasLink && hasActualProperties)
			spans.Add(new TypeSpan("{} ", "object-icon"));
	}

	private static void AppendDictionarySpans(List<TypeSpan> spans, TypeInfo typeInfo, string typeName, bool hasActualProperties)
	{
		var valueTypeName = typeName.StartsWith("string to ") ? typeName["string to ".Length..] : typeName;
		if (string.IsNullOrEmpty(valueTypeName))
			valueTypeName = "unknown";

		spans.Add(new TypeSpan("map ", "map-keyword"));
		spans.Add(new TypeSpan("string"));
		spans.Add(new TypeSpan(" to ", "map-keyword"));
		if (typeInfo.HasLink || hasActualProperties)
			spans.Add(new TypeSpan("{} ", "object-icon"));
		spans.Add(new TypeSpan(valueTypeName));
	}
}
