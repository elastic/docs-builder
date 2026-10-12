// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.ApiExplorer.Components.PropertyTree;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Model;
using Elastic.ApiExplorer.Supplemental;
using Microsoft.AspNetCore.Html;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Operations;

/// <summary>A query string parameter with its structural display data precomputed.</summary>
public record ApiQueryParameter
{
	/// <summary>The underlying OpenAPI parameter; views read scalar values off it directly.</summary>
	public required IOpenApiParameter Parameter { get; init; }

	public required TypeAnnotation? Type { get; init; }
	public required IReadOnlyList<ConstraintDisplay> Constraints { get; init; }
	public required IReadOnlyList<string> EnumValues { get; init; }
	public required IReadOnlyList<UnionBadge> UnionOptions { get; init; }
	public string UnionLabel { get; init; } = SchemaHelpers.UnionLabel(null);
	public required HtmlString DescriptionHtml { get; init; }
	public required string? DescriptionMarkdown { get; init; }
}

/// <summary>A path parameter with its structural display data precomputed.</summary>
public record ApiPathParameter
{
	public required IOpenApiParameter Parameter { get; init; }
	public required TypeAnnotation? Type { get; init; }
	public required IReadOnlyList<string> EnumValues { get; init; }
	public required IReadOnlyList<UnionBadge> UnionOptions { get; init; }
	public string UnionLabel { get; init; } = SchemaHelpers.UnionLabel(null);
	public required HtmlString DescriptionHtml { get; init; }
	public required string? DescriptionMarkdown { get; init; }

	/// <summary>True when the parameter's route segment can be left out (a sibling route without it exists).</summary>
	public required bool Optional { get; init; }

	public string? Name => Parameter.Name;
	public bool? Deprecated => Parameter.Deprecated;

	/// <summary>OpenAPI forces path parameters required; one that only some routes contain is optional.</summary>
	public required bool Required { get; init; }
	public HtmlString Description => DescriptionHtml;
}

public partial record OperationPageModel
{
	private static ApiPathParameter BuildPathParameter(
		IOpenApiParameter parameter,
		SchemaAnalyzer analyzer,
		ApiPropertyTreeBuilder builder,
		ApiRenderContext context,
		ApiSupplementalDoc? supplemental,
		OperationEndpoint endpoint
	)
	{
		var schema = parameter.Schema;
		var typeInfo = analyzer.GetTypeInfo(schema);
		var description = supplemental?.ParameterOr(parameter.Name ?? "", parameter.Description) ?? parameter.Description;
		var type = schema is not null ? builder.DescribePathParameter(schema) : null;
		var optional = parameter.Name is { } name && endpoint.OptionalPathParameters.Contains(name);
		// The type chip already lists X | X[]; a One of row would repeat those alternatives.
		var alternativesInType = type?.Text.Contains(" | ", StringComparison.Ordinal) == true;
		return new ApiPathParameter
		{
			Parameter = parameter,
			Type = type,
			EnumValues = typeInfo.EnumValues ?? [],
			UnionOptions = alternativesInType ? [] : UnionBadges(typeInfo),
			UnionLabel = SchemaHelpers.UnionLabel(typeInfo.UnionKeyword),
			DescriptionHtml = ApiMarkdown.Render(context, description),
			DescriptionMarkdown = description,
			Optional = optional,
			Required = parameter.Required && !optional
		};
	}

	private static ApiQueryParameter BuildQueryParameter(
		IOpenApiParameter parameter,
		SchemaAnalyzer analyzer,
		ApiPropertyTreeBuilder builder,
		ApiRenderContext context,
		ApiSupplementalDoc? supplemental
	)
	{
		var schema = parameter.Schema;
		var typeInfo = analyzer.GetTypeInfo(schema);
		var description = supplemental?.ParameterOr(parameter.Name ?? "", parameter.Description) ?? parameter.Description;
		return new ApiQueryParameter
		{
			Parameter = parameter,
			Type = schema is not null ? builder.Describe(schema) : null,
			Constraints = schema is not null ? ApiPropertyTreeBuilder.BuildConstraints(schema) : [],
			EnumValues = typeInfo.EnumValues ?? [],
			UnionOptions = UnionBadges(typeInfo),
			UnionLabel = SchemaHelpers.UnionLabel(typeInfo.UnionKeyword),
			DescriptionHtml = ApiMarkdown.Render(context, description),
			DescriptionMarkdown = description
		};
	}

	private static UnionBadge[] UnionBadges(TypeInfo typeInfo)
	{
		var names = (typeInfo.UnionOptions ?? []).Select(o => o.Name);
		return names
			.Where(n => !string.IsNullOrEmpty(n))
			.Select(n => new UnionBadge(n, ApiPropertyTreeBuilder.IsTypeOptionBadge(n)))
			.ToArray();
	}
}
