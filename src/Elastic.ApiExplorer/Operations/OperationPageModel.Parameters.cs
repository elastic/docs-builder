// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.ApiExplorer.Components.PropertyTree;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Supplemental;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Operations;

/// <summary>A collapsible section of parameter rows, such as Query parameters; the model for <c>_ParameterListSection</c>.</summary>
/// <param name="MarkdownHeading">The section's heading in the page's markdown copy.</param>
public record ParameterListSection(
	ParameterLocation Location,
	ParamSectionHeader Header,
	string ListId,
	ApiPropertyList Parameters,
	string MarkdownHeading
);

/// <summary>What the rows of every parameter location are built from.</summary>
internal sealed record ParameterRowSource(ApiPropertyTreeBuilder Builder, ApiSupplementalDoc? Supplemental, OperationEndpoint Endpoint);

public partial record OperationPageModel
{
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
	/// body. Anchors read <c>path-{name}</c>, <c>query-{name}</c> or <c>header-{name}</c>.
	/// </summary>
	private static ApiPropertyList BuildParameters(
		IEnumerable<IOpenApiParameter> parameters,
		ParameterLocation location,
		ParameterRowSource source
	)
	{
		var rows = parameters
			.Where(p => p.In == location && !(location == ParameterLocation.Header && IgnoredHeaders.Contains(p.Name ?? "")))
			.Select(
				p => new ParameterRowInput(
					p,
					source.Supplemental?.ParameterOr(p.Name ?? "", p.Description) ?? p.Description,
					RouteWithout(p, source.Endpoint)
				)
			)
			.ToArray();
		return source.Builder.BuildParameterList(
			rows,
			new PropertyTreeScope { Prefix = location.ToString().ToLowerInvariant(), IsRequest = true }
		);
	}

	/// <summary>The route to use without a path parameter that some routes leave out (a sibling route has none); null for any other parameter.</summary>
	private static string? RouteWithout(IOpenApiParameter parameter, OperationEndpoint endpoint) =>
		parameter is { In: ParameterLocation.Path, Name: { } name } && endpoint.OptionalPathParameters.Contains(name)
			? endpoint.ShortestRoute
			: null;
}
