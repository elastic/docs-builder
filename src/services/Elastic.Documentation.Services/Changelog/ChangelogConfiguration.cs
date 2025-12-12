// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Services.Changelog;

/// <summary>
/// Configuration for changelog generation
/// </summary>
public class ChangelogConfiguration
{
	public List<string> AvailableTypes { get; set; } =
	[
		"feature", // A new feature or enhancement.
		"enhancement", // An improvement to an existing feature.
		"bug-fix", // A bug fix.
		"known-issue", // A problem that is known to exist in the product.
		"breaking-change", // A breaking change to the documented behavior of the product.
		"deprecation", // Functionality that is deprecated and will be removed in a future release.
		"docs", // Major documentation changes or reorganizations.
		"regression", // Functionality that no longer works or behaves incorrectly.
		"security", // An advisory about a potentialsecurity vulnerability.
		"other" // Changes that do not fit into any of the other categories.
	];

	public List<string> AvailableSubtypes { get; set; } =
	[
		"api", // A change that breaks an API.
		"behavioral", // A change that breaks the way something works.
		"configuration", // A change that breaks the configuration.
		"dependency", // A change that breaks a dependency, such as a third-party product.
		"subscription", // A change that breaks licensing behavior.
		"plugin", // A change that breaks a plugin.
		"security", // A change that breaks authentication, authorization, or permissions.
		"other" // A breaking change that do not fit into any of the other categories.
	];

	public List<string> AvailableLifecycles { get; set; } =
	[
		"preview", // A technical preview of a feature or enhancement.
		"beta", // A beta release of a feature or enhancement.
		"ga", // A generally available release of a feature or enhancement.
	];

	public List<string>? AvailableAreas { get; set; }

	public List<string>? AvailableProducts { get; set; }

	/// <summary>
	/// Mapping from GitHub label names to changelog type values
	/// </summary>
	public Dictionary<string, string>? LabelToType { get; set; }

	/// <summary>
	/// Mapping from GitHub label names to changelog area values
	/// Multiple labels can map to the same area, and a single label can map to multiple areas (comma-separated)
	/// </summary>
	public Dictionary<string, string>? LabelToAreas { get; set; }

	public static ChangelogConfiguration Default => new();
}

