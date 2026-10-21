// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Diagnostics;

namespace Elastic.Documentation.ReleaseNotes;

/// <summary>A changelog file a bundle is assembled from: its name, the checksum of its content, and the entry read from it.</summary>
public sealed record BundleSource(string FileName, string Checksum, ChangelogEntry Data);

/// <summary>A product as the CLI names it for a bundle (<c>--output-products</c>): <c>*</c> as a target or lifecycle is none.</summary>
public sealed record BundleProductSpec(string? Product, string? Target, string? Lifecycle);

/// <summary>
/// An amend to apply to a bundle. <paramref name="PatchesDescription"/> is false for the reconciler-owned notes sidecar, which never patches the intro.
/// <paramref name="FilePath"/> (the sidecar's path, when it has one) fixes the order amends are applied in: see <see cref="Bundle.ApplyAmends"/>.
/// </summary>
public sealed record BundleAmend(Bundle Amend, bool PatchesDescription = true, string? FilePath = null);

/// <summary>
/// The operations that make a <see cref="Bundle"/> from something that was not a file, or change one: pure functions (no IO, no clock) that return the
/// bundle checked by <see cref="BundleReadConstraints"/>. A bundle <i>built</i> from facts carries every finding; one <i>derived</i> from a bundle that was
/// read carries only the errors, because the warnings it had were reported when it was read.
/// </summary>
public partial record Bundle
{
	/// <summary>
	/// A bundle assembled from the changelog files that matched: its products are the output products when given, otherwise the distinct
	/// (product, version, lifecycle) of the entries (the version is the first of a note's <c>versions</c>); its entries are the files' entries, inline.
	/// A product listed with several targets is a finding of <c>bundle-product-single-target</c>.
	/// </summary>
	public static Result<Bundle> Assemble(
		IReadOnlyList<BundleSource> sources,
		IReadOnlyList<BundleProductSpec>? outputProducts,
		string? repo,
		string? owner,
		IReadOnlyCollection<string>? hideFeatures
	) =>
		DomainOperation.Built(
			BundleReadConstraints.All,
			new Bundle
			{
				Products = ProductsOf(sources, outputProducts, repo, owner),
				HideFeatures = hideFeatures?.Count > 0 ? hideFeatures.ToList() : [],
				Entries = sources.Select(s => s.Data.ToBundledEntry(s.FileName, s.Checksum)).ToList()
			}
		);

	private static List<BundledProduct> ProductsOf(
		IReadOnlyList<BundleSource> sources,
		IReadOnlyList<BundleProductSpec>? outputProducts,
		string? repo,
		string? owner
	)
	{
		if (outputProducts is { Count: > 0 })
		{
			return outputProducts
				.OrderBy(p => p.Product)
				.ThenBy(p => p.Target ?? string.Empty)
				.ThenBy(p => p.Lifecycle ?? string.Empty)
				.Select(p => BundledProduct.Of(p.Product, p.Target, p.Lifecycle, repo, owner))
				.ToList();
		}

		if (sources.Count == 0)
			return [];

		var productVersions = new HashSet<(string product, string version, Lifecycle? lifecycle)>();
		foreach (var source in sources)
		{
			if (source.Data.Products == null)
				continue;
			foreach (var product in source.Data.Products)
			{
				// `Target` is obsolete; prefer the first entry of `Versions` (notes).
				// For PR-anchored entries neither field is set → version is "".
#pragma warning disable CS0618 // reading obsolete Target for backward compat
				var version = (product.Versions.Count > 0 ? product.Versions[0] : null) ?? product.Target ?? string.Empty;
#pragma warning restore CS0618
				_ = productVersions.Add((product.ProductId, version, product.Lifecycle));
			}
		}

		return productVersions
			.OrderBy(pv => pv.product)
			.ThenBy(pv => pv.version)
			.ThenBy(pv => pv.lifecycle?.ToStringFast(true) ?? string.Empty)
			.Select(
				pv => new BundledProduct(pv.product, string.IsNullOrWhiteSpace(pv.version) ? null : pv.version, pv.lifecycle, repo, owner)
			)
			.ToList();
	}

