// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation;
using Elastic.Documentation.ReleaseNotes;

namespace Elastic.Changelog;

/// <summary>
/// Product argument for changelog operations (string-based for CLI input and wildcard filtering).
/// Different from domain ProductReference which uses strongly typed enums.
/// </summary>
public record ProductArgument
{
	/// <summary>Product ID or wildcard pattern.</summary>
	public string? Product { get; init; }

	/// <summary>Target version or wildcard pattern.</summary>
	public string? Target { get; init; }

	/// <summary>Lifecycle string or wildcard pattern.</summary>
	public string? Lifecycle { get; init; }

	/// <summary>
	/// Converts this ProductArgument to a ProductReference domain type.
	/// </summary>
	public ProductReference ToProductReference() => new()
	{
		ProductId = Product ?? "",
		Target = Target,
		Lifecycle = ParseLifecycle(Lifecycle)
	};

	/// <summary>
	/// Converts this ProductArgument to a BundledProduct domain type.
	/// </summary>
	public BundledProduct ToBundledProduct() => new()
	{
		ProductId = Product ?? "",
		Target = Target,
		Lifecycle = ParseLifecycle(Lifecycle)
	};

	private static Lifecycle? ParseLifecycle(string? value)
	{
		if (string.IsNullOrEmpty(value))
			return null;

		return LifecycleExtensions.TryParse(value, out var result, ignoreCase: true, allowMatchingMetadataAttribute: true)
			? result
			: null;
	}
}
