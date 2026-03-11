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
	/// Gets the preferred area for subsection grouping when publish rules with areas are active.
	/// With include_areas: returns the first entry area that is in the include list.
	/// With exclude_areas: returns the first entry area that is not in the exclude list.
	/// When no relevant rules exist, returns the first area.
	/// </summary>
	public static string GetPreferredArea(this PublishBlocker? publishBlocker, ChangelogEntry entry)
	{
		if (entry.Areas is not { Count: > 0 })
			return string.Empty;
		if (publishBlocker?.Areas is not { Count: > 0 })
			return entry.Areas[0];
		return publishBlocker.AreasMode switch
		{
			FieldMode.Include => entry.Areas.FirstOrDefault(a => IsAreaListed(publishBlocker, a)) ?? entry.Areas[0],
			FieldMode.Exclude => entry.Areas.FirstOrDefault(a => !IsAreaListed(publishBlocker, a)) ?? entry.Areas[0],
			_ => entry.Areas[0]
		};
	}

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

	/// <summary>
	/// Resolves the applicable <see cref="PublishBlocker"/> for an entry from a set of per-product rules.
	/// </summary>
	/// <remarks>
	/// Algorithm — intersection + alphabetical first-match:
	/// <list type="number">
	/// <item>Compute the intersection of <paramref name="contextIds"/> and <paramref name="entryOwnIds"/>.
	///   This restricts rule lookup to products the entry actually claims to belong to.</item>
	/// <item>Sort the intersection alphabetically (case-insensitive, ascending) for a deterministic result.</item>
	/// <item>Return the per-product rule for the first matching product ID in the sorted intersection.</item>
	/// <item>If the intersection is empty (unusual: entry's products are disjoint from the bundle context),
	///   fall back to <paramref name="entryOwnIds"/> sorted alphabetically, then to <paramref name="globalBlocker"/>.</item>
	/// </list>
	/// </remarks>
	/// <param name="contextIds">
	/// The product IDs that define this bundle's context — from <c>output_products</c> during bundling,
	/// or from the bundle's top-level <c>products</c> field during rendering.
	/// When no context is set, pass the entry's own product IDs here as a fallback.
	/// </param>
	/// <param name="entryOwnIds">The product IDs declared on the individual changelog entry.</param>
	/// <param name="byProduct">Per-product blocker overrides keyed by product ID.</param>
	/// <param name="globalBlocker">Global blocker returned when no per-product rule matches.</param>
	public static PublishBlocker? ResolveBlocker(
		IEnumerable<string> contextIds,
		IEnumerable<string> entryOwnIds,
		IReadOnlyDictionary<string, PublishBlocker> byProduct,
		PublishBlocker? globalBlocker)
	{
		var entrySet = new HashSet<string>(entryOwnIds, StringComparer.OrdinalIgnoreCase);

		// Intersection: context products that the entry actually belongs to, sorted alphabetically
		var candidates = contextIds
			.Where(id => entrySet.Contains(id))
			.OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
			.ToList();

		// Edge case: empty intersection (entry's products are disjoint from the bundle context).
		// Fall back to the entry's own products so context-only rules don't bleed across.
		if (candidates.Count == 0)
			candidates = entrySet
				.OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
				.ToList();

		foreach (var id in candidates)
		{
			if (byProduct.TryGetValue(id, out var blocker))
				return blocker;
		}

		return globalBlocker;
	}

	private static bool IsAreaListed(PublishBlocker blocker, string area) =>
		blocker.Areas?.Any(l => l.Equals(area, StringComparison.OrdinalIgnoreCase)) ?? false;
}
