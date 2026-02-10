// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using ConsoleAppFramework;
using Elastic.Changelog;

namespace Documentation.Builder.Arguments;

[AttributeUsage(AttributeTargets.Parameter)]
public class ProductInfoParserAttribute : Attribute, IArgumentParser<List<ProductArgument>>
{
	public static bool TryParse(ReadOnlySpan<char> s, out List<ProductArgument> result)
	{
		result = [];

		// Split by comma to get individual product entries
		var productEntries = s.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

		foreach (var entry in productEntries)
		{
			// Split by whitespace to get product, target, lifecycle
			var parts = entry.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

			if (parts.Length == 0)
				continue;

			var productInfo = new ProductArgument
			{
				Product = parts[0],
				Target = parts.Length > 1 ? parts[1] : null,
				Lifecycle = parts.Length > 2 ? parts[2] : null
			};

			result.Add(productInfo);
		}

		return result.Count > 0;
	}
}

