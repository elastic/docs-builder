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
///   <item><term>Status no-label</term><description>Cannot-generate body with label tables.</description></item>
///   <item><term>Status success, no staged file</term><description>Resolved body (clears a stale failure comment).</description></item>
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

		var body = SelectBody(metadata, input.MetadataDir);
		if (body is null)
		{
			_logger.LogInformation("No comment body selected for PR #{PrNumber} — nothing to post", metadata.PrNumber);
			return true;
		}

		var posted = await commentService.UpsertStickyCommentAsync(owner, repo, metadata.PrNumber, body, ctx);
		if (!posted)
			_logger.LogWarning("Comment post did not succeed for PR #{PrNumber} — continuing", metadata.PrNumber);

		return true;
	}

	private string? SelectBody(GithubDecisionMetadata metadata, string metadataDir)
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

		// Step 1 (label gate): labels are missing — tell the author which ones to add.
		// Dispatches on Gate when present; falls back to status for artifacts written before Gate was added.
		if (isNoLabel && metadata.Gate is null or ValidationGate.Labels)
		{
			_logger.LogInformation("Rendering labels-needed body for PR #{PrNumber}", metadata.PrNumber);
			return ChangelogCommentRenderer.RenderLabelsNeeded(
				metadata.LabelTable,
				metadata.ProductLabelTable,
				metadata.SkipLabels,
				metadata.ConfigFile
			);
		}

		// Success with no staged file: post resolved body to clear a stale failure comment.
		if (isSuccess)
		{
			_logger.LogInformation("Rendering resolved body for PR #{PrNumber}", metadata.PrNumber);
			return ChangelogCommentRenderer.RenderResolved();
		}

		// Skipped: clear any stale failure comment.
		if (IsSkipped(metadata.Status))
		{
			_logger.LogInformation("Rendering skipped body for PR #{PrNumber}", metadata.PrNumber);
			return ChangelogCommentRenderer.RenderSkipped();
		}

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
