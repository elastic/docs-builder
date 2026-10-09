// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.ApiExplorer.Components.PropertyTree;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Model;
using Elastic.ApiExplorer.Operations;

namespace Elastic.ApiExplorer.Types;

/// <summary>
/// Everything structural a schema type page renders, precomputed before the view runs.
/// Scalar values (description, example) are read off the raw schema in the view.
/// </summary>
public record SchemaPageModel
{
	/// <summary>Dictionary display name for container types that represent maps.</summary>
	public required string? DictionaryTypeName { get; init; }

	public required ExternalDocLink? ExternalDocs { get; init; }
	/// <summary>The variants of a union type, built by the same rules as a request or response body that is a union.</summary>
	public required ApiUnionVariants? UnionVariants { get; init; }

	/// <summary>Which keyword the union came from; it names the section.</summary>
	public required UnionKeyword UnionKeyword { get; init; }

	/// <summary>The sentence under the section heading; an array of a union says each item is one of the variants.</summary>
	public required string UnionIntro { get; init; }

	/// <summary>Which of the type's fields it needs, when its <c>oneOf</c>/<c>anyOf</c> only lists <c>required</c> sets.</summary>
	public required RequiredAlternatives? Requires { get; init; }

	public required ApiPropertyList? Properties { get; init; }
	public required TypeAnnotation? AdditionalPropertiesType { get; init; }
	public required IReadOnlyList<string> EnumValues { get; init; }

	public static SchemaPageModel Create(ApiSchema schema, ApiRenderContext context)
	{
		var openApiSchema = schema.Schema;
		var options = new PropertyDisplayOptions
		{
			RenderMarkdown = markdown => ApiMarkdown.Render(context, markdown),
			ApiRootUrl = context.CurrentNavigation.NavigationRoot.Url,
			ShowDeprecated = false,
			ShowVersionInfo = false,
			ShowExternalDocs = false,
			UseHiddenUntilFound = false,
			SchemaResolveCache = context.SchemaResolveCache
		};
		var builder = new ApiPropertyTreeBuilder(context.Model, options, schema.SchemaId);
		var analyzer = new SchemaAnalyzer(context.Model, schema.SchemaId, context.SchemaResolveCache);
		var rootAncestors = new HashSet<string> { schema.SchemaId };
		var typeInfo = analyzer.GetTypeInfo(openApiSchema);
		var keyword = typeInfo.UnionKeyword ?? UnionKeyword.OneOf;
		// Variant anchors keep their keyword prefix (oneof-variant-…), so links to them survive.
		var variantScope = new PropertyTreeScope { Prefix = keyword.ToSchemaKeyword().ToLowerInvariant(), AncestorRefs = rootAncestors };
		var built = ApiBodyContent.BuildUnionVariants(openApiSchema, variantScope, analyzer, builder);
		// The section's intro sentence carries the label, so the list does not repeat it.
		var variants = built is null ? null : built with { Label = null };
		var intro = UnionIntroFor(typeInfo, built);
		var listed = ApiBodyContent.ListedProperties(openApiSchema, variants, analyzer);

		ExternalDocLink? externalDocs = null;
		if (openApiSchema.ExternalDocs?.Url is not null)
		{
			var url = openApiSchema.ExternalDocs.Url.ToString();
			externalDocs = new ExternalDocLink(url, ApiPropertyTreeBuilder.IsElasticDocsUrl(url), openApiSchema.ExternalDocs.Description);
		}

		return new SchemaPageModel
		{
			DictionaryTypeName = TypePages.TryGet(schema.SchemaId, out var typePage) ? typePage.MapOf : null,
			ExternalDocs = externalDocs,
			UnionVariants = variants,
			UnionKeyword = keyword,
			UnionIntro = intro,
			Requires = builder.DescribeRequiredAlternatives(openApiSchema),
			Properties = listed is null
				? null
				: builder.BuildPropertyList(listed, new PropertyTreeScope { Prefix = "", AncestorRefs = rootAncestors }),
			AdditionalPropertiesType = openApiSchema.AdditionalProperties is { } addProps ? builder.Describe(addProps) : null,
			EnumValues = analyzer.GetEnumValues(openApiSchema)
		};
	}

	/// <summary>
	/// "This type can be one of the following:", or for an array of a union the body label, "An array; each item is one of:",
	/// since the variants then describe each item rather than the type.
	/// </summary>
	internal static string UnionIntroFor(TypeInfo typeInfo, ApiUnionVariants? variants) =>
		typeInfo.IsArray && variants?.Label is { } arrayLabel
			? arrayLabel
			: SchemaCommonMark.UnionIntro(typeInfo.UnionKeyword ?? UnionKeyword.OneOf);
}
