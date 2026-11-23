// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Search.Contract;

namespace Elastic.Documentation.Search.ApiReference;

/// <summary>A route as typed: its method, if any, and the path through <see cref="ApiEndpointPath.NormalizeQuery"/>.</summary>
public sealed record TypedEndpoint(string? Method, string Path)
{
	public string[] Segments => Path.Split('/', StringSplitOptions.RemoveEmptyEntries);
}

/// <summary>
/// Decides which spec route a typed endpoint names. The index matches endpoints loosely, by every template a typed path
/// could be an instance of, so the gateway ranks its candidates here.
/// </summary>
public static class ApiRouteMatch
{
	public const int NoMatch = -1;
	private const string AllIndices = "_all";

	/// <summary>A leading word before the path is the method; a word no route serves simply matches nothing.</summary>
	public static TypedEndpoint Parse(string endpoint)
	{
		var words = endpoint.Split((char[]?)null, 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		var method = words.Length == 2 ? words[0].ToUpperInvariant() : null;
		return new TypedEndpoint(method, ApiEndpointPath.NormalizeQuery(words[^1]));
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
		var typedSegments = typed.Segments;
		if (routeSegments.Length != typedSegments.Length)
			return NoMatch;

		var literals = 0;
		for (var i = 0; i < routeSegments.Length; i++)
		{
			if (routeSegments[i].StartsWith('{'))
				continue;
			if (!SameLiteral(routeSegments[i], typedSegments[i]))
				return NoMatch;
			literals++;
		}
		return literals == 0 && routeSegments.Length > 0 && !MayFillOnlyParameters(typed) ? NoMatch : literals;
	}

	/// <summary>
	/// Whether the typed endpoint can name a route made only of parameters, such as <c>/{index}</c>. Index names cannot start
	/// with <c>_</c>, so a typed system route like <c>/_bulk</c> or a typo of one never fills such a route. The index
	/// expression <c>_all</c> is the one exception.
	/// </summary>
	public static bool MayFillOnlyParameters(TypedEndpoint typed) =>
		typed.Segments is { Length: > 0 } segments && !segments.Any(s => s.StartsWith('_') && s != AllIndices);

	/// <summary>The best <see cref="Score"/> over every route of <paramref name="api"/>.</summary>
	public static int BestScore(TypedEndpoint typed, ApiOperationMetadata api) =>
		api.Routes.Select(r => Score(typed, r)).DefaultIfEmpty(NoMatch).Max();

	/// <summary>Every route of <paramref name="api"/> as <c>METHOD /spec/path</c>, verbatim from the spec.</summary>
	public static string[] Endpoints(ApiOperationMetadata api) => [.. api.Routes.SelectMany(r => r.Methods.Select(m => $"{m} {r.Path}"))];

	// NormalizeQuery writes a typed {param} as *, which names no literal.
	private static bool SameLiteral(string routeSegment, string typedSegment) =>
		typedSegment != "*" && string.Equals(routeSegment.TrimStart('_'), typedSegment.TrimStart('_'), StringComparison.OrdinalIgnoreCase);
}
