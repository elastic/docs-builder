// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Services.ReleaseNotes;

/// <summary>
/// Configuration for release notes generation, including label mappings
/// </summary>
public class ReleaseNotesConfiguration
{
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

