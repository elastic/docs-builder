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
/// Service for fetching and processing PR information
/// </summary>
public class PrInfoProcessor(IGitHubPrService? githubPrService, ILogger logger)
{
	/// <summary>
	/// Fetches PR information and derives changelog fields from it.
	/// </summary>
	public async Task<PrProcessingResult> ProcessPrAsync(
		IDiagnosticsCollector collector,
		CreateChangelogArguments input,
		ChangelogConfiguration config,
		string prUrl,
		Cancel ctx)
	{
		var prInfo = await TryFetchPrInfoAsync(prUrl, input.Owner, input.Repo, ctx);

		if (prInfo == null)
		{
			collector.EmitWarning(string.Empty, $"Failed to fetch PR information from GitHub for PR: {prUrl}. Generating basic changelog with provided values.");
			return new PrProcessingResult
			{
				FetchFailed = true,
				ShouldSkip = false
			};
		}

		// Check for label blockers
		if (ShouldSkipPrDueToLabelBlockers(prInfo.Labels.ToArray(), input.Products, config, collector, prUrl))
		{
			return new PrProcessingResult
			{
				FetchFailed = false,
				ShouldSkip = true
			};
		}

		// Process PR info and derive fields
		var derivedFields = DeriveFieldsFromPr(collector, input, config, prInfo, prUrl);

		return new PrProcessingResult
		{
			FetchFailed = false,
			ShouldSkip = false,
			DerivedFields = derivedFields,
			PrInfo = prInfo
		};
	}

	/// <summary>
	/// Checks if a PR should be skipped due to label blockers (for multi-PR processing).
	/// </summary>
	public async Task<(bool shouldSkip, GitHubPrInfo? prInfo)> CheckPrForBlockersAsync(
		IDiagnosticsCollector collector,
		string prUrl,
		string? owner,
		string? repo,
		IReadOnlyList<ProductArgument> products,
		ChangelogConfiguration config,
		Cancel ctx)
	{
		var prInfo = await TryFetchPrInfoAsync(prUrl, owner, repo, ctx);

		if (prInfo == null)
		{
			collector.EmitWarning(string.Empty, $"Failed to fetch PR information from GitHub for PR: {prUrl}. Generating basic changelog with provided values.");
			return (false, null);
		}

		var shouldSkip = ShouldSkipPrDueToLabelBlockers(prInfo.Labels.ToArray(), products, config, collector, prUrl);
		return (shouldSkip, prInfo);
	}

