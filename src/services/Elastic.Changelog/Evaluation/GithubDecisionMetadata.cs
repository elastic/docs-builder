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
	/// <summary>Comma-separated list of type labels that all matched when only one is allowed.</summary>
	public string? AmbiguousTypeLabels { get; init; }
	public string? ConfigFile { get; init; }
	/// <summary>Default branch for building config file links (e.g. "main"). Defaults to "main" when null.</summary>
	public string? DefaultBranch { get; init; }
	public string? ChangelogDir { get; init; }
	public string? ChangelogFilename { get; init; }
	public CreateRules? CreateRules { get; init; }
	/// <summary>Which validation gate wrote this record. Null for artifacts written before this field was added.</summary>
	public ValidationGate? Gate { get; init; }
	/// <summary>Outcome of the changelog commit step, written by <c>changelog github-decision</c> after apply.</summary>
	public CommitOutcome? CommitOutcome { get; init; }
	/// <summary>Repo-relative path to the committed changelog file, when <see cref="CommitOutcome"/> is <c>Committed</c>.</summary>
	public string? CommittedFile { get; init; }
	/// <summary>Findings from the entry-file validation gate. Non-null and non-empty when <see cref="Gate"/> is <see cref="ValidationGate.Entries"/> and validation failed.</summary>
	public List<EntryFinding>? EntryFindings { get; init; }
}

/// <summary>A single finding from the changelog entry file validation gate.</summary>
public record EntryFinding
{
	public required string File { get; init; }
	public required string Severity { get; init; }
	public required string Message { get; init; }
}

/// <summary>
/// Which validation gate wrote the metadata, used by the comment renderer to select the
/// stage-appropriate body.
/// <list type="bullet">
///   <item><see cref="Labels"/> — written by <c>validate-labels</c> (Step 1: label gate).</item>
///   <item><see cref="File"/> — written by <c>evaluate-pr</c> (Step 2: changelog file gate).</item>
///   <item><see cref="Entries"/> — written by <c>validate-entries</c> (Step 2: entry-file content gate).</item>
/// </list>
/// Nullable on the record so artifacts without this field still deserialize.
/// </summary>
public enum ValidationGate
{
	/// <summary>Written by <c>validate-labels</c>. Only label presence is checked.</summary>
	Labels,
	/// <summary>Written by <c>evaluate-pr</c>. The changelog file presence is evaluated.</summary>
	File,
	/// <summary>Written by <c>validate-entries</c>. The content of changelog entry files is validated.</summary>
	Entries,
	/// <summary>Written by <c>validate-entries</c> pre-flight. The repository is not registered in <c>products.yml</c>.</summary>
	Onboarding,
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
[JsonSerializable(typeof(ValidationGate))]
[JsonSerializable(typeof(CommitOutcome))]
[JsonSerializable(typeof(CreateRules))]
[JsonSerializable(typeof(FieldMode))]
[JsonSerializable(typeof(MatchMode))]
[JsonSerializable(typeof(EntryFinding))]
[JsonSerializable(typeof(List<EntryFinding>))]
public sealed partial class GithubDecisionMetadataJsonContext : JsonSerializerContext;
