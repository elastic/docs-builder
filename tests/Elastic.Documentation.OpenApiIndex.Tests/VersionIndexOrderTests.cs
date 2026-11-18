// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using AwesomeAssertions;
using CsCheck;

namespace Elastic.Documentation.OpenApiIndex.Tests;

public class VersionIndexOrderTests
{
	private static readonly Gen<string> Key = Gen.Select(
		Gen.OneOf("elastic/elasticsearch", "elastic/kibana", "acme/api"),
		Gen.OneOf("main", "master", "8.16", "8.17", "9.0", "9.12", "9.3", "x.y", "8.", "", "8.1.2", "-1.2"),
		Gen.OneOf("openapi.json", "other.json"),
		(repo, version, file) => $"{repo}/{version}/{file}"
	);

	private static string Json(IEnumerable<string> keys) =>
		JsonSerializer.Serialize(
			VersionIndexBuilder.Build(keys).Index,
			VersionIndexJsonContext.Default.SortedDictionaryStringSortedDictionaryStringSortedDictionaryStringVersionIndexEntry
		);

	/// <summary>
	/// Law: the built index is the same whatever order the bucket listing returned the keys in.
	/// Consequence if broken: two rebuilds of one bucket publish different indexes, and the published one depends on the listing order.
	/// </summary>
	[Test]
	public void Build_IsOrderIndependent() =>
		Key
			.Array[0, 12]
			.SelectMany(keys => Gen.Shuffle((string[])keys.Clone()).Select(shuffled => (Keys: keys, Shuffled: shuffled)))
			.Sample(x => Json(x.Keys) == Json(x.Shuffled));
}
