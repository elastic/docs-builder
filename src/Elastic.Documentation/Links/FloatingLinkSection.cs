// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using System.Text.Json.Serialization;

namespace Elastic.Documentation.Links;

/// <summary>
/// The pages of one floating section, such as one API, in <c>links.json</c>. Most pages exist in every version,
/// so each page path appears once with a bit mask of the versions that contain it, and a lookup is a few hash
/// lookups and a bit test no matter how many versions there are.
/// </summary>
public record FloatingLinkSection
{
	/// <summary>The most versions one section can hold, one bit each.</summary>
	public const int MaxVersions = 64;

	/// <summary>The URL root the section owns, relative to the site root, for example <c>api/doc/elasticsearch</c>.</summary>
	[JsonPropertyName("url_root")]
	public required string UrlRoot { get; init; }

	/// <summary>The version names. The index of a name is its bit position in the masks. <c>main</c> has no URL segment.</summary>
	[JsonPropertyName("versions")]
	public required string[] Versions { get; init; }

	/// <summary>The versions that exist for links but not in the sitemap, as a bit mask.</summary>
	[JsonPropertyName("hidden")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public ulong Hidden { get; init; }

	/// <summary>
	/// Each page path, relative to the version root and without slashes at either end, mapped to the versions that
	/// contain it. The landing page of a version has the empty path.
	/// </summary>
	[JsonPropertyName("pages")]
	public required Dictionary<string, ulong> Pages { get; init; }

	/// <summary>Former page paths that forward to a canonical path, for example the operations a collapsed page replaced.</summary>
	[JsonPropertyName("aliases")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public Dictionary<string, string>? Aliases { get; init; }

	[JsonIgnore]
	private FrozenDictionary<string, int> VersionBits =>
		field ??= Versions.Take(MaxVersions).Select((version, bit) => (version, bit)).ToFrozenDictionary(v => v.version, v => v.bit);

	/// <summary>
	/// Finds <paramref name="page"/> in <paramref name="version"/> and returns its path from the site root.
	/// A path that is not a page of that version, directly or through an alias, does not resolve.
	/// </summary>
	public bool TryResolvePage(string version, string page, out string sitePath)
	{
		sitePath = string.Empty;
		if (!VersionBits.TryGetValue(version, out var bit))
			return false;

		var mask = 1UL << bit;
		if (!Pages.TryGetValue(page, out var versions) || (versions & mask) == 0)
		{
			if (Aliases is null || !Aliases.TryGetValue(page, out var canonical))
				return false;
			if (!Pages.TryGetValue(canonical, out versions) || (versions & mask) == 0)
				return false;
			page = canonical;
		}

		var versionSegment = version == "main" ? string.Empty : $"/v{version}";
		sitePath = page.Length == 0 ? $"{UrlRoot}{versionSegment}" : $"{UrlRoot}{versionSegment}/{page}";
		return true;
	}
}