	/// <summary>
	/// The bundle of one release scraped from a release notes page: one product at <paramref name="version"/> (and <paramref name="lifecycle"/>), the
	/// page's intro as the description (none when blank), its release date, and the inline entries it listed.
	/// </summary>
	public static Result<Bundle> ForRelease(
		string productId,
		string version,
		Lifecycle? lifecycle,
		string? repo,
		string? owner,
		string? description,
		DateOnly? releaseDate,
		IReadOnlyList<BundledEntry> entries
	) =>
		DomainOperation.Built(
			BundleReadConstraints.All,
			new Bundle
			{
				Products =
				[
					new BundledProduct { ProductId = productId, Target = version, Lifecycle = lifecycle, Repo = repo, Owner = owner }
				],
				Description = string.IsNullOrEmpty(description) ? null : description,
				ReleaseDate = releaseDate,
				Entries = entries
			}
		);

	/// <summary>
	/// This bundle as it is written when the command finishes. A <paramref name="description"/> given (already substituted) replaces the intro. The release date is
	/// <paramref name="releaseDate"/> when given (an explicit override), otherwise the one it has, otherwise <paramref name="today"/> unless
	/// <paramref name="suppressReleaseDate"/>. A non-blank <paramref name="gitRef"/> records the endpoint a commit-range bundle was cut for.
	/// </summary>
	public Result<Bundle> Finalize(
		string? description,
		DateOnly? releaseDate,
		string? gitRef,
		DateOnly today,
		bool suppressReleaseDate = false
	) =>
		DomainOperation.Derived(
			BundleReadConstraints.All,
			this with
			{
				Description = description ?? Description,
				ReleaseDate = releaseDate ?? ReleaseDate ?? (suppressReleaseDate ? null : today),
				GitRef = string.IsNullOrWhiteSpace(gitRef) ? GitRef : gitRef
			}
		);

	/// <summary>
	/// An amend document for a bundle: it copies the parent's products (so registry routing and <c>:version:</c> selection work), records the
	/// <paramref name="exclusions"/> and adds <paramref name="entriesToAdd"/>. A <see langword="null"/> <paramref name="description"/> inherits the
	/// parent's intro, an empty one clears it, any other replaces it.
	/// </summary>
	public static Result<Bundle> AmendOf(
		IReadOnlyList<BundledProduct> parentProducts,
		IReadOnlyList<BundledEntry> entriesToAdd,
		IReadOnlyList<BundledEntry> exclusions,
		string? description = null
	) =>
		DomainOperation.Derived(
			BundleReadConstraints.All,
			new Bundle { Products = parentProducts, ExcludeEntries = exclusions, Entries = entriesToAdd, Description = description }
		);

	/// <summary>
	/// This bundle with its amends applied (see <see cref="BundleAmendMerger"/>): each amend's exclusions, then its additions; the intro is
	/// patched by the amends that patch it. The one way a bundle takes its amends.
	/// <para>
	/// Order: amends are numbered, so the order matters, and it is defined here and not by the caller: amends are applied by
	/// <see cref="BundleAmendMerger.GetAmendMergeOrder"/> of their <see cref="BundleAmend.FilePath"/> (numbered amends ascending, the notes sidecar last), ties
	/// by file path (ordinal); amends without a path keep the order they were given in, after the ones that have one.
	/// </para>
	/// <para>
	/// Idempotent: an added entry whose file name and checksum are already in the bundle is not added again, so applying the same amends twice is
	/// applying them once.
	/// </para>
	/// </summary>
	public Result<Bundle> ApplyAmends(IReadOnlyList<BundleAmend> amends)
	{
		var ordered = amends
			.OrderBy(a => a.FilePath is null)
			.ThenBy(a => a.FilePath is null ? 0 : BundleAmendMerger.GetAmendMergeOrder(a.FilePath))
			.ThenBy(a => a.FilePath, StringComparer.Ordinal)
			.ToList();
		return DomainOperation.Derived(
			BundleReadConstraints.All,
			this with
			{
				Entries = BundleAmendMerger.MergeEntries(Entries, ordered.Select(a => a.Amend).ToList()),
				Description = BundleAmendMerger.MergeDescription(
					Description,
					ordered.Where(a => a.PatchesDescription).Select(a => a.Amend).ToList()
				)
			}
		);
	}

	/// <summary>This bundle without the references to repositories outside <paramref name="allowRepos"/>, its entries' and its intro's.</summary>
	public Result<PrivateLinkScrub<Bundle>> WithoutPrivateLinks(
		IReadOnlyList<string> allowRepos,
		string defaultOwner = "elastic",
		string? defaultBundleRepo = null
	) => LinkAllowlist.WithoutPrivateLinks(this, allowRepos, defaultOwner, defaultBundleRepo);
}
