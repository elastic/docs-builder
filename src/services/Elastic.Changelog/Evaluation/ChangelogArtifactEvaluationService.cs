// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;
using System.IO.Abstractions;
using System.Text.Json;
using Actions.Core.Services;
using Elastic.Changelog.Creation;
using Elastic.Changelog.GitHub;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Services;
using Microsoft.Extensions.Logging;

namespace Elastic.Changelog.Evaluation;

/// <summary>Service implementing the changelog evaluate-artifact CI command.</summary>
public class ChangelogArtifactEvaluationService(
	ILoggerFactory logFactory,
	IGitHubPrService gitHubPrService,
	ICoreService coreService,
	IFileSystem? fileSystem = null
) : IService
{
	private readonly ILogger _logger = logFactory.CreateLogger<ChangelogArtifactEvaluationService>();
	private readonly IFileSystem _fileSystem = fileSystem ?? new FileSystem();

	public async Task<bool> EvaluateArtifact(IDiagnosticsCollector collector, EvaluateArtifactArguments input, Cancel ctx)
	{
		if (!_fileSystem.File.Exists(input.MetadataPath))
		{
			_logger.LogInformation("Metadata file not found at {Path}, nothing to evaluate", input.MetadataPath);
			return true;
		}

		var artifactMetadataJson = await _fileSystem.File.ReadAllTextAsync(input.MetadataPath, ctx);
		var metadata = JsonSerializer.Deserialize(artifactMetadataJson, ChangelogArtifactMetadataJsonContext.Default.ChangelogArtifactMetadata);
		if (metadata is null)
		{
			collector.EmitError(input.MetadataPath, "Failed to deserialize artifact metadata");
			return false;
		}

		// Fetch current PR info from GitHub to validate whether the artifact is still relevant
		var prInfo = await gitHubPrService.FetchPrInfoAsync(
			metadata.PrNumber.ToString(CultureInfo.InvariantCulture), input.Owner, input.Repo, ctx
		);
		if (prInfo is null)
		{
			collector.EmitError(input.MetadataPath, $"Failed to fetch PR #{metadata.PrNumber} from GitHub");
			return false;
		}

		// Validate SHA
		if (!string.Equals(prInfo.HeadSha, metadata.HeadSha, StringComparison.OrdinalIgnoreCase))
		{
			_logger.LogInformation("PR head has moved ({OldSha} → {NewSha}), skipping — newer run will handle it",
				metadata.HeadSha, prInfo.HeadSha);
			return true;
		}

		// Re-check labels against create rules
		if (PrInfoProcessor.AreAllProductsBlocked(prInfo.Labels.ToArray(), metadata.CreateRules))
		{
			_logger.LogInformation("Labels changed since generate, all products blocked — aborting gracefully");
			return true;
		}

		// Set action flags
		var shouldCommit = false;
		var shouldCommentSuccess = false;
		var shouldCommentFailure = false;

		var statusParsed = PrEvaluationResultExtensions.TryParse(
			metadata.Status, out var metadataStatus, ignoreCase: true, allowMatchingMetadataAttribute: true
		);

		if (statusParsed && metadataStatus == PrEvaluationResult.Success)
		{
			if (input.CommentOnly || prInfo.IsFork)
				shouldCommentSuccess = true;
			else
				shouldCommit = true;
		}

		if (statusParsed && metadataStatus == PrEvaluationResult.NoLabel)
			shouldCommentFailure = true;

		await coreService.SetOutputAsync("pr-number", metadata.PrNumber.ToString(CultureInfo.InvariantCulture));
		await coreService.SetOutputAsync("head-ref", metadata.HeadRef);
		await coreService.SetOutputAsync("head-sha", metadata.HeadSha);
		await coreService.SetOutputAsync("status", metadata.Status);
		await coreService.SetOutputAsync("config-file", metadata.ConfigFile ?? string.Empty);
		await coreService.SetOutputAsync("changelog-dir", metadata.ChangelogDir ?? string.Empty);
		await coreService.SetOutputAsync("changelog-filename", metadata.ChangelogFilename ?? string.Empty);
		await coreService.SetOutputAsync("label-table", metadata.LabelTable ?? string.Empty);
		await coreService.SetOutputAsync("should-commit", shouldCommit ? "true" : "false");
		await coreService.SetOutputAsync("should-comment-success", shouldCommentSuccess ? "true" : "false");
		await coreService.SetOutputAsync("should-comment-failure", shouldCommentFailure ? "true" : "false");

		_logger.LogInformation(
			"Artifact evaluation complete: status={Status}, commit={Commit}, commentSuccess={CommentSuccess}, commentFailure={CommentFailure}",
			metadata.Status, shouldCommit, shouldCommentSuccess, shouldCommentFailure);

		return true;
	}
}
