// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using Elastic.Documentation.ReleaseNotes;

namespace Elastic.Documentation.Configuration.ReleaseNotes;

/// <summary>
/// The immutable result of prefetching CDN-hosted changelog bundles for every product declared under
/// <c>release_notes</c> in docset.yml. Built once at startup by <see cref="ReleaseNotesFetcher"/> and
/// consumed by the <c>{changelog}</c> directive via <see cref="IReleaseNotesResolver"/>.
/// </summary>
public sealed record FetchedReleaseNotes
{
	/// <summary>Loaded bundles per product id. A declared product with no usable bundles maps to an empty list.</summary>
	public required FrozenDictionary<string, IReadOnlyList<LoadedBundle>> BundlesByProduct { get; init; }

	/// <summary>Product ids declared under <c>release_notes</c>, used to distinguish "undeclared" from "declared but empty".</summary>
	public required FrozenSet<string> DeclaredProducts { get; init; }

	public static FetchedReleaseNotes Empty { get; } = new()
	{
		BundlesByProduct = FrozenDictionary<string, IReadOnlyList<LoadedBundle>>.Empty,
		DeclaredProducts = []
	};
}
