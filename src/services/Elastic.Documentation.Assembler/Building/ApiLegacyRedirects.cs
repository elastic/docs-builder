// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Diagnostics;

namespace Elastic.Documentation.Assembler.Building;

/// <summary>
/// The redirects from old bump.sh API URLs. They mirror <c>modules/elastic-docs-v3/api-root-redirects.json</c> in
/// elastic/docs-infra (#459), which the native viewer resolves exact match first. Keep this table in sync with that file.
/// </summary>
// TODO: Delete this class once the API Explorer migration (elastic/docs-eng-team#436) is finalised.
public static class ApiLegacyRedirects
{
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
		["/docs/api/doc/elasticsearch/servers"] = "/docs/api/doc/elasticsearch",
		["/docs/api/doc/elasticsearch/v8/servers"] = "/docs/api/doc/elasticsearch/v8",
		["/docs/api/doc/elasticsearch/v9/servers"] = "/docs/api/doc/elasticsearch/v9",
		["/docs/api/doc/elasticsearch-serverless/servers"] = "/docs/api/doc/elasticsearch-serverless",
		["/docs/api/doc/cloud-enterprise/servers"] = "/docs/api/doc/cloud-enterprise",
		["/docs/api/doc/cloud-enterprise/v3/servers"] = "/docs/api/doc/cloud-enterprise/v3",
		["/docs/api/doc/cloud-billing/group/endpoint-budgets"] = "/docs/api/doc/cloud-billing/group/endpoint-billing-budgets",
		["/docs/api/doc/kibana/topic/topic-kibana-spaces"] = "/docs/api/doc/kibana/kibana-api-overview",
		["/docs/api/doc/kibana/v8/topic/topic-kibana-spaces"] = "/docs/api/doc/kibana/v8/kibana-api-overview",
		["/docs/api/doc/kibana/v9/topic/topic-kibana-spaces"] = "/docs/api/doc/kibana/v9/kibana-api-overview",
		["/docs/api/doc/cloud/topic/topic-authentication"] = "/docs/api/doc/cloud/authentication",
		["/docs/api/doc/cloud-billing/topic/topic-authentication"] = "/docs/api/doc/cloud-billing/authentication",
		["/docs/api/doc/kibana/v9/group/endpoint-significant_events"] = "/docs/api/doc/kibana/v9/group/endpoint-streams",
		["/docs/api/doc/kibana/v9/operation/operation-delete-streams-name-queries-queryid"] =
			"/docs/api/doc/kibana/v9/group/endpoint-streams",
		["/docs/api/doc/kibana/v9/operation/operation-get-streams-name-queries"] = "/docs/api/doc/kibana/v9/group/endpoint-streams",
		["/docs/api/doc/kibana/v9/operation/operation-get-streams-name-significant-events"] =
			"/docs/api/doc/kibana/v9/group/endpoint-streams",
		["/docs/api/doc/kibana/v9/operation/operation-post-streams-name-queries-bulk"] = "/docs/api/doc/kibana/v9/group/endpoint-streams",
		["/docs/api/doc/kibana/v9/operation/operation-put-streams-name-queries-queryid"] = "/docs/api/doc/kibana/v9/group/endpoint-streams"
	};

	/// <summary>True for a root with no pages at all, so a link to it has nowhere to land.</summary>
	public static bool IsGone(string path) => GoneRoots.Contains(RootOf(path));

	/// <summary>True when the edge redirects this path to another one.</summary>
	public static bool IsRedirected(string path) => RedirectedPages.ContainsKey(path) || RedirectedRoots.ContainsKey(RootOf(path));

	/// <summary>The path the edge serves for <paramref name="path"/>: its redirect target, or the path itself.</summary>
	public static string Resolve(string path)
	{
		if (RedirectedPages.TryGetValue(path, out var page))
			return page;

		var root = RootOf(path);
		return RedirectedRoots.TryGetValue(root, out var newRoot)
			? string.Concat(ApiLinkMatcher.ApiDocPrefix, newRoot, path.AsSpan(ApiLinkMatcher.ApiDocPrefix.Length + root.Length))
			: path;
	}

	private static string RootOf(string path)
	{
		if (!path.StartsWith(ApiLinkMatcher.ApiDocPrefix, StringComparison.Ordinal))
			return string.Empty;
		var rest = path[ApiLinkMatcher.ApiDocPrefix.Length..];
		var slash = rest.IndexOf('/');
		if (slash >= 0)
			return rest[..slash];

		// A product root also has spec downloads next to it: kibana.json and kibana.yaml.
		var extension = Path.GetExtension(rest);
		return extension is ".json" or ".yaml" ? rest[..^extension.Length] : rest;
	}
}
