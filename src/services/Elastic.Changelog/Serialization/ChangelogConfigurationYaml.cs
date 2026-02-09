// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Changelog.Serialization;

/// <summary>
/// Internal DTO for YAML deserialization of changelog configuration.
/// Maps directly to the YAML file structure.
/// </summary>
internal record ChangelogConfigurationYaml
{
	/// <summary>
	/// Pivot configuration for types, subtypes, and areas with label mappings.
	/// </summary>
	public PivotConfigurationYaml? Pivot { get; set; }

	/// <summary>
	/// Available lifecycle values.
	/// </summary>
	public List<string>? Lifecycles { get; set; }

	/// <summary>
	/// Products configuration.
	/// Can be either:
	/// - A simple list of product IDs (backward compatible) -> parsed as products.available
	/// - A ProductsConfigYaml object with available/default
	/// </summary>
	public ProductsConfigYaml? Products { get; set; }

	/// <summary>
	/// Block configuration combining create and publish blockers.
	/// </summary>
	public BlockConfigurationYaml? Block { get; set; }

	/// <summary>
	/// Extraction configuration for release notes and issues.
	/// </summary>
	public ExtractConfigurationYaml? Extract { get; set; }

	/// <summary>
	/// Bundle configuration with profiles and defaults.
	/// </summary>
	public BundleConfigurationYaml? Bundle { get; set; }
}

/// <summary>
/// Internal DTO for block configuration in YAML.
/// </summary>
internal record BlockConfigurationYaml
{
	/// <summary>
	/// Global labels that block changelog creation (comma-separated string).
	/// </summary>
	public string? Create { get; set; }

	/// <summary>
	/// Configuration for blocking changelog entries from publishing based on type or area.
	/// </summary>
	public PublishBlockerYaml? Publish { get; set; }

	/// <summary>
	/// Per-product override blockers.
	/// Keys can be comma-separated product IDs.
	/// </summary>
	public Dictionary<string, ProductBlockersYaml?>? Product { get; set; }
}

/// <summary>
/// Internal DTO for product-specific blockers in YAML.
/// </summary>
internal record ProductBlockersYaml
{
	/// <summary>
	/// Labels that block creation for this product (comma-separated string).
	/// </summary>
	public string? Create { get; set; }

	/// <summary>
	/// Configuration for blocking changelog entries from publishing based on type or area.
	/// </summary>
	public PublishBlockerYaml? Publish { get; set; }
}

/// <summary>
/// Internal DTO for publish blocker configuration in YAML.
/// </summary>
internal record PublishBlockerYaml
{
	/// <summary>
	/// Entry types to block from publishing.
	/// </summary>
	public List<string>? Types { get; set; }

	/// <summary>
	/// Entry areas to block from publishing.
	/// </summary>
	public List<string>? Areas { get; set; }
}

/// <summary>
/// Internal DTO for pivot configuration in YAML.
/// </summary>
internal record PivotConfigurationYaml
{
	/// <summary>
	/// Type definitions with optional labels and subtypes.
	/// </summary>
	public Dictionary<string, TypeEntryYaml?>? Types { get; set; }

	/// <summary>
	/// Default subtype definitions with optional labels.
	/// </summary>
	public Dictionary<string, string?>? Subtypes { get; set; }

	/// <summary>
	/// Area definitions with labels.
	/// </summary>
	public Dictionary<string, string?>? Areas { get; set; }

	/// <summary>
	/// Labels that trigger the highlight flag (comma-separated string).
	/// </summary>
	public string? Highlight { get; set; }
}

/// <summary>
/// Internal DTO for products configuration in YAML.
/// </summary>
internal record ProductsConfigYaml
{
	/// <summary>
	/// List of available product IDs (empty = all from products.yml).
	/// </summary>
	public List<string>? Available { get; set; }

	/// <summary>
	/// Default products to use when --products is not specified.
	/// </summary>
	public List<DefaultProductYaml>? Default { get; set; }
}

/// <summary>
/// Internal DTO for default product specification in YAML.
/// </summary>
internal record DefaultProductYaml
{
	/// <summary>
	/// Product ID.
	/// </summary>
	public string? Product { get; set; }

	/// <summary>
	/// Default lifecycle (defaults to "ga").
	/// </summary>
	public string? Lifecycle { get; set; }
}

/// <summary>
/// Internal DTO for bundle configuration in YAML.
/// </summary>
internal record BundleConfigurationYaml
{
	/// <summary>
	/// Input directory containing changelog YAML files.
	/// </summary>
	public string? Directory { get; set; }

	/// <summary>
	/// Output directory for bundled changelog files.
	/// </summary>
	public string? OutputDirectory { get; set; }

	/// <summary>
	/// Whether to resolve (copy contents) by default.
	/// </summary>
	public bool? Resolve { get; set; }

	/// <summary>
	/// Named bundle profiles.
	/// </summary>
	public Dictionary<string, BundleProfileYaml>? Profiles { get; set; }
}

/// <summary>
/// Internal DTO for bundle profile in YAML.
/// </summary>
internal record BundleProfileYaml
{
	/// <summary>
	/// Product filter pattern for input changelogs.
	/// Supports {version} and {lifecycle} placeholders.
	/// </summary>
	public string? Products { get; set; }

	/// <summary>
	/// Output filename pattern.
	/// Supports {version} placeholder.
	/// </summary>
	public string? Output { get; set; }

	/// <summary>
	/// Feature IDs to mark as hidden in the bundle output.
	/// </summary>
	public List<string>? HideFeatures { get; set; }
}

/// <summary>
/// Internal DTO for extract configuration in YAML.
/// </summary>
internal record ExtractConfigurationYaml
{
	/// <summary>
	/// Whether to extract release notes from PR descriptions by default.
	/// Defaults to true.
	/// </summary>
	public bool? ReleaseNotes { get; set; }

	/// <summary>
	/// Whether to extract linked issues from PR body by default.
	/// Defaults to true.
	/// </summary>
	public bool? Issues { get; set; }
}

/// <summary>
/// Internal DTO for type entry in YAML.
/// Can represent either a simple label string or an object with labels and subtypes.
/// </summary>
internal record TypeEntryYaml
{
	/// <summary>
	/// Labels for this type (comma-separated string).
	/// </summary>
	public string? Labels { get; set; }

	/// <summary>
	/// Type-specific subtype definitions.
	/// </summary>
	public Dictionary<string, string?>? Subtypes { get; set; }

	/// <summary>
	/// Creates a TypeEntryYaml from a simple label string.
	/// </summary>
	public static TypeEntryYaml FromLabels(string? labels) => new() { Labels = labels };
}
