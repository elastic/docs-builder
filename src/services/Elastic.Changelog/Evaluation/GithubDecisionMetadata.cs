// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Serialization;
using Elastic.Documentation.Configuration.Changelog;
using Elastic.Documentation.ReleaseNotes;

namespace Elastic.Changelog.Evaluation;

/// <summary>
/// GitHub-steering data transferred between CI jobs: every field drives a GitHub Actions decision —
/// <c>CanCommit</c> → should-commit, <c>HeadRepo</c>/<c>HeadRef</c>/<c>HeadSha</c> → the checkout,
/// <c>ChangelogDir</c>/<c>ChangelogFilename</c> → the write target, and <c>LabelTable</c>/
/// <c>ProductLabelTable</c>/<c>SkipLabels</c>/<c>Status</c> → the PR comment bodies.
/// This is an ephemeral upload that exists only for the duration of one check run.
/// </summary>
public record GithubDecisionMetadata
{
	public required int PrNumber { get; init; }
	public required string HeadRef { get; init; }
	public required string HeadSha { get; init; }
	public required string Status { get; init; }
	public required bool IsFork { get; init; }
	public required bool CanCommit { get; init; }
	public required bool MaintainerCanModify { get; init; }
	public string? HeadRepo { get; init; }
	public string? LabelTable { get; init; }
	public string? ProductLabelTable { get; init; }
	public string? SkipLabels { get; init; }
	public string? ConfigFile { get; init; }
	public string? ChangelogDir { get; init; }
	public string? ChangelogFilename { get; init; }
	public CreateRules? CreateRules { get; init; }
	/// <summary>Outcome of the changelog commit step, written by <c>changelog github-decision</c> after apply.</summary>
	public CommitOutcome? CommitOutcome { get; init; }
	/// <summary>Repo-relative path to the committed changelog file, when <see cref="CommitOutcome"/> is <c>Committed</c>.</summary>
	public string? CommittedFile { get; init; }
}

/// <summary>Outcome of the apply job's changelog commit step.</summary>
public enum CommitOutcome
{
	/// <summary>The commit step has not run or was not recorded.</summary>
	None,
	/// <summary>The changelog file was committed and pushed to the PR branch.</summary>
	Committed,
	/// <summary>The commit step ran but failed.</summary>
	Failed,
}

[JsonSourceGenerationOptions(WriteIndented = true, UseStringEnumConverter = true, PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
[JsonSerializable(typeof(GithubDecisionMetadata))]
[JsonSerializable(typeof(CommitOutcome))]
[JsonSerializable(typeof(CreateRules))]
[JsonSerializable(typeof(FieldMode))]
[JsonSerializable(typeof(MatchMode))]
public sealed partial class GithubDecisionMetadataJsonContext : JsonSerializerContext;
