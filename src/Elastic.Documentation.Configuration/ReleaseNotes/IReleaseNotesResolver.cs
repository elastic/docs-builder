// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.ReleaseNotes;

namespace Elastic.Documentation.Configuration.ReleaseNotes;

/// <summary>
/// Resolves prefetched CDN changelog bundles for the <c>{changelog}</c> directive's <c>:cdn:</c> mode.
/// Mirrors the cross-link resolver pattern: the build fetches all declared products up front and the
/// directive only reads the already-loaded in-memory set — no per-directive HTTP.
/// </summary>
public interface IReleaseNotesResolver
{
	/// <summary>Whether <paramref name="product"/> was declared under <c>release_notes</c> in docset.yml.</summary>
	bool IsDeclared(string product);

	/// <summary>
	/// Gets the prefetched bundles for <paramref name="product"/>. Returns false when the product was not
	/// declared (or not fetched); a declared product with no usable bundles returns true with an empty list.
	/// </summary>
	bool TryGetBundles(string product, out IReadOnlyList<LoadedBundle> bundles);
}

/// <summary>
/// Default resolver for build paths that do not source release notes from the CDN (tests, refactor
/// tooling). Every product is treated as undeclared so a stray <c>:cdn:</c> directive emits a clear error.
/// </summary>
public sealed class NoopReleaseNotesResolver : IReleaseNotesResolver
{
	public static NoopReleaseNotesResolver Instance { get; } = new();

	private NoopReleaseNotesResolver() { }

	/// <inheritdoc />
	public bool IsDeclared(string product) => false;

	/// <inheritdoc />
	public bool TryGetBundles(string product, out IReadOnlyList<LoadedBundle> bundles)
	{
		bundles = [];
		return false;
	}
}

/// <summary>
/// Resolver backed by an immutable <see cref="FetchedReleaseNotes"/>. The backing set can be populated
/// after construction (<see cref="Populate"/>) so the assembler can share a single resolver across all
/// documentation sets and fill it once every docset.yml has been parsed.
/// </summary>
public sealed class ReleaseNotesResolver(FetchedReleaseNotes? fetched = null) : IReleaseNotesResolver
{
	private FetchedReleaseNotes _fetched = fetched ?? FetchedReleaseNotes.Empty;

	/// <summary>Replaces the backing set. Used by the assembler's two-phase startup fetch.</summary>
	public void Populate(FetchedReleaseNotes fetched) => _fetched = fetched;

	/// <inheritdoc />
	public bool IsDeclared(string product) => _fetched.DeclaredProducts.Contains(product);

	/// <inheritdoc />
	public bool TryGetBundles(string product, out IReadOnlyList<LoadedBundle> bundles)
	{
		if (_fetched.BundlesByProduct.TryGetValue(product, out var found))
		{
			bundles = found;
			return true;
		}

		bundles = [];
		return false;
	}
}
