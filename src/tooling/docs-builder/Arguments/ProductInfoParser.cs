// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections;
using Elastic.Changelog;
using Nullean.Argh.Parsing;

namespace Documentation.Builder.Arguments;

/// <summary>
/// Wrapper for a parsed list of <see cref="ProductArgument"/> entries.
/// Use with <c>[ArgumentParser(typeof(ProductInfoParser))]</c> on command parameters.
/// </summary>
/// <remarks>
/// Input: comma-separated entries, each space-separated as product target lifecycle.
/// Example: elasticsearch 9.2.0 ga, cloud-serverless 2025-08-05 ga
/// </remarks>
public sealed class ProductArgumentList(List<ProductArgument> items) : IReadOnlyList<ProductArgument>
{
	private readonly List<ProductArgument> _items = items;

	public int Count => _items.Count;
	public ProductArgument this[int index] => _items[index];
	public IEnumerator<ProductArgument> GetEnumerator() => _items.GetEnumerator();
	IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();

	public static implicit operator List<ProductArgument>(ProductArgumentList v) => v._items;
}

/// <summary>Parses a comma-separated product list into a <see cref="ProductArgumentList"/>.</summary>
public class ProductInfoParser : IArgumentParser<ProductArgumentList>
{
	public bool TryParse(string raw, out ProductArgumentList result)
	{
		var parsed = new List<ProductArgument>();
		foreach (var entry in raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
		{
			var parts = entry.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
			if (parts.Length == 0)
				continue;
			parsed.Add(new ProductArgument
			{
				Product = parts[0],
				Target = parts.Length > 1 ? parts[1] : null,
				Lifecycle = parts.Length > 2 ? parts[2] : null
			});
		}
		result = new ProductArgumentList(parsed);
		return parsed.Count > 0;
	}
}
