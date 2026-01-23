// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using YamlDotNet.Serialization;

namespace Elastic.Changelog.Serialization;

/// <summary>
/// DTO for YAML deserialization of changelog entries.
/// Maps directly to the YAML file structure.
/// Used by bundling service for direct deserialization with error handling.
/// </summary>
public record ChangelogEntryYaml
{
	public string? Pr { get; set; }
	public List<string>? Issues { get; set; }
	public string? Type { get; set; }
	public string? Subtype { get; set; }
	public List<ProductInfoYaml>? Products { get; set; }
	public List<string>? Areas { get; set; }
	public string? Title { get; set; }
	public string? Description { get; set; }
	public string? Impact { get; set; }
	public string? Action { get; set; }
	[YamlMember(Alias = "feature-id", ApplyNamingConventions = false)]
	public string? FeatureId { get; set; }
	public bool? Highlight { get; set; }
}

/// <summary>
/// DTO for product info in YAML.
/// Used by bundling service for direct deserialization with error handling.
/// </summary>
public record ProductInfoYaml
{
	public string? Product { get; set; }
	public string? Target { get; set; }
	public string? Lifecycle { get; set; }
}
