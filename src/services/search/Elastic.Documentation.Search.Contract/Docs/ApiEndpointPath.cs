// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.RegularExpressions;

namespace Elastic.Documentation.Search.Contract;

/// <summary>Query terms that keep the same number of the typed endpoint's literal segments.</summary>
public sealed record EndpointCandidateGroup(int LiteralSegments, IReadOnlyList<string> Terms);

/// <summary>
/// The rules that turn an OpenAPI path into <see cref="ApiOperationMetadata.Endpoints"/> keys and a typed
/// query into terms to look up against them. Indexing and querying share the <c>*</c> placeholder for a
/// parameter, so a typed value in any position, interior included, matches the template.
/// </summary>
public static partial class ApiEndpointPath
{
	private const string Wildcard = "*";

	// 2^8 terms at most; a long pasted query would otherwise grow the clause count exponentially.
	private const int MaxQuerySegments = 8;

	/// <summary>
	/// Every way a user could type an endpoint path: lowercased, no leading <c>/</c>, each path also without
	/// its leading and/or trailing <c>{param}</c> segments, and every <c>{param}</c> written as <c>*</c>.
	/// Deduped in first-seen order. A key needs at least one literal segment.
	/// </summary>
	public static string[] IndexKeys(IEnumerable<string> paths) =>
		paths.SelectMany(Variants).Where(HasLiteralSegment).Distinct(StringComparer.Ordinal).ToArray();

	/// <summary>
	/// Every template the normalized query could be an instance of: each subset of segments replaced by <c>*</c>,
	/// grouped by how many literal segments remain, most literals first. Groups with no literal are dropped.
	/// </summary>
	public static IReadOnlyList<EndpointCandidateGroup> QueryCandidates(string endpoint)
	{
		var segments = endpoint.Split('/', StringSplitOptions.RemoveEmptyEntries).Take(MaxQuerySegments).ToArray();
		var byLiterals = new SortedDictionary<int, List<string>>(Comparer<int>.Create((a, b) => b.CompareTo(a)));
		var n = segments.Length;
		// Segment i owns bit n-1-i, so a descending mask lists wildcard positions in lexicographic order.
		for (var mask = (1 << n) - 1; mask >= 0; mask--)
		{
			var templated = segments.Select((s, i) => (mask & (1 << (n - 1 - i))) != 0 ? Wildcard : s).ToArray();
			var literals = templated.Count(s => s != Wildcard);
			if (literals == 0)
				continue;
			var terms = byLiterals.TryGetValue(literals, out var existing) ? existing : byLiterals[literals] = [];
			var term = string.Join('/', templated);
			if (!terms.Contains(term))
				terms.Add(term);
		}
		return byLiterals.Select(g => new EndpointCandidateGroup(g.Key, g.Value)).ToList();
	}

	/// <summary>
	/// Strips a leading HTTP method and <c>/</c>, lowercases, and writes each <c>{param}</c> segment as <c>*</c>:
	/// <c>GET /{index}/_search</c> becomes <c>*/_search</c>.
	/// </summary>
	public static string NormalizeQuery(string searchQuery)
	{
		var normalized = HttpMethodPrefix().Replace(searchQuery.Trim(), string.Empty).TrimStart('/').ToLowerInvariant().Trim();
		return string.Join('/', normalized.Split('/').Select(s => IsPathParameter(s) ? Wildcard : s));
	}

	private static IEnumerable<string> Variants(string path)
	{
		var segments = path
			.Trim()
			.TrimStart('/')
			.ToLowerInvariant()
			.Split('/', StringSplitOptions.RemoveEmptyEntries)
			.Select(s => IsPathParameter(s) ? Wildcard : s)
			.ToArray();
		var leading = segments.TakeWhile(s => s == Wildcard).Count();
		var trailing = Enumerable.Reverse(segments).TakeWhile(s => s == Wildcard).Count();
		var innerLength = Math.Max(0, segments.Length - leading - trailing);

		yield return string.Join('/', segments);
		yield return string.Join('/', segments.Skip(leading));
		yield return string.Join('/', segments.Take(segments.Length - trailing));
		yield return string.Join('/', segments.Skip(leading).Take(innerLength));
	}

	private static bool HasLiteralSegment(string key) => key.Split('/').Any(s => s.Length > 0 && s != Wildcard);

	private static bool IsPathParameter(string segment) => segment.StartsWith('{') && segment.EndsWith('}');

	[GeneratedRegex(@"^(GET|POST|PUT|DELETE|HEAD|PATCH)(?:\s+|$)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
	private static partial Regex HttpMethodPrefix();
}
