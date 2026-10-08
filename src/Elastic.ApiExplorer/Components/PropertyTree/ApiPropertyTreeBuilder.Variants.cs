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
			// An X[] / X pair describes the same schema twice. Listed one after the other, only the plain variant carries the
			// fields and text; behind chips each variant shows alone, so the array one carries them too.
			var hasBothVariants = variantsToRender.Count(v => v.BaseName == variant.BaseName) > 1;
			var leansOnPlain = variant.IsArray && hasBothVariants && variantsToRender.Count < ApiUnionVariants.ChipThreshold;
			var showProperties = hasProperties && !leansOnPlain;

			var newAncestors = scope.AncestorRefs is not null ? new HashSet<string>(scope.AncestorRefs) : [];
			// An inline map takes its value's $ref, but the value type is listed below under the key row, not above it.
			if (!string.IsNullOrEmpty(variant.Ref) && !_analyzer.GetTypeInfo(variant.Schema).IsDictionary)
				_ = newAncestors.Add(variant.Ref);

			var nestedCount = (variant.Props?.Count ?? 0) + (dictionaryValue is null ? 0 : 1);
			var isCollapsible = showProperties && nestedCount > 0;
			var defaultExpanded = ComputeDefaultExpanded();

			var description = leansOnPlain ? null : FirstParagraph(variant.Schema?.Description);

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
							AncestorRefs = newAncestors,
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

		return new ApiUnionVariants { Variants = variants, ContainerId = $"{scope.Prefix}-union-options" };
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
}
