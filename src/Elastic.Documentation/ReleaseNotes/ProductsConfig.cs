// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Generator.Equals;

namespace Elastic.Documentation.ReleaseNotes;

/// <summary>
/// Products configuration for changelog
/// </summary>
/// <remarks>
/// Constructed through <c>ChangelogConfiguration</c>.
/// Members are <c>internal init</c>: only tests (<c>InternalsVisibleTo</c>) can build one directly.
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
/// Constructed through <c>ProductsConfig</c>.
/// Members are <c>internal init</c>: only tests (<c>InternalsVisibleTo</c>) can build one directly.
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
