// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.ReleaseNotes;

/// <summary>
/// Domain type representing bundled changelog data.
/// Contains products and entries for a changelog bundle.
/// </summary>
public record Bundle
{
	/// <summary>Products included in this bundle.</summary>
	public IReadOnlyList<BundledProduct> Products { get; init; } = [];

	/// <summary>
	/// Feature IDs that should be hidden when rendering this bundle.
	/// Entries with matching feature-id values will be commented out in the output.
	/// </summary>
	public IReadOnlyList<string> HideFeatures { get; init; } = [];

	/// <summary>Changelog entries in this bundle.</summary>
	public IReadOnlyList<BundledEntry> Entries { get; init; } = [];
}