	private DerivedPrFields? DeriveFieldsFromPr(
		IDiagnosticsCollector collector,
		CreateChangelogArguments input,
		ChangelogConfiguration config,
		GitHubPrInfo prInfo,
		string prUrl)
	{
		var derived = new DerivedPrFields();

		// Extract release notes from PR body if requested
		if (input.ExtractReleaseNotes)
		{
			var (releaseNoteTitle, releaseNoteDescription) = ReleaseNotesExtractor.ExtractReleaseNotes(prInfo.Body);

			// Use short release note as title if title was not explicitly provided
			if (releaseNoteTitle != null && string.IsNullOrWhiteSpace(input.Title))
			{
				derived.Title = releaseNoteTitle;
				logger.LogInformation("Using extracted release note as title: {Title}", derived.Title);
			}

			// Use long release note as description if description was not explicitly provided
			if (releaseNoteDescription != null && string.IsNullOrWhiteSpace(input.Description))
			{
				derived.Description = releaseNoteDescription;
				logger.LogInformation("Using extracted release note as description (length: {Length} characters)", releaseNoteDescription.Length);
			}
		}

		// Use PR title if title was not explicitly provided and not already derived
		if (string.IsNullOrWhiteSpace(input.Title) && derived.Title == null)
		{
			if (string.IsNullOrWhiteSpace(prInfo.Title))
			{
				collector.EmitError(string.Empty, $"PR {prUrl} does not have a title. Please provide --title or ensure the PR has a title.");
				return null;
			}

			var prTitle = prInfo.Title;
			// Strip prefix if requested
			if (input.StripTitlePrefix)
				prTitle = ChangelogTextUtilities.StripSquareBracketPrefix(prTitle);
			derived.Title = prTitle;
			logger.LogInformation("Using PR title: {Title}", derived.Title);
		}
		else if (!string.IsNullOrWhiteSpace(input.Title))
			logger.LogDebug("Using explicitly provided title, ignoring PR title");

		// Map labels to type if type was not explicitly provided
		if (string.IsNullOrWhiteSpace(input.Type))
		{
			if (config.LabelToType == null || config.LabelToType.Count == 0)
			{
				collector.EmitError(string.Empty, $"Cannot derive type from PR {prUrl} labels: no type mapping configured in changelog.yml. Please provide --type or configure pivot.types in changelog.yml.");
				return null;
			}

			var mappedType = MapLabelsToType(prInfo.Labels.ToArray(), config.LabelToType);
			if (mappedType == null)
			{
				var availableLabels = prInfo.Labels.Count > 0 ? string.Join(", ", prInfo.Labels) : "none";
				collector.EmitError(string.Empty, $"Cannot derive type from PR {prUrl} labels ({availableLabels}). No matching label found in type mapping. Please provide --type or add pivot.types with labels in changelog.yml.");
				return null;
			}
			derived.Type = mappedType;
			logger.LogInformation("Mapped PR labels to type: {Type}", derived.Type);
		}
		else
			logger.LogDebug("Using explicitly provided type, ignoring PR labels");

		// Map labels to areas if areas were not explicitly provided
		if ((input.Areas == null || input.Areas.Length == 0) && config.LabelToAreas != null)
		{
			var mappedAreas = MapLabelsToAreas(prInfo.Labels.ToArray(), config.LabelToAreas);
			if (mappedAreas.Count > 0)
			{
				derived.Areas = mappedAreas.ToArray();
				logger.LogInformation("Mapped PR labels to areas: {Areas}", string.Join(", ", mappedAreas));
			}
		}
		else if (input.Areas is { Length: > 0 })
			logger.LogDebug("Using explicitly provided areas, ignoring PR labels");

		// Check highlight labels if CLI highlight not set
		if (input.Highlight == null && config.HighlightLabels is { Count: > 0 })
		{
			var hasHighlightLabel = prInfo.Labels.Any(label =>
				config.HighlightLabels.Contains(label, StringComparer.OrdinalIgnoreCase));
			if (hasHighlightLabel)
			{
				derived.Highlight = true;
				logger.LogInformation("PR has highlight label, setting highlight: true");
			}
		}
		else if (input.Highlight != null)
			logger.LogDebug("Using explicitly provided highlight value, ignoring PR labels");

		// Extract linked issues from PR body if config enabled and issues not provided
		if (input.ExtractIssues && (input.Issues == null || input.Issues.Length == 0))
		{
			if (prInfo.LinkedIssues.Count > 0)
			{
				derived.Issues = prInfo.LinkedIssues.ToArray();
				logger.LogInformation("Extracted {Count} linked issues from PR body: {Issues}",
					prInfo.LinkedIssues.Count, string.Join(", ", prInfo.LinkedIssues));
			}
		}
		else if (input.Issues is { Length: > 0 })
			logger.LogDebug("Using explicitly provided issues, ignoring PR body");

		return derived;
	}

	private bool ShouldSkipPrDueToLabelBlockers(
		string[] prLabels,
		IReadOnlyList<ProductArgument> products,
		ChangelogConfiguration config,
		IDiagnosticsCollector collector,
		string prUrl)
	{
		var createRules = config.Rules?.Create;
		if (createRules == null)
			return false;

		// Check product-specific overrides first, then fall back to global
		if (createRules.ByProduct is { Count: > 0 })
		{
			foreach (var product in products)
			{
				var normalizedProductId = product.Product?.Replace('_', '-') ?? string.Empty;
				if (createRules.ByProduct.TryGetValue(normalizedProductId, out var productRules))
				{
					// Product-specific rules override global rules
					if (ShouldSkipByCreateRules(prLabels, productRules, collector, prUrl, product.Product))
						return true;
				}
				else if (ShouldSkipByCreateRules(prLabels, createRules, collector, prUrl, null))
					return true;
			}
			return false;
		}

		// No product-specific rules - check global
		return ShouldSkipByCreateRules(prLabels, createRules, collector, prUrl, null);
	}

