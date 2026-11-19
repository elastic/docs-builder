// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;
using Nullean.Entities;

namespace Elastic.Documentation.OpenApi;

/// <summary>Every rule a <see cref="VersionIndex"/> must satisfy, in the order they are reported.</summary>
[Constraints<VersionIndex>]
public static partial class VersionIndexConstraints
{
	public static readonly Constraint<VersionIndex> VersionMonikerMainOrMajor = Constraints<VersionIndex>.Check(
		index => index
			.AllMonikers()
			.Where(m => !IsMainOrMajor(m.Moniker.Key))
			.Select(
				m => Violation.Warning(
					$"Version key '{m.Moniker.Key}' of {m.Repository}/{m.File} is neither 'main' nor a major integer, so the API explorer builds a 'v{m.Moniker.Key}' url and sorts it as 0."
				)
			)
	).Describe("A version moniker in the index is 'main' or an integer major; anything else produces v<moniker> urls sorted as 0.");

	public static readonly Constraint<VersionIndex> VersionKeyMatchesVersion = Constraints<VersionIndex>.Check(
		index => index
			.AllMonikers()
			.Where(m => !KeyMatchesVersion(m.Moniker.Key, m.Moniker.Version))
			.Select(
				m => Violation.Error(
					$"Version key '{m.Moniker.Key}' of {m.Repository}/{m.File} resolves to version '{m.Moniker.Version}', which is not {(m.Moniker.Key == "main" ? "'main' or 'master'" : $"a '{m.Moniker.Key}.<minor>' release")}."
				)
			)
	).Describe("The version a key resolves to belongs to that key: 'main' resolves to main or master, a major to a release of that major.");

	private static bool IsMainOrMajor(string key) =>
		key == "main" || int.TryParse(key, NumberStyles.None, CultureInfo.InvariantCulture, out _);

	/// <summary>Monikers that are neither 'main' nor an integer have their own rule and are not judged here.</summary>
	private static bool KeyMatchesVersion(string key, string? version)
	{
		if (key == "main")
			return version is "main" or "master";
		if (!IsMainOrMajor(key))
			return true;
		// A JSON `"version": null` still resolves on main (an empty version segment in the object key), so it is a finding here and never a throw.
		if (version is null)
			return false;
		var dot = version.IndexOf('.', StringComparison.Ordinal);
		return dot > 0
			&& version[..dot] == key
			&& int.TryParse(version[(dot + 1)..], NumberStyles.None, CultureInfo.InvariantCulture, out _);
	}
}
