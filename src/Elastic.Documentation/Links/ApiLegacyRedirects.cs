// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Diagnostics;

namespace Elastic.Documentation.Links;

/// <summary>
/// The redirects from old bump.sh API URLs, from their legacy <c>/docs/api/doc/</c> path to the current one. They mirror <c>modules/elastic-docs-v3/api-root-redirects.json</c> in
/// elastic/docs-infra (#459), which the native viewer resolves exact match first. Keep this table in sync with that file.
/// </summary>
// TODO: Delete this class once the API Explorer migration (elastic/docs-eng-team#436) is finalised and every link index
// has been republished without /doc roots.
public static class ApiLegacyRedirects
{
	private const string ApiSegment = "api/";
	private const string LegacySegment = ApiSegment + "doc/";

	/// <summary>The bump.sh form of an API URL, <c>/docs/api/doc/{key}/...</c>. Content and old link indexes still hold it.</summary>
	public const string LegacyPrefix = "/docs/" + LegacySegment;

	private static readonly IReadOnlyDictionary<string, string> RedirectedRoots = new Dictionary<string, string>
	{
		["serverless"] = "kibana-serverless",
		["elastic-cloud-serverless"] = "cloud-serverless"
	};

	private static readonly IReadOnlySet<string> GoneRoots = new HashSet<string> { "observability-serverless" };

	// Exact page redirects from the same file. Bump.sh served a Servers page for specs that declare no servers,
	// Cloud Billing renamed its Budgets tag, Kibana removed Streams operations from its 9.6 spec, and some bump.sh topic
	// pages are a children: page or the /authentication page in the native viewer.
	private static readonly IReadOnlyDictionary<string, string> RedirectedPages = new Dictionary<string, string>
	{
		["/docs/api/doc/elasticsearch/servers"] = "/docs/api/elasticsearch",
		["/docs/api/doc/elasticsearch/v8/servers"] = "/docs/api/elasticsearch/v8",
		["/docs/api/doc/elasticsearch/v9/servers"] = "/docs/api/elasticsearch/v9",
		["/docs/api/doc/elasticsearch-serverless/servers"] = "/docs/api/elasticsearch-serverless",
		["/docs/api/doc/cloud-enterprise/servers"] = "/docs/api/cloud-enterprise",
		["/docs/api/doc/cloud-enterprise/v3/servers"] = "/docs/api/cloud-enterprise/v3",
		["/docs/api/doc/cloud-billing/group/endpoint-budgets"] = "/docs/api/cloud-billing/group/endpoint-billing-budgets",
		["/docs/api/doc/kibana/topic/topic-kibana-spaces"] = "/docs/api/kibana/kibana-api-overview",
		["/docs/api/doc/kibana/v8/topic/topic-kibana-spaces"] = "/docs/api/kibana/v8/kibana-api-overview",
		["/docs/api/doc/kibana/v9/topic/topic-kibana-spaces"] = "/docs/api/kibana/v9/kibana-api-overview",
		["/docs/api/doc/cloud/topic/topic-authentication"] = "/docs/api/cloud/authentication",
		["/docs/api/doc/cloud-billing/topic/topic-authentication"] = "/docs/api/cloud-billing/authentication",
		["/docs/api/doc/kibana/v9/group/endpoint-significant_events"] = "/docs/api/kibana/v9/group/endpoint-streams",
		["/docs/api/doc/kibana/v9/operation/operation-delete-streams-name-queries-queryid"] = "/docs/api/kibana/v9/group/endpoint-streams",
		["/docs/api/doc/kibana/v9/operation/operation-get-streams-name-queries"] = "/docs/api/kibana/v9/group/endpoint-streams",
		["/docs/api/doc/kibana/v9/operation/operation-get-streams-name-significant-events"] = "/docs/api/kibana/v9/group/endpoint-streams",
		["/docs/api/doc/kibana/v9/operation/operation-post-streams-name-queries-bulk"] = "/docs/api/kibana/v9/group/endpoint-streams",
		["/docs/api/doc/kibana/v9/operation/operation-put-streams-name-queries-queryid"] = "/docs/api/kibana/v9/group/endpoint-streams"
	};

	/// <summary>True for a root with no pages at all, so a link to it has nowhere to land.</summary>
	public static bool IsGone(string path) => GoneRoots.Contains(RootOf(path));

	/// <summary>True when the edge redirects this path to another one.</summary>
	public static bool IsRedirected(string path) => RedirectedPages.ContainsKey(path) || RedirectedRoots.ContainsKey(RootOf(path));

	/// <summary>
	/// The current-form path the edge serves for <paramref name="path"/>, a legacy <c>/docs/api/doc/</c> path: its redirect
	/// target, or the same page under <c>/docs/api/</c>.
	/// </summary>
	public static string Resolve(string path)
	{
		if (RedirectedPages.TryGetValue(path, out var page))
			return page;

		var root = RootOf(path);
		return RedirectedRoots.TryGetValue(root, out var newRoot)
			? string.Concat(ApiLinkMatcher.ApiPrefix, newRoot, path.AsSpan(LegacyPrefix.Length + root.Length))
			: ToCanonical(path);
	}

	private static string RootOf(string path)
	{
		if (!path.StartsWith(LegacyPrefix, StringComparison.Ordinal))
			return string.Empty;
		var rest = path[LegacyPrefix.Length..];
		var slash = rest.IndexOf('/');
		return slash < 0 ? rest : rest[..slash];
	}

	/// <summary>True when <paramref name="path"/>, a normalized path, uses the legacy prefix.</summary>
	public static bool IsLegacy(string path) => path.StartsWith(LegacyPrefix, StringComparison.Ordinal);

	/// <summary>True for <c>/docs/api/doc</c> itself, which names no API.</summary>
	public static bool IsLegacyRoot(string path) => LegacyPrefix.AsSpan(0, LegacyPrefix.Length - 1).SequenceEqual(path);

	/// <summary>
	/// The current form of an API site path such as <c>api/doc/kibana/v9</c> or <c>/docs/api/doc/kibana</c>:
	/// <c>api/kibana/v9</c>, <c>/docs/api/kibana</c>. Any other path comes back unchanged.
	/// </summary>
	public static string ToCanonical(string sitePath)
	{
		var at = ApiSegmentIndex(sitePath, LegacySegment);
		return at < 0 ? sitePath : string.Concat(sitePath.AsSpan(0, at + ApiSegment.Length), sitePath.AsSpan(at + LegacySegment.Length));
	}

	/// <summary>The legacy form of an API site path, for links to a host that still proxies bump.sh.</summary>
	public static string ToLegacy(string sitePath)
	{
		var canonical = ToCanonical(sitePath);
		var at = ApiSegmentIndex(canonical, ApiSegment);
		return at < 0 ? canonical : canonical.Insert(at + ApiSegment.Length, LegacySegment[ApiSegment.Length..]);
	}

	// The segment starts the path or follows /docs/, and something after it names an API.
	private static int ApiSegmentIndex(string sitePath, string segment)
	{
		var start = sitePath.StartsWith("/docs/", StringComparison.Ordinal) ? "/docs/".Length : sitePath.StartsWith('/') ? 1 : 0;
		return sitePath.Length > start + segment.Length && sitePath.AsSpan(start).StartsWith(segment, StringComparison.Ordinal)
			? start
			: -1;
	}
}
