// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Configuration.Changelog;

/// <summary>
/// Configuration for bundle operations
/// </summary>
public record BundleConfiguration
{
	/// <summary>
	/// Input directory containing changelog YAML files.
	/// Defaults to "docs/changelog"
	/// </summary>
	public string? Directory { get; init; }

	/// <summary>
	/// Output directory for bundled changelog files.
	/// Defaults to "docs/releases"
	/// </summary>
	public string? OutputDirectory { get; init; }

	/// <summary>
	/// Whether to resolve (copy contents of each changelog file into the entries array).
	/// Defaults to true
	/// </summary>
	public bool Resolve { get; init; } = true;

	/// <summary>
	/// Named bundle profiles for different release scenarios.
	/// </summary>
	public IReadOnlyDictionary<string, BundleProfile>? Profiles { get; init; }
}

/// <summary>
/// A named bundle profile configuration.
/// Profiles can be invoked with a version number or promotion report URL.
/// </summary>
public record BundleProfile
{
	/// <summary>
	/// Product filter pattern for input changelogs.
	/// Format: "product {version} {lifecycle}" where placeholders are substituted at runtime.
	/// Examples:
	/// - "elasticsearch {version} {lifecycle}"
	/// - "cloud-serverless {version} *"
	/// </summary>
	public string? Products { get; init; }

	/// <summary>
	/// Output filename pattern.
	/// {version} is substituted at runtime.
	/// Examples:
	/// - "elasticsearch-{version}.yaml"
	/// - "serverless-{version}.yaml"
	/// </summary>
	public string? Output { get; init; }
}
