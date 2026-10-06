// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Versions;

namespace Elastic.Documentation.ReleaseNotes;

public partial record LoadedBundle
{
	/// <summary>
	/// Merges the bundles that share a target version into one (entries concatenated, repos joined with <c>+</c>, descriptions joined, the first release date
	/// kept) and orders the result newest first. Each merged bundle is re-checked by <see cref="LoadedBundleConstraints"/>; a rule it breaks that is an error
	/// is in the result (a warning the sources carried was reported when they were loaded).
	/// </summary>
	public static Result<IReadOnlyList<LoadedBundle>> MergeByTarget(IReadOnlyList<LoadedBundle> bundles)
	{
		if (bundles.Count <= 1)
			return Result.Success(bundles);

		var merged = bundles.GroupBy(b => b.Version).Select(MergeGroup).OrderByDescending(b => VersionOrDate.Parse(b.Version)).ToList();
		var errors = merged.SelectMany(b => ReleaseNotesOperation.Derived(LoadedBundleConstraints.All, b).Diagnostics).ToList();
		return Result.Success<IReadOnlyList<LoadedBundle>>(merged, errors);
	}

	private static LoadedBundle MergeGroup(IGrouping<string, LoadedBundle> group)
	{
		var bundlesList = group.ToList();

		if (bundlesList.Count == 1)
			return bundlesList[0];

		// Merge entries from all bundles
		var mergedEntries = bundlesList.SelectMany(b => b.Entries).ToList();

		// Combine repo names from all contributing bundles
		var combinedRepo = string.Join("+", bundlesList.Select(b => b.Repo).Distinct().OrderBy(r => r));

		// Use the first bundle's metadata as the base
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
