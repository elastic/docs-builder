// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Services.ReleaseNotes;

/// <summary>
/// Configuration for release notes generation, including label mappings
/// </summary>
public class ReleaseNotesConfiguration
{
	public LabelMappings LabelMappings { get; set; } = new();
	public List<string> AvailableTypes { get; set; } =
	[
		"feature",
		"enhancement",
		"bug-fix",
		"known-issue",
		"breaking-change",
		"deprecation",
		"docs",
		"regression",
		"security",
		"other"
	];

	public List<string> AvailableSubtypes { get; set; } =
	[
		"api",
		"behavioral",
		"configuration",
		"dependency",
		"subscription",
		"plugin",
		"security",
		"other"
	];

	public List<string> AvailableLifecycles { get; set; } =
	[
		"preview",
		"beta",
		"ga"
	];

	public static ReleaseNotesConfiguration Default => new();
}

public class LabelMappings
{
	/// <summary>
	/// Maps PR labels to type values (e.g., "bug" -> "bug-fix")
	/// </summary>
	public Dictionary<string, string> Type { get; set; } = [];

	/// <summary>
	/// Maps PR labels to subtype values (e.g., "breaking:api" -> "api")
	/// </summary>
	public Dictionary<string, string> Subtype { get; set; } = [];

	/// <summary>
	/// Maps PR labels to product IDs (e.g., "product:elasticsearch" -> "elasticsearch")
	/// </summary>
	public Dictionary<string, string> Product { get; set; } = [];

	/// <summary>
	/// Maps PR labels to area values (e.g., "area:search" -> "search")
	/// </summary>
	public Dictionary<string, string> Area { get; set; } = [];

	/// <summary>
	/// Maps PR labels to lifecycle values (e.g., "lifecycle:preview" -> "preview")
	/// </summary>
	public Dictionary<string, string> Lifecycle { get; set; } = [];

	/// <summary>
	/// Maps PR labels to highlight flag (e.g., "highlight" -> true)
	/// </summary>
	public Dictionary<string, bool> Highlight { get; set; } = [];
}
