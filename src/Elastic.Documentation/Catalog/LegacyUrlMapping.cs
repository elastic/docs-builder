// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Generator.Equals;

namespace Elastic.Documentation.Catalog;

/// <summary>Maps legacy <c>/guide</c> URL prefixes to products. Part of the <see cref="GlobalCatalog"/> aggregate.</summary>
/// <remarks>
/// <para><b>Built by:</b> <c>CatalogLoader.Load</c> in <c>Elastic.Documentation.Configuration</c>, which reads <c>versions.yml</c>, <c>products.yml</c> and <c>legacy-url-mappings.yml</c> as one unit and returns a <c>Result&lt;GlobalCatalog&gt;</c> (<c>LegacyUrlMappingCatalog.None</c> is the empty catalog).</para>
/// <para><b>Why it cannot be built from outside:</b> Its members are <c>internal init</c>: only <c>Elastic.Documentation</c>, <c>Elastic.Documentation.Configuration</c> and the test projects (<c>InternalsVisibleTo</c>) can set them, so a record that reaches a service has been through a loader or a checked operation.</para>
/// <para><b>Members that must always be set</b> (they were <c>required</c> before the type was sealed; C# forbids <c>required</c> on a member whose setter is less visible than the type, CS9032): <c>Mappings</c>. A loader or test that leaves one out gets the default; the aggregate's constraint set (<see cref="LegacyUrlMappingCatalogInProductCatalogConstraints"/>) is where a missing member is reported, and the loader's mapping is what normally sets them.</para>
/// <para><b>In a test:</b> build it with an object initializer through <c>InternalsVisibleTo</c> (<c>new LegacyUrlMappingCatalog { ... }</c>) and <c>with</c> for variations.</para>
/// </remarks>
[Equatable]
public sealed partial record LegacyUrlMappingCatalog
{
	[OrderedEquality]
	public IReadOnlyCollection<LegacyUrlMapping> Mappings { get; internal init; } = default!;

	public static LegacyUrlMappingCatalog None { get; } = new() { Mappings = [] };
}

/// <summary>One legacy <c>/guide</c> URL prefix and the product and versions it maps to.</summary>
/// <remarks>
/// <para><b>Built by:</b> <c>CatalogLoader.Load</c> in <c>Elastic.Documentation.Configuration</c>, which reads <c>versions.yml</c>, <c>products.yml</c> and <c>legacy-url-mappings.yml</c> as one unit and returns a <c>Result&lt;GlobalCatalog&gt;</c>.</para>
/// <para><b>Why it cannot be built from outside:</b> Its members are <c>internal init</c>: only <c>Elastic.Documentation</c>, <c>Elastic.Documentation.Configuration</c> and the test projects (<c>InternalsVisibleTo</c>) can set them, so a record that reaches a service has been through a loader or a checked operation.</para>
/// <para><b>Members that must always be set</b> (they were <c>required</c> before the type was sealed; C# forbids <c>required</c> on a member whose setter is less visible than the type, CS9032): <c>BaseUrl</c>, <c>Product</c>, <c>LegacyVersions</c>. A loader or test that leaves one out gets the default; the aggregate's constraint set (<see cref="LegacyUrlMappingCatalogInProductCatalogConstraints"/>) is where a missing member is reported, and the loader's mapping is what normally sets them.</para>
/// <para><b>In a test:</b> build it with an object initializer through <c>InternalsVisibleTo</c> (<c>new LegacyUrlMapping { ... }</c>) and <c>with</c> for variations.</para>
/// </remarks>
[Equatable]
public sealed partial record LegacyUrlMapping
{
	public string BaseUrl { get; internal init; } = default!;
	[DefaultEquality]
	public Product Product { get; internal init; } = default!;
	[OrderedEquality]
	public IReadOnlyCollection<string> LegacyVersions { get; internal init; } = default!;
}
