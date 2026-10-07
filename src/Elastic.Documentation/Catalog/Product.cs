// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using Elastic.Documentation.Search.Contract;
using Generator.Equals;

namespace Elastic.Documentation.Catalog;

/// <summary>Every product from <c>products.yml</c>, with lookups by repository and display name. Part of the <see cref="GlobalCatalog"/> aggregate.</summary>
/// <remarks>
/// <para><b>Built by:</b> <c>CatalogLoader.Load</c> in <c>Elastic.Documentation.Configuration</c>, which reads <c>versions.yml</c>, <c>products.yml</c> and <c>legacy-url-mappings.yml</c> as one unit and returns a <c>Result&lt;GlobalCatalog&gt;</c>, or the pure <see cref="ProductCatalog.FromPublicProducts"/>.</para>
/// <para><b>Why it cannot be built from outside:</b> Its members are <c>internal init</c>: only <c>Elastic.Documentation</c>, <c>Elastic.Documentation.Configuration</c> and the test projects (<c>InternalsVisibleTo</c>) can set them, so a record that reaches a service has been through a loader or a checked operation.</para>
/// <para><b>Members that must always be set</b> (they were <c>required</c> before the type was sealed; C# forbids <c>required</c> on a member whose setter is less visible than the type, CS9032): <c>Products</c>, <c>PublicReferenceProducts</c>, <c>ProductDisplayNames</c>. A loader or test that leaves one out gets the default; the aggregate's constraint set (<see cref="ProductCatalogConstraints"/>) is where a missing member is reported, and the loader's mapping is what normally sets them.</para>
/// <para><b>In a test:</b> build it with <c>CatalogFixtures.Catalog</c> or <c>DomainGen.Of&lt;ProductCatalog&gt;()</c> (tests), or <c>ProductCatalog.FromPublicProducts</c> to keep the three lookups consistent.</para>
/// </remarks>
[Equatable]
public sealed partial record ProductCatalog : IProductNameLookup
{
	[UnorderedEquality]
	public FrozenDictionary<string, Product> Products { get; internal init; } = default!;

	/// <summary>A catalog of <paramref name="products"/>, every one of them a public reference product.</summary>
	public static ProductCatalog FromPublicProducts(IEnumerable<KeyValuePair<string, Product>> products)
	{
		var byId = products.ToFrozenDictionary();
		return new ProductCatalog
		{
			Products = byId,
			PublicReferenceProducts = byId,
			ProductDisplayNames = byId.ToFrozenDictionary(p => p.Key, p => p.Value.DisplayName)
		};
	}

	/// <summary>
	/// Products with the <c>public-reference</c> feature enabled. These can appear in
	/// applies_to blocks, page frontmatter, and get display-name substitutions.
	/// When a product has no explicit <c>features</c> map, it is included here by default.
	/// </summary>
	[UnorderedEquality]
	public FrozenDictionary<string, Product> PublicReferenceProducts { get; internal init; } = default!;

	/// <summary>
	/// Product id to display name mappings for fast lookups.
	/// </summary>
	[UnorderedEquality]
	public FrozenDictionary<string, string> ProductDisplayNames { get; internal init; } = default!;

	private FrozenDictionary<string, string>.AlternateLookup<ReadOnlySpan<char>>? _displayNameLookup;

	/// <summary>
	/// Gets the alternate lookup for span-based product id lookups.
	/// </summary>
	[IgnoreEquality]
	public FrozenDictionary<string, string>.AlternateLookup<ReadOnlySpan<char>> DisplayNameLookup =>
		_displayNameLookup ??= ProductDisplayNames.GetAlternateLookup<ReadOnlySpan<char>>();

	/// <summary>
	/// Returns every product that maps to <paramref name="repository"/>.
	/// A product matches when its ID equals the repo name, or when its explicit
	/// <c>repository:</c> field equals the repo name (case-insensitive).
	/// One repo → many products is valid (e.g., <c>cloud</c> → cloud-hosted / cloud-serverless / cloud-enterprise).
	/// </summary>
	public IReadOnlyList<Product> GetProductsByRepositoryName(string repository)
	{
		var tokens = repository.Split('/');
		var repositoryName = tokens.Last();
		if (Products.TryGetValue(repositoryName, out var direct))
			return [direct];
		return Products
			.Values
			.Where(p => p.Repository is not null && p.Repository.Equals(repositoryName, StringComparison.OrdinalIgnoreCase))
			.ToList();
	}

	/// <summary>
	/// Returns the single product that maps to <paramref name="repository"/>, or <c>null</c>
	/// when there is no match or more than one match. Use <see cref="GetProductsByRepositoryName"/>
	/// when a repo may host multiple products.
	/// </summary>
	public Product? GetProductByRepositoryName(string repository)
	{
		var matches = GetProductsByRepositoryName(repository);
		return matches.Count == 1 ? matches[0] : null;
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

	/// <inheritdoc />
	public bool TryGetProductName(string productId, out string displayName)
	{
		if (ProductDisplayNames.TryGetValue(productId, out var name))
		{
			displayName = name;
			return true;
		}
		displayName = productId;
		return false;
	}
}

/// <summary>
/// The release-notes onboarding path a product follows, declared via <c>features.release-notes</c>
/// in <c>products.yml</c>. See the release-notes onboarding RFC: a product either commits its final
/// release bundles before release (<see cref="Prestage"/>) or cuts them at release time
/// (<see cref="OnRelease"/>, the default).
/// </summary>
public enum ReleaseNotesPath
{
	/// <summary>Product does not participate in release notes automation (<c>release-notes: false</c>).</summary>
	None,

