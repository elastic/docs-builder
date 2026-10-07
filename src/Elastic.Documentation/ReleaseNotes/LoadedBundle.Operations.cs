// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Versions;

namespace Elastic.Documentation.ReleaseNotes;

public partial record LoadedBundle
{
	/// <summary>
	/// Merges the bundles that share a target version into one (entries concatenated, repos joined with <c>+</c>, descriptions joined, the earliest release date
	/// kept) and orders the result newest first. The result does not depend on the order of <paramref name="bundles"/>: the bundles of one version are taken in
	/// the canonical order of <see cref="CanonicalOrder"/> (release date ascending with undated last, then file path, repository and owner, all ordinal), the
	/// first of them supplies the metadata, and versions that compare equal as versions are ordered by their text. Each merged bundle is re-checked by <see cref="LoadedBundleConstraints"/>; a rule it breaks that is an error
	/// is in the result (a warning the sources carried was reported when they were loaded).
	/// </summary>
	public static Result<IReadOnlyList<LoadedBundle>> MergeByTarget(IReadOnlyList<LoadedBundle> bundles)
	{
		if (bundles.Count <= 1)
			return Result.Success(bundles);

		var merged = bundles
			.GroupBy(b => b.Version)
			.Select(MergeGroup)
			.OrderByDescending(b => VersionOrDate.Parse(b.Version))
			.ThenBy(b => b.Version, StringComparer.Ordinal)
			.ToList();
		var errors = merged.SelectMany(b => DomainOperation.Derived(LoadedBundleConstraints.All, b).Diagnostics).ToList();
		return Result.Success<IReadOnlyList<LoadedBundle>>(merged, errors);
	}

	/// <summary>The canonical order the bundles of one version are merged in: release date ascending (undated last), then file path, repository, owner (ordinal).</summary>
	private static List<LoadedBundle> CanonicalOrder(IEnumerable<LoadedBundle> bundles) =>
		bundles
			.OrderBy(b => b.Data?.ReleaseDate is null)
			.ThenBy(b => b.Data?.ReleaseDate)
			.ThenBy(b => b.FilePath, StringComparer.Ordinal)
			.ThenBy(b => b.Repo, StringComparer.Ordinal)
			.ThenBy(b => b.Owner, StringComparer.Ordinal)
			.ToList();

	private static LoadedBundle MergeGroup(IGrouping<string, LoadedBundle> group)
	{
		var bundlesList = CanonicalOrder(group);

		if (bundlesList.Count == 1)
			return bundlesList[0];

		// Merge entries from all bundles
		var mergedEntries = bundlesList.SelectMany(b => b.Entries).ToList();

		// Combine repo names from all contributing bundles
		var combinedRepo = string.Join("+", bundlesList.Select(b => b.Repo).Distinct().OrderBy(r => r, StringComparer.Ordinal));

		// The first bundle in canonical order supplies the metadata
		var first = bundlesList[0];

		var descriptions = bundlesList.Select(b => b.Data?.Description).Where(d => !string.IsNullOrEmpty(d)).ToList();

		var mergedDescription = descriptions.Count switch
		{
			0 => null,
			1 => descriptions[0],
			_ => string.Join("\n\n", descriptions)
		};

		var releaseDates = bundlesList.Select(b => b.Data?.ReleaseDate).Where(d => d.HasValue).Select(d => d!.Value).Distinct().ToList();

		var mergedReleaseDate = releaseDates.Count switch
		{
			0 => (DateOnly?)null,
			_ => releaseDates[0]
		};

		var mergedData = first.Data != null
			? first.Data with { Description = mergedDescription, ReleaseDate = mergedReleaseDate }
			: new Bundle { Description = mergedDescription, ReleaseDate = mergedReleaseDate };

		return new LoadedBundle(first.Version, combinedRepo, first.Owner, mergedData, first.FilePath, mergedEntries);
	}
}
