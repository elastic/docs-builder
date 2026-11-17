// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using Elastic.Documentation.Search.Contract;

namespace Elastic.Documentation.Mcp.Remote.Gateways;

/// <summary>A route as typed, split into lowercase segments with each <c>{param}</c> written as <c>*</c>.</summary>
public sealed record TypedEndpoint(string? Method, string[] Segments);

/// <summary>
/// Decides which spec route a typed endpoint names. The index matches endpoints loosely, by every template a typed path
/// could be an instance of, so the gateway ranks its candidates here.
/// </summary>
public static class ApiRouteMatch
{
	public const int NoMatch = -1;
	private const string Wildcard = "*";

	private static readonly FrozenSet<string> HttpMethods = new[]
	{
		"GET",
		"POST",
		"PUT",
		"DELETE",
		"HEAD",
		"PATCH",
		"OPTIONS"
	}.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

	public static TypedEndpoint Parse(string endpoint)
	{
		var words = endpoint.Split((char[]?)null, 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		var method = words.Length == 2 && HttpMethods.Contains(words[0]) ? words[0].ToUpperInvariant() : null;
		var path = method is null ? endpoint.Trim() : words[1];
		return new TypedEndpoint(method, [.. path.Split('/', StringSplitOptions.RemoveEmptyEntries).Select(NormalizeSegment)]);
	}

	/// <summary>
	/// How many literal segments of <paramref name="route"/> the typed endpoint spells out, or <see cref="NoMatch"/>.
	/// A route parameter accepts any typed segment; a literal needs the same word, its leading underscore optional.
	/// </summary>
	public static int Score(TypedEndpoint typed, ApiRouteEntry route)
	{
		if (typed.Method is { } method && !route.Methods.Contains(method, StringComparer.OrdinalIgnoreCase))
			return NoMatch;

		var routeSegments = route.Path.Split('/', StringSplitOptions.RemoveEmptyEntries);
		if (routeSegments.Length != typed.Segments.Length)
			return NoMatch;

		var literals = 0;
		for (var i = 0; i < routeSegments.Length; i++)
		{
			if (IsParameter(routeSegments[i]))
				continue;
			if (!SameLiteral(routeSegments[i], typed.Segments[i]))
				return NoMatch;
			literals++;
		}
		return literals;
	}

	/// <summary>The best <see cref="Score"/> over every route of <paramref name="api"/>.</summary>
	public static int BestScore(TypedEndpoint typed, ApiOperationMetadata api) =>
		api.Routes.Select(r => Score(typed, r)).DefaultIfEmpty(NoMatch).Max();

	private static bool SameLiteral(string routeSegment, string typedSegment) =>
		typedSegment != Wildcard
			&& string.Equals(routeSegment.TrimStart('_'), typedSegment.TrimStart('_'), StringComparison.OrdinalIgnoreCase);

	private static string NormalizeSegment(string segment) => IsParameter(segment) ? Wildcard : segment.ToLowerInvariant();

	private static bool IsParameter(string segment) => segment.StartsWith('{') && segment.EndsWith('}');
}
