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
	/// Default GitHub repository name applied to all profiles that do not specify their own.
	/// Used for generating correct PR/issue links when the product ID differs from the repo name.
	/// </summary>
	public string? Repo { get; init; }

	/// <summary>
	/// Default GitHub repository owner applied to all profiles that do not specify their own.
	/// </summary>
	public string? Owner { get; init; }

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

	/// <summary>
	/// Output products pattern. When set, overrides the products array derived from matched changelogs.
	/// Supports {version} and {lifecycle} placeholders.
	/// </summary>
	public string? OutputProducts { get; init; }

	/// <summary>
	/// GitHub repository name stored on each product in the bundle output.
	/// Used for generating correct PR/issue links when the product ID differs from the repo name.
	/// </summary>
	public string? Repo { get; init; }

	/// <summary>
	/// GitHub repository owner stored on each product in the bundle output.
	/// Used for generating correct PR/issue links. Defaults to "elastic" when not specified.
	/// </summary>
	public string? Owner { get; init; }

	/// <summary>
	/// Feature IDs to mark as hidden in the bundle output.
	/// When the bundle is rendered, entries with matching feature-id values will be commented out.
	/// </summary>
	public IReadOnlyList<string>? HideFeatures { get; init; }
}