	/// <summary>Final bundles are built and uploaded at release time (<c>release-notes</c> omitted, <c>true</c>, or <c>on-release</c>).</summary>
	OnRelease,

	/// <summary>Release bundles are reviewed and committed to the repository before release (<c>release-notes: prestage</c>).</summary>
	Prestage
}

/// <summary>Declares which docs-builder subsystems a product participates in.</summary>
/// <remarks>
/// <para><b>Built by:</b> <c>CatalogLoader.Load</c> in <c>Elastic.Documentation.Configuration</c>, which reads <c>versions.yml</c>, <c>products.yml</c> and <c>legacy-url-mappings.yml</c> as one unit and returns a <c>Result&lt;GlobalCatalog&gt;</c> (the <c>features</c> block of a product; <c>ProductFeatures.All</c> is the value used when a product declares none).</para>
/// <para><b>Why it cannot be built from outside:</b> Its members are <c>internal init</c>: only <c>Elastic.Documentation</c>, <c>Elastic.Documentation.Configuration</c> and the test projects (<c>InternalsVisibleTo</c>) can set them, so a record that reaches a service has been through a loader or a checked operation.</para>
/// <para><b>In a test:</b> build it with an object initializer through <c>InternalsVisibleTo</c> (<c>new ProductFeatures { ... }</c>) and <c>with</c> for variations.</para>
/// </remarks>
public sealed record ProductFeatures
{
	/// <summary>Product can be referenced in applies_to blocks, page frontmatter, and gets display-name substitutions.</summary>
	public bool PublicReference { get; internal init; }

	/// <summary>
	/// The product's release-notes onboarding path. <see cref="ReleaseNotesPath.OnRelease"/> when
	/// <c>features.release-notes</c> is omitted or <c>true</c> (preserving the historical boolean
	/// participation default), <see cref="ReleaseNotesPath.None"/> when <c>false</c>.
	/// </summary>
	public ReleaseNotesPath ReleaseNotes { get; internal init; }

	/// <summary>Whether the product participates in the changelog / release-notes system at all.</summary>
	public bool ParticipatesInReleaseNotes => ReleaseNotes != ReleaseNotesPath.None;

	/// <summary>All features enabled -- the implicit default when no <c>features</c> map is present in YAML.</summary>
	public static ProductFeatures All => new() { PublicReference = true, ReleaseNotes = ReleaseNotesPath.OnRelease };

	public static readonly FrozenSet<string> KnownKeys = FrozenSet.ToFrozenSet(
		["public-reference", "release-notes"],
		StringComparer.OrdinalIgnoreCase
	);
}

/// <summary>A product from <c>products.yml</c>. Its versioning system is resolved while loading the <see cref="GlobalCatalog"/>.</summary>
/// <remarks>
/// <para><b>Built by:</b> <c>CatalogLoader.Load</c> in <c>Elastic.Documentation.Configuration</c>, which reads <c>versions.yml</c>, <c>products.yml</c> and <c>legacy-url-mappings.yml</c> as one unit and returns a <c>Result&lt;GlobalCatalog&gt;</c>.</para>
/// <para><b>Why it cannot be built from outside:</b> Its members are <c>internal init</c>: only <c>Elastic.Documentation</c>, <c>Elastic.Documentation.Configuration</c> and the test projects (<c>InternalsVisibleTo</c>) can set them, so a record that reaches a service has been through a loader or a checked operation.</para>
/// <para><b>Members that must always be set</b> (they were <c>required</c> before the type was sealed; C# forbids <c>required</c> on a member whose setter is less visible than the type, CS9032): <c>Id</c>, <c>DisplayName</c>. A loader or test that leaves one out gets the default; the aggregate's constraint set (<see cref="ProductCatalogConstraints"/>) reports it: <see cref="ProductCatalogConstraints.ProductIdMatchesKey"/> and <see cref="ProductCatalogConstraints.ProductHasDisplayName"/> report an empty Id or DisplayName.</para>
/// <para><b>In a test:</b> build it with <c>CatalogFixtures.Kibana</c> (tests) or an object initializer (<c>new Product { Id = ..., DisplayName = ... }</c>) and <c>with</c> for variations.</para>
/// </remarks>
public sealed record Product
{
	public string Id { get; internal init; } = default!;
	public string DisplayName { get; internal init; } = default!;
	public VersioningSystem? VersioningSystem { get; internal init; }
	public string? Repository { get; internal init; }
	public ProductFeatures Features { get; internal init; } = ProductFeatures.All;
}
