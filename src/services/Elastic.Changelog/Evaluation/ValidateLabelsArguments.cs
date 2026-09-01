// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Changelog.Evaluation;

/// <summary>Arguments for the changelog validate-labels command.</summary>
public record ValidateLabelsArguments
{
	public required string Config { get; init; }
	public required string[] PrLabels { get; init; }

	// PR context — passed when running under GitHub Actions so a GithubDecisionMetadata file can be
	// written for the downstream github-comment command to pick up.
	public int PrNumber { get; init; }
	public string HeadRef { get; init; } = "";
	public string HeadSha { get; init; } = "";
	public bool IsFork { get; init; }
	public bool CanCommit { get; init; }
	public bool MaintainerCanModify { get; init; }
	public string? HeadRepo { get; init; }
	public string? ConfigFile { get; init; }
}
