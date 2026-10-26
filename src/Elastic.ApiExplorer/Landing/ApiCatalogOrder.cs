// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.ApiExplorer.Landing;

/// <summary>
/// The order APIs are listed in, shared by the catalog page and the product switcher so the two agree:
/// the core stack, each followed by its Serverless variant, then the Elastic Cloud APIs, then everything
/// else by title.
/// </summary>
internal static class ApiCatalogOrder
{
	public static readonly string[] StackKeys = ["elasticsearch", "kibana", "logstash"];

	public static readonly string[] OrchestrationKeys = ["cloud", "cloud-billing", "cloud-connect", "cloud-enterprise", "cloud-serverless"];

	/// <summary>A variant is listed right after the API it is a variant of, and shown under its card on the catalog.</summary>
	public static readonly IReadOnlyDictionary<string, string> VariantParents = new Dictionary<string, string>(StringComparer.Ordinal)
	{
		["elasticsearch-serverless"] = "elasticsearch",
		["kibana-serverless"] = "kibana"
	};

	private static readonly string[] Pinned =
	[
		.. StackKeys.SelectMany(key => new[] { key }.Concat(VariantParents.Where(v => v.Value == key).Select(v => v.Key))),
		.. OrchestrationKeys
	];

	/// <summary>Position of a pinned API, or <see cref="int.MaxValue"/> when it is ordered by title like the rest.</summary>
	public static int Rank(string key)
	{
		var index = Array.IndexOf(Pinned, key);
		return index >= 0 ? index : int.MaxValue;
	}
}
