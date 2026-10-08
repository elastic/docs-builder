// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using Nullean.Entities;

namespace Elastic.Documentation.OpenApiIndex;

/// <summary>The same object keys, as a bucket listing returned them and in another order.</summary>
public sealed record KeyOrders(IReadOnlyList<string> Listed, IReadOnlyList<string> Permuted);

/// <summary>The laws of building the index from a listing: about how the keys arrive, not about the index they build.</summary>
[Constraints]
public static partial class VersionIndexOrderConstraints
{
	private static string Json(IEnumerable<string> keys) =>
		JsonSerializer.Serialize(
			VersionIndexBuilder.Build(keys).Index,
			VersionIndexJsonContext.Default.SortedDictionaryStringSortedDictionaryStringSortedDictionaryStringVersionIndexEntry
		);

	public static Constraint<KeyOrders> BuildIsOrderIndependent { get; } = Constraints<KeyOrders>.Always(
		k => Json(k.Permuted) == Json(k.Listed),
		"The index must be the same whatever order the keys arrive in."
	).Describe("Two rebuilds of one bucket publish the same index, whatever order the listing returned the keys in.");
}
