// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Changelog.GitHub;
using Elastic.Documentation.FileSystems;
using Elastic.Documentation.Services;
using Microsoft.Extensions.Logging;

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
		if (isSuccess && !metadata.CanCommit)
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
			return ChangelogCommentRenderer.RenderMissingEntry(metadata.ChangelogDir, metadata.PrNumber);
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
