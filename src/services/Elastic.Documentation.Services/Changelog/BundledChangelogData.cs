// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Services.Changelog;

/// <summary>
/// Data structure for bundled changelog YAML file
/// </summary>
public class BundledChangelogData
{
	public List<BundledProduct> Products { get; set; } = [];
	public List<BundledEntry> Entries { get; set; } = [];
}

public class BundledProduct
{
	public string Product { get; set; } = string.Empty;
	public string? Target { get; set; }
	public string? Lifecycle { get; set; }
}

public class BundledEntry
{
	public BundledFile File { get; set; } = new();

	// Resolved changelog fields (only populated when --resolve is used)
	public string? Type { get; set; }
	public string? Title { get; set; }
	public List<ProductInfo>? Products { get; set; }
	public string? Description { get; set; }
	public string? Impact { get; set; }
	public string? Action { get; set; }
	public string? FeatureId { get; set; }
	public bool? Highlight { get; set; }
	public string? Subtype { get; set; }
	public List<string>? Areas { get; set; }
	public string? Pr { get; set; }
	public List<string>? Issues { get; set; }
}

public class BundledFile
{
	public string Name { get; set; } = string.Empty;
	public string Checksum { get; set; } = string.Empty;
}

