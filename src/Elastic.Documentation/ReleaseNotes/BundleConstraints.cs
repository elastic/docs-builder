// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Nullean.Entities;

namespace Elastic.Documentation.ReleaseNotes;

/// <summary>Every rule a <see cref="Bundle"/> must satisfy on its own, in the order they are reported.</summary>
[Constraints]
public static partial class BundleConstraints
{
	public static Constraint<Bundle> BundleProductSingleTarget { get; } = Constraints<Bundle>
		.Consistent(
			m => m.Products,
			p => p.ProductId,
			Target,
			StringComparer.OrdinalIgnoreCase,
			StringComparer.Ordinal,
			describe: (product, targets) => $"Product '{product}' has multiple targets in bundle: {string.Join(", ", targets)}"
		)
		.WithSeverity(Severity.Warning)
		.Describe("R84: A bundle lists a product with one target, not several.");

	public static Constraint<Bundle> BundleHasOneProduct { get; } = Constraints<Bundle>.Check(b =>
	{
		var products = b.Products.Select(p => p.ProductId).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
		return products.Count > 1
			? [Violation.Warning($"A bundle belongs to one product, but this one lists {products.Count}: {string.Join(", ", products)}.")]
			: [];
	}).Describe(
		"R94: A bundle belongs to one product: bundles and products are one to one. A repository that hosts several products (elastic/cloud) has one profile, and so one bundle, per product."
	);

	public static Constraint<Bundle> BundleReleaseDateParses { get; } = Constraints<Bundle>
		.Always(
			b => b.ReleaseDateAuthored.Raw,
			raw => string.IsNullOrWhiteSpace(raw) || DateOnly.TryParseExact(
				raw,
				"yyyy-MM-dd",
				System.Globalization.CultureInfo.InvariantCulture,
				System.Globalization.DateTimeStyles.None,
				out _
			),
			describe: raw => $"release-date '{raw}' is not a YYYY-MM-DD date; the bundle is read without one."
		)
		.WithSeverity(Severity.Warning)
		.Describe("R82: A bundle's release-date is YYYY-MM-DD; a malformed one is silently read as no date.");

	public static Constraint<Bundle, BundledEntry> BundleEntriesInlined { get; } = Constraints<Bundle>.Each(
		m => m.Entries,
		e => !string.IsNullOrWhiteSpace(e.Title) && e.Type is not null,
		e =>
			$"Entry '{EntryName(e)}' in bundle has no inline content: title and type are required. Re-create the bundle with 'changelog bundle'."
	).Describe(
		"R85: Every bundle entry is inlined, with a title and a type (stage dependent: a warning in the loader and the directive's CDN mode, an error in local mode and the render CLI)."
	);

	public static Constraint<Bundle> BundleEntryFilesUnique { get; } = Constraints<Bundle>
		.Unique(
			m => m.Entries.Where(e => !string.IsNullOrWhiteSpace(e.File?.Name)),
			e => e.File!.Name,
			StringComparer.OrdinalIgnoreCase,
			describe: (name, _) => $"Changelog file '{name}' appears multiple times in the same bundle"
		)
		.WithSeverity(Severity.Warning)
		.Describe(
			"R89: A changelog file appears once within a bundle (render CLI only; across bundles it is a rule of the loaded bundles)."
		);

	internal static string EntryName(BundledEntry e) => !string.IsNullOrWhiteSpace(e.File?.Name) ? e.File.Name : e.Title ?? "<unnamed>";

	private static string Target(BundledProduct p)
	{
		var target = string.IsNullOrWhiteSpace(p.Target) ? "(no target)" : p.Target;
		return p.Lifecycle is { } lifecycle ? $"{target} {lifecycle.ToStringFast(true)}" : target;
	}
}

/// <summary>A parent bundle with an amend file (a <see cref="Bundle"/> whose <c>ExcludeEntries</c> and <c>Entries</c> remove and add entries) applied to it.</summary>
public sealed record BundleAmendOfParent(Bundle Parent, Bundle Amend);

/// <summary>Every rule an amend <see cref="Bundle"/> must satisfy against the bundle it amends. <c>--force</c> and an already-excluded entry being skipped are decided at run time, not by these.</summary>
[Constraints]
public static partial class BundleAmendConstraints
{
	public static Constraint<BundleAmendOfParent> AmendRemovalsExist { get; } = Constraints<BundleAmendOfParent>.References(
		m => m.Amend.ExcludeEntries.Where(x => FileName(x) is not null),
		x => Enumerable.Repeat(FileName(x)!, 1),
		m => m.Parent.Entries.Select(FileName).OfType<string>(),
		StringComparer.OrdinalIgnoreCase,
		describe: (_, name) => $"Changelog '{name}' was not found in the effective bundle (parent plus existing amend files)."
	).Describe("R92: Every entry an amend removes is in the bundle it amends.");

	public static Constraint<BundleAmendOfParent> AmendRemovalChecksumsMatch { get; } = Constraints<BundleAmendOfParent>.Each(
		m => m.Amend.ExcludeEntries,
		(m, x) => ChecksumMismatch(m.Parent, x)
			? $"Bundle contains '{FileName(x)}' but with a different checksum than the sourced changelog. Re-create the bundle or use --force to remove by file name only."
			: null
	).Describe(
		"R92: An entry an amend removes has the checksum of the bundle's entry, unless the checksum is empty. `changelog amend --force` accepts a mismatch: the service then reports this as a warning and writes the bundle's checksum."
	);

	public static Constraint<BundleAmendOfParent> AmendAddedEntriesNew { get; } = Constraints<BundleAmendOfParent>
		.Disjoint(
			m => m.Amend.Entries.Select(FileName).OfType<string>(),
			m => m.Parent.Entries.Select(FileName).OfType<string>(),
			StringComparer.OrdinalIgnoreCase,
			describe: name => $"Changelog '{name}' to add is already in the bundle; duplicates are not detected when adding."
		)
		.WithSeverity(Severity.Warning)
		.Describe(
			"R93: An entry an amend adds is not already in the bundle (an added entry also needs a title and a type: that is bundle-entries-inlined on the amend)."
		);

	private static string? FileName(BundledEntry e)
	{
		if (string.IsNullOrWhiteSpace(e.File?.Name))
			return null;
		var normalized = e.File.Name.Replace('\\', '/');
		var slash = normalized.LastIndexOf('/');
		return slash >= 0 ? normalized[(slash + 1)..] : normalized;
	}

	// The bundle has the file, and none of its entries with that name carries the checksum to remove (an empty checksum matches by name).
	private static bool ChecksumMismatch(Bundle parent, BundledEntry removal)
	{
		if (FileName(removal) is not { } name || string.IsNullOrWhiteSpace(removal.File?.Checksum))
			return false;
		var named = parent.Entries.Where(e => SameFile(e, name)).ToList();
		return named.Count > 0
			&& !named.Exists(e => string.Equals(e.File?.Checksum, removal.File.Checksum, StringComparison.OrdinalIgnoreCase));
	}

	private static bool SameFile(BundledEntry e, string name) => string.Equals(FileName(e), name, StringComparison.OrdinalIgnoreCase);
}
