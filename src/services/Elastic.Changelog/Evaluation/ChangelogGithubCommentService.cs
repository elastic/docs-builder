// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Changelog.GitHub;
using Elastic.Documentation.Configuration.ReleaseNotes;
using Elastic.Documentation.FileSystems;
using Elastic.Documentation.ReleaseNotes;
using Elastic.Documentation.Services;
using Microsoft.Extensions.Logging;
using YamlDotNet.Core;

namespace Elastic.Changelog.Evaluation;

/// <summary>
/// Service implementing the hidden <c>changelog github-comment</c> command.
/// Reads the decision metadata and renders + posts the appropriate sticky PR comment.
///
/// Body selection:
/// <list type="table">
///   <item><term><c>CommitOutcome == Committed</c></term><description>Entry-committed body with blob + edit links.</description></item>
///   <item><term><c>CommitOutcome == Failed</c></term><description>Comment-only body, commit-failed variant.</description></item>
///   <item><term>Status success and <c>!CanCommit</c></term><description>Comment-only body (fork / comment-only strategy).</description></item>
///   <item><term>Status no-label</term><description>Labels-needed body with label tables.</description></item>
///   <item><term>Status skipped</term><description>Skipped body (hidden as 'resolved') to clear stale failure comment.</description></item>
///   <item><term>Status missing-impact-action</term><description>Missing-sections body with a copy-pasteable Impact/Action template.</description></item>
///   <item><term>Status success, breaking-change entry</term><description>Keeps the sticky comment and echoes the captured Impact and Action in a collapsible block.</description></item>
///   <item><term>Status success, no staged file</term><description>Deletes the sticky comment — no need to litter once labels are validated.</description></item>
/// </list>
/// </summary>
public class ChangelogGithubCommentService(
	ILoggerFactory logFactory,
	IGitHubCommentService commentService,
	IRunnerTempFileSystem fileSystem
) : IService
{
	private readonly ILogger _logger = logFactory.CreateLogger<ChangelogGithubCommentService>();
	private readonly GithubDecisionMetadataWriter _reader = new(logFactory, fileSystem);

	/// <summary>
	/// Reads metadata, selects the appropriate body, and upserts the sticky comment.
	/// A comment failure logs a warning but returns <c>true</c> so the exit code reflects the
	/// verdict, not a transient API error.
	/// </summary>
	public async Task<bool> PostComment(GithubCommentArguments input, Cancel ctx)
	{
		var metadata = await _reader.ReadAsync(input.MetadataPath, ctx);
		if (metadata is null)
		{
			_logger.LogWarning("Decision metadata not found at {Path} — skipping comment", input.MetadataPath);
			return true;
		}

		// Resolve owner/repo from the GITHUB_REPOSITORY env var injected by the command layer.
		var owner = input.Owner;
		var repo = input.Repo;

		var body = SelectBody(metadata, input.MetadataDir, owner, repo);
		if (body is null)
		{
			if (IsSuccess(metadata.Status) || IsSkipped(metadata.Status))
			{
				_logger.LogInformation(
					"PR #{PrNumber} is {Status} — deleting stale failure comment if present",
					metadata.PrNumber,
					metadata.Status
				);
				_ = await commentService.DeleteStickyCommentAsync(owner, repo, metadata.PrNumber, ctx);
			}
			else
				_logger.LogInformation("No comment body selected for PR #{PrNumber} — nothing to post", metadata.PrNumber);
			return true;
		}

		var nodeId = await commentService.UpsertStickyCommentAsync(owner, repo, metadata.PrNumber, body, ctx);
		if (nodeId is null)
			_logger.LogWarning("Comment post did not succeed for PR #{PrNumber} — continuing", metadata.PrNumber);

		return true;
	}

	private string? SelectBody(GithubDecisionMetadata metadata, string metadataDir, string owner, string repo)
	{
		var body = SelectBaseBody(metadata, metadataDir, owner, repo);

		// Breaking changes keep their comment on success and echo what was captured, so there are no surprises at bundling time.
		var generated = metadata.Gate is not ValidationGate.Entries && (IsSuccess(metadata.Status) || metadata.CommitOutcome is not null);
		if (!generated)
			return body;

		// evaluate-pr records the captured sections in the metadata. Runs that stage an entry file (changelog add) are read from the YAML.
		var breakingTypeName = ChangelogEntryType.BreakingChange.ToStringFast(true);
		if (string.Equals(metadata.ResolvedType, breakingTypeName, StringComparison.OrdinalIgnoreCase))
		{
			_logger.LogInformation("Echoing captured Impact/Action for breaking change in PR #{PrNumber}", metadata.PrNumber);
			// A gate-only run stages no entry file, so the generic comment-only body would warn that the entry could not be read.
			return body is null || metadata.CommitOutcome is null
				? ChangelogCommentRenderer.RenderBreakingChangeResolved(metadata.Impact, metadata.Action)
				: ChangelogCommentRenderer.AppendBreakingChangeCaptured(body, metadata.Impact, metadata.Action);
		}

		var entry = TryReadStagedEntry(metadataDir);
		if (entry is not { Type: ChangelogEntryType.BreakingChange })
			return body;

		_logger.LogInformation("Echoing captured Impact/Action for breaking change in PR #{PrNumber}", metadata.PrNumber);
		return body is null
			? ChangelogCommentRenderer.RenderBreakingChangeResolved(entry.Impact, entry.Action)
			: ChangelogCommentRenderer.AppendBreakingChangeCaptured(body, entry.Impact, entry.Action);
	}

	private ChangelogEntry? TryReadStagedEntry(string metadataDir)
	{
		var (yaml, _) = ReadStagedYaml(metadataDir);
		if (string.IsNullOrWhiteSpace(yaml))
			return null;

		try
		{
			return ReleaseNotesSerialization.DeserializeEntry(yaml);
		}
		catch (YamlException ex)
		{
			_logger.LogWarning(ex, "Could not parse staged changelog YAML from {Dir}", metadataDir);
			return null;
		}
	}

	private string? SelectBaseBody(GithubDecisionMetadata metadata, string metadataDir, string owner, string repo)
	{
		// Gate failure: breaking-change PR lacks Impact and/or Action sections.
		if (
			metadata.Gate == ValidationGate.File
			&& string.Equals(metadata.Status, "missing-impact-action", StringComparison.OrdinalIgnoreCase)
		)
		{
			_logger.LogInformation("Rendering missing-impact-action body for PR #{PrNumber}", metadata.PrNumber);
			return ChangelogCommentRenderer.RenderMissingImpactAction(metadata.MissingSections, metadata.PrNumber);
		}

		// Committed: entry-committed body with blob + edit links.
		if (metadata.CommitOutcome == CommitOutcome.Committed && !string.IsNullOrWhiteSpace(metadata.CommittedFile))
		{
			_logger.LogInformation("Rendering entry-committed body for PR #{PrNumber}", metadata.PrNumber);
			return ChangelogCommentRenderer.RenderEntryCommitted(
				metadata.HeadRepo ?? "",
				metadata.HeadRepo ?? "",
				metadata.HeadRef,
				metadata.CommittedFile
			);
		}

		// Commit failed: comment-only body, commit-failed variant.
		if (metadata.CommitOutcome == CommitOutcome.Failed)
		{
			_logger.LogInformation("Rendering comment-only/commit-failed body for PR #{PrNumber}", metadata.PrNumber);
			var (yamlContent, yamlFilename) = ReadStagedYaml(metadataDir);
			return ChangelogCommentRenderer.RenderCommentOnly(
				metadata.ChangelogDir,
				yamlContent,
				yamlFilename,
				metadata.IsFork,
				commitFailed: true
			);
		}

		var isSuccess = IsSuccess(metadata.Status);
		var isNoLabel = IsNoLabel(metadata.Status);

		// Success but cannot commit (fork or comment-only strategy): comment-only informational body.
		// The Entries gate never stages a YAML file, so CanCommit is irrelevant there.
		if (isSuccess && !metadata.CanCommit && metadata.Gate is not ValidationGate.Entries)
		{
			_logger.LogInformation("Rendering comment-only/no-commit body for PR #{PrNumber}", metadata.PrNumber);
			var (yamlContent, yamlFilename) = ReadStagedYaml(metadataDir);
			return ChangelogCommentRenderer.RenderCommentOnly(
				metadata.ChangelogDir,
				yamlContent,
				yamlFilename,
				metadata.IsFork,
				commitFailed: false
			);
		}

		// Pre-flight (onboarding gate): repository not registered in products.yml.
		if (metadata.Gate == ValidationGate.Onboarding)
		{
			_logger.LogInformation("Rendering repository-not-onboarded body for PR #{PrNumber}", metadata.PrNumber);
			return ChangelogCommentRenderer.RenderRepositoryNotOnboarded(repo);
		}

		// Step 2 (entry gate): changelog entry file content has validation errors.
		if (metadata.Gate == ValidationGate.Entries && metadata.EntryFindings is { Count: > 0 })
		{
			_logger.LogInformation("Rendering entries-invalid body for PR #{PrNumber}", metadata.PrNumber);
			return ChangelogCommentRenderer.RenderEntriesInvalid(metadata.EntryFindings, owner, repo, metadata.DefaultBranch);
		}

		// Step 2 (file gate): changelog file missing (require-changelog-file failure).
		if (metadata.Gate == ValidationGate.File && string.Equals(metadata.Status, "missing-entry", StringComparison.OrdinalIgnoreCase))
		{
			_logger.LogInformation("Rendering missing-entry body for PR #{PrNumber}", metadata.PrNumber);
			return ChangelogCommentRenderer.RenderMissingEntry(
				metadata.ChangelogDir,
				metadata.PrNumber,
				metadata.IsFork,
				metadata.CanCommit,
				metadata.ResolvedProducts
			);
		}

		// Step 1 (label gate): labels are missing — tell the author which ones to add.
		// Dispatches on Gate when present; falls back to status for artifacts written before Gate was added.
		if (isNoLabel && metadata.Gate is null or ValidationGate.Labels)
		{
			_logger.LogInformation("Rendering labels-needed body for PR #{PrNumber}", metadata.PrNumber);
			return ChangelogCommentRenderer.RenderLabelsNeeded(
				metadata.LabelTable,
				metadata.ProductLabelTable,
				metadata.SkipLabels,
				metadata.ConfigFile,
				metadata.AmbiguousTypeLabels,
				owner,
				repo,
				metadata.DefaultBranch
			);
		}

		// Success or skipped: signal to delete the sticky comment (no body needed).
		return null;
	}

	/// <summary>
	/// Reads the first <c>.yaml</c> sibling of <c>metadata.json</c> in the artifact directory.
	/// Returns <c>(null, null)</c> when none is found.
	/// </summary>
	private (string? content, string? filename) ReadStagedYaml(string metadataDir)
	{
		try
		{
			var yamlFiles = fileSystem.Directory.GetFiles(metadataDir, "*.yaml");

			if (yamlFiles.Length == 0)
				return (null, null);

			var first = yamlFiles[0];
			var content = fileSystem.File.ReadAllText(first);
			return (content.Trim(), fileSystem.Path.GetFileName(first));
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			_logger.LogWarning(ex, "Could not read staged YAML from {Dir}", metadataDir);
			return (null, null);
		}
	}

	private static bool IsSuccess(string status) =>
		string.Equals(status, "success", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(status, "proceed", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(status, "ok", StringComparison.OrdinalIgnoreCase);

	private static bool IsNoLabel(string status) => string.Equals(status, "no-label", StringComparison.OrdinalIgnoreCase);
	private static bool IsSkipped(string status) => string.Equals(status, "skipped", StringComparison.OrdinalIgnoreCase);
}

/// <summary>Arguments for the hidden <c>changelog github-comment</c> command.</summary>
public record GithubCommentArguments
{
	public required string MetadataPath { get; init; }
	/// <summary>Directory containing the metadata file (used to locate the staged YAML sibling).</summary>
	public required string MetadataDir { get; init; }
	public required string Owner { get; init; }
	public required string Repo { get; init; }
}
