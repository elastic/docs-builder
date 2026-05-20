// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using Elastic.Documentation.Configuration.Versions;
using YamlDotNet.Serialization;

namespace Elastic.Documentation.Configuration.Products;

public record ProductsConfiguration
{
	public required FrozenDictionary<string, Product> Products { get; init; }

	/// <summary>
	/// Products with the <c>public-reference</c> feature enabled. These can appear in
	/// applies_to blocks, page frontmatter, and get display-name substitutions.
	/// When a product has no explicit <c>features</c> map, it is included here by default.
	/// </summary>
	public required FrozenDictionary<string, Product> PublicReferenceProducts { get; init; }

	/// <summary>
	/// Product id to display name mappings for fast lookups.
	/// </summary>
	public required FrozenDictionary<string, string> ProductDisplayNames { get; init; }

	private FrozenDictionary<string, string>.AlternateLookup<ReadOnlySpan<char>>? _displayNameLookup;

	/// <summary>
	/// Gets the alternate lookup for span-based product id lookups.
	/// </summary>
	public FrozenDictionary<string, string>.AlternateLookup<ReadOnlySpan<char>> DisplayNameLookup =>
		_displayNameLookup ??= ProductDisplayNames.GetAlternateLookup<ReadOnlySpan<char>>();

	public Product? GetProductByRepositoryName(string repository)
	{
		var tokens = repository.Split('/');
		var repositoryName = tokens.Last();
		if (Products.TryGetValue(repositoryName, out var product))
			return product;
		var match = Products.Values.SingleOrDefault(p => p.Repository is not null && p.Repository.Equals(repositoryName, StringComparison.OrdinalIgnoreCase));
		return match;
	}

	/// <summary>
	/// Gets the display name for a product id. Returns the id if not found.
	/// </summary>
	public string GetDisplayName(string productId) =>
		ProductDisplayNames.TryGetValue(productId, out var displayName) ? displayName : productId;

	/// <summary>
	/// Gets the display name for a product id using span-based lookup. Returns the span as string if not found.
	/// </summary>
	public string GetDisplayName(ReadOnlySpan<char> productId) =>
		DisplayNameLookup.TryGetValue(productId, out var displayName) ? displayName : productId.ToString();
}

[YamlSerializable]
public record ProductLink
{
	public string Id { get; set; } = string.Empty;
}

/// <summary>Declares which docs-builder subsystems a product participates in.</summary>
public record ProductFeatures
{
	/// <summary>Product can be referenced in applies_to blocks, page frontmatter, and gets display-name substitutions.</summary>
	public bool PublicReference { get; init; }

	/// <summary>Product participates in the changelog / release-notes system.</summary>
	public bool ReleaseNotes { get; init; }

	/// <summary>All features enabled -- the implicit default when no <c>features</c> map is present in YAML.</summary>
	public static ProductFeatures All => new() { PublicReference = true, ReleaseNotes = true };

	public static readonly FrozenSet<string> KnownKeys = FrozenSet.ToFrozenSet(["public-reference", "release-notes"], StringComparer.OrdinalIgnoreCase);
}

[YamlSerializable]
public record Product
{
	public required string Id { get; init; }
	public required string DisplayName { get; init; }
	public VersioningSystem? VersioningSystem { get; init; }
	public string? Repository { get; init; }
	public ProductFeatures Features { get; init; } = ProductFeatures.All;
}

