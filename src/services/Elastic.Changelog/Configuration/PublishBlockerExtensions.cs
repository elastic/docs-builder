// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation;
using Elastic.Documentation.Configuration.Changelog;

namespace Elastic.Changelog.Configuration;

/// <summary>
/// Extension methods for PublishBlocker that depend on ChangelogEntry
/// </summary>
public static class PublishBlockerExtensions
{
	/// <summary>
	/// Checks if a changelog entry should be blocked from publishing
	/// </summary>
	public static bool ShouldBlock(this PublishBlocker blocker, ChangelogEntry entry)
	{
		// Check if entry type is blocked
		if (blocker.Types?.Count > 0)
		{
			var entryTypeName = entry.Type.ToStringFast(true);
			if (blocker.Types.Any(t => t.Equals(entryTypeName, StringComparison.OrdinalIgnoreCase)))
				return true;
		}

		// Check if any of the entry's areas are blocked
		if (blocker.Areas?.Count > 0
			&& entry.Areas?.Count > 0
			&& entry.Areas.Any(area => blocker.Areas.Any(blocked => blocked.Equals(area, StringComparison.OrdinalIgnoreCase))))
		{
			return true;
		}

		return false;
	}
}