	internal static bool ShouldSkipByCreateRules(
		string[] prLabels,
		CreateRules rules,
		IDiagnosticsCollector collector,
		string prUrl,
		string? productContext)
	{
		if (rules.Labels == null || rules.Labels.Count == 0)
			return false;

		var mode = rules.Mode;
		var match = rules.Match;
		var prefix = mode == FieldMode.Include ? "[+include]" : "[-exclude]";
		var productSuffix = productContext != null ? $" for product '{productContext}'" : "";

		if (mode == FieldMode.Exclude)
		{
			// Exclude mode: skip if any/all labels match
			var matchingLabel = match switch
			{
				MatchMode.All => prLabels.All(label => rules.Labels.Contains(label, StringComparer.OrdinalIgnoreCase))
					? string.Join(", ", prLabels)
					: null,
				_ => rules.Labels.FirstOrDefault(blockerLabel => prLabels.Contains(blockerLabel, StringComparer.OrdinalIgnoreCase))
			};

			if (matchingLabel != null)
			{
				collector.EmitWarning(string.Empty, $"{prefix} Skipping changelog creation for PR {prUrl} due to blocking label '{matchingLabel}'{productSuffix} (match: {match.ToString().ToLowerInvariant()}).");
				return true;
			}
		}
		else
		{
			// Include mode: skip if labels do NOT match
			var hasMatch = match switch
			{
				MatchMode.All => prLabels.All(label => rules.Labels.Contains(label, StringComparer.OrdinalIgnoreCase)),
				_ => prLabels.Any(label => rules.Labels.Contains(label, StringComparer.OrdinalIgnoreCase))
			};

			if (!hasMatch)
			{
				var labelsList = string.Join(", ", rules.Labels);
				collector.EmitWarning(string.Empty, $"{prefix} Skipping changelog creation for PR {prUrl}, no labels match rules.create.include [{labelsList}]{productSuffix} (match: {match.ToString().ToLowerInvariant()}).");
				return true;
			}
		}

		return false;
	}

	private async Task<GitHubPrInfo?> TryFetchPrInfoAsync(string? prUrl, string? owner, string? repo, Cancel ctx)
	{
		if (string.IsNullOrWhiteSpace(prUrl) || githubPrService == null)
			return null;

		try
		{
			var prInfo = await githubPrService.FetchPrInfoAsync(prUrl, owner, repo, ctx);
			if (prInfo != null)
				logger.LogInformation("Successfully fetched PR information from GitHub");
			else
				logger.LogWarning("Unable to fetch PR information from GitHub. Continuing with provided values.");
			return prInfo;
		}
		catch (Exception ex)
		{
			if (ex is OutOfMemoryException or
				StackOverflowException or
				AccessViolationException or
				ThreadAbortException)
				throw;
			logger.LogWarning(ex, "Error fetching PR information from GitHub. Continuing with provided values.");
			return null;
		}
	}

	internal static string? MapLabelsToType(string[] labels, IReadOnlyDictionary<string, string> labelToTypeMapping) => labels
		.Select(label => labelToTypeMapping.TryGetValue(label, out var mappedType) ? mappedType : null)
		.FirstOrDefault(mappedType => mappedType != null);

	internal static List<string> MapLabelsToAreas(string[] labels, IReadOnlyDictionary<string, string> labelToAreasMapping)
	{
		var areas = new HashSet<string>();
		var areaList = labels
			.Where(label => labelToAreasMapping.ContainsKey(label))
			.SelectMany(label => labelToAreasMapping[label]
				.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
		foreach (var area in areaList)
			_ = areas.Add(area);
		return areas.ToList();
	}
}

/// <summary>
/// Result of processing PR information
/// </summary>
public record PrProcessingResult
{
	public required bool FetchFailed { get; init; }
	public required bool ShouldSkip { get; init; }
	public DerivedPrFields? DerivedFields { get; init; }
	public GitHubPrInfo? PrInfo { get; init; }
}

/// <summary>
/// Fields derived from PR or issue information
/// </summary>
public record DerivedPrFields
{
	public string? Title { get; set; }
	public string? Type { get; set; }
	public string? Description { get; set; }
	public string[]? Areas { get; set; }
	public bool? Highlight { get; set; }
	public string[]? Issues { get; set; }

	/// <summary>
	/// Linked PRs derived from issue body (when creating changelog from --issues)
	/// </summary>
	public string[]? Prs { get; set; }
}
