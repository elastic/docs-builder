// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using YamlDotNet.Serialization;

namespace Elastic.Documentation.Configuration.ReleaseNotes;

/// <summary>
/// DTO for YAML serialization of bundled changelog data.
/// Maps directly to the bundle YAML file structure.
/// </summary>
public sealed record BundleDto
{
	public List<BundledProductDto>? Products { get; set; }
	public List<BundledEntryDto>? Entries { get; set; }
}

/// <summary>
/// DTO for bundled product info in YAML.
/// </summary>
public sealed record BundledProductDto
{
	public string? Product { get; set; }
	public string? Target { get; set; }
	public string? Lifecycle { get; set; }
}

/// <summary>
/// DTO for bundled entry in YAML.
/// </summary>
public sealed record BundledEntryDto
{
	public BundledFileDto? File { get; set; }
	public string? Type { get; set; }
	public string? Title { get; set; }
	public List<ProductInfoDto>? Products { get; set; }
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
/// DTO for bundled file info in YAML.
/// </summary>
public sealed record BundledFileDto
{
	public string? Name { get; set; }
	public string? Checksum { get; set; }
}
