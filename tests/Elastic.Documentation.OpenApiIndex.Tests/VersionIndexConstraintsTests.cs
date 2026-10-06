// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Nullean.Entities.CsCheck;
using RootVersionIndex = System.Collections.Generic.SortedDictionary<string, System.Collections.Generic.SortedDictionary<string, System.Collections.Generic.SortedDictionary<string, Elastic.Documentation.OpenApiIndex.VersionIndexEntry>>>;

namespace Elastic.Documentation.OpenApiIndex.Tests;

public class VersionIndexConstraintsTests
{
	[Test]
	public void Laws() => VersionIndexConstraints.All.Laws(Valid(), CounterExamples);

	private static RootVersionIndex With(params string[] versionKeys)
	{
		var byMajor = new SortedDictionary<string, VersionIndexEntry>(StringComparer.Ordinal);
		foreach (var key in versionKeys)
			byMajor[key] = new VersionIndexEntry { Version = key == "main" ? "main" : $"{key}.0" };
		return new RootVersionIndex(StringComparer.Ordinal)
		{
			["elastic/elasticsearch"] = new(StringComparer.Ordinal) { ["openapi.json"] = byMajor }
		};
	}

	private static RootVersionIndex Valid() => With("main", "8", "9");

	private static IReadOnlyDictionary<string, Func<RootVersionIndex, RootVersionIndex>> CounterExamples =>
		new Dictionary<string, Func<RootVersionIndex, RootVersionIndex>>
		{
			["version-moniker-main-or-major"] = _ => With("main", "8", "next"),
			["version-key-matches-version"] = _ =>
			{
				var index = With("main", "8");
				index["elastic/elasticsearch"]["openapi.json"]["8"] = new VersionIndexEntry { Version = "9.1" };
				return index;
			}
		};
}
