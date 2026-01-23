// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;
using Elastic.Documentation;

namespace Elastic.Changelog;

/// <summary>
/// Domain type representing bundled changelog data.
/// Contains products and entries for a changelog bundle.
/// </summary>
public record Bundle
{
	/// <summary>Products included in this bundle.</summary>
	public IReadOnlyList<BundledProduct> Products { get; init; } = [];

	/// <summary>Changelog entries in this bundle.</summary>
	public IReadOnlyList<BundledEntry> Entries { get; init; } = [];
}

/// <summary>
/// Product included in a bundle with strongly typed lifecycle.
/// </summary>
public record BundledProduct
{
	/// <summary>
	/// Parameterless constructor for object initializer syntax.
	/// </summary>
	public BundledProduct() { }

	/// <summary>
	/// Constructor with all parameters.
	/// </summary>
	[SetsRequiredMembers]
	public BundledProduct(string productId, string? target = null, Lifecycle? lifecycle = null)
	{
		ProductId = productId;
		Target = target;
		Lifecycle = lifecycle;
	}

	/// <summary>The product identifier.</summary>
	public required string ProductId { get; init; }

	/// <summary>Optional target version.</summary>
	public string? Target { get; init; }

	/// <summary>The lifecycle stage of the feature for this product.</summary>
	public Lifecycle? Lifecycle { get; init; }
}

/// <summary>
/// A changelog entry within a bundle, with strongly typed enums.
/// </summary>
public record BundledEntry
{
	/// <summary>File information (name and checksum).</summary>
	public BundledFile? File { get; init; }

	/// <summary>The type of changelog entry (feature, bug-fix, etc.).</summary>
	public ChangelogEntryType? Type { get; init; }

	/// <summary>The title of the changelog entry.</summary>
	public string? Title { get; init; }

	/// <summary>Products affected by this changelog entry.</summary>
	public IReadOnlyList<ProductReference>? Products { get; init; }

	/// <summary>Optional description with more details.</summary>
	public string? Description { get; init; }

	/// <summary>Impact statement for breaking changes.</summary>
	public string? Impact { get; init; }

	/// <summary>Required action for breaking changes.</summary>
	public string? Action { get; init; }

	/// <summary>Optional feature ID for tracking.</summary>
	public string? FeatureId { get; init; }

	/// <summary>Whether this entry should be highlighted.</summary>
	public bool? Highlight { get; init; }

	/// <summary>The subtype of changelog entry (only applicable to breaking changes).</summary>
	public ChangelogEntrySubtype? Subtype { get; init; }

	/// <summary>Areas affected by this changelog entry.</summary>
	public IReadOnlyList<string>? Areas { get; init; }

	/// <summary>Pull request URL or reference.</summary>
	public string? Pr { get; init; }

	/// <summary>Related issue URLs or references.</summary>
	public IReadOnlyList<string>? Issues { get; init; }
}

/// <summary>
/// File information in a bundled changelog entry.
/// </summary>
public record BundledFile
{
	/// <summary>The filename.</summary>
	public required string Name { get; init; }

	/// <summary>The file checksum.</summary>
	public required string Checksum { get; init; }
}
