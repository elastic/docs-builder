// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using System.Text.Json.Serialization;

namespace Elastic.Documentation.Links;

/// <summary>
/// One version of a floating section, such as one version of an API. The producer states where the version lives,
/// so a reader never needs to know how the section routes its URLs.
/// </summary>
public record FloatingLinkVersion
{
	/// <summary>The name of the version, for example <c>8</c> or <c>main</c>. Authors use it to pick a version in a cross-link.</summary>
	[JsonPropertyName("name")]
	public required string Name { get; init; }

	/// <summary>
	/// The URL root of this version, relative to the site root and without slashes at either end. A page is at
	/// this root plus <c>/</c> plus its path, and the landing page is the root itself.
	/// </summary>
	[JsonPropertyName("root")]
	public required string Root { get; init; }

	/// <summary>The version exists for links but not in the sitemap or first-level walks.</summary>
	[JsonPropertyName("hidden")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public bool Hidden { get; init; }

	/// <summary>The version a cross-link reaches when it names none.</summary>
	[JsonPropertyName("current")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public bool Current { get; init; }
}

/// <summary>
/// The pages of one floating section, such as one API, in <c>links.json</c>. Most pages exist in every version,
/// so each page path appears once with a bit mask of the versions that contain it, and a lookup is a few hash
/// lookups and a bit test no matter how many versions there are.
/// </summary>
public record FloatingLinkSection
{
	/// <summary>The most versions one section can hold, one bit each.</summary>
	public const int MaxVersions = 64;

	/// <summary>The versions of the section. The index of a version is its bit position in the page masks.</summary>
	[JsonPropertyName("versions")]
	public required FloatingLinkVersion[] Versions { get; init; }

	/// <summary>
	/// Each page path, relative to the version root and without slashes at either end, mapped to the versions that
	/// contain it. The landing page of a version has the empty path.
	/// </summary>
	[JsonPropertyName("pages")]
	public required Dictionary<string, ulong> Pages { get; init; }

	/// <summary>
	/// Former page paths, for example the operations a collapsed page replaced. They forward to a canonical page.
	/// The names of a version map each canonical path to its aliases, so a canonical path is written once however many
	/// aliases it has. An alias exists only in the versions that list it and may name different pages in different versions.
	/// </summary>
	[JsonPropertyName("aliases")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public Dictionary<string, Dictionary<string, string[]>>? Aliases { get; init; }

	[JsonIgnore]
	private FrozenDictionary<string, int> VersionBits =>
		field ??= Versions.Take(MaxVersions).Select((version, bit) => (version.Name, bit)).ToFrozenDictionary(v => v.Name, v => v.bit);

	/// <summary>The reverse of <see cref="Aliases"/>, built once, so that finding the canonical page of an alias is one lookup.</summary>
	[JsonIgnore]
	private FrozenDictionary<string, FrozenDictionary<string, string>> CanonicalByAlias =>
		field ??= (Aliases ?? []).ToFrozenDictionary(
			version => version.Key,
			version => version
				.Value
				.SelectMany(canonical => canonical.Value.Select(alias => (alias, canonical: canonical.Key)))
				.DistinctBy(pair => pair.alias)
				.ToFrozenDictionary(pair => pair.alias, pair => pair.canonical)
		);

	/// <summary>Whether <paramref name="name"/> is the name of a version of this section.</summary>
	public bool HasVersion(string name) => VersionBits.ContainsKey(name);

	/// <summary>
	/// Finds <paramref name="page"/> in the version called <paramref name="version"/>, or in the current version
	/// when <paramref name="version"/> is <c>null</c>, and returns its path from the site root.
	/// A path that is not a page of that version, directly or through an alias, does not resolve.
	/// </summary>
	public bool TryResolvePage(string? version, string page, out string sitePath)
	{
		sitePath = string.Empty;
		var bit = version is null ? Array.FindIndex(Versions, v => v.Current) : VersionBits.GetValueOrDefault(version, -1);
		if (bit is < 0 or >= MaxVersions)
			return false;

		var mask = 1UL << bit;
		if (!Pages.TryGetValue(page, out var versions) || (versions & mask) == 0)
		{
			if (
				!CanonicalByAlias.TryGetValue(Versions[bit].Name, out var versionAliases)
				|| !versionAliases.TryGetValue(page, out var canonical)
			)
				return false;
			if (!Pages.TryGetValue(canonical, out versions) || (versions & mask) == 0)
				return false;
			page = canonical;
		}

		var root = Versions[bit].Root;
		sitePath = page.Length == 0 ? root : $"{root}/{page}";
		return true;
	}
}
