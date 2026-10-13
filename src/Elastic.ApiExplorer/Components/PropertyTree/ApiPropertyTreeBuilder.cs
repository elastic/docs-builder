// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Model;
using Elastic.ApiExplorer.Operations;
using Elastic.Documentation.AppliesTo;
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
	/// <summary>The <c>$ref</c> ids of the object types above this level; a row that refers to one again stops there.</summary>
	public IReadOnlySet<string>? AncestorRefs { get; init; }
	public IReadOnlyDictionary<string, string>? DescriptionOverrides { get; init; }

	/// <summary>The variant or row these properties belong to, used to say where a shared listing sits.</summary>
	public string? Owner { get; init; }

	/// <summary>Overrides the schema's own required set at the top level; never inherited by children.</summary>
	public ISet<string>? RequiredProperties { get; init; }
}

public partial class ApiPropertyTreeBuilder(
	OpenApiDocument document,
	PropertyDisplayOptions options,
	string? currentPageSchemaId = null,
	PageShapes? pageShapes = null
)
{
	// Keep in sync with the max-width of `.schema-type .type-constraint` in api-docs.css.
	private const int MaxInlineConstraintLength = 40;

	private readonly PageShapes _shapes = pageShapes ?? new();

	private readonly SchemaAnalyzer _analyzer = new(document, currentPageSchemaId, options.SchemaResolveCache);

	/// <summary>One renderable property before its display fields are derived.</summary>
	/// <param name="Description">The schema's, or for a parameter row the parameter's.</param>
	/// <param name="AppliesTo">The schema's <c>x-state</c>, or for a parameter row the parameter's when it declares one.</param>
	private sealed record PropertyRow(
		string Name,
		IOpenApiSchema Schema,
		TypeInfo TypeInfo,
		string AnchorId,
		bool IsRequired,
		bool IsLast,
		bool IsRecursive,
		string? Description,
		bool IsDeprecated,
		ApplicableTo? AppliesTo,
		string? ShapeKey = null,
		RepeatedShape? Repeats = null
	);

	/// <summary>Builds the property rows for a schema; null when it has no renderable properties.</summary>
	public ApiPropertyList? BuildPropertyList(IOpenApiSchema? schema, PropertyTreeScope scope)
	{
		var effective = _analyzer.Flatten(schema);
		var properties = effective.Properties;
		if (properties.Count == 0)
			return null;

		var requiredProps = scope.RequiredProperties ?? effective.Required;
		var propArray = properties.ToArray();
		var items = new List<ApiProperty>(propArray.Length);
		for (var i = 0; i < propArray.Length; i++)
		{
			var (name, propSchema) = propArray[i];
			if (propSchema is null)
				continue;

			// The synthetic map row reads "<string>"; its anchor avoids a real property named "string" on the same list.
			var anchorName = name == DictionaryKeyName ? properties.ContainsKey("string") ? "string-map" : "string" : name;
			var row = CreateRow(name, propSchema, anchorName, scope) with
			{
				IsRequired = requiredProps.Contains(name),
				IsLast = i == propArray.Length - 1
			};
			items.Add(BuildProperty(row, scope));
		}

		return new ApiPropertyList(items);
	}

	/// <summary>
	/// Parameters as property rows, so an object or a map lists its fields the way a body property does. Each parameter
	/// gives its row's name, description, requiredness and deprecation; its schema gives the rest.
	/// </summary>
	public ApiPropertyList BuildParameterList(
		IReadOnlyList<(IOpenApiParameter Parameter, string? Description)> parameters,
		PropertyTreeScope scope
	)
	{
		var items = new List<ApiProperty>(parameters.Count);
		for (var i = 0; i < parameters.Count; i++)
		{
			var (parameter, description) = parameters[i];
			var name = parameter.Name ?? "";
			var schema = parameter.Schema ?? new OpenApiSchema();
			var row = CreateRow(name, schema, name, scope) with
			{
				IsRequired = parameter.Required,
				IsLast = i == parameters.Count - 1,
				Description = description,
				IsDeprecated = parameter.Deprecated,
				AppliesTo = XStateApplicability.From(parameter.Extensions) ?? XStateApplicability.From(schema.Extensions)
			};
			items.Add(BuildProperty(row, scope));
		}

		return new ApiPropertyList(items);
	}

	/// <summary>A row for a schema under <paramref name="scope"/>, neither required nor last until the caller says so.</summary>
	private PropertyRow CreateRow(string name, IOpenApiSchema schema, string anchorName, PropertyTreeScope scope)
	{
		var typeInfo = _analyzer.GetTypeInfo(schema);
		var shapeKey = ShapeKey(typeInfo);
		var ancestor = _shapes.Ancestor(shapeKey);
		return new PropertyRow(
			name,
			schema,
			typeInfo,
			string.IsNullOrEmpty(scope.Prefix) ? anchorName : $"{scope.Prefix}-{anchorName}",
			IsRequired: false,
			IsLast: false,
			IsRecursive: ancestor is not null || DetectRecursion(schema, typeInfo, scope.AncestorRefs),
			Description: schema.Description,
			IsDeprecated: schema.Deprecated,
			AppliesTo: XStateApplicability.From(schema.Extensions),
			ShapeKey: shapeKey,
			Repeats: ancestor
		);
	}

	/// <summary>The display form (icons, keywords, name) of a schema's type.</summary>
	public TypeAnnotation Describe(IOpenApiSchema? schema)
	{
		var typeInfo = _analyzer.GetTypeInfo(schema);
		var annotation = WithTypeLink(BuildAnnotation(typeInfo, HasActualProperties(schema)), OwnPageLink(typeInfo));
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

		return DetectSimpleArrayUnion(_analyzer.GetTypeInfo(resolved)) is not null ? Describe(resolved) : Describe(schema);
	}

	/// <summary>Validation constraint labels for a schema, then its default; empty when it declares none.</summary>
	public static IReadOnlyList<ConstraintDisplay> BuildConstraints(IOpenApiSchema schema)
	{
		var constraints = BuildLimits(schema);
		var defaultValue = schema.Default?.ToString();
		if (!string.IsNullOrEmpty(defaultValue))
			constraints.Add(new ConstraintDisplay($"default: {defaultValue}"));

		return constraints;
	}

	/// <summary>The format and validation limits of a schema: what a value must look like, unlike its default.</summary>
	private static List<ConstraintDisplay> BuildLimits(IOpenApiSchema schema)
	{
		var constraints = new List<ConstraintDisplay>();

		// The format refines the type, so it reads first: string · format: date-time. An array's comes from its items.
		if (Format(schema) is { } format)
			constraints.Add(new ConstraintDisplay($"format: {format}"));
		if (schema.MinLength.HasValue)
			constraints.Add(new ConstraintDisplay($"min length: {schema.MinLength.Value}"));
		if (schema.MaxLength.HasValue)
			constraints.Add(new ConstraintDisplay($"max length: {schema.MaxLength.Value}"));
		if (!string.IsNullOrEmpty(schema.Minimum))
			constraints.Add(new ConstraintDisplay($"min: {schema.Minimum}"));
		if (!string.IsNullOrEmpty(schema.Maximum))
			constraints.Add(new ConstraintDisplay($"max: {schema.Maximum}"));
		if (schema.MinItems.HasValue)
			constraints.Add(new ConstraintDisplay($"min items: {schema.MinItems.Value}"));
		if (schema.MaxItems.HasValue)
			constraints.Add(new ConstraintDisplay($"max items: {schema.MaxItems.Value}"));
		if (!string.IsNullOrEmpty(schema.ExclusiveMinimum))
			constraints.Add(new ConstraintDisplay($"> {schema.ExclusiveMinimum}"));
		if (!string.IsNullOrEmpty(schema.ExclusiveMaximum))
			constraints.Add(new ConstraintDisplay($"< {schema.ExclusiveMaximum}"));
		if (schema.MinProperties.HasValue)
			constraints.Add(new ConstraintDisplay($"min properties: {schema.MinProperties.Value}"));
		if (schema.MaxProperties.HasValue)
			constraints.Add(new ConstraintDisplay($"max properties: {schema.MaxProperties.Value}"));
		if (schema.UniqueItems == true)
			constraints.Add(new ConstraintDisplay("unique"));
		if (schema.MultipleOf.HasValue)
			constraints.Add(new ConstraintDisplay($"× {schema.MultipleOf.Value}"));
		if (!string.IsNullOrEmpty(schema.Pattern))
			constraints.Add(new ConstraintDisplay($"pattern: {schema.Pattern}"));

		return constraints;
	}

	private static string? Format(IOpenApiSchema schema)
	{
		if (!string.IsNullOrWhiteSpace(schema.Format))
			return schema.Format;
		return schema.Type?.HasFlag(JsonSchemaType.Array) == true && !string.IsNullOrWhiteSpace(schema.Items?.Format)
			? schema.Items.Format
			: null;
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
			var title = label.Length > MaxInlineConstraintLength ? label : null;
			spans.Add(new TypeSpan(label, SchemaHelpers.ConstraintCssClass, title));
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
		var (propSchema, typeInfo, isRecursive) = (row.Schema, row.TypeInfo, row.IsRecursive);
		var expansion = ComputeExpansion(propSchema, typeInfo, scope.Depth, isRecursive);
		var (descriptionHtml, descriptionMarkdown) = RenderDescription(row.Name, row.Description, scope);
		var typeLink = BuildTypeLink(typeInfo, expansion);
		var (children, repeats) = isRecursive || row.Repeats is not null
			? (ApiPropertyChildren.None, row.Repeats)
			: ListChildren(row, scope, expansion);

		return new ApiProperty
		{
			Name = row.Name,
			Schema = propSchema,
			AnchorId = row.AnchorId,
			Depth = scope.Depth,
			IsRequired = row.IsRequired,
			IsNullable = _analyzer.IsNullable(propSchema),
			IsLast = row.IsLast,
			IsRecursive = isRecursive,
			IsRequest = scope.IsRequest,
			Type = WithTypeLink(
				WithConstraints(BuildAnnotation(typeInfo, HasActualProperties(propSchema)), BuildConstraints(propSchema)),
				typeLink
			),
			DescriptionHtml = descriptionHtml,
			DescriptionMarkdown = descriptionMarkdown,
			ShowDeprecatedBadge = options.ShowDeprecated && row.IsDeprecated,
			Availability = options.ShowVersionInfo ? AvailabilityBadge.Create(row.AppliesTo, options.VersionsConfiguration) : null,
			ExternalDocs = BuildExternalDocs(propSchema, typeInfo),
			Constraints = BuildConstraints(propSchema),
			// A union that mixes literals with objects lists the literals in its "One of:" row instead.
			EnumValues = MixesLiteralsWithObjects(typeInfo, expansion) ? [] : typeInfo.EnumValues ?? [],
			// Kept on a repeat too, so it hashes like the row it repeats; the views show "Same … as" in its place.
			Union = typeInfo.IsUnion ? BuildUnionDisplay(propSchema, typeInfo, expansion) : null,
			Repeats = repeats,
			// Type annotation already reads "…[]"; skip the redundant "Array of:" row.
			ArrayItemTypeName = null,
			TypeLink = typeLink,
			AlsoIncludes = BuildAlsoIncludes(typeInfo),
			Requires = DescribeRequiredAlternatives(propSchema),
			SingleOrArray = DescribeSingleOrArray(typeInfo, expansion),
			// A repeat lists nothing itself, so it gets no show/hide toggle.
			IsCollapsible = repeats is null && expansion.IsCollapsible,
			DefaultExpanded = expansion.DefaultExpanded,
			NestedCount = repeats is null ? expansion.NestedCount : 0,
			Children = children
		};
	}

	/// <summary>The "Requires … of:" row of an object whose <c>oneOf</c>/<c>anyOf</c> only lists <c>required</c> sets.</summary>
	public RequiredAlternatives? DescribeRequiredAlternatives(IOpenApiSchema schema) =>
		UnionSchemas.TryGetRequiredAlternatives(_analyzer.ResolveSchema(schema), out var keyword, out var alternatives)
			? new RequiredAlternatives(
				keyword == UnionKeyword.AnyOf ? "Requires at least one of:" : "Requires exactly one of:",
				alternatives.Select(static fields => string.Join(" + ", fields)).ToArray()
			)
			: null;

	// Object rows start collapsed. The request body root is the property list, so its fields stay visible.
	private static bool ComputeDefaultExpanded() => false;

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
			if (span.Bare || span.SchemaId is null || span.SchemaId != typeLink.SchemaId)
				continue;
			spans[i] = span with { Href = url, CssClass = SchemaHelpers.LinkedCssClass };
		}

		return new TypeAnnotation(spans);
	}

	/// <summary>The merged-in types with a page of their own; any other one has only a code name, and its fields are listed already.</summary>
	private IReadOnlyList<TypePageLink> BuildAlsoIncludes(TypeInfo typeInfo) =>
		typeInfo.AlsoIncludes?.Where(static c => TypePages.TryGet(c.LinkedSchemaId, out _))
			.Select(c => new TypePageLink(PageTitle(c.LinkedSchemaId), PageUrl(c.LinkedSchemaId), c.LinkedSchemaId))
			.ToArray()
			?? [];

	private static string PageTitle(string? schemaId) => TypePages.TryGet(schemaId, out var page) ? page.Title : "";

	/// <summary>The link a row's type gets: its own page, or for an <c>X | X[]</c> union, the page of <c>X</c>.</summary>
	private TypePageLink? BuildTypeLink(TypeInfo typeInfo, Expansion expansion)
	{
		if (OwnPageLink(typeInfo) is { } own)
			return own;
		if (
			expansion.ArrayUnion is not { BaseName: var baseName }
			|| typeInfo.UnionOptions!.FirstOrDefault(o => o.Name == baseName)?.Schema is not { } baseSchema
		)
			return null;
		var linkedSchemaId = _analyzer.GetTypeInfo(baseSchema).LinkedSchemaId;
		return PageUrl(linkedSchemaId) is { } url ? new TypePageLink(PageTitle(linkedSchemaId), url, linkedSchemaId) : null;
	}

	/// <summary>The link to the page of a type documented on its own (for a map, its value's), named by the page's title.</summary>
	private TypePageLink? OwnPageLink(TypeInfo typeInfo) =>
		PageUrl(typeInfo.LinkedSchemaId) is { } url
			? new TypePageLink(PageTitle(typeInfo.LinkedSchemaId), url, typeInfo.LinkedSchemaId)
			: null;

	/// <summary>The page of a type documented on its own (see <see cref="TypePages"/>); null for any other type.</summary>
	private string? PageUrl(string? linkedSchemaId) => linkedSchemaId is null ? null : TypePages.Url(options.ApiRootUrl, linkedSchemaId);

	/// <summary>
	/// <c>X</c> when an <c>X | X[]</c> row lists <c>X</c>'s fields and its type does not read <c>X | X[]</c>. Such a row
	/// hides its options line; a row that lists <c>X</c>'s variants names both shapes in the list's label instead.
	/// </summary>
	private SingleOrArray? DescribeSingleOrArray(TypeInfo typeInfo, Expansion expansion)
	{
		if (expansion is not { ArrayUnion: { Expands: true } arrayUnion, Plan: ChildPlan.Properties } || typeInfo.IsFormula)
			return null;

		// A map reads "map string to X", as its type does elsewhere, not a bare "string to X".
		var single = typeInfo.UnionOptions?.FirstOrDefault(o => o is { IsArray: false } && o.BaseName == arrayUnion.BaseName);
		var isMap = single?.Schema is { } schema && _analyzer.GetTypeInfo(schema).IsDictionary;
		var name = single?.ShownBase ?? SchemaHelpers.ReadableSchemaName(arrayUnion.BaseName);
		return isMap ? new SingleOrArray($"map {name}", $"[] map {name}") : new SingleOrArray(name, $"{name}[]");
	}

	private UnionDisplay? BuildUnionDisplay(IOpenApiSchema propSchema, TypeInfo typeInfo, Expansion expansion)
	{
		// The "Values:" row already lists the literals; a "One of:" row would only repeat the member types.
		if (typeInfo.EnumValues is { Length: > 0 } && !expansion.HasUnionOptions)
			return null;

		// The variants listed below the union's own properties carry the label.
		if (expansion.Plan is ChildPlan.Properties { UnionVariants: not null })
			return null;

		// An X | X[] union needs no options row when its type already reads X | X[] or X's fields expand below it.
		// A named one (`union NodeIds`) that does not expand has only this row to name X.
		var namesItsOptions = typeInfo.IsFormula;
		if (expansion.ArrayUnion is { } arrayUnion && (namesItsOptions || arrayUnion.Expands))
			return null;

		var discriminator = _analyzer.GetUnionDiscriminator(propSchema)?.PropertyName;

		var sortedOptions = (typeInfo.UnionOptions ?? [])
			.DistinctBy(o => o.Name)
			.OrderByDescending(o => o.IsArray)
			.Select(o => (Text: o.Shown, Url: PageUrl(_analyzer.GetTypeInfo(o.Schema).LinkedSchemaId)))
			.DistinctBy(static o => o.Text)
			.ToArray();

		// Any other union whose type already reads `number | string` would only repeat it, unless the row has more to say.
		if (
			namesItsOptions
			&& !expansion.HasUnionOptions
			&& discriminator is null
			&& NamesTheSameOptions(typeInfo.Alternatives!, sortedOptions.Select(static o => o.Text))
		)
			return null;

		if (sortedOptions.Length > 0 || expansion.HasUnionOptions)
		{
			var mixed = MixesLiteralsWithObjects(typeInfo, expansion);
			var badges = mixed
				? typeInfo.EnumValues!.Select(static v => new UnionBadge(v, IsTypeOption: false))
				: (expansion.HasUnionOptions ? [] : sortedOptions.Where(static o => !SchemaHelpers.IsInternalSchemaName(o.Text))).Select(
					static o => new UnionBadge(o.Text, IsTypeOptionBadge(o.Text), o.Url)
				);
			return new UnionDisplay
			{
				Kind = UnionDisplayKind.Badges,
				Keyword = typeInfo.UnionKeyword,
				DiscriminatorProperty = discriminator,
				Badges = badges.ToArray(),
				MoreOptions = mixed ? MoreOptionsText(typeInfo) : null
			};
		}

		return null;
	}

	/// <summary>Whether a type formula such as <c>number | string</c> names exactly these options, in any order.</summary>
	private static bool NamesTheSameOptions(IReadOnlyList<TypeAlternative> alternatives, IEnumerable<string> options) =>
		new HashSet<string>(alternatives.Select(static a => a.Text), StringComparer.Ordinal).SetEquals(options);

	/// <summary>A union with literal members as well as object variants, e.g. a sort order: <c>asc</c>, <c>desc</c> or an object.</summary>
	private static bool MixesLiteralsWithObjects(TypeInfo typeInfo, Expansion expansion) =>
		typeInfo is { IsUnion: true, EnumValues.Length: > 0 } && expansion.HasUnionOptions;

	/// <summary>
	/// What the variant list below a mixed union holds besides the literals. It lists every other option, so a
	/// primitive member such as <c>integer</c> makes it more than objects.
	/// </summary>
	private static string MoreOptionsText(TypeInfo typeInfo) =>
		typeInfo.UnionOptions!.All(static o => o.IsObject) ? "or an object listed below" : "or a type listed below";

	internal static bool IsTypeOptionBadge(string option) =>
		SchemaHelpers.ShapeNames.Contains(option.TrimEnd('[', ']'))
			|| SchemaHelpers.PrimitiveTypeNames.Contains(option)
			|| SchemaHelpers.PrimitiveTypeNames.Contains(option.TrimEnd('[', ']'))
			|| char.IsUpper(option[0])
			|| option.EndsWith("[]");

	private ApiPropertyChildren BuildDictionaryChildren(PropertyRow row, PropertyTreeScope childScope, Expansion expansion)
	{
		var keyAnchorId = $"{row.AnchorId}-string";
		var dictIsCollapsible = expansion.NestedCount > 0;
		var dictDefaultExpanded = ComputeDefaultExpanded();
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
				EnumValues = _analyzer.GetEnumValues(row.TypeInfo.DictValueSchema),
				Properties = BuildPropertyList(
					row.TypeInfo.DictValueSchema,
					childScope with { Prefix = keyAnchorId, Depth = childScope.Depth + 1 }
				)
					?? new ApiPropertyList([])
			}
		};
	}

	/// <summary>The ancestors below a row: these plus the type the row expands, the item or map value type for a collection.</summary>
	private static IReadOnlySet<string> AugmentAncestors(TypeInfo typeInfo, IReadOnlySet<string>? ancestors)
	{
		var newAncestors = ancestors is not null ? new HashSet<string>(ancestors) : [];
		if (typeInfo is { IsObject: true, SchemaRef: { Length: > 0 } reference })
			_ = newAncestors.Add(reference);
		return newAncestors;
	}

	/// <summary>
	/// Whether the row refers back to a type above it: by its own <c>$ref</c>, which an array or a map takes from its item
	/// or value, or by the <c>$ref</c> of a union option.
	/// </summary>
	private bool DetectRecursion(IOpenApiSchema propSchema, TypeInfo typeInfo, IReadOnlySet<string>? ancestors)
	{
		if (ancestors is null || ancestors.Count == 0)
			return false;

		var options = typeInfo.UnionOptions
			?? (UnionSchemas.TryGet(propSchema, out _, out var members) ? _analyzer.GetUnionOptions(members) : []);
		return IsAncestor(typeInfo.SchemaRef, ancestors) || options.Any(o => IsAncestor(o.Ref, ancestors));
	}

	private static bool IsAncestor(string? reference, IReadOnlySet<string> ancestors) =>
		!string.IsNullOrEmpty(reference) && ancestors.Contains(reference);
}
