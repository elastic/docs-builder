// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Changelog.Evaluation;

/// <summary>Arguments for the changelog evaluate-pr command.</summary>
public record EvaluatePrArguments
{
	public required string Config { get; init; }
	public required string Owner { get; init; }
	public required string Repo { get; init; }
	public required int PrNumber { get; init; }
	public required string PrTitle { get; init; }
	public string? PrBody { get; init; }
	public required string[] PrLabels { get; init; }
	public required string HeadRef { get; init; }
	public required string HeadSha { get; init; }
	public string? EventAction { get; init; }
	public bool TitleChanged { get; init; }
	public bool BodyChanged { get; init; }
	public bool StripTitlePrefix { get; init; }
	public string BotName { get; init; } = "github-actions[bot]";

	/// <summary>
	/// When true, a missing changelog entry file causes evaluation to fail with
	/// <see cref="PrEvaluationResult.MissingEntry"/> instead of proceeding.
	/// Passed as a workflow input (<c>require-changelog-file</c>) so repos can opt into the gate
	/// without editing <c>changelog.yml</c>.
	/// </summary>
	public bool RequireChangelogFile { get; init; }
}
