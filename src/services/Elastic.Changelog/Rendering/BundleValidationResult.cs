// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.ReleaseNotes;

namespace Elastic.Changelog.Rendering;

/// <summary>
/// Result of bundle validation containing validated bundle data
/// </summary>
public record BundleValidationResult
{
	/// <summary>
	/// Whether validation was successful
	/// </summary>
	public required bool IsValid { get; init; }

	/// <summary>
	/// List of validated bundle data with their inputs and directories
	/// </summary>
	public required IReadOnlyList<ValidatedBundle> Bundles { get; init; }

	/// <summary>
	/// File names seen across bundles (for duplicate detection)
	/// </summary>
	public required IReadOnlyDictionary<string, List<string>> SeenFileNames { get; init; }

	/// <summary>
	/// PRs seen across bundles (for duplicate detection)
	/// </summary>
	public required IReadOnlyDictionary<string, List<string>> SeenPrs { get; init; }
}

/// <summary>
/// A validated bundle with its associated metadata
/// </summary>
public record ValidatedBundle
{
	public required Bundle Data { get; init; }
	public required BundleInput Input { get; init; }
	public required string Directory { get; init; }
}
