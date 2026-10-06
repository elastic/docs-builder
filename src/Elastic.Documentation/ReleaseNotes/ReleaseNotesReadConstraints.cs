// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Nullean.Entities;

namespace Elastic.Documentation.ReleaseNotes;

/// <summary>
/// The rules of <see cref="ChangelogEntryConstraints"/> that hold wherever an entry is <i>read</i> (bundling, matching, reconciling, scrubbing),
/// as opposed to authored or validated. The rest are stage dependent: they hold when an entry is created or validated (<c>validate-entries</c>
/// reports them with its own pinned messages from the file's shape, which a record no longer carries), but a published pool object that predates
/// them must still be readable, so reading must not report them.
/// </summary>
public static class ChangelogEntryReadConstraints
{
	/// <summary>The checks the loader runs on every entry it reads.</summary>
	public static ConstraintSet<ChangelogEntry> All { get; } = ChangelogEntryConstraints.All.Without(
		// R34/R36: the bundle builder reports a missing title or type with its own pinned messages (only when it resolves entries), and an entry read for
		// matching, reconciling or scrubbing is not required to be complete
		ChangelogEntryConstraints.EntryTitleRequired,
		ChangelogEntryConstraints.EntryTypeRequired,
		// R35: size warnings are an authoring concern
		ChangelogEntryConstraints.EntryTitleMax80,
		ChangelogEntryConstraints.EntryDescriptionMax600,
		// R40/R41/R42: validate-entries and create own these (published objects legitimately predate them; target is read as versions)
		ChangelogEntryConstraints.EntrySubtypeOnlyBreakingChange,
		ChangelogEntryConstraints.EntryProductsRequired,
		ChangelogEntryConstraints.EntryProductIdRequired,
		ChangelogEntryConstraints.EntryProductTargetObsolete,
		// R45/R63: the file name is checked against the PR by validate-entries, which knows the PR number it expects
		ChangelogEntryConstraints.EntryFileNameStartsWithPr,
		ChangelogEntryConstraints.EntryPrMatchesFileName
	);
}

/// <summary>
/// The rules of <see cref="BundleConstraints"/> that hold when a bundle is <i>read</i>. <c>bundle-entries-inlined</c> is stage dependent (the loader
/// skips a not-inlined entry with its own pinned warning, the render CLI errors) and <c>bundle-entry-files-unique</c> is a render CLI rule.
/// </summary>
public static class BundleReadConstraints
{
	/// <summary>The checks the loader runs on every bundle it reads.</summary>
	public static ConstraintSet<Bundle> All { get; } = BundleConstraints.All.Without(
		BundleConstraints.BundleEntriesInlined,
		BundleConstraints.BundleEntryFilesUnique
	);
}

/// <summary>
/// The rules of <see cref="LoadedBundleConstraints"/> that hold for a bundle read from content. A bundle file's name is only meaningful where the file is
/// authored (a local folder); content fetched from the CDN is named by the registry, not by the generator's convention, so <c>bundle-filename-convention</c> is not checked.
/// </summary>
public static class LoadedBundleReadConstraints
{
	/// <summary>The checks run on bundles loaded from CDN content.</summary>
	public static ConstraintSet<LoadedBundle> FromContent { get; } = LoadedBundleConstraints.All.Without(
		LoadedBundleConstraints.BundleFilenameConvention
	);
}
