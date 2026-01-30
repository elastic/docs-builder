// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Configuration.Changelog;

/// <summary>
/// Products configuration for changelog
/// </summary>
public record ProductsConfig
{
	/// <summary>
	/// List of available product IDs (empty = all from products.yml).
	/// Use this to restrict which products can be used.
	/// </summary>
	public IReadOnlyList<string>? Available { get; init; }

	/// <summary>
	/// Default products to use when --products is not specified.
	/// Each entry has product ID and optional lifecycle.
	/// </summary>
	public IReadOnlyList<DefaultProduct>? Default { get; init; }
}

/// <summary>
/// Default product specification
/// </summary>
public record DefaultProduct
{
	/// <summary>
	/// Product ID
	/// </summary>
	public required string Product { get; init; }

	/// <summary>
	/// Default lifecycle (defaults to "ga")
	/// </summary>
	public string Lifecycle { get; init; } = "ga";
}
