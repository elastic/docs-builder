// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Changelog.GitHub;
using Elastic.Documentation.Configuration.Changelog;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.ReleaseNotes;
using Microsoft.Extensions.Logging;

namespace Elastic.Changelog.Creation;

/// <summary>
/// Service for fetching and processing GitHub issue information for changelog derivation
/// </summary>
public class IssueInfoProcessor(IGitHubPrService? githubService, ILogger logger)
{
	/// <summary>
	/// Fetches issue information and derives changelog fields from it.
	/// </summary>
	public async Task<IssueProcessingResult> ProcessIssueAsync(
		IDiagnosticsCollector collector,
		CreateChangelogArguments input,
		ChangelogConfiguration config,
		string issueUrl,
		Cancel ctx)
	{
		var issueInfo = await TryFetchIssueInfoAsync(issueUrl, input.Owner, input.Repo, ctx);

		if (issueInfo == null)
		{
			collector.EmitWarning(string.Empty, $"Failed to fetch issue information from GitHub for issue: {issueUrl}. Generating basic changelog with provided values.");
			return new IssueProcessingResult
			{
				FetchFailed = true,
				ShouldSkip = false
			};
		}

		if (ShouldSkipIssueDueToLabelBlockers(issueInfo.Labels.ToArray(), input.Products, config, collector, issueUrl))
		{
			return new IssueProcessingResult
			{
				FetchFailed = false,
				ShouldSkip = true
			};
		}

		var derivedFields = DeriveFieldsFromIssue(collector, input, config, issueInfo, issueUrl);

		return new IssueProcessingResult
		{
			FetchFailed = false,
			ShouldSkip = false,
			DerivedFields = derivedFields,
			IssueInfo = issueInfo
		};
	}

	/// <summary>
	/// Checks if an issue should be skipped due to label blockers
	/// </summary>
	public async Task<(bool shouldSkip, GitHubIssueInfo? issueInfo)> CheckIssueForBlockersAsync(
		IDiagnosticsCollector collector,
		string issueUrl,
		string? owner,
		string? repo,
		IReadOnlyList<ProductArgument> products,
		ChangelogConfiguration config,
		Cancel ctx)
	{
		var issueInfo = await TryFetchIssueInfoAsync(issueUrl, owner, repo, ctx);

		if (issueInfo == null)
		{
			collector.EmitWarning(string.Empty, $"Failed to fetch issue information from GitHub for issue: {issueUrl}. Generating basic changelog with provided values.");
			return (false, null);
		}

		var shouldSkip = ShouldSkipIssueDueToLabelBlockers(issueInfo.Labels.ToArray(), products, config, collector, issueUrl);
		return (shouldSkip, issueInfo);
	}

