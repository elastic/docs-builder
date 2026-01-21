// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using YamlDotNet.Serialization;

namespace Elastic.Changelog;

/// <summary>
/// Data structure for changelog YAML file matching the exact schema
/// </summary>
public record ChangelogData
{
	// Automated fields
	public string? Pr { get; set; }
	public IReadOnlyList<string>? Issues { get; set; }
	public string Type { get; set; } = "";
	public string? Subtype { get; set; }
	public IReadOnlyList<ProductInfo> Products { get; set; } = [];
	public IReadOnlyList<string>? Areas { get; set; }

	// Non-automated fields
	public string Title { get; set; } = "";
	public string? Description { get; set; }
	public string? Impact { get; set; }
	public string? Action { get; set; }
	[YamlMember(Alias = "feature-id", ApplyNamingConventions = false)]
	public string? FeatureId { get; set; }
	public bool? Highlight { get; set; }
}

public record ProductInfo
{
	public string Product { get; set; } = "";
	public string? Target { get; set; }
	public string? Lifecycle { get; set; }
}
