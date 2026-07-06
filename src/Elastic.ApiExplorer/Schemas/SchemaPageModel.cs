// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.ApiExplorer.Schema;

namespace Elastic.ApiExplorer.Schemas;

/// <summary>
/// Everything structural a schema type page renders, precomputed before the view runs.
/// Scalar values (description, enum literals, example) are read off the raw schema in the view.
/// </summary>
public record SchemaPageModel
{
	/// <summary>Dictionary display name for container types that represent maps.</summary>
	public required string? DictionaryTypeName { get; init; }

	public required ExternalDocLink? ExternalDocs { get; init; }
	public required ApiUnionVariants? OneOfVariants { get; init; }
	public required ApiUnionVariants? AnyOfVariants { get; init; }
	public required ApiPropertyList? Properties { get; init; }
	public required TypeAnnotation? AdditionalPropertiesType { get; init; }

	public static SchemaPageModel Create(ApiSchema schema, ApiRenderContext context)
	{
		var openApiSchema = schema.Schema;
		var options = new PropertyDisplayOptions
		{
			RenderMarkdown = markdown => ApiMarkdown.Render(context.MarkdownRenderer, markdown),
			ApiRootUrl = context.CurrentNavigation.NavigationRoot.Url,
			ShowDeprecated = false,
			ShowVersionInfo = false,
			ShowExternalDocs = false,
			UseHiddenUntilFound = false,
			CollapseMode = CollapseMode.DepthBased
		};
		var builder = new ApiPropertyTreeBuilder(context.Model, options, schema.DisplayName);
		var rootAncestors = new HashSet<string> { schema.DisplayName };

		ExternalDocLink? externalDocs = null;
		if (openApiSchema.ExternalDocs?.Url is not null)
		{
			var url = openApiSchema.ExternalDocs.Url.ToString();
			externalDocs = new ExternalDocLink(url, ApiPropertyTreeBuilder.IsElasticDocsUrl(url));
		}

		return new SchemaPageModel
		{
			DictionaryTypeName = schema.DisplayName switch
			{
				"AggregationContainer" => "Dictionary<string, AggregationContainer>",
				"Aggregate" => "Dictionary<string, Aggregate>",
				_ => null
			},
			ExternalDocs = externalDocs,
			OneOfVariants = openApiSchema.OneOf is { Count: > 0 }
				? builder.BuildUnionVariantsForSchemas(openApiSchema.OneOf, "oneof", rootAncestors) ?? ApiUnionVariants.Empty
				: null,
			AnyOfVariants = openApiSchema.AnyOf is { Count: > 0 }
				? builder.BuildUnionVariantsForSchemas(openApiSchema.AnyOf, "anyof", rootAncestors) ?? ApiUnionVariants.Empty
				: null,
			Properties = builder.BuildPropertyList(openApiSchema, new PropertyTreeScope { Prefix = "", Ancestors = rootAncestors }),
			AdditionalPropertiesType = openApiSchema.AdditionalProperties is { } addProps
				? builder.Describe(addProps)
				: null
		};
	}
}
