// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Changelog;

/// <summary>
/// Data structure for bundled changelog YAML file
/// </summary>
public record BundledChangelogData
{
	public IReadOnlyList<BundledProduct> Products { get; set; } = [];
	public IReadOnlyList<BundledEntry> Entries { get; set; } = [];
}

public record BundledProduct
{
	public string Product { get; set; } = "";
	public string? Target { get; set; }
	public string? Lifecycle { get; set; }
}

public record BundledEntry
{
	public BundledFile? File { get; set; }

	// Resolved changelog fields (only populated when --resolve is used)
	public string? Type { get; set; }
	public string? Title { get; set; }
	public IReadOnlyList<ProductInfo>? Products { get; set; }
	public string? Description { get; set; }
	public string? Impact { get; set; }
	public string? Action { get; set; }
	public string? FeatureId { get; set; }
	public bool? Highlight { get; set; }
	public string? Subtype { get; set; }
	public IReadOnlyList<string>? Areas { get; set; }
	public string? Pr { get; set; }
	public IReadOnlyList<string>? Issues { get; set; }
}

public record BundledFile
{
	public string Name { get; set; } = "";
	public string Checksum { get; set; } = "";
}
