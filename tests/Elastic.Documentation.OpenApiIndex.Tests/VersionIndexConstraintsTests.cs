// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;
using Nullean.Entities.CsCheck;
using RootVersionIndex = System.Collections.Generic.SortedDictionary<string, System.Collections.Generic.SortedDictionary<string, System.Collections.Generic.SortedDictionary<string, Elastic.Documentation.OpenApiIndex.VersionIndexEntry>>>;

namespace Elastic.Documentation.OpenApiIndex.Tests;

public class VersionIndexConstraintsTests
{
	/// <summary>
	/// An index as it is built from the S3 keys: every version moniker is 'main' or a major, and resolves to a version of that major (main to main). The monikers and the
	/// repositories and specs they sit under are the generated ones; the versions follow from the monikers.
	/// </summary>
	private static RootVersionIndex Valid(RootVersionIndex index)
	{
		var result = new RootVersionIndex(StringComparer.Ordinal);
		foreach (var (repo, specs) in index)
		{
			var byRepo = result[repo] = new(StringComparer.Ordinal);
			foreach (var (spec, versions) in specs)
			{
				var bySpec = byRepo[spec] = new SortedDictionary<string, VersionIndexEntry>(StringComparer.Ordinal);
				var i = 0;
				foreach (var entry in versions.Values)
				{
					var key = i++ == 0 ? "main" : (7 + i).ToString(CultureInfo.InvariantCulture);
					bySpec[key] = entry with { Version = key == "main" ? "main" : $"{key}.{entry.Version.Length % 10}" };
				}
			}
		}
		return result;
	}

	[Test]
	[MethodDataSource(nameof(Axes))]
	public void EveryDeclaredAxisHolds(AxisCase axis) => axis.Run();

	public static IEnumerable<Func<AxisCase>> Axes() =>
		AutoTest
			.For(VersionIndexConstraints.All, GenAuto.Of<RootVersionIndex>(AutoMode.Plausible, o => o.Normalize<RootVersionIndex>(Valid)))
			.CounterExamples(CounterExamples)
			.Seed(Elastic.Documentation.Tests.Domain.Props.Seed)
			.Cases
			.Select(c => (Func<AxisCase>)(() => c));

	// each case adds one entry that is wrong in one way, under a repository the generator never names
	private static RootVersionIndex Plus(RootVersionIndex index, string moniker, string version)
	{
		var copy = new RootVersionIndex(index, StringComparer.Ordinal)
		{
			["elastic/extra"] = new(StringComparer.Ordinal)
			{
				["openapi.json"] = new SortedDictionary<string, VersionIndexEntry>(StringComparer.Ordinal)
				{
					[moniker] = new VersionIndexEntry { Version = version }
				}
			}
		};
		return copy;
	}

	private static IReadOnlyDictionary<string, Func<RootVersionIndex, RootVersionIndex>> CounterExamples =>
		new Dictionary<string, Func<RootVersionIndex, RootVersionIndex>>
		{
			["version-moniker-main-or-major"] = i => Plus(i, "next", "next.0"),
			["version-key-matches-version"] = i => Plus(i, "8", "9.1")
		};
}
