// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;
using System.Text.Json;
using AwesomeAssertions;
using CsCheck;
using Elastic.Documentation.Tests.Domain;
using RootVersionIndex = System.Collections.Generic.SortedDictionary<string, System.Collections.Generic.SortedDictionary<string, System.Collections.Generic.SortedDictionary<string, Elastic.Documentation.OpenApiIndex.VersionIndexEntry>>>;

namespace Elastic.Documentation.OpenApiIndex.Tests;

/// <summary>Operation and loader laws of the version index, with a fixed seed.</summary>
public class VersionIndexProperties
{
	private static Gen<string> Key { get; } = Gen.Select(
		Gen.OneOfConst("elastic/elasticsearch", "elastic/kibana", "acme/api"),
		Gen.OneOfConst("main", "master", "8.16", "8.17", "9.0", "9.12", "9.3", "x.y", "8.", "", "8.1.2", "-1.2"),
		Gen.OneOfConst("openapi.json", "other.json"),
		(repo, version, file) => $"{repo}/{version}/{file}"
	);

	private static Gen<string[]> Keys { get; } = Gen.Select(
		Key.Array[0, 12],
		Gen.OneOfConst("", "a/b", "a/b/c/d/e", "/", "x//y/z").Array[0, 2],
		(keys, noise) => (string[])[.. keys, .. noise]
	);

	private static string Json(RootVersionIndex index) =>
		JsonSerializer.Serialize(
			index,
			VersionIndexJsonContext.Default.SortedDictionaryStringSortedDictionaryStringSortedDictionaryStringVersionIndexEntry
		);

	private static bool IsValid(string key, out string repo, out string file, out string major, out int minor)
	{
		repo = file = major = "";
		minor = 0;
		var parts = key.Split('/');
		if (parts.Length != 4 || Array.Exists(parts, string.IsNullOrEmpty))
			return false;
		(repo, file) = ($"{parts[0]}/{parts[1]}", parts[3]);
		if (parts[2] is "main" or "master")
		{
			major = "main";
			minor = parts[2] == "main" ? 1 : 0;
			return true;
		}
		var dot = parts[2].IndexOf('.');
		if (dot <= 0 || dot == parts[2].Length - 1)
			return false;
		major = parts[2][..dot];
		return int.TryParse(major, NumberStyles.None, CultureInfo.InvariantCulture, out _)
			&& int.TryParse(parts[2][(dot + 1)..], NumberStyles.None, CultureInfo.InvariantCulture, out minor);
	}

	/// <summary>
	/// Law: building the index from any keys never throws, reports exactly the keys of the wrong shape, and keeps for each repository, file and major the key
	/// of the highest minor.
	/// Consequence if broken: the published version index points an API explorer at an older spec than the bucket holds, or the rebuild dies on a stray object.
	/// </summary>
	[Test]
	public void Build_KeepsTheHighestMinor_AndReportsTheKeysItCannotRead() =>
		Keys.Check(keys =>
		{
			var build = VersionIndexBuilder.Build(keys);

			var valid = keys.Where(k => IsValid(k, out _, out _, out _, out _)).ToArray();
			build.InvalidKeys.Should().Equal(keys.Where(k => !IsValid(k, out _, out _, out _, out _)));

			foreach (var group in valid.Select(k =>
			{
				_ = IsValid(k, out var r, out var f, out var m, out var n);
				return (r, f, m, n, k);
			}).GroupBy(x => (x.r, x.f, x.m)))
			{
				var best = group.Max(x => x.n);
				var allowed = group.Where(x => x.n == best).Select(x => x.k.Split('/')[2]).ToHashSet();
				allowed.Should().Contain(build.Index[group.Key.r][group.Key.f][group.Key.m].Version);
			}
			build
				.Index
				.Values
				.SelectMany(f => f.Values)
				.SelectMany(m => m.Values)
				.Count()
				.Should()
				.Be(
					valid
						.Select(k =>
						{
							_ = IsValid(k, out var r, out var f, out var m, out _);
							return (r, f, m);
						})
						.Distinct()
						.Count()
				);
		});

	/// <summary>
	/// Law: the index is the same whatever order the keys arrive in.
	/// Consequence if broken: two rebuilds of one bucket publish different indexes, and the published one depends on the listing order.
	/// </summary>
	[Test]
	public void Build_IsOrderIndependent() =>
		Keys.SelectMany(
			keys => Gen
				.Int
				.Array[keys.Length]
				.Select(order => (keys, keys.Zip(order).OrderBy(p => p.Second).Select(p => p.First).ToArray()))
		).Check(x =>
		{
			var (keys, permuted) = x;
			Json(VersionIndexBuilder.Build(permuted).Index).Should().Be(Json(VersionIndexBuilder.Build(keys).Index));
		});

	/// <summary>
	/// Law: an index written and loaded again is the index that was written, and a built index breaks no rule.
	/// Consequence if broken: the index the lambda publishes is not the one the API explorer reads.
	/// </summary>
	[Test]
	public void Build_ThenSerialize_ThenLoad_RoundTrips() =>
		Keys.Check(keys =>
		{
			var build = VersionIndexBuilder.Build(keys);

			var loaded = VersionIndexLoader.Load(Json(build.Index));

			loaded.Index.Should().NotBeNull();
			Json(loaded.Index!).Should().Be(Json(build.Index));
			loaded.HasErrors.Should().Be(build.Report.HasErrors);
		});

	/// <summary>
	/// Law: loading is total: truncated, mutated or garbage text is an index or an error, never an exception.
	/// Consequence if broken: a corrupt index.json crashes the API explorer's page instead of reporting that it is unreadable.
	/// </summary>
	[Test]
	public void Load_OfAnyText_ReturnsALoad() =>
		Gen.Select(Keys, Gen.Int[0, 6], Gen.Double[0, 1], Gen.String).Check(x =>
		{
			var (keys, mutation, at, garbage) = x;
			var json = Json(VersionIndexBuilder.Build(keys).Index);
			var index = (int)(at * json.Length);
			var text = mutation switch
			{
				0 => json[..index],
				1 => json.Remove(index == json.Length ? 0 : index, 1),
				2 => json.Insert(index, garbage),
				3 => json.Replace("\"version\"", "\"unknown\"", StringComparison.Ordinal),
				4 => json.Replace("{", "[", StringComparison.Ordinal),
				5 => json.Replace("{", "{\"k\":null,", StringComparison.Ordinal),
				_ => garbage
			};

			var load = VersionIndexLoader.Load(text);

			(load.Index is not null || load.Error is not null).Should().BeTrue();
		});
}
