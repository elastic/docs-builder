// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;
using Nullean.Entities;

namespace Elastic.Documentation.OpenApiIndex;

/// <summary>Every rule a <see cref="RootVersionIndex"/> must satisfy, in the order they are reported.</summary>
[Constraints]
public static partial class VersionIndexConstraints
{
	public static Constraint<RootVersionIndex> VersionMonikerMainOrMajor { get; } = Constraints<RootVersionIndex>.Check(
		index => index
			.SelectMany(repo => repo.Value.SelectMany(spec => spec.Value.Keys.Select(key => (Repo: repo.Key, Spec: spec.Key, Key: key))))
			.Where(m => m.Key != "main" && !int.TryParse(m.Key, NumberStyles.None, CultureInfo.InvariantCulture, out _))
			.Select(
				m => Violation.Warning(
					$"Version key '{m.Key}' of {m.Repo}/{m.Spec} is neither 'main' nor a major integer, so the API explorer builds a 'v{m.Key}' url and sorts it as 0."
				)
			)
	).Describe("A8: A version moniker in the index is 'main' or an integer major; anything else produces v<moniker> urls sorted as 0.");

	public static Constraint<RootVersionIndex> VersionKeyMatchesVersion { get; } = Constraints<RootVersionIndex>.Check(
		index => index
			.SelectMany(
				repo => repo.Value.SelectMany(
					spec => spec.Value.Select(entry => (Repo: repo.Key, Spec: spec.Key, entry.Key, entry.Value.Version))
				)
			)
			.Where(m => !KeyMatchesVersion(m.Key, m.Version))
			.Select(
				m => Violation.Error(
					$"Version key '{m.Key}' of {m.Repo}/{m.Spec} resolves to version '{m.Version}', which is not {(m.Key == "main" ? "'main' or 'master'" : $"a '{m.Key}.<minor>' release")}."
				)
			)
	).Describe(
		"A13: The version a key resolves to belongs to that key: 'main' resolves to main or master, a major to a release of that major."
	);

	/// <summary>Monikers that are neither 'main' nor an integer have their own rule (A8) and are not judged here.</summary>
	private static bool KeyMatchesVersion(string key, string version)
	{
		if (key == "main")
			return version is "main" or "master";
		if (!int.TryParse(key, NumberStyles.None, CultureInfo.InvariantCulture, out _))
			return true;
		var dot = version.IndexOf('.', StringComparison.Ordinal);
		return dot > 0
			&& version[..dot] == key
			&& int.TryParse(version[(dot + 1)..], NumberStyles.None, CultureInfo.InvariantCulture, out _);
	}
}
