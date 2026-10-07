// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Model;
using Elastic.ApiExplorer.Operations;
using Microsoft.AspNetCore.Html;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Components.PropertyTree;

/// <summary>
/// Builds the renderable <see cref="ApiProperty"/> tree for a schema, moving every structural
/// decision (recursion, unions, dictionaries, collapse state) out of the views. The tree is built
/// eagerly; recursion detection prunes descent exactly where rendering previously stopped.
/// </summary>
/// <summary>Position of one level of the property tree walk: anchor prefix, depth and recursion ancestry.</summary>
public sealed record PropertyTreeScope
{
	public required string Prefix { get; init; }
	public bool IsRequest { get; init; }
	public int Depth { get; init; }
	public IReadOnlySet<string>? Ancestors { get; init; }
	public IReadOnlyDictionary<string, string>? DescriptionOverrides { get; init; }

	/// <summary>The variant or row these properties belong to, used to say where a shared listing sits.</summary>
	public string? Owner { get; init; }

	/// <summary>Overrides the schema's own required set at the top level; never inherited by children.</summary>
	public ISet<string>? RequiredProperties { get; init; }
}

public class ApiPropertyTreeBuilder(
	OpenApiDocument document,
	PropertyDisplayOptions options,
	string? currentPageType = null,
	PageShapes? pageShapes = null
)
{
	private readonly PageShapes _shapes = pageShapes ?? new();
	private readonly SchemaAnalyzer _analyzer = new(document, currentPageType, options.SchemaResolveCache);

	/// <summary>One renderable property before its display fields are derived.</summary>
	private sealed record PropertyRow(
		string Name,
		IOpenApiSchema Schema,
		TypeInfo TypeInfo,
		string AnchorId,
		bool IsRequired,
		bool IsLast,
		bool IsRecursive,
		string? ShapeKey = null,
		RepeatedShape? Repeats = null
	);

	/// <summary>Builds the property rows for a schema; null when it has no renderable properties.</summary>
	public ApiPropertyList? BuildPropertyList(IOpenApiSchema? schema, PropertyTreeScope scope)
	{
		var properties = _analyzer.GetSchemaProperties(schema);
		if (properties is null || properties.Count == 0)
			return null;

		var requiredProps = scope.RequiredProperties ?? schema?.Required ?? new HashSet<string>();
		var propArray = properties.ToArray();
		var items = new List<ApiProperty>(propArray.Length);
		for (var i = 0; i < propArray.Length; i++)
		{
			var (name, propSchema) = propArray[i];
			if (propSchema is null)
				continue;

			var typeInfo = _analyzer.GetTypeInfo(propSchema);
			var shapeKey = ShapeKey(typeInfo);
			var (repeats, repeatsAncestor) = _shapes.Find(shapeKey);
			// The synthetic map row reads "<string>"; its anchor avoids a real property named "string" on the same list.
			var anchorName = name == DictionaryKeyName ? properties.ContainsKey("string") ? "string-map" : "string" : name;
			var propId = string.IsNullOrEmpty(scope.Prefix) ? anchorName : $"{scope.Prefix}-{anchorName}";
			var row = new PropertyRow(
				name,
				propSchema,
				typeInfo,
				propId,
				IsRequired: requiredProps.Contains(name),
				IsLast: i == propArray.Length - 1,
				IsRecursive: repeatsAncestor || DetectRecursion(propSchema, typeInfo, scope.Ancestors),
				ShapeKey: shapeKey,
				Repeats: repeats
			);
			items.Add(BuildProperty(row, scope));
		}

		return new ApiPropertyList(items);
	}

	/// <summary>Builds the expanded variants for a top-level oneOf/anyOf union (schema pages).</summary>
	public ApiUnionVariants? BuildUnionVariantsForSchemas(
		IList<IOpenApiSchema> unionSchemas,
		PropertyTreeScope scope,
		OpenApiDiscriminator? discriminator = null
	)
	{
		var unionOptions = unionSchemas
			.Where(s => s is not null)
			.Select(s =>
			{
				var info = _analyzer.GetTypeInfo(s);
				return new UnionOption(info.TypeName, info.SchemaRef, info.IsObject, s, info.IsArray);
			})
			.ToList();
		return BuildUnionVariants(unionOptions, scope, discriminator);
	}

	/// <summary>The display form (icons, keywords, name) of a schema's type.</summary>
	public TypeAnnotation Describe(IOpenApiSchema? schema)
	{
		var typeInfo = _analyzer.GetTypeInfo(schema);
		var annotation = BuildAnnotation(typeInfo, HasActualProperties(schema));
		return schema is null ? annotation : WithConstraints(annotation, BuildConstraints(schema));
	}

	/// <summary>
	/// Path-parameter type chip. A <c>$ref</c> to <c>X | X[]</c> is described from the resolved union
	/// so both alternatives show. <see cref="Describe"/> still returns the wrapper name, which query
	/// and body rows keep.
	/// </summary>
	public TypeAnnotation DescribePathParameter(IOpenApiSchema? schema)
	{
		var resolved = _analyzer.ResolveSchema(schema);
		if (ReferenceEquals(resolved, schema) || resolved is null)
			return Describe(schema);

		var (isSimpleArrayUnion, _) = DetectSimpleArrayUnion(_analyzer.GetTypeInfo(resolved));
		return isSimpleArrayUnion ? Describe(resolved) : Describe(schema);
	}

	/// <summary>Validation constraint labels for a schema; empty when it declares none.</summary>
	public static IReadOnlyList<ConstraintDisplay> BuildConstraints(IOpenApiSchema schema)
	{
		var constraints = new List<ConstraintDisplay>();

		if (schema.MinLength.HasValue)
			constraints.Add(new ConstraintDisplay($"min: {schema.MinLength.Value}"));
		if (schema.MaxLength.HasValue)
			constraints.Add(new ConstraintDisplay($"max: {schema.MaxLength.Value}"));
		if (!string.IsNullOrEmpty(schema.Minimum))
			constraints.Add(new ConstraintDisplay($"min: {schema.Minimum}"));
		if (!string.IsNullOrEmpty(schema.Maximum))
			constraints.Add(new ConstraintDisplay($"max: {schema.Maximum}"));
		if (schema.MinItems.HasValue)
			constraints.Add(new ConstraintDisplay($"min: {schema.MinItems.Value}"));
		if (schema.MaxItems.HasValue)
			constraints.Add(new ConstraintDisplay($"max: {schema.MaxItems.Value}"));
		if (!string.IsNullOrEmpty(schema.ExclusiveMinimum))
			constraints.Add(new ConstraintDisplay($"> {schema.ExclusiveMinimum}"));
		if (!string.IsNullOrEmpty(schema.ExclusiveMaximum))
			constraints.Add(new ConstraintDisplay($"< {schema.ExclusiveMaximum}"));
		if (schema.UniqueItems == true)
			constraints.Add(new ConstraintDisplay("unique"));
		if (schema.MultipleOf.HasValue)
			constraints.Add(new ConstraintDisplay($"× {schema.MultipleOf.Value}"));
		if (!string.IsNullOrEmpty(schema.Pattern))
			constraints.Add(new ConstraintDisplay($"pattern: {schema.Pattern}"));

		var defaultValue = schema.Default?.ToString();
		if (!string.IsNullOrEmpty(defaultValue))
			constraints.Add(new ConstraintDisplay($"default: {defaultValue}"));

		return constraints;
	}

	private static TypeAnnotation WithConstraints(TypeAnnotation type, IReadOnlyList<ConstraintDisplay> constraints)
	{
		if (constraints.Count == 0)
			return type;

		var spans = new List<TypeSpan>(type.Spans.Count + (constraints.Count * 2));
		spans.AddRange(type.Spans);
		foreach (var constraint in constraints)
		{
			spans.Add(new TypeSpan(" · ", Bare: true));
			var label = constraint.Code is null ? constraint.Text : $"{constraint.Text}{constraint.Code}";
			spans.Add(new TypeSpan(label, SchemaHelpers.ConstraintCssClass));
		}

		return new TypeAnnotation(spans);
	}

	private bool HasActualProperties(IOpenApiSchema? schema) => _analyzer.GetSchemaProperties(schema)?.Count > 0;

	private (HtmlString Html, string? Markdown) RenderDescription(string name, string? specDescription, PropertyTreeScope scope)
	{
		// ponytail: match property Name only. Nested paths if authors need them.
		var description = scope.IsRequest
			&& scope.DescriptionOverrides is { Count: > 0 }
			&& scope.DescriptionOverrides.TryGetValue(name, out var overrideText) ? overrideText : specDescription;
		if (string.IsNullOrWhiteSpace(description))
			return (HtmlString.Empty, null);

		return (options.RenderMarkdown(description), description);
	}

	private ApiProperty BuildProperty(PropertyRow row, PropertyTreeScope scope)
	{
		var (_, propSchema, typeInfo, _, _, _, isRecursive, _, _) = row;
		var expansion = ComputeExpansion(propSchema, typeInfo, scope.Depth, isRecursive);
		var (descriptionHtml, descriptionMarkdown) = RenderDescription(row.Name, propSchema.Description, scope);
		var typeLink = BuildTypeLink(typeInfo, expansion);

		return new ApiProperty
		{
			Name = row.Name,
			Schema = propSchema,
			AnchorId = row.AnchorId,
			Depth = scope.Depth,
			IsRequired = row.IsRequired,
			IsLast = row.IsLast,
			IsRecursive = isRecursive,
			IsRequest = scope.IsRequest,
			Type = WithTypeLink(
				WithConstraints(BuildAnnotation(typeInfo, HasActualProperties(propSchema)), BuildConstraints(propSchema)),
				typeLink
			),
			DescriptionHtml = descriptionHtml,
			DescriptionMarkdown = descriptionMarkdown,
			ShowDeprecatedBadge = options.ShowDeprecated && propSchema.Deprecated,
			Availability = options.ShowVersionInfo ? AvailabilityBadgeHelper.FromSchema(propSchema, options.VersionsConfiguration) : null,
			ExternalDocs = BuildExternalDocs(propSchema, typeInfo),
			Constraints = BuildConstraints(propSchema),
			EnumValues = typeInfo.EnumValues ?? [],
			Union = typeInfo.IsUnion && row.Repeats is null ? BuildUnionDisplay(propSchema, typeInfo, expansion) : null,
			Repeats = row.Repeats,
			// Type annotation already reads "[] …"; skip the redundant "Array of:" row.
			ArrayItemTypeName = null,
			TypeLink = typeLink,
			AlsoIncludes = BuildAlsoIncludes(typeInfo),
			// A repeat lists nothing itself, so it gets no show/hide toggle.
			IsCollapsible = row.Repeats is null && expansion.IsCollapsible,
			DefaultExpanded = expansion.DefaultExpanded,
			NestedCount = row.Repeats is null ? expansion.NestedCount : 0,
			Children = isRecursive || row.Repeats is not null ? ApiPropertyChildren.None : ListChildren(row, scope, expansion)
		};
	}

	/// <summary>Everything the original view's opening code block derived about a property's expansion.</summary>
	private sealed record Expansion(
		bool HasNestedProps,
		bool HasDictValueProps,
		IOpenApiSchema? ArrayItemSchema,
		bool HasArrayItemProps,
		bool IsSimpleArrayUnion,
		string? SimpleUnionBaseName,
		bool HasUnionOptions,
		bool SimpleUnionHasExpandableProps,
		IOpenApiSchema? SimpleUnionSchema,
		List<UnionOption>? SimpleUnionNestedOptions,
		int NestedCount,
		bool HasChildren,
		bool IsCollapsible,
		bool DefaultExpanded
	);

	private Expansion ComputeExpansion(IOpenApiSchema propSchema, TypeInfo typeInfo, int depth, bool isRecursive)
	{
		var dictHasLinkedValue = typeInfo is { IsDictionary: true, HasLink: true };
		// A union whose properties come only from an allOf expands as variants; one that declares properties itself keeps listing them.
		var hasNestedProps = typeInfo is { IsObject: true, HasLink: false }
			&& (!typeInfo.IsUnion || propSchema.Properties is { Count: > 0 })
			&& depth < options.MaxDepth
			&& HasActualProperties(propSchema);
		var hasDictValueProps = typeInfo is { IsDictionary: true, DictValueSchema: not null }
			&& depth < options.MaxDepth
			&& !dictHasLinkedValue
			&& HasActualProperties(typeInfo.DictValueSchema);
		var arrayItemSchema = typeInfo.IsArray && propSchema.Items is not null ? propSchema.Items : null;
		var hasArrayItemProps = arrayItemSchema is not null
			&& !typeInfo.HasLink
			&& depth < options.MaxDepth
			&& HasActualProperties(arrayItemSchema);

		var (isSimpleArrayUnion, simpleUnionBaseName) = DetectSimpleArrayUnion(typeInfo);

		var hasUnionOptions = typeInfo is { IsUnion: true, UnionOptions: not null }
			&& depth < options.MaxDepth
			&& !isSimpleArrayUnion
			&& typeInfo.UnionOptions.Any(_analyzer.UnionOptionHasProperties);

		var (simpleUnionHasExpandableProps, simpleUnionSchema, simpleUnionNestedOptions) = ResolveSimpleUnionExpansion(
			typeInfo,
			isSimpleArrayUnion,
			simpleUnionBaseName,
			depth
		);

		var nestedCount = 0;
		if (hasNestedProps)
			nestedCount = _analyzer.GetSchemaProperties(propSchema)?.Count ?? 0;
		else if (hasDictValueProps)
			nestedCount = _analyzer.GetSchemaProperties(typeInfo.DictValueSchema)?.Count ?? 0;
		else if (hasArrayItemProps)
			nestedCount = _analyzer.GetSchemaProperties(arrayItemSchema)?.Count ?? 0;
		else if (hasUnionOptions)
			nestedCount = typeInfo.UnionOptions!.Count(_analyzer.UnionOptionHasProperties);
		else if (simpleUnionHasExpandableProps && simpleUnionNestedOptions is { Count: > 0 })
			nestedCount = simpleUnionNestedOptions.Count(_analyzer.UnionOptionHasProperties);
		else if (simpleUnionHasExpandableProps && simpleUnionSchema is not null)
			nestedCount = _analyzer.GetSchemaProperties(simpleUnionSchema)?.Count ?? 0;

		var hasChildren = (hasNestedProps || hasDictValueProps || hasArrayItemProps || hasUnionOptions || simpleUnionHasExpandableProps)
			&& !isRecursive;
		var isCollapsible = hasChildren && nestedCount > 1 && !hasUnionOptions && !hasDictValueProps;
		var defaultExpanded = ComputeDefaultExpanded(depth, nestedCount);

		return new Expansion(
			hasNestedProps,
			hasDictValueProps,
			arrayItemSchema,
			hasArrayItemProps,
			isSimpleArrayUnion,
			simpleUnionBaseName,
			hasUnionOptions,
			simpleUnionHasExpandableProps,
			simpleUnionSchema,
			simpleUnionNestedOptions,
			nestedCount,
			hasChildren,
			isCollapsible,
			defaultExpanded
		);
	}

	private static (bool IsSimpleArrayUnion, string? BaseName) DetectSimpleArrayUnion(TypeInfo typeInfo)
	{
		if (typeInfo is not { IsUnion: true, UnionOptions.Count: > 0 })
			return (false, null);

		var distinctOptions = typeInfo.UnionOptions.DistinctBy(o => o.Name).ToArray();
		if (distinctOptions.Length != 2)
			return (false, null);

		var baseNames = distinctOptions.Select(o => o.BaseName).Distinct().ToArray();
		if (baseNames.Length == 1 && !string.IsNullOrEmpty(baseNames[0]))
			return (true, baseNames[0]);
		return (false, null);
	}

	private (bool Expandable, IOpenApiSchema? Schema, List<UnionOption>? NestedOptions) ResolveSimpleUnionExpansion(
		TypeInfo typeInfo,
		bool isSimpleArrayUnion,
		string? simpleUnionBaseName,
		int depth
	)
	{
		if (!isSimpleArrayUnion || string.IsNullOrEmpty(simpleUnionBaseName) || depth >= options.MaxDepth)
			return (false, null, null);

		var baseOption = typeInfo.UnionOptions!.FirstOrDefault(o => o.Name == simpleUnionBaseName);
		if (baseOption?.Schema is null)
			return (false, null, null);

		var baseTypeInfo = _analyzer.GetTypeInfo(baseOption.Schema);
		if (baseTypeInfo.HasLink || !_analyzer.UnionOptionHasProperties(baseOption))
			return (false, null, null);

		var directProps = _analyzer.GetSchemaProperties(baseOption.Schema);
		var nestedOptions = directProps is null or { Count: 0 } ? _analyzer.GetNestedUnionOptions(baseOption.Schema) : null;
		return (true, baseOption.Schema, nestedOptions);
	}

	private bool ComputeDefaultExpanded(int depth, int nestedCount) =>
		options.CollapseMode == CollapseMode.DepthBased && depth != 0 && nestedCount is > 0 and < 5;

	private ExternalDocLink? BuildExternalDocs(IOpenApiSchema propSchema, TypeInfo typeInfo)
	{
		if (!options.ShowExternalDocs || propSchema.ExternalDocs?.Url is null || typeInfo.HasLink)
			return null;
		var url = propSchema.ExternalDocs.Url.ToString();
		return new ExternalDocLink(url, IsElasticDocsUrl(url), propSchema.ExternalDocs.Description);
	}

	internal static bool IsElasticDocsUrl(string url) => url.Contains("www.elastic.co/docs") || url.Contains("elastic.co/guide");

	private static TypeAnnotation WithTypeLink(TypeAnnotation type, TypePageLink? typeLink)
	{
		if (typeLink is not { Url: { Length: > 0 } url })
			return type;

		var spans = new List<TypeSpan>(type.Spans);
		for (var i = 0; i < spans.Count; i++)
		{
			var span = spans[i];
			if (span.Bare || span.Text != typeLink.TypeName)
				continue;
			spans[i] = span with { Href = url, CssClass = SchemaHelpers.LinkedCssClass };
		}

		return new TypeAnnotation(spans);
	}

	private IReadOnlyList<TypePageLink> BuildAlsoIncludes(TypeInfo typeInfo) =>
		typeInfo.AlsoIncludes?.Select(
			c => new TypePageLink(c.Name, c.HasLink ? SchemaHelpers.GetContainerPageUrl(options.ApiRootUrl, c.Name) : null)
		).ToArray()
			?? [];

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
			var baseOption = typeInfo.UnionOptions!.FirstOrDefault(o => o.Name == expansion.SimpleUnionBaseName);
			if (baseOption?.Schema is not null && _analyzer.GetTypeInfo(baseOption.Schema).HasLink)
				linkedTypeName = expansion.SimpleUnionBaseName;
		}

		if (string.IsNullOrEmpty(linkedTypeName))
			return null;
		return new TypePageLink(linkedTypeName, SchemaHelpers.GetContainerPageUrl(options.ApiRootUrl, linkedTypeName));
	}

	private UnionDisplay? BuildUnionDisplay(IOpenApiSchema propSchema, TypeInfo typeInfo, Expansion expansion)
	{
		// The "Values:" row already lists the literals; a "One of:" row would only repeat the member types.
		if (typeInfo.EnumValues is { Length: > 0 } && !expansion.HasUnionOptions)
			return null;

		var sortedOptions = (typeInfo.UnionOptions ?? [])
			.DistinctBy(o => o.Name)
			.OrderByDescending(o => o.IsArray)
			.Select(o => o.Name)
			.ToArray();

		if (expansion.IsSimpleArrayUnion)
			return null;

		if (sortedOptions.Length > 0 || expansion.HasUnionOptions)
		{
			var badgeOptions = expansion.HasUnionOptions
				? []
				: sortedOptions.Where(static o => !SchemaHelpers.IsInternalSchemaName(o)).ToArray();
			return new UnionDisplay
			{
				Kind = UnionDisplayKind.Badges,
				Keyword = typeInfo.UnionKeyword,
				DiscriminatorProperty = _analyzer.GetUnionDiscriminator(propSchema)?.PropertyName,
				Badges = badgeOptions.Select(o => new UnionBadge(o, IsTypeOptionBadge(o))).ToArray()
			};
		}

		return null;
	}

	internal static bool IsTypeOptionBadge(string option) =>
		SchemaHelpers.PrimitiveTypeNames.Contains(option)
			|| SchemaHelpers.PrimitiveTypeNames.Contains(option.TrimEnd('[', ']'))
			|| char.IsUpper(option[0])
			|| option.EndsWith("[]");

	private ApiPropertyChildren BuildChildren(PropertyRow row, PropertyTreeScope scope, Expansion expansion)
	{
		var typeInfo = row.TypeInfo;
		var childScope = scope with
		{
			Prefix = row.AnchorId,
			Depth = scope.Depth + 1,
			Ancestors = AugmentAncestors(typeInfo, scope.Ancestors),
			RequiredProperties = null,
			Owner = row.Name
		};
		var useHidden = options.UseHiddenUntilFound && expansion.IsCollapsible && !expansion.DefaultExpanded;

		if (expansion.HasDictValueProps)
			return BuildDictionaryChildren(row, childScope, expansion);

		if (expansion.HasNestedProps)
		{
			return new ApiPropertyChildren
			{
				Kind = ChildKind.PropertyList,
				UseHidden = useHidden,
				Properties = BuildPropertyList(row.Schema, childScope) ?? new ApiPropertyList([])
			};
		}

		if (expansion.HasArrayItemProps)
		{
			return new ApiPropertyChildren
			{
				Kind = ChildKind.PropertyList,
				UseHidden = useHidden,
				Properties = BuildPropertyList(expansion.ArrayItemSchema, childScope) ?? new ApiPropertyList([])
			};
		}

		if (expansion.HasUnionOptions)
		{
			return new ApiPropertyChildren
			{
				Kind = ChildKind.UnionVariants,
				UseHidden = false,
				Variants = BuildUnionVariants(typeInfo.UnionOptions!, childScope, _analyzer.GetUnionDiscriminator(row.Schema))
					?? ApiUnionVariants.Empty
			};
		}

		if (expansion.SimpleUnionHasExpandableProps && expansion.SimpleUnionNestedOptions is { Count: > 0 })
		{
			return new ApiPropertyChildren
			{
				Kind = ChildKind.SimpleUnionVariants,
				UseHidden = useHidden,
				Variants = BuildUnionVariants(expansion.SimpleUnionNestedOptions, childScope, _analyzer.GetUnionDiscriminator(row.Schema))
					?? ApiUnionVariants.Empty
			};
		}

		if (expansion.SimpleUnionHasExpandableProps && expansion.SimpleUnionSchema is not null)
		{
			return new ApiPropertyChildren
			{
				Kind = ChildKind.PropertyList,
				UseHidden = useHidden,
				Properties = BuildPropertyList(expansion.SimpleUnionSchema, childScope) ?? new ApiPropertyList([])
			};
		}

		return ApiPropertyChildren.None;
	}

	private ApiPropertyChildren BuildDictionaryChildren(PropertyRow row, PropertyTreeScope childScope, Expansion expansion)
	{
		var keyAnchorId = $"{row.AnchorId}-string";
		var dictIsCollapsible = expansion.NestedCount > 1;
		var dictDefaultExpanded = ComputeDefaultExpanded(childScope.Depth, expansion.NestedCount);
		return new ApiPropertyChildren
		{
			Kind = ChildKind.Dictionary,
			UseHidden = false,
			Dictionary = new DictionaryChildDisplay
			{
				KeyAnchorId = keyAnchorId,
				Depth = childScope.Depth,
				IsCollapsible = dictIsCollapsible,
				DefaultExpanded = dictDefaultExpanded,
				NestedCount = expansion.NestedCount,
				UseHidden = options.UseHiddenUntilFound && dictIsCollapsible && !dictDefaultExpanded,
				ValueType = Describe(row.TypeInfo.DictValueSchema),
				Properties = BuildPropertyList(
					row.TypeInfo.DictValueSchema,
					childScope with { Prefix = keyAnchorId, Depth = childScope.Depth + 1 }
				)
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

	/// <summary>
	/// Lists a row's children and records the row as the page's first listing of its shape. While the children are built
	/// the shape counts as an ancestor, so a generated spec that inlines a recursive union several levels deep stops at
	/// the first repeat.
	/// </summary>
	private ApiPropertyChildren ListChildren(PropertyRow row, PropertyTreeScope scope, Expansion expansion)
	{
		if (row.ShapeKey is not { } key)
			return BuildChildren(row, scope, expansion);

		var shape = new RepeatedShape(row.Name, row.AnchorId, row.TypeInfo.IsUnion, scope.Owner);
		_shapes.BeginListing(key, shape);
		var children = BuildChildren(row, scope, expansion);
		_shapes.EndListing(key, shape, CountRows(children));
		return children;
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
	/// Each field's name, requiredness and type, and one more level for inline objects. A union-typed field contributes
	/// only its type, so a generated spec that repeats a recursive union level after level still matches its parent.
	/// </summary>
	private string Fingerprint(IOpenApiSchema? schema, int depth)
	{
		var properties = _analyzer.GetSchemaProperties(schema);
		if (properties is null)
			return "";

		var required = (_analyzer.ResolveSchema(schema) ?? schema)?.Required ?? new HashSet<string>();
		return string.Join(",", properties.OrderBy(p => p.Key, StringComparer.Ordinal).Select(p =>
		{
			var type = _analyzer.GetTypeInfo(p.Value);
			var nested = depth > 1 && type is { IsObject: true, IsUnion: false, SchemaRef: null or "" }
				? $"{{{Fingerprint(type.IsArray ? p.Value.Items : p.Value, depth - 1)}}}"
				: "";
			return $"{p.Key}{(required.Contains(p.Key) ? "!" : "")}:{(type.IsArray ? "[]" : "")}{type.TypeName}@{type.SchemaRef}{nested}";
		}));
	}

	private bool DetectRecursion(IOpenApiSchema propSchema, TypeInfo typeInfo, IReadOnlySet<string>? ancestors)
	{
		if (ancestors is null)
			return false;

		if (IsAncestorType(typeInfo.TypeName, ancestors))
			return true;

		if (typeInfo.IsArray && propSchema.Items is not null && IsAncestorType(_analyzer.GetTypeInfo(propSchema.Items).TypeName, ancestors))
			return true;

		if (
			typeInfo is { IsDictionary: true, DictValueSchema: not null }
			&& IsAncestorType(_analyzer.GetTypeInfo(typeInfo.DictValueSchema).TypeName, ancestors)
		)
			return true;

		if (
			typeInfo is { IsUnion: true, UnionOptions: not null }
			&& typeInfo.UnionOptions.Select(option => option.BaseName).Any(baseName => IsAncestorType(baseName, ancestors))
		)
			return true;

		return DetectDirectUnionRecursion(propSchema, ancestors);
	}

	private bool DetectDirectUnionRecursion(IOpenApiSchema propSchema, IReadOnlySet<string> ancestors)
	{
		if (!UnionSchemas.TryGet(propSchema, out _, out var unionSchemas))
			return false;

		foreach (var unionSchema in unionSchemas.Where(s => s is not null))
		{
			var unionTypeInfo = _analyzer.GetTypeInfo(unionSchema);
			var typeName = unionTypeInfo.TypeName;
			var baseName = typeName?.EndsWith("[]") == true ? typeName[..^2] : typeName;
			if (IsAncestorType(baseName, ancestors))
				return true;

			if (
				unionTypeInfo.IsArray
				&& unionSchema.Items is not null
				&& IsAncestorType(_analyzer.GetTypeInfo(unionSchema.Items).TypeName, ancestors)
			)
				return true;
		}

		return false;
	}

	// Primitive names and the "oneOf"/"anyOf" label of an inline union name no schema, so they never mark a recursion.
	private static bool IsAncestorType(string? typeName, IReadOnlySet<string> ancestors) =>
		!string.IsNullOrEmpty(typeName)
			&& !SchemaHelpers.IsPrimitiveTypeName(typeName)
			&& !UnionSchemas.IsKeywordName(typeName)
			&& ancestors.Contains(typeName);

	private const string DictionaryKeyName = "<string>";

	/// <summary>Builds the variants of a union whose options are already classified, so each keeps its name and <c>$ref</c>.</summary>
	public ApiUnionVariants? BuildUnionVariants(
		List<UnionOption> unionOptions,
		PropertyTreeScope scope,
		OpenApiDiscriminator? discriminator = null
	)
	{
		if (unionOptions.Count == 0 || !unionOptions.Any(o => o.IsObject))
			return null;

		var variantsToRender = CollectVariantsToRender(unionOptions);
		if (variantsToRender.Count == 0)
			return null;

		// Children of union variants always show deprecated/version/external-docs regardless of page settings.
		var childBuilder = new ApiPropertyTreeBuilder(
			document,
			options with { ShowDeprecated = true, ShowVersionInfo = true, ShowExternalDocs = true },
			currentPageType,
			_shapes
		);

		var variants = new List<ApiUnionVariant>(variantsToRender.Count);
		var usedIds = new HashSet<string>(StringComparer.Ordinal);
		foreach (var variant in variantsToRender)
		{
			var dictionaryValue = variant.PageUrl is null ? _analyzer.GetExpandableDictionaryValue(variant.Schema) : null;
			var hasProperties = variant.Props is { Count: > 0 } || dictionaryValue is not null;
			var optionId = UniqueId(
				$"{scope.Prefix}-variant-{variant.Name.ToLowerInvariant().Replace(" ", "-").Replace("[]", "-array")}",
				usedIds
			);
			var hasBothVariants = variantsToRender.Count(v => v.BaseName == variant.BaseName) > 1;
			var showProperties = hasProperties && (!variant.IsArray || !hasBothVariants);

			var newAncestors = scope.Ancestors is not null ? new HashSet<string>(scope.Ancestors) : [];
			if (!string.IsNullOrEmpty(variant.BaseName))
				_ = newAncestors.Add(variant.BaseName);

			var nestedCount = (variant.Props?.Count ?? 0) + (dictionaryValue is null ? 0 : 1);
			var isCollapsible = showProperties && nestedCount > 1;
			var defaultExpanded = ComputeDefaultExpanded(scope.Depth, nestedCount);

			// An X[] / X pair describes the same schema twice, so only the plain variant carries the text.
			var description = !variant.IsArray || !hasBothVariants ? FirstParagraph(variant.Schema?.Description) : null;

			variants.Add(new ApiUnionVariant
			{
				DisplayName = variant.Label ?? SchemaHelpers.ReadableSchemaName(variant.BaseName),
				PageUrl = variant.PageUrl,
				IsArrayVariant = variant.IsArray,
				IsObjectType = variant.IsObject,
				AnchorId = optionId,
				ShowProperties = showProperties && variant.Schema is not null,
				IsCollapsible = isCollapsible,
				DefaultExpanded = defaultExpanded,
				NestedCount = nestedCount,
				UseHidden = options.UseHiddenUntilFound && isCollapsible && !defaultExpanded,
				Properties = showProperties && variant.Schema is not null
					? childBuilder.BuildPropertyList(
						dictionaryValue is null ? variant.Schema : WithDictionaryKeyRow(variant.Schema, variant.Props, dictionaryValue),
						scope with
						{
							Prefix = optionId,
							Depth = scope.Depth + 1,
							Ancestors = newAncestors,
							RequiredProperties = null,
							Owner = variant.Label ?? SchemaHelpers.ReadableSchemaName(variant.BaseName)
						}
					) ?? new ApiPropertyList([])
					: null,
				DescriptionHtml = description is null ? HtmlString.Empty : options.RenderMarkdown(description),
				DescriptionMarkdown = description,
				DiscriminatorLabel = variant.IsArray ? null : BuildDiscriminatorLabel(discriminator, variant)
			});
		}

		return new ApiUnionVariants
		{
			Variants = variants,
			ShouldCollapse = variantsToRender.Count > 2,
			ContainerId = $"{scope.Prefix}-union-options",
			UseHiddenUntilFound = options.UseHiddenUntilFound
		};
	}

	/// <summary>
	/// The variant's properties plus a <c>&lt;string&gt;</c> key row for its map values, the way a plain dictionary property
	/// lists them. A pure map has no properties, so it shows the key row alone.
	/// </summary>
	private static OpenApiSchema WithDictionaryKeyRow(
		IOpenApiSchema schema,
		IDictionary<string, IOpenApiSchema>? properties,
		IOpenApiSchema value
	) =>
		new()
		{
			Type = JsonSchemaType.Object,
			Properties = new Dictionary<string, IOpenApiSchema>(properties ?? new Dictionary<string, IOpenApiSchema>())
			{
				[DictionaryKeyName] = value
			},
			Required = schema.Required
		};

	/// <summary>Two variants with the same name (two inline <c>object</c> members) still need distinct anchors.</summary>
	private static string UniqueId(string id, HashSet<string> used)
	{
		var candidate = id;
		for (var n = 2; !used.Add(candidate); n++)
			candidate = $"{id}-{n}";
		return candidate;
	}

	private static string? FirstParagraph(string? description) =>
		description?.Split(["\r\n\r\n", "\n\n"], 2, StringSplitOptions.TrimEntries)[0] is { Length: > 0 } first ? first : null;

	/// <summary>
	/// An explicit <c>mapping</c> entry wins. Otherwise an enum on the variant's discriminator property lists the values that select it.
	/// Otherwise a referenced variant is selected by its schema name, OpenAPI's implicit mapping.
	/// </summary>
	private string? BuildDiscriminatorLabel(OpenApiDiscriminator? discriminator, VariantCandidate variant)
	{
		if (discriminator?.PropertyName is not { Length: > 0 } propertyName)
			return null;

		var mapped = discriminator.Mapping?.FirstOrDefault(
			m => !string.IsNullOrEmpty(variant.Ref) && m.Value.Reference.Id == variant.Ref
		).Key;
		if (!string.IsNullOrEmpty(mapped))
			return $"{propertyName}: {mapped}";

		var property = variant.Props is not null && variant.Props.TryGetValue(propertyName, out var schema) ? schema : null;
		var values = _analyzer.GetEnumValues(property);
		if (values.Count > 0)
			return $"{propertyName}: {string.Join(" | ", values)}";

		// OpenAPI's implicit mapping: a referenced schema with no mapping entry, and no enum on the property, is selected by its own name.
		return string.IsNullOrEmpty(variant.Ref) ? null : $"{propertyName}: {variant.Ref}";
	}

	private sealed record VariantCandidate(
		string Name,
		string BaseName,
		string? Ref,
		bool IsArray,
		bool IsObject,
		IOpenApiSchema? Schema,
		IDictionary<string, IOpenApiSchema>? Props,
		string? PageUrl = null,
		string? Label = null
	);

	/// <summary>
	/// Named options pair up as <c>X</c> and <c>X[]</c> by name. An inline object has no name to pair on, so each one
	/// stays its own variant instead of folding into another <c>object</c>.
	/// </summary>
	private static object VariantGroupKey(UnionOption option) =>
		option is { Ref: null or "", IsObject: true, Schema: { } schema } ? schema : option.BaseName;

	private List<VariantCandidate> CollectVariantsToRender(List<UnionOption> unionOptions)
	{
		// One group per base name, array variant first; groups keep the order their first option appeared in.
		var variantsToRender = new List<VariantCandidate>();
		foreach (var variants in unionOptions.GroupBy(VariantGroupKey).Select(g => g.OrderByDescending(o => o.IsArray).ToList()))
		{
			var primaryOption = variants.FirstOrDefault(o => !o.IsArray);
			if (primaryOption?.Schema is null)
				primaryOption = variants.First();

			var baseName = primaryOption.BaseName;
			var schemaToRender = primaryOption.Schema;
			// An array-only variant describes its items; the array schema itself carries no properties.
			if (primaryOption.IsArray && schemaToRender?.Items is { } items)
				schemaToRender = items;
			// A type with its own page (QueryContainer, …) links there instead of listing its fields again, as property rows do.
			var pageUrl = schemaToRender is not null && _analyzer.GetTypeInfo(schemaToRender).HasLink
				? SchemaHelpers.GetContainerPageUrl(options.ApiRootUrl, baseName)
				: null;
			var optionProps = primaryOption.IsObject && schemaToRender is not null && pageUrl is null
				? _analyzer.GetSchemaProperties(schemaToRender)
				: null;

			VariantCandidate Candidate(bool isArray) =>
				new(
					isArray ? $"{baseName}[]" : baseName,
					baseName,
					primaryOption.Ref,
					isArray,
					primaryOption.IsObject,
					schemaToRender,
					optionProps,
					pageUrl,
					primaryOption.Label
				);

			var hasArrayVariant = variants.Any(v => v.IsArray);
			var hasNonArrayVariant = variants.Any(v => !v.IsArray);
			if (hasArrayVariant)
				variantsToRender.Add(Candidate(isArray: true));
			if (hasNonArrayVariant)
				variantsToRender.Add(Candidate(isArray: false));
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
			AppendArrayPrefix(spans);
			AppendArrayKeywordSpans(spans, typeInfo, hasActualProperties);
			AppendDisplayedTypeName(spans, typeInfo, hasActualProperties);
			return new TypeAnnotation(spans);
		}

		AppendScalarKeywordSpans(spans, typeInfo, hasActualProperties);

		// A union and a multi-type schema (`type: [number, string]`) both read as a formula of their parts.
		if (typeName.Contains(" | ", StringComparison.Ordinal))
		{
			AppendUnionFormulaSpans(spans, typeName);
			return new TypeAnnotation(spans);
		}

		if (typeInfo.HasLink)
			AppendObjectIcon(spans);
		AppendDisplayedTypeName(spans, typeInfo, hasActualProperties);
		return new TypeAnnotation(spans);
	}

	private static void AppendArrayPrefix(List<TypeSpan> spans)
	{
		spans.Add(new TypeSpan("[]", SchemaHelpers.WrapperArrayIconCssClass));
		spans.Add(new TypeSpan(" ", Bare: true));
	}

	private static void AppendObjectIcon(List<TypeSpan> spans)
	{
		spans.Add(new TypeSpan("{}", SchemaHelpers.WrapperObjectIconCssClass));
		spans.Add(new TypeSpan(" ", Bare: true));
	}

	private static void AppendArrayKeywordSpans(List<TypeSpan> spans, TypeInfo typeInfo, bool hasActualProperties)
	{
		if (typeInfo.IsValueType && !string.IsNullOrEmpty(typeInfo.ValueTypeBase))
		{
			spans.Add(new TypeSpan(typeInfo.ValueTypeBase, SchemaHelpers.ValueKeywordCssClass));
			spans.Add(new TypeSpan(" ", Bare: true));
		}
		else if (typeInfo.IsEnum)
			AppendWrapperKeyword(spans, "enum", SchemaHelpers.WrapperEnumCssClass);
		else if (typeInfo.IsUnion)
			AppendWrapperKeyword(spans, "union", SchemaHelpers.WrapperUnionCssClass);
		else if (typeInfo.IsObject && !string.IsNullOrEmpty(typeInfo.SchemaRef) && (hasActualProperties || typeInfo.HasLink))
			AppendObjectIcon(spans);
	}

	private static void AppendScalarKeywordSpans(List<TypeSpan> spans, TypeInfo typeInfo, bool hasActualProperties)
	{
		if (typeInfo.IsEnum)
			AppendWrapperKeyword(spans, "enum", SchemaHelpers.WrapperEnumCssClass);
		else if (typeInfo.IsUnion)
			AppendWrapperKeyword(spans, "union", SchemaHelpers.WrapperUnionCssClass);
		else if (typeInfo.IsValueType && !string.IsNullOrEmpty(typeInfo.ValueTypeBase))
		{
			spans.Add(new TypeSpan(typeInfo.ValueTypeBase, SchemaHelpers.ValueKeywordCssClass));
			spans.Add(new TypeSpan(" ", Bare: true));
		}
		else if (typeInfo.IsObject && !string.IsNullOrEmpty(typeInfo.SchemaRef) && !typeInfo.HasLink && hasActualProperties)
			AppendObjectIcon(spans);
	}

	private static void AppendDictionarySpans(List<TypeSpan> spans, TypeInfo typeInfo, string typeName, bool hasActualProperties)
	{
		var valueTypeName = typeName.StartsWith("string to ") ? typeName["string to ".Length..] : typeName;
		if (string.IsNullOrEmpty(valueTypeName))
			valueTypeName = "unknown";

		spans.Add(new TypeSpan("map", SchemaHelpers.WrapperMapKeywordCssClass));
		spans.Add(new TypeSpan(" ", Bare: true));
		spans.Add(NamedTypeSpan("string", null));
		spans.Add(new TypeSpan(" to ", Bare: true));
		if (typeInfo.HasLink || hasActualProperties)
			AppendObjectIcon(spans);
		AppendInternalOrNamed(spans, valueTypeName, typeInfo.HasLink || hasActualProperties);
	}

	private static void AppendDisplayedTypeName(List<TypeSpan> spans, TypeInfo typeInfo, bool hasActualProperties)
	{
		var typeName = typeInfo.TypeName ?? "unknown";
		if (!SchemaHelpers.IsInternalSchemaName(typeName))
		{
			// "enum" is a keyword marker already appended — inline enums have no distinct type name to show.
			if (typeInfo.IsEnum && typeName == "enum")
				return;
			spans.Add(NamedTypeSpan(typeName, typeInfo.SchemaRef, typeInfo.IsValueType));
			return;
		}

		if (typeInfo.IsEnum || typeInfo.IsUnion || typeInfo.IsValueType || typeInfo.HasLink)
			return;
		if (typeInfo.IsObject && hasActualProperties)
			return;

		spans.Add(new TypeSpan("object", SchemaHelpers.PrimitiveCssClass));
	}

	private static void AppendInternalOrNamed(List<TypeSpan> spans, string typeName, bool labeledAlready)
	{
		if (!SchemaHelpers.IsInternalSchemaName(typeName))
		{
			spans.Add(NamedTypeSpan(typeName, null));
			return;
		}

		if (!labeledAlready)
			spans.Add(new TypeSpan("object", SchemaHelpers.PrimitiveCssClass));
	}

	private static void AppendWrapperKeyword(List<TypeSpan> spans, string keyword, string cssClass)
	{
		spans.Add(new TypeSpan(keyword, cssClass));
		spans.Add(new TypeSpan(" ", Bare: true));
	}

	private static void AppendUnionFormulaSpans(List<TypeSpan> spans, string typeName)
	{
		var parts = typeName.Split(" | ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		for (var i = 0; i < parts.Length; i++)
		{
			if (i > 0)
				spans.Add(new TypeSpan(" | ", Bare: true));
			AppendUnionPartSpans(spans, parts[i]);
		}
	}

	private static void AppendUnionPartSpans(List<TypeSpan> spans, string part)
	{
		if (!part.EndsWith("[]", StringComparison.Ordinal))
		{
			AppendInternalOrNamed(spans, part, labeledAlready: false);
			return;
		}

		AppendArrayPrefix(spans);
		AppendInternalOrNamed(spans, part[..^2], labeledAlready: false);
	}

	private static TypeSpan NamedTypeSpan(string typeName, string? schemaRef, bool isValueType = false)
	{
		var css = isValueType ? SchemaHelpers.ValueCssClass : SchemaHelpers.TypeAtomCssClassOrNull(typeName);
		return new(typeName, CssClass: css, Title: string.IsNullOrEmpty(schemaRef) ? null : schemaRef);
	}
}
