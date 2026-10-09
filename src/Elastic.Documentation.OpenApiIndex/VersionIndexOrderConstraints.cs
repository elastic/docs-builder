// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using Nullean.Entities;

namespace Elastic.Documentation.OpenApiIndex;

/// <summary>The object keys of a bucket listing, in the order it returned them.</summary>
public sealed record KeyListing(IReadOnlyList<string> Keys);

/// <summary>The laws of building the index from a listing: about how the keys arrive, not about the index they build.</summary>
[Constraints]
public static partial class VersionIndexOrderConstraints
{
	private static string Json(IEnumerable<string> keys) =>
		JsonSerializer.Serialize(
			VersionIndexBuilder.Build(keys).Index,
			VersionIndexJsonContext.Default.SortedDictionaryStringSortedDictionaryStringSortedDictionaryStringVersionIndexEntry
		);

	public static Constraint<KeyListing> BuildIsOrderIndependent { get; } = Constraints<KeyListing>.OrderIndependent(
		k => k.Keys,
		(k, keys) => k with { Keys = keys },
		k => Json(k.Keys),
		message: "The index must be the same whatever order the keys arrive in."
	).Describe("Two rebuilds of one bucket publish the same index, whatever order the listing returned the keys in.");
}
