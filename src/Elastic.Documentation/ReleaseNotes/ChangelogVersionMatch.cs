// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.ReleaseNotes;

/// <summary>
/// Shared matching rule for the <c>changelog</c> directive's <c>:version:</c> filter. A bundle
/// matches a requested version when the requested value equals either the bundle's declared target
/// or its file name (with or without extension), compared case-insensitively and ignoring a leading
/// <c>v</c> on either side (release tags are <c>v9.5.0</c>; generated version pages are <c>9.5.0</c>).
/// Kept in one place so the directive's post-load filter and the CDN fetcher's download-time filter agree.
/// </summary>
public static class ChangelogVersionMatch
{
	/// <param name="requested">The user-supplied <c>:version:</c> value (already trimmed).</param>
	/// <param name="target">The bundle's declared target (may be null/empty).</param>
	/// <param name="file">The bundle file name or path (may be null/empty).</param>
	public static bool Matches(string requested, string? target, string? file)
	{
		// empty/whitespace → match all versions
		if (string.IsNullOrWhiteSpace(requested))
			return true;

		var value = StripLeadingV(requested.Trim());

		if (!string.IsNullOrWhiteSpace(target) && string.Equals(StripLeadingV(target.Trim()), value, StringComparison.OrdinalIgnoreCase))
			return true;

		if (string.IsNullOrWhiteSpace(file))
			return false;

		var name = Path.GetFileName(file);
		return string.Equals(StripLeadingV(name), value, StringComparison.OrdinalIgnoreCase)
			|| string.Equals(StripLeadingV(Path.GetFileNameWithoutExtension(file)), value, StringComparison.OrdinalIgnoreCase);
	}

	private static string StripLeadingV(string version) => version.TrimStart('v', 'V');
}
