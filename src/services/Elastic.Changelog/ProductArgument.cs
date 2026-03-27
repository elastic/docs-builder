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

	/// <summary>
	/// Formats a product spec string matching the CLI format: "product [target] [lifecycle]".
	/// </summary>
	public string ToSpecString()
	{
		if (string.IsNullOrWhiteSpace(Product))
			return string.Empty;

		var spec = Product;
		if (!string.IsNullOrWhiteSpace(Target))
			spec += $" {Target}";
		if (!string.IsNullOrWhiteSpace(Lifecycle))
			spec += $" {Lifecycle}";
		return spec;
	}

	/// <summary>
	/// Formats a list of product arguments as a comma-separated spec string.
	/// </summary>
	public static string FormatProductSpecs(IReadOnlyList<ProductArgument> products) =>
		string.Join(", ", products.Select(p => p.ToSpecString()).Where(s => s.Length > 0));

	/// <summary>
	/// Parses a comma-separated product spec string into a list of ProductArguments.
	/// Each entry has the format "product [target] [lifecycle]".
	/// </summary>
	public static IReadOnlyList<ProductArgument> ParseProductSpecs(string? specs)
	{
		if (string.IsNullOrWhiteSpace(specs))
			return [];

		var entries = specs.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		var result = new List<ProductArgument>();

		foreach (var entry in entries)
		{
			var parts = entry.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
			if (parts.Length == 0)
				continue;

			result.Add(new ProductArgument
			{
				Product = parts[0],
				Target = parts.Length > 1 ? parts[1] : null,
				Lifecycle = parts.Length > 2 ? parts[2] : null
			});
		}

		return result;
	}

	private static Lifecycle? ParseLifecycle(string? value)
	{
		if (string.IsNullOrEmpty(value))
			return null;

		return LifecycleExtensions.TryParse(value, out var result, ignoreCase: true, allowMatchingMetadataAttribute: true)
			? result
			: null;
	}
}
