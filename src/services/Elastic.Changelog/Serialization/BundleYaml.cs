// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using YamlDotNet.Serialization;

namespace Elastic.Changelog.Serialization;

/// <summary>
/// Internal DTO for YAML serialization of bundled changelog data.
/// Maps directly to the bundle YAML file structure.
/// </summary>
internal record BundleYaml
{
	public List<BundledProductYaml>? Products { get; set; }
	public List<BundledEntryYaml>? Entries { get; set; }
}

/// <summary>
/// Internal DTO for bundled product info in YAML.
/// </summary>
internal record BundledProductYaml
{
	public string? Product { get; set; }
	public string? Target { get; set; }
	public string? Lifecycle { get; set; }
}

/// <summary>
/// Internal DTO for bundled entry in YAML.
/// </summary>
internal record BundledEntryYaml
{
	public BundledFileYaml? File { get; set; }
	public string? Type { get; set; }
	public string? Title { get; set; }
	public List<ProductInfoYaml>? Products { get; set; }
	public string? Description { get; set; }
	public string? Impact { get; set; }
	public string? Action { get; set; }
	[YamlMember(Alias = "feature-id", ApplyNamingConventions = false)]
	public string? FeatureId { get; set; }
	public bool? Highlight { get; set; }
	public string? Subtype { get; set; }
	public List<string>? Areas { get; set; }
	public string? Pr { get; set; }
	public List<string>? Issues { get; set; }
}

/// <summary>
/// Internal DTO for bundled file info in YAML.
/// </summary>
internal record BundledFileYaml
{
	public string? Name { get; set; }
	public string? Checksum { get; set; }
}
