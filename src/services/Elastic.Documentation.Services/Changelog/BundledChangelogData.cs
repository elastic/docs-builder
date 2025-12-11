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
	public string Version { get; set; } = string.Empty;
}

public class BundledEntry
{
	public string Kind { get; set; } = string.Empty;
	public string Summary { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;
	public string Component { get; set; } = string.Empty;
	public List<string>? Pr { get; set; }
	public List<string>? Issue { get; set; }
	public string Impact { get; set; } = string.Empty;
	public string Action { get; set; } = string.Empty;
	public long Timestamp { get; set; }
	public BundledFile? File { get; set; }
}

public class BundledFile
{
	public string Name { get; set; } = string.Empty;
	public string Checksum { get; set; } = string.Empty;
}