	private DerivedPrFields? DeriveFieldsFromIssue(
		IDiagnosticsCollector collector,
		CreateChangelogArguments input,
		ChangelogConfiguration config,
		GitHubIssueInfo issueInfo,
		string issueUrl)
	{
		var derived = new DerivedPrFields();

		if (input.ExtractReleaseNotes ?? false)
		{
			var (releaseNoteTitle, releaseNoteDescription) = ReleaseNotesExtractor.ExtractReleaseNotes(issueInfo.Body);

			if (releaseNoteTitle != null && string.IsNullOrWhiteSpace(input.Title))
			{
				derived.Title = releaseNoteTitle;
				logger.LogInformation("Using extracted release note as title: {Title}", derived.Title);
			}

			if (releaseNoteDescription != null && string.IsNullOrWhiteSpace(input.Description))
			{
				derived.Description = releaseNoteDescription;
				logger.LogInformation("Using extracted release note as description (length: {Length} characters)", releaseNoteDescription.Length);
			}
		}

		if (string.IsNullOrWhiteSpace(input.Title) && derived.Title == null)
		{
			if (string.IsNullOrWhiteSpace(issueInfo.Title))
			{
				collector.EmitError(string.Empty, $"Issue {issueUrl} does not have a title. Please provide --title or ensure the issue has a title.");
				return null;
			}

			var issueTitle = issueInfo.Title;
			if (input.StripTitlePrefix)
				issueTitle = ChangelogTextUtilities.StripSquareBracketPrefix(issueTitle);
			derived.Title = issueTitle;
			logger.LogInformation("Using issue title: {Title}", derived.Title);
		}
		else if (!string.IsNullOrWhiteSpace(input.Title))
			logger.LogDebug("Using explicitly provided title, ignoring issue title");

		if (string.IsNullOrWhiteSpace(input.Type))
		{
			if (config.LabelToType == null || config.LabelToType.Count == 0)
			{
				collector.EmitError(string.Empty, $"Cannot derive type from issue {issueUrl} labels: no type mapping configured in changelog.yml. Please provide --type or configure pivot.types in changelog.yml.");
				return null;
			}

			var mappedType = PrInfoProcessor.MapLabelsToType(issueInfo.Labels.ToArray(), config.LabelToType);
			if (mappedType == null)
			{
				var availableLabels = issueInfo.Labels.Count > 0 ? string.Join(", ", issueInfo.Labels) : "none";
				collector.EmitError(string.Empty, $"Cannot derive type from issue {issueUrl} labels ({availableLabels}). No matching label found in type mapping. Please provide --type or add pivot.types with labels in changelog.yml.");
				return null;
			}
			derived.Type = mappedType;
			logger.LogInformation("Mapped issue labels to type: {Type}", derived.Type);
		}
		else
			logger.LogDebug("Using explicitly provided type, ignoring issue labels");

		if ((input.Areas == null || input.Areas.Length == 0) && config.LabelToAreas != null)
		{
			var mappedAreas = PrInfoProcessor.MapLabelsToAreas(issueInfo.Labels.ToArray(), config.LabelToAreas);
			if (mappedAreas.Count > 0)
			{
				derived.Areas = mappedAreas.ToArray();
				logger.LogInformation("Mapped issue labels to areas: {Areas}", string.Join(", ", mappedAreas));
			}
		}
		else if (input.Areas is { Length: > 0 })
			logger.LogDebug("Using explicitly provided areas, ignoring issue labels");

		if (input.Highlight == null && config.HighlightLabels is { Count: > 0 })
		{
			var hasHighlightLabel = issueInfo.Labels.Any(label =>
				config.HighlightLabels.Contains(label, StringComparer.OrdinalIgnoreCase));
			if (hasHighlightLabel)
			{
				derived.Highlight = true;
				logger.LogInformation("Issue has highlight label, setting highlight: true");
			}
		}
		else if (input.Highlight != null)
			logger.LogDebug("Using explicitly provided highlight value, ignoring issue labels");

		// Include the current issue in Issues array
		derived.Issues = input.Issues is { Length: > 0 }
			? input.Issues
			: [issueUrl];

		// Extract linked PRs from issue body
		if ((input.ExtractIssues ?? false) && issueInfo.LinkedPrs.Count > 0)
		{
			derived.Prs = issueInfo.LinkedPrs.ToArray();
			logger.LogInformation("Extracted {Count} linked PRs from issue body: {Prs}",
				issueInfo.LinkedPrs.Count, string.Join(", ", issueInfo.LinkedPrs));
		}

		return derived;
	}

	private bool ShouldSkipIssueDueToLabelBlockers(
		string[] issueLabels,
		IReadOnlyList<ProductArgument> products,
		ChangelogConfiguration config,
		IDiagnosticsCollector collector,
		string issueUrl)
	{
		var createRules = config.Rules?.Create;
		if (createRules == null)
			return false;

		if (createRules.ByProduct is { Count: > 0 })
		{
			foreach (var product in products)
			{
				var normalizedProductId = product.Product?.Replace('_', '-') ?? string.Empty;
				if (createRules.ByProduct.TryGetValue(normalizedProductId, out var productRules))
				{
					if (PrInfoProcessor.ShouldSkipByCreateRules(issueLabels, productRules, collector, issueUrl, product.Product))
						return true;
				}
				else if (PrInfoProcessor.ShouldSkipByCreateRules(issueLabels, createRules, collector, issueUrl, null))
					return true;
			}
			return false;
		}

		return PrInfoProcessor.ShouldSkipByCreateRules(issueLabels, createRules, collector, issueUrl, null);
	}

	private async Task<GitHubIssueInfo?> TryFetchIssueInfoAsync(string? issueUrl, string? owner, string? repo, Cancel ctx)
	{
		if (string.IsNullOrWhiteSpace(issueUrl) || githubService == null)
			return null;

		try
		{
			var issueInfo = await githubService.FetchIssueInfoAsync(issueUrl, owner, repo, ctx);
			if (issueInfo != null)
				logger.LogInformation("Successfully fetched issue information from GitHub");
			else
				logger.LogWarning("Unable to fetch issue information from GitHub. Continuing with provided values.");
			return issueInfo;
		}
		catch (Exception ex)
		{
			if (ex is OutOfMemoryException or
				StackOverflowException or
				AccessViolationException or
				ThreadAbortException)
				throw;
			logger.LogWarning(ex, "Error fetching issue information from GitHub. Continuing with provided values.");
			return null;
		}
	}
}

/// <summary>
/// Result of processing issue information
/// </summary>
public record IssueProcessingResult
{
	public required bool FetchFailed { get; init; }
	public required bool ShouldSkip { get; init; }
	public DerivedPrFields? DerivedFields { get; init; }
	public GitHubIssueInfo? IssueInfo { get; init; }
}
