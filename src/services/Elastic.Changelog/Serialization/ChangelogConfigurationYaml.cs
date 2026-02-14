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
	/// Available lifecycle values (string or list).
	/// </summary>
	public YamlLenientList? Lifecycles { get; set; }

	/// <summary>
	/// Products configuration.
	/// Can be either:
	/// - A simple list of product IDs (backward compatible) -> parsed as products.available
	/// - A ProductsConfigYaml object with available/default
	/// </summary>
	public ProductsConfigYaml? Products { get; set; }

	/// <summary>
	/// Rules configuration combining create and publish blockers (new format).
	/// </summary>
	public RulesConfigurationYaml? Rules { get; set; }

	/// <summary>
	/// Old block configuration key. If present, emit error directing user to rename to 'rules'.
	/// </summary>
	public object? Block { get; set; }

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
/// Internal DTO for rules configuration in YAML.
/// </summary>
internal record RulesConfigurationYaml
{
	/// <summary>
	/// Global match mode for multi-valued fields ("any" or "all"). Default: "any".
	/// </summary>
	public string? Match { get; set; }

	/// <summary>
	/// Create rules controlling which PRs generate changelog entries.
	/// </summary>
	public CreateRulesYaml? Create { get; set; }

	/// <summary>
	/// Publish rules controlling which entries appear in rendered output.
	/// </summary>
	public PublishRulesYaml? Publish { get; set; }
}

/// <summary>
/// Internal DTO for create rules in YAML.
/// </summary>
internal record CreateRulesYaml
{
	/// <summary>
	/// Labels to exclude (string or list). Cannot be combined with Include.
	/// </summary>
	public YamlLenientList? Exclude { get; set; }

	/// <summary>
	/// Labels to include (string or list). Cannot be combined with Include.
	/// </summary>
	public YamlLenientList? Include { get; set; }

	/// <summary>
	/// Match mode for labels ("any" or "all"). Inherits from rules.match if not set.
	/// </summary>
	public string? Match { get; set; }

	/// <summary>
	/// Per-product create rule overrides.
	/// Keys can be comma-separated product IDs.
	/// </summary>
	public Dictionary<string, CreateRulesYaml?>? Products { get; set; }
}

/// <summary>
/// Internal DTO for publish rules in YAML.
/// </summary>
internal record PublishRulesYaml
{
	/// <summary>
	/// Match mode for areas ("any" or "all"). Inherits from rules.match if not set.
	/// </summary>
	public string? MatchAreas { get; set; }

	/// <summary>
	/// Entry types to exclude from publishing (string or list).
	/// </summary>
	public YamlLenientList? ExcludeTypes { get; set; }

	/// <summary>
	/// Entry types to include for publishing (string or list, only these types are shown).
	/// </summary>
	public YamlLenientList? IncludeTypes { get; set; }

	/// <summary>
	/// Entry areas to exclude from publishing (string or list).
	/// </summary>
	public YamlLenientList? ExcludeAreas { get; set; }

	/// <summary>
	/// Entry areas to include for publishing (string or list, only these areas are shown).
	/// </summary>
	public YamlLenientList? IncludeAreas { get; set; }

	/// <summary>
	/// Per-product publish rule overrides.
	/// Keys can be comma-separated product IDs.
	/// </summary>
	public Dictionary<string, PublishRulesYaml?>? Products { get; set; }
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
	/// Default subtype definitions with optional labels (string or list per value).
	/// </summary>
	public Dictionary<string, YamlLenientList?>? Subtypes { get; set; }

	/// <summary>
	/// Area definitions with labels (string or list per value).
	/// </summary>
	public Dictionary<string, YamlLenientList?>? Areas { get; set; }

	/// <summary>
	/// Labels that trigger the highlight flag (string or list).
	/// </summary>
	public YamlLenientList? Highlight { get; set; }
}

/// <summary>
/// Internal DTO for products configuration in YAML.
/// </summary>
internal record ProductsConfigYaml
{
	/// <summary>
	/// List of available product IDs (string or list, empty = all from products.yml).
	/// </summary>
	public YamlLenientList? Available { get; set; }

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
	/// Feature IDs to mark as hidden in the bundle output (string or list).
	/// </summary>
	public YamlLenientList? HideFeatures { get; set; }
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
	/// Type-specific subtype definitions (string or list per value).
	/// </summary>
	public Dictionary<string, YamlLenientList?>? Subtypes { get; set; }

	/// <summary>
	/// Creates a TypeEntryYaml from a simple label string.
	/// </summary>
	public static TypeEntryYaml FromLabels(string? labels) => new() { Labels = labels };
}
