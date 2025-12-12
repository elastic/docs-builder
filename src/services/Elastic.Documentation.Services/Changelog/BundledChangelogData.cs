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
}

public class BundledEntry
{
	public BundledFile File { get; set; } = new();
}

public class BundledFile
{
	public string Name { get; set; } = string.Empty;
	public string Checksum { get; set; } = string.Empty;
}

