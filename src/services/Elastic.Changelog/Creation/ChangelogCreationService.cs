// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Changelog.Configuration;
using Elastic.Changelog.GitHub;
using Elastic.Documentation.Changelog;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Services;
using Microsoft.Extensions.Logging;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Elastic.Changelog.Creation;

/// <summary>
/// Service for creating changelog entries
/// </summary>
public class ChangelogCreationService(
	ILoggerFactory logFactory,
	IConfigurationContext configurationContext,
	IGitHubPrService? githubPrService = null,
	IFileSystem? fileSystem = null
) : IService
{
	private readonly ILogger _logger = logFactory.CreateLogger<ChangelogCreationService>();
	private readonly IFileSystem _fileSystem = fileSystem ?? new FileSystem();
	private readonly ChangelogConfigurationLoader _configLoader = new(logFactory, configurationContext, fileSystem ?? new FileSystem());

	public async Task<bool> CreateChangelog(
		IDiagnosticsCollector collector,
		ChangelogInput input,
		Cancel ctx
	)
	{
		try
		{
			// Load changelog configuration
			var config = await _configLoader.LoadChangelogConfiguration(collector, input.Config, ctx);
			if (config == null)
			{
				collector.EmitError(string.Empty, "Failed to load changelog configuration");
				return false;
			}

			// Handle multiple PRs if provided (more than one PR)
			if (input.Prs != null && input.Prs.Length > 1)
			{
				return await CreateChangelogsForMultiplePrs(collector, input, config, ctx);
			}

			// Single PR or no PR - use existing logic
			return await CreateSingleChangelog(collector, input, config, ctx);
		}
		catch (IOException ioEx)
		{
			collector.EmitError(string.Empty, $"IO error creating changelog: {ioEx.Message}", ioEx);
			return false;
		}
		catch (UnauthorizedAccessException uaEx)
		{
			collector.EmitError(string.Empty, $"Access denied creating changelog: {uaEx.Message}", uaEx);
			return false;
		}
	}

	private async Task<bool> CreateChangelogsForMultiplePrs(
		IDiagnosticsCollector collector,
		ChangelogInput input,
		ChangelogConfiguration config,
		Cancel ctx
	)
	{
		if (input.Prs == null || input.Prs.Length == 0)
			return false;

		// Validate that if PRs are just numbers, owner and repo must be provided
		var allAreNumbers = input.Prs.All(pr => int.TryParse(pr.Trim(), out _));
		if (allAreNumbers && (string.IsNullOrWhiteSpace(input.Owner) || string.IsNullOrWhiteSpace(input.Repo)))
		{
			collector.EmitError(string.Empty, "When --prs contains only numbers, both --owner and --repo must be provided");
			return false;
		}

		var successCount = 0;
		var skippedCount = 0;

		foreach (var prTrimmed in input.Prs.Select(pr => pr.Trim()).Where(prTrimmed => !string.IsNullOrWhiteSpace(prTrimmed)))
		{
			// Fetch PR information
			var prInfo = await TryFetchPrInfoAsync(prTrimmed, input.Owner, input.Repo, ctx);
			if (prInfo == null)
			{
				// PR fetch failed - continue anyway to generate basic changelog
				collector.EmitWarning(string.Empty, $"Failed to fetch PR information from GitHub for PR: {prTrimmed}. Generating basic changelog with provided values.");
			}
			else
			{
				// Check for label blockers (only if we successfully fetched PR info)
				var shouldSkip = ShouldSkipPrDueToLabelBlockers(prInfo.Labels.ToArray(), input.Products, config, collector, prTrimmed);
				if (shouldSkip)
				{
					skippedCount++;
					continue;
				}
			}

			// Create a copy of input for this PR
			var prInput = new ChangelogInput
			{
				Title = input.Title,
				Type = input.Type,
				Products = input.Products,
				Subtype = input.Subtype,
				Areas = input.Areas,
				Prs = [prTrimmed],
				Owner = input.Owner,
				Repo = input.Repo,
				Issues = input.Issues,
				Description = input.Description,
				Impact = input.Impact,
				Action = input.Action,
				FeatureId = input.FeatureId,
				Highlight = input.Highlight,
				Output = input.Output,
				Config = input.Config,
				UsePrNumber = input.UsePrNumber,
				StripTitlePrefix = input.StripTitlePrefix,
				ExtractReleaseNotes = input.ExtractReleaseNotes
			};

			// Process this PR (treat as single PR)
			var result = await CreateSingleChangelog(collector, prInput, config, ctx);
			if (result)
				successCount++;
		}

		if (successCount == 0 && skippedCount == 0)
			return false;

		_logger.LogInformation("Processed {SuccessCount} PR(s) successfully, skipped {SkippedCount} PR(s)", successCount, skippedCount);
		return successCount > 0;
	}

	private bool ShouldSkipPrDueToLabelBlockers(
		string[] prLabels,
		List<ProductInfo> products,
		ChangelogConfiguration config,
		IDiagnosticsCollector collector,
		string prUrl
	)
	{
		if (config.AddBlockers == null || config.AddBlockers.Count == 0)
			return false;

		foreach (var product in products)
		{
			var normalizedProductId = product.Product.Replace('_', '-');
			if (config.AddBlockers.TryGetValue(normalizedProductId, out var blockerLabels))
			{
				var matchingBlockerLabel = blockerLabels
					.FirstOrDefault(blockerLabel => prLabels.Contains(blockerLabel, StringComparer.OrdinalIgnoreCase));
				if (matchingBlockerLabel != null)
				{
					collector.EmitWarning(string.Empty, $"Skipping changelog creation for PR {prUrl} due to blocking label '{matchingBlockerLabel}' for product '{product.Product}'. This label is configured to prevent changelog creation for this product.");
					return true;
				}
			}
		}

		return false;
	}

	private async Task<bool> CreateSingleChangelog(
		IDiagnosticsCollector collector,
		ChangelogInput input,
		ChangelogConfiguration config,
		Cancel ctx
	)
	{
		// Get the PR URL if Prs is provided (for single PR processing)
		var prUrl = input.Prs != null && input.Prs.Length > 0 ? input.Prs[0] : null;
		var prFetchFailed = false;

		// Validate that if PR is just a number, owner and repo must be provided
		if (!string.IsNullOrWhiteSpace(prUrl)
			&& int.TryParse(prUrl, out _)
			&& (string.IsNullOrWhiteSpace(input.Owner) || string.IsNullOrWhiteSpace(input.Repo)))
		{
			collector.EmitError(string.Empty, "When --prs is specified as just a number, both --owner and --repo must be provided");
			return false;
		}

		// If PR is specified, try to fetch PR information and derive title/type
		if (!string.IsNullOrWhiteSpace(prUrl))
		{
			var prInfo = await TryFetchPrInfoAsync(prUrl, input.Owner, input.Repo, ctx);
			if (prInfo == null)
			{
				// PR fetch failed - continue anyway if --prs was provided
				prFetchFailed = true;
				collector.EmitWarning(string.Empty, $"Failed to fetch PR information from GitHub for PR: {prUrl}. Generating basic changelog with provided values.");
			}
			else
			{
				// Check for label blockers (only if we successfully fetched PR info)
				var shouldSkip = ShouldSkipPrDueToLabelBlockers(prInfo.Labels.ToArray(), input.Products, config, collector, prUrl);
				if (shouldSkip)
				{
					// Return true but don't create changelog (similar to multiple PRs behavior)
					return true;
				}

				// Extract release notes from PR body if requested
				if (input.ExtractReleaseNotes)
				{
					var (releaseNoteTitle, releaseNoteDescription) = ReleaseNotesExtractor.ExtractReleaseNotes(prInfo.Body);

					// Use short release note as title if title was not explicitly provided
					if (releaseNoteTitle != null && string.IsNullOrWhiteSpace(input.Title))
					{
						input.Title = releaseNoteTitle;
						_logger.LogInformation("Using extracted release note as title: {Title}", input.Title);
					}

					// Use long release note as description if description was not explicitly provided
					if (releaseNoteDescription != null && string.IsNullOrWhiteSpace(input.Description))
					{
						input.Description = releaseNoteDescription;
						_logger.LogInformation("Using extracted release note as description (length: {Length} characters)", releaseNoteDescription.Length);
					}
				}

				// Use PR title if title was not explicitly provided
				if (string.IsNullOrWhiteSpace(input.Title))
				{
					if (string.IsNullOrWhiteSpace(prInfo.Title))
					{
						collector.EmitError(string.Empty, $"PR {prUrl} does not have a title. Please provide --title or ensure the PR has a title.");
						return false;
					}
					var prTitle = prInfo.Title;
					// Strip prefix if requested
					if (input.StripTitlePrefix)
					{
						prTitle = ChangelogTextUtilities.StripSquareBracketPrefix(prTitle);
					}
					input.Title = prTitle;
					_logger.LogInformation("Using PR title: {Title}", input.Title);
				}
				else
				{
					_logger.LogDebug("Using explicitly provided title, ignoring PR title");
				}

				// Map labels to type if type was not explicitly provided
				if (string.IsNullOrWhiteSpace(input.Type))
				{
					if (config.LabelToType == null || config.LabelToType.Count == 0)
					{
						collector.EmitError(string.Empty, $"Cannot derive type from PR {prUrl} labels: no label-to-type mapping configured in changelog.yml. Please provide --type or configure label_to_type in changelog.yml.");
						return false;
					}

					var mappedType = MapLabelsToType(prInfo.Labels.ToArray(), config.LabelToType);
					if (mappedType == null)
					{
						var availableLabels = prInfo.Labels.Count > 0 ? string.Join(", ", prInfo.Labels) : "none";
						collector.EmitError(string.Empty, $"Cannot derive type from PR {prUrl} labels ({availableLabels}). No matching label found in label_to_type mapping. Please provide --type or add a label mapping in changelog.yml.");
						return false;
					}
					input.Type = mappedType;
					_logger.LogInformation("Mapped PR labels to type: {Type}", input.Type);
				}
				else
				{
					_logger.LogDebug("Using explicitly provided type, ignoring PR labels");
				}

				// Map labels to areas if areas were not explicitly provided
				if (input.Areas != null && input.Areas.Length == 0 && config.LabelToAreas != null)
				{
					var mappedAreas = MapLabelsToAreas(prInfo.Labels.ToArray(), config.LabelToAreas);
					if (mappedAreas.Count > 0)
					{
						input.Areas = mappedAreas.ToArray();
						_logger.LogInformation("Mapped PR labels to areas: {Areas}", string.Join(", ", mappedAreas));
					}
				}
				else if (input.Areas != null && input.Areas.Length > 0)
				{
					_logger.LogDebug("Using explicitly provided areas, ignoring PR labels");
				}
			}
		}

		// Validate required fields (must be provided either explicitly or derived from PR)
		// If PR fetch failed, allow missing title/type and warn instead of erroring
		if (string.IsNullOrWhiteSpace(input.Title))
		{
			if (prFetchFailed)
			{
				collector.EmitWarning(string.Empty, "Title is missing. The changelog will be created with title commented out. Please manually update the title field.");
			}
			else
			{
				collector.EmitError(string.Empty, "Title is required. Provide --title or specify --prs to derive it from the PR.");
				return false;
			}
		}

		if (string.IsNullOrWhiteSpace(input.Type))
		{
			if (prFetchFailed)
			{
				collector.EmitWarning(string.Empty, "Type is missing. The changelog will be created with type commented out. Please manually update the type field.");
			}
			else
			{
				collector.EmitError(string.Empty, "Type is required. Provide --type or specify --prs to derive it from PR labels (requires label_to_type mapping in changelog.yml).");
				return false;
			}
		}

		if (input.Products.Count == 0)
		{
			collector.EmitError(string.Empty, "At least one product is required");
			return false;
		}

		// Validate type is in allowed list (only if type is provided)
		if (!string.IsNullOrWhiteSpace(input.Type) && !config.AvailableTypes.Contains(input.Type))
		{
			collector.EmitError(string.Empty, $"Type '{input.Type}' is not in the list of available types. Available types: {string.Join(", ", config.AvailableTypes)}");
			return false;
		}

		// Validate subtype if provided
		if (!string.IsNullOrWhiteSpace(input.Subtype) && !config.AvailableSubtypes.Contains(input.Subtype))
		{
			collector.EmitError(string.Empty, $"Subtype '{input.Subtype}' is not in the list of available subtypes. Available subtypes: {string.Join(", ", config.AvailableSubtypes)}");
			return false;
		}

		// Validate areas if configuration provides available areas
		if (config.AvailableAreas != null && config.AvailableAreas.Count > 0 && input.Areas != null)
		{
			foreach (var area in input.Areas.Where(area => !config.AvailableAreas.Contains(area)))
			{
				collector.EmitError(string.Empty, $"Area '{area}' is not in the list of available areas. Available areas: {string.Join(", ", config.AvailableAreas)}");
				return false;
			}
		}

		// Always validate products against products.yml
		var validProductIds = configurationContext.ProductsConfiguration.Products.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
		foreach (var product in input.Products)
		{
			// Normalize product ID (replace underscores with hyphens for comparison)
			var normalizedProductId = product.Product.Replace('_', '-');
			if (!validProductIds.Contains(normalizedProductId))
			{
				var availableProducts = string.Join(", ", validProductIds.OrderBy(p => p));
				collector.EmitError(string.Empty, $"Product '{product.Product}' is not in the list of available products from config/products.yml. Available products: {availableProducts}");
				return false;
			}
		}

		// Validate lifecycle values in products
		foreach (var product in input.Products.Where(product => !string.IsNullOrWhiteSpace(product.Lifecycle) && !config.AvailableLifecycles.Contains(product.Lifecycle)))
		{
			collector.EmitError(string.Empty, $"Lifecycle '{product.Lifecycle}' for product '{product.Product}' is not in the list of available lifecycles. Available lifecycles: {string.Join(", ", config.AvailableLifecycles)}");
			return false;
		}

		// Build changelog data from input
		var changelogData = BuildChangelogData(input, prUrl);

		// Generate YAML file
		var yamlContent = GenerateYaml(changelogData, config, string.IsNullOrWhiteSpace(input.Title), string.IsNullOrWhiteSpace(input.Type));

		// Determine output path
		var outputDir = input.Output ?? _fileSystem.Directory.GetCurrentDirectory();
		if (!_fileSystem.Directory.Exists(outputDir))
		{
			_ = _fileSystem.Directory.CreateDirectory(outputDir);
		}

		// Generate filename
		string filename;
		if (input.UsePrNumber && !string.IsNullOrWhiteSpace(prUrl))
		{
			// Use PR number as filename when --use-pr-number is specified
			var prNumber = ChangelogTextUtilities.ExtractPrNumber(prUrl, input.Owner, input.Repo);
			if (prNumber.HasValue)
			{
				filename = $"{prNumber.Value}.yaml";
			}
			else
			{
				// Fall back to timestamp-slug format if PR number extraction fails
				collector.EmitWarning(string.Empty, $"Failed to extract PR number from '{prUrl}'. Falling back to timestamp-based filename.");
				var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
				var slug = string.IsNullOrWhiteSpace(input.Title)
					? $"pr-{prUrl.Replace("/", "-").Replace(":", "-")}"
					: ChangelogTextUtilities.SanitizeFilename(input.Title);
				filename = $"{timestamp}-{slug}.yaml";
			}
		}
		else
		{
			// Default: timestamp-slug.yaml
			var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
			var slug = string.IsNullOrWhiteSpace(input.Title)
				? string.IsNullOrWhiteSpace(prUrl)
					? "changelog"
					: $"pr-{prUrl.Replace("/", "-").Replace(":", "-")}"
				: ChangelogTextUtilities.SanitizeFilename(input.Title);
			filename = $"{timestamp}-{slug}.yaml";
		}
		var filePath = _fileSystem.Path.Combine(outputDir, filename);

		// Write file
		await _fileSystem.File.WriteAllTextAsync(filePath, yamlContent, ctx);
		_logger.LogInformation("Created changelog fragment: {FilePath}", filePath);

		return true;
	}

	private static ChangelogData BuildChangelogData(ChangelogInput input, string? prUrl = null) =>
		new()
		{
			Title = input.Title ?? string.Empty,
			Type = input.Type ?? string.Empty,
			Subtype = input.Subtype,
			Description = input.Description,
			Impact = input.Impact,
			Action = input.Action,
			FeatureId = input.FeatureId,
			Highlight = input.Highlight,
			Pr = prUrl ?? (input.Prs != null && input.Prs.Length > 0 ? input.Prs[0] : null),
			Products = input.Products.Select(p => new ProductInfo { Product = p.Product, Target = p.Target, Lifecycle = p.Lifecycle }).ToList(),
			Areas = input.Areas.Length > 0 ? input.Areas.ToList() : null,
			Issues = input.Issues.Length > 0 ? input.Issues.ToList() : null
		};

	private string GenerateYaml(ChangelogData data, ChangelogConfiguration config, bool titleMissing = false, bool typeMissing = false)
	{
		// Create a mutable copy for serialization adjustments
		var title = data.Title;
		var type = data.Type;
		var areas = data.Areas;
		var issues = data.Issues;

		// Temporarily remove title/type if they're missing so they don't appear in YAML
		var serializeData = data with
		{
			Title = titleMissing ? string.Empty : data.Title,
			Type = typeMissing ? string.Empty : data.Type,
			Areas = areas != null && areas.Count == 0 ? null : areas,
			Issues = issues != null && issues.Count == 0 ? null : issues
		};

		var serializer = new StaticSerializerBuilder(new ChangelogYamlStaticContext())
			.WithNamingConvention(UnderscoredNamingConvention.Instance)
			.ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull | DefaultValuesHandling.OmitEmptyCollections)
			.Build();

		var yaml = serializer.Serialize(serializeData);

		// Comment out missing title/type fields - insert at the beginning of the YAML data
		if (titleMissing || typeMissing)
		{
			var lines = yaml.Split('\n').ToList();
			var commentedFields = new List<string>();

			if (titleMissing)
			{
				commentedFields.Add("# title: # TODO: Add title");
			}
			if (typeMissing)
			{
				commentedFields.Add("# type: # TODO: Add type (e.g., feature, enhancement, bug-fix, breaking-change)");
			}

			// Find the first non-empty, non-comment line (start of actual YAML data)
			var insertIndex = lines.FindIndex(line =>
				!string.IsNullOrWhiteSpace(line) &&
				!line.TrimStart().StartsWith('#') &&
				!line.TrimStart().StartsWith("---", StringComparison.Ordinal));

			lines.InsertRange(insertIndex >= 0 ? insertIndex : lines.Count, commentedFields);

			yaml = string.Join('\n', lines);
		}

		// Build types list
		var typesList = string.Join("\n", config.AvailableTypes.Select(t => $"#   - {t}"));

		// Build subtypes list
		var subtypesList = string.Join("\n", config.AvailableSubtypes.Select(s => $"#   - {s}"));

		// Build lifecycles list
		var lifecyclesList = string.Join("\n", config.AvailableLifecycles.Select(l => $"#       - {l}"));

		// Add schema comments using raw string literal
		var result = $"""
			##### Required fields #####

			# title:
			#   A required string that is a short, user-facing headline.
			#   (Max 80 characters)

			# type:
			#   A required string that contains the type of change
			#   It can be one of:
			{typesList}

			# products:
			#   A required array of objects that denote the affected products
			#   Each product object contains:
			#
			#   - product:
			#       A required string with a valid product ID.
			#       Valid values are defined in https://github.com/elastic/docs-builder/blob/main/config/products.yml
			#
			#     target:
			#       An optional string with the target version or date.
			#
			#     lifecycle:
			#       An optional string for new features or enhancements that have a specific availability.
			#       It can be one of:
			{lifecyclesList}

			##### Optional fields #####

			# action:
			#   An optional string that describes what users must do to mitigate
			#   the impact of a breaking change or known issue.

			# areas:
			#   An optional array of strings that denotes the parts/components/services
			#   of the product that are affected.

			# description:
			#   An optional string that provides additional information.
			#   (Max 600 characters).

			# feature-id:
			#   An optional string to associate a feature or enhanceent with a
			#   unique feature flag.

			# highlight:
			#   An optional boolean for items that should be included in release
			#   highlights or the UI to draw user attention.

			# impact:
			#   An optional string that describes how the user's environment is
			#   affected by a breaking change or known issue.

			# issues:
			#   An optional array of strings that contain the issues that are
			#   relevant to the PR.

			# pr:
			#   An optional string that contains the pull request number.

			# subtype:
			#   An optional string that applies only to breaking changes.
			#   It can be one of:
			{subtypesList}

			{yaml}
			""";

		return result;
	}

	private async Task<GitHubPrInfo?> TryFetchPrInfoAsync(string? prUrl, string? owner, string? repo, Cancel ctx)
	{
		if (string.IsNullOrWhiteSpace(prUrl) || githubPrService == null)
		{
			return null;
		}

		try
		{
			var prInfo = await githubPrService.FetchPrInfoAsync(prUrl, owner, repo, ctx);
			if (prInfo != null)
			{
				_logger.LogInformation("Successfully fetched PR information from GitHub");
			}
			else
			{
				_logger.LogWarning("Unable to fetch PR information from GitHub. Continuing with provided values.");
			}
			return prInfo;
		}
		catch (Exception ex)
		{
			if (ex is OutOfMemoryException or
				StackOverflowException or
				AccessViolationException or
				ThreadAbortException)
			{
				throw;
			}
			_logger.LogWarning(ex, "Error fetching PR information from GitHub. Continuing with provided values.");
			return null;
		}
	}

	private static string? MapLabelsToType(string[] labels, IReadOnlyDictionary<string, string> labelToTypeMapping) => labels
		.Select(label => labelToTypeMapping.TryGetValue(label, out var mappedType) ? mappedType : null)
		.FirstOrDefault(mappedType => mappedType != null);

	private static List<string> MapLabelsToAreas(string[] labels, IReadOnlyDictionary<string, string> labelToAreasMapping)
	{
		var areas = new HashSet<string>();
		var areaList = labels
			.Where(label => labelToAreasMapping.ContainsKey(label))
			.SelectMany(label => labelToAreasMapping[label]
				.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
		foreach (var area in areaList)
		{
			_ = areas.Add(area);
		}
		return areas.ToList();
	}
}
