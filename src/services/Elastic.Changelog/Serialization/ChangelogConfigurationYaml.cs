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
	/// Available products - list of product IDs.
	/// </summary>
	public List<string>? Products { get; set; }

	/// <summary>
	/// Block configuration combining create and publish blockers.
	/// </summary>
	public BlockConfigurationYaml? Block { get; set; }
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
	/// Global labels that block changelog publishing/rendering (comma-separated string).
	/// </summary>
	public string? Publish { get; set; }

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
	/// Labels that block publishing for this product (comma-separated string).
	/// </summary>
	public string? Publish { get; set; }
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
