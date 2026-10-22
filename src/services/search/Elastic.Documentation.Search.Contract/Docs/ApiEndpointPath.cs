// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.RegularExpressions;

namespace Elastic.Documentation.Search.Contract;

/// <summary>A contiguous run of the typed endpoint's segments and how many segments it spans.</summary>
public readonly record struct EndpointCandidate(string Term, int Segments);

/// <summary>
/// The rules that turn an OpenAPI path into <see cref="ApiOperationMetadata.Endpoints"/> keys and a typed
/// query into terms to look up against them. Indexing and querying share this class so the two cannot drift.
/// </summary>
public static partial class ApiEndpointPath
{
	// Bounds the quadratic number of runs a long pasted query would otherwise produce.
	private const int MaxQuerySegments = 8;

	/// <summary>
	/// Every way a user could type an endpoint path: lowercased, no leading <c>/</c>, each path also without
	/// its leading and/or trailing <c>{param}</c> segments. Deduped in first-seen order. Braces stay;
	/// <c>endpoint_normalizer</c> removes them in the index. A key needs at least one literal segment.
	/// </summary>
	public static string[] IndexKeys(IEnumerable<string> paths) =>
		paths.SelectMany(Variants).Where(HasLiteralSegment).Distinct(StringComparer.Ordinal).ToArray();

	/// <summary>
	/// Every contiguous run of segments in an already normalized query, longest first then leftmost first.
	/// A typed segment may be a real value standing in for a <c>{param}</c>, and only the runs without it can match.
	/// </summary>
	public static IReadOnlyList<EndpointCandidate> QueryCandidates(string endpoint)
	{
		var segments = endpoint.Split('/', StringSplitOptions.RemoveEmptyEntries).Take(MaxQuerySegments).ToArray();
		var seen = new HashSet<string>(StringComparer.Ordinal);
		var candidates = new List<EndpointCandidate>();
		for (var length = segments.Length; length > 0; length--)
		{
			for (var start = 0; start + length <= segments.Length; start++)
			{
				var term = string.Join('/', segments, start, length);
				if (seen.Add(term))
					candidates.Add(new EndpointCandidate(term, length));
			}
		}
		return candidates;
	}

	/// <summary>Strips a leading HTTP method and <c>/</c>, then lowercases: <c>GET /_search</c> becomes <c>_search</c>.</summary>
	public static string NormalizeQuery(string searchQuery) =>
		HttpMethodPrefix().Replace(searchQuery.Trim(), string.Empty).TrimStart('/').ToLowerInvariant().Trim();

	private static IEnumerable<string> Variants(string path)
	{
		var segments = path.Trim().TrimStart('/').ToLowerInvariant().Split('/', StringSplitOptions.RemoveEmptyEntries);
		var leading = segments.TakeWhile(IsPathParameter).Count();
		var trailing = Enumerable.Reverse(segments).TakeWhile(IsPathParameter).Count();
		var innerLength = Math.Max(0, segments.Length - leading - trailing);

		yield return string.Join('/', segments);
		yield return string.Join('/', segments.Skip(leading));
		yield return string.Join('/', segments.Take(segments.Length - trailing));
		yield return string.Join('/', segments.Skip(leading).Take(innerLength));
	}

	// The normalizer strips braces, so a key of only {param} segments would match plain words like "index".
	private static bool HasLiteralSegment(string key) => key.Split('/').Any(s => s.Length > 0 && !IsPathParameter(s));

	private static bool IsPathParameter(string segment) => segment.StartsWith('{') && segment.EndsWith('}');

	[GeneratedRegex(@"^(GET|POST|PUT|DELETE|HEAD|PATCH)(?:\s+|$)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
	private static partial Regex HttpMethodPrefix();
}
