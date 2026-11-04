// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using SharpYaml.Serialization;

namespace Elastic.Documentation.Configuration.ReleaseNotes;

/// <summary>
/// DTO for YAML serialization of bundled changelog data.
/// Maps directly to the bundle YAML file structure.
/// </summary>
public sealed record BundleDto
{
	public List<BundledProductDto>? Products { get; set; }
	/// <summary>
	/// Optional introductory description text for this bundle.
	/// Empty string is the clear-intro sentinel and must be emitted; null is omitted (inherit).
	/// </summary>
	public string? Description { get; set; }
	/// <summary>
	/// Optional release date for this bundle.
	/// Purely informative; rendered after the release heading.
	/// </summary>
	[YamlPropertyName("release-date")]
	public string? ReleaseDate { get; set; }
	/// <summary>
	/// Optional git ref of the published endpoint this bundle was cut for.
	/// Written by commit-range bundling (<c>--end-git-ref</c>); serialized as <c>git_ref</c>.
	/// </summary>
	[YamlPropertyName("git_ref")]
	public string? GitRef { get; set; }
	/// <summary>
	/// Feature IDs that should be hidden when rendering this bundle.
	/// Entries with matching feature-id values will be commented out in the output.
	/// </summary>
	[YamlPropertyName("hide-features")]
	public List<string>? HideFeatures { get; set; }
	public List<BundledEntryDto>? Entries { get; set; }
	/// <summary>
	/// Entries to exclude when this amend file is merged with its parent bundle.
	/// </summary>
	[YamlPropertyName("exclude-entries")]
	public List<BundledEntryDto>? ExcludeEntries { get; set; }
}

/// <summary>
/// DTO for bundled product info in YAML.
/// </summary>
public sealed record BundledProductDto
{
	public string? Product { get; set; }
	public string? Target { get; set; }
	public string? Lifecycle { get; set; }
	/// <summary>
	/// GitHub repository name for generating PR/issue links.
	/// If not specified, falls back to Product ID.
	/// </summary>
	public string? Repo { get; set; }
	/// <summary>
	/// GitHub owner (organization or user) for generating PR/issue links.
	/// If not specified, defaults to "elastic".
	/// </summary>
	public string? Owner { get; set; }
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
	[YamlPropertyName("feature-id")]
	public string? FeatureId { get; set; }
	public bool? Highlight { get; set; }
	public string? Subtype { get; set; }
	public List<string>? Areas { get; set; }
	public string? Pr { get; set; }
	public List<string>? Prs { get; set; }
	public List<string>? Issues { get; set; }
	public string? Link { get; set; }
}

/// <summary>
/// DTO for bundled file info in YAML.
/// </summary>
public sealed record BundledFileDto
{
	public string? Name { get; set; }
	public string? Checksum { get; set; }
}
