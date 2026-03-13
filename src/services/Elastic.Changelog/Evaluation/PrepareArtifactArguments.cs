// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Changelog.Evaluation;

/// <summary>Arguments for the changelog prepare-artifact command.</summary>
public record PrepareArtifactArguments
{
	public required string StagingDir { get; init; }
	public required string OutputDir { get; init; }
	public required string EvaluateStatus { get; init; }
	public required string GenerateOutcome { get; init; }
	public required int PrNumber { get; init; }
	public required string HeadRef { get; init; }
	public required string HeadSha { get; init; }
	public string? LabelTable { get; init; }
	public string? Config { get; init; }

	/// <summary>
	/// Filename of a previously committed changelog for this PR.
	/// When set, the staging file is renamed to match so the same path is overwritten on the branch.
	/// </summary>
	public string? ExistingChangelogFilename { get; init; }
}
