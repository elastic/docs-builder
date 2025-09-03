// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using Elastic.Documentation.Configuration.Versions;

namespace Elastic.Documentation.Configuration.Builder;

public static class ProductVersionMapper
{
	public static FrozenDictionary<string, IReadOnlyCollection<string>> CreateHistoryVersionMapping(VersionsConfiguration versions, FrozenDictionary<string, IReadOnlyCollection<string>> historyMappings)
	{
		var productIdToLegacyKeyMap = new Dictionary<string, string>();
		foreach (var legacyKey in historyMappings.Keys)
		{
			_ = productIdToLegacyKeyMap.TryAdd(InferProductId(legacyKey), legacyKey);
		}

		var resolvedProducts = new Dictionary<string, IReadOnlyCollection<string>>
		{
			["docs-content"] = historyMappings.GetValueOrDefault(productIdToLegacyKeyMap[InferProductId("docs-content")]) ?? []
		};

		foreach (var id in versions.Products.Keys)
		{
			IReadOnlyCollection<string> legacyVersions = [];
			if (productIdToLegacyKeyMap.TryGetValue(id, out var legacyKey))
			{
				legacyVersions = historyMappings.GetValueOrDefault(legacyKey) ?? [];
			}
			resolvedProducts[id] = legacyVersions;
		}

		return resolvedProducts.ToFrozenDictionary();

		static string InferProductId(string url)
		{
			var parts = url.Trim('/').Split('/');
			if (parts.Length > 1 && parts[0] == "en")
			{
				var potentialProduct = parts[1];
				return potentialProduct switch
				{
					"ecs-logging" when parts.Length > 2 => $"ecs_logging_{parts[2].Replace("-", "_")}",
					"apm" when parts.Length > 3 && parts[2] == "agent" => $"apm_agent_{parts[3].Replace("-", "_")}",
					"apm" when parts.Length > 2 && parts[2] != "agent" => $"apm_{parts[2].Replace("-", "_")}",
					"beats" when parts.Length > 2 => parts[2],
					"elasticsearch" when parts.Length > 2 && parts[2] == "client" => parts.Length > 3
						? parts[3]
						: "elasticsearch-client",
					_ => potentialProduct
				};
			}
			return url.Equals("docs-content", StringComparison.OrdinalIgnoreCase) ? "elastic-stack" : url;
		}
	}
}
