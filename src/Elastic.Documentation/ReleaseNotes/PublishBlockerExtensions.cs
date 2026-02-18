// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.ReleaseNotes;

/// <summary>
/// Extension methods for PublishBlocker that depend on ChangelogEntry.
/// </summary>
public static class PublishBlockerExtensions
{
	/// <summary>
	/// Checks if a changelog entry should be blocked from publishing.
	/// Supports both exclude (block if matches) and include (block if doesn't match) modes.
	/// </summary>
	public static bool ShouldBlock(this PublishBlocker blocker, ChangelogEntry entry)
	{
		if (ShouldBlockByType(blocker, entry))
			return true;

		return ShouldBlockByArea(blocker, entry);
	}

	/// <summary>
	/// Checks if an entry type matches the blocker's type list.
	/// </summary>
	public static bool MatchesType(this PublishBlocker blocker, string entryTypeName) =>
		blocker.Types?.Count > 0 &&
		blocker.Types.Any(t => t.Equals(entryTypeName, StringComparison.OrdinalIgnoreCase));

	/// <summary>
	/// Checks if entry areas match the blocker's area list using the configured match mode.
	/// </summary>
	public static bool MatchesArea(this PublishBlocker blocker, IReadOnlyList<string>? entryAreas)
	{
		if (blocker.Areas?.Count is null or 0 || entryAreas?.Count is null or 0)
			return false;

		return blocker.MatchAreas switch
		{
			MatchMode.All => entryAreas.All(area =>
				blocker.Areas.Any(listed => listed.Equals(area, StringComparison.OrdinalIgnoreCase))),
			_ => entryAreas.Any(area =>
				blocker.Areas.Any(listed => listed.Equals(area, StringComparison.OrdinalIgnoreCase)))
		};
	}

	private static bool ShouldBlockByType(PublishBlocker blocker, ChangelogEntry entry)
	{
		if (blocker.Types?.Count is null or 0)
			return false;

		var entryTypeName = entry.Type.ToStringFast(true);
		var matches = blocker.MatchesType(entryTypeName);

		return blocker.TypesMode switch
		{
			FieldMode.Exclude => matches,
			FieldMode.Include => !matches,
			_ => false
		};
	}

	private static bool ShouldBlockByArea(PublishBlocker blocker, ChangelogEntry entry)
	{
		if (blocker.Areas?.Count is null or 0)
			return false;

		// For include mode with no entry areas, the entry doesn't match the include list → blocked
		if (blocker.AreasMode == FieldMode.Include && entry.Areas?.Count is null or 0)
			return true;

		if (entry.Areas?.Count is null or 0)
			return false;

		var matches = blocker.MatchesArea(entry.Areas);

		return blocker.AreasMode switch
		{
			FieldMode.Exclude => matches,
			FieldMode.Include => !matches,
			_ => false
		};
	}
}
