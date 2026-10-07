// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Generator.Equals;

namespace Elastic.Documentation.ReleaseNotes;

/// <summary>
/// Products configuration for changelog
/// </summary>
/// <remarks>
/// <para><b>Built by:</b> <c>ChangelogConfigurationLoader</c> in <c>Elastic.Documentation.Configuration</c> (<c>Load</c>, <c>LoadRequired</c>, <c>LoadForProfileMode</c>), reading <c>changelog.yml</c> (the <c>products</c> block).</para>
/// <para><b>Why it cannot be built from outside:</b> Its members are <c>internal init</c>: only <c>Elastic.Documentation</c>, <c>Elastic.Documentation.Configuration</c> and the test projects (<c>InternalsVisibleTo</c>) can set them, so a record that reaches a service has been through a loader or a checked operation.</para>
/// <para><b>In a test:</b> build it with an object initializer through <c>InternalsVisibleTo</c> (<c>new ProductsConfig { ... }</c>) and <c>with</c> for variations.</para>
/// </remarks>
[Equatable]
public partial record ProductsConfig
{
	/// <summary>
	/// List of available product IDs (empty = all from products.yml).
	/// Use this to restrict which products can be used.
	/// </summary>
	[OrderedEquality]
	public IReadOnlyList<string>? Available { get; internal init; }

	/// <summary>
	/// Default products to use when --products is not specified.
	/// Each entry has product ID and optional lifecycle.
	/// </summary>
	[OrderedEquality]
	public IReadOnlyList<DefaultProduct>? Default { get; internal init; }
}

/// <summary>
/// Default product specification
/// </summary>
/// <remarks>
/// <para><b>Built by:</b> <c>ChangelogConfigurationLoader</c> in <c>Elastic.Documentation.Configuration</c> (<c>Load</c>, <c>LoadRequired</c>, <c>LoadForProfileMode</c>), reading <c>changelog.yml</c> (the <c>products.default</c> block).</para>
/// <para><b>Why it cannot be built from outside:</b> Its members are <c>internal init</c>: only <c>Elastic.Documentation</c>, <c>Elastic.Documentation.Configuration</c> and the test projects (<c>InternalsVisibleTo</c>) can set them, so a record that reaches a service has been through a loader or a checked operation.</para>
/// <para><b>Members that must always be set</b> (they were <c>required</c> before the type was sealed; C# forbids <c>required</c> on a member whose setter is less visible than the type, CS9032): <c>Product</c>. A loader or test that leaves one out gets the default; the aggregate's constraint set (<see cref="ChangelogConfigurationConstraints"/>) reports it: <see cref="ChangelogConfigurationConstraints.DefaultProductHasId"/> reports a default product with no id.</para>
/// <para><b>In a test:</b> build it with an object initializer through <c>InternalsVisibleTo</c> (<c>new DefaultProduct { ... }</c>) and <c>with</c> for variations.</para>
/// </remarks>
public record DefaultProduct
{
	/// <summary>
	/// Product ID
	/// </summary>
	public string Product { get; internal init; } = default!;

	/// <summary>
	/// Default lifecycle (defaults to "ga")
	/// </summary>
	public string Lifecycle { get; internal init; } = "ga";
}
