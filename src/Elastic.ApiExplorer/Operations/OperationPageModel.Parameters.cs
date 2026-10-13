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

/// <summary>A collapsible section of parameter rows, such as Query parameters; the model for <c>_ParameterListSection</c>.</summary>
/// <param name="MarkdownHeading">The section's heading in the page's markdown copy.</param>
public record ParameterListSection(ParamSectionHeader Header, string ListId, ApiPropertyList Parameters, string MarkdownHeading);

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

	// OpenAPI ignores header parameters with these names: the media types and the security schemes describe them.
	private static readonly HashSet<string> IgnoredHeaders =
	[
		with(StringComparer.OrdinalIgnoreCase),
		"Accept",
		"Content-Type",
		"Authorization"
	];

	/// <summary>
	/// The parameters of one location as property rows: an object or a map lists its fields below the parameter, as in a
	/// body. Anchors read <c>query-{name}</c> or <c>header-{name}</c>.
	/// </summary>
	private static ApiPropertyList BuildParameters(
		IEnumerable<IOpenApiParameter> parameters,
		ParameterLocation location,
		ApiPropertyTreeBuilder builder,
		ApiSupplementalDoc? supplemental
	)
	{
		var described = parameters
			.Where(p => p.In == location && !(location == ParameterLocation.Header && IgnoredHeaders.Contains(p.Name ?? "")))
			.Select(p => (p, supplemental?.ParameterOr(p.Name ?? "", p.Description) ?? p.Description))
			.ToArray();
		return builder.BuildParameterList(described, new PropertyTreeScope { Prefix = location.ToString().ToLowerInvariant() });
	}

	private static UnionBadge[] UnionBadges(TypeInfo typeInfo)
	{
		var names = (typeInfo.UnionOptions ?? []).Select(o => o.Shown).Distinct();
		return names
			.Where(n => !string.IsNullOrEmpty(n))
			.Select(n => new UnionBadge(n, ApiPropertyTreeBuilder.IsTypeOptionBadge(n)))
			.ToArray();
	}
}
