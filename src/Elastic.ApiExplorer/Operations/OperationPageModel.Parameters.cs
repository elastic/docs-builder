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
		var rows = WithSpaceParameter(parameters, location, source.Endpoint)
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
			location == ParameterLocation.Path ? InRouteOrder(rows, source.Endpoint) : rows,
			new PropertyTreeScope { Prefix = location.ToString().ToLowerInvariant(), JsonPath = "", IsRequest = true }
		);
	}

	/// <summary>
	/// Required path parameters first, each group in the order its placeholders appear in the route that holds the most of that
	/// group, the leading route when several tie. The spec merges an operation's own parameters before the path item's, so
	/// declaration order is not route order, and a longer alias route can lack a parameter. The optional Kibana space parameter
	/// never picks a route, so a space variant cannot outrank the leading one. A parameter no route holds sorts first in its group.
	/// </summary>
	internal static ParameterRowInput[] InRouteOrder(ParameterRowInput[] rows, OperationEndpoint endpoint)
	{
		var requiredRoute = RouteHolding(rows.Where(static r => r.IsRequired), endpoint);
		var optionalRoute = RouteHolding(rows.Where(r => !r.IsRequired && r.Parameter.Name != endpoint.SpaceParameter), endpoint);
		return [
			.. rows.OrderBy(static r => !r.IsRequired).ThenBy(
				r => (r.IsRequired ? requiredRoute : optionalRoute).IndexOf(Placeholder(r), StringComparison.Ordinal)
			)
		];
	}

	private static string RouteHolding(IEnumerable<ParameterRowInput> group, OperationEndpoint endpoint)
	{
		var placeholders = group.Select(Placeholder).ToArray();
		return endpoint
			.Rows
			.Select(static r => r.Route)
			.MaxBy(route => placeholders.Count(p => route.Contains(p, StringComparison.Ordinal)))!;
	}

	private static string Placeholder(ParameterRowInput row) => $"{{{row.Parameter.Name}}}";

	/// <summary>
	/// Kibana specs declare the <c>/s/{space_id}</c> route only in the description, so the page lists the parameter itself,
	/// before the other optional ones as it is the route's first segment, unless the spec declares it.
	/// </summary>
	private static IEnumerable<IOpenApiParameter> WithSpaceParameter(
		IEnumerable<IOpenApiParameter> parameters,
		ParameterLocation location,
		OperationEndpoint endpoint
	)
	{
		if (
			location != ParameterLocation.Path
			|| endpoint.SpaceParameter is not { } name
			|| parameters.Any(p => p.In == location && p.Name == name)
		)
			return parameters;

		IOpenApiParameter space = new OpenApiParameter
		{
			Name = name,
			In = ParameterLocation.Path,
			Required = false,
			Description = OperationEndpoint.SpaceParameterDescription,
			Schema = new OpenApiSchema { Type = JsonSchemaType.String }
		};
		return parameters.Prepend(space);
	}

	/// <summary>The route to use without a path parameter that some routes leave out (a sibling route has none); null for any other parameter.</summary>
	private static string? RouteWithout(IOpenApiParameter parameter, OperationEndpoint endpoint) =>
		parameter is { In: ParameterLocation.Path, Name: { } name } && endpoint.OptionalPathParameters.Contains(name)
			? endpoint.ShortestRoute
			: null;
}
