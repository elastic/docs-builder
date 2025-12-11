// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;
using System.IO.Abstractions;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Services.Changelog;
using Microsoft.Extensions.Logging;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
namespace Elastic.Documentation.Services;

public partial class ChangelogService(
	ILoggerFactory logFactory,
	IConfigurationContext configurationContext,
	IGitHubPrService? githubPrService = null
) : IService
{
	private readonly ILogger _logger = logFactory.CreateLogger<ChangelogService>();
	private readonly IFileSystem _fileSystem = new FileSystem();
	private readonly IGitHubPrService? _githubPrService = githubPrService;

	public async Task<bool> CreateChangelog(
		IDiagnosticsCollector collector,
		ChangelogInput input,
		Cancel ctx
	)
	{
		try
		{
			// Load changelog configuration
			var config = await LoadChangelogConfiguration(collector, input.Config, ctx);
			if (config == null)
			{
				collector.EmitError(string.Empty, "Failed to load changelog configuration");
				return false;
			}

			// Validate that if PR is just a number, owner and repo must be provided
			if (!string.IsNullOrWhiteSpace(input.Pr)
				&& int.TryParse(input.Pr, out _)
				&& (string.IsNullOrWhiteSpace(input.Owner) || string.IsNullOrWhiteSpace(input.Repo)))
			{
				collector.EmitError(string.Empty, "When --pr is specified as just a number, both --owner and --repo must be provided");
				return false;
			}

			// If PR is specified, try to fetch PR information and derive title/type
			if (!string.IsNullOrWhiteSpace(input.Pr))
			{
				var prInfo = await TryFetchPrInfoAsync(input.Pr, input.Owner, input.Repo, ctx);
				if (prInfo == null)
				{
					collector.EmitError(string.Empty, $"Failed to fetch PR information from GitHub for PR: {input.Pr}. Cannot derive title and type.");
					return false;
				}

				// Use PR title if title was not explicitly provided
				if (string.IsNullOrWhiteSpace(input.Title))
				{
					if (string.IsNullOrWhiteSpace(prInfo.Title))
					{
						collector.EmitError(string.Empty, $"PR {input.Pr} does not have a title. Please provide --title or ensure the PR has a title.");
						return false;
					}
					input.Title = prInfo.Title;
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
						collector.EmitError(string.Empty, $"Cannot derive type from PR {input.Pr} labels: no label-to-type mapping configured in changelog.yml. Please provide --type or configure label_to_type in changelog.yml.");
						return false;
					}

					var mappedType = MapLabelsToType(prInfo.Labels, config.LabelToType);
					if (mappedType == null)
					{
						var availableLabels = prInfo.Labels.Length > 0 ? string.Join(", ", prInfo.Labels) : "none";
						collector.EmitError(string.Empty, $"Cannot derive type from PR {input.Pr} labels ({availableLabels}). No matching label found in label_to_type mapping. Please provide --type or add a label mapping in changelog.yml.");
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
				if ((input.Areas == null || input.Areas.Length == 0) && config.LabelToAreas != null)
				{
					var mappedAreas = MapLabelsToAreas(prInfo.Labels, config.LabelToAreas);
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

			// Validate required fields (must be provided either explicitly or derived from PR)
			if (string.IsNullOrWhiteSpace(input.Title))
			{
				collector.EmitError(string.Empty, "Title is required. Provide --title or specify --pr to derive it from the PR.");
				return false;
			}

			if (string.IsNullOrWhiteSpace(input.Type))
			{
				collector.EmitError(string.Empty, "Type is required. Provide --type or specify --pr to derive it from PR labels (requires label_to_type mapping in changelog.yml).");
				return false;
			}

			if (input.Products.Count == 0)
			{
				collector.EmitError(string.Empty, "At least one product is required");
				return false;
			}

			// Validate type is in allowed list
			if (!config.AvailableTypes.Contains(input.Type))
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
			var changelogData = BuildChangelogData(input);

			// Generate YAML file
			var yamlContent = GenerateYaml(changelogData, config);

			// Determine output path
			var outputDir = input.Output ?? Directory.GetCurrentDirectory();
			if (!_fileSystem.Directory.Exists(outputDir))
			{
				_ = _fileSystem.Directory.CreateDirectory(outputDir);
			}

			// Generate filename (timestamp-slug.yaml)
			var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
			var slug = SanitizeFilename(input.Title);
			var filename = $"{timestamp}-{slug}.yaml";
			var filePath = _fileSystem.Path.Combine(outputDir, filename);

			// Write file
			await _fileSystem.File.WriteAllTextAsync(filePath, yamlContent, ctx);
			_logger.LogInformation("Created changelog fragment: {FilePath}", filePath);

			return true;
		}
		catch (OperationCanceledException)
		{
			// If cancelled, don't emit error; propagate cancellation signal.
			throw;
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

	private async Task<ChangelogConfiguration?> LoadChangelogConfiguration(
		IDiagnosticsCollector collector,
		string? configPath,
		Cancel ctx
	)
	{
		// Determine config file path
		var finalConfigPath = configPath ?? _fileSystem.Path.Combine(Directory.GetCurrentDirectory(), "docs", "changelog.yml");

		if (!_fileSystem.File.Exists(finalConfigPath))
		{
			// Use default configuration if file doesn't exist
			_logger.LogWarning("Changelog configuration not found at {ConfigPath}, using defaults", finalConfigPath);
			return ChangelogConfiguration.Default;
		}

		try
		{
			var yamlContent = await _fileSystem.File.ReadAllTextAsync(finalConfigPath, ctx);
			var deserializer = new StaticDeserializerBuilder(new ChangelogYamlStaticContext())
				.WithNamingConvention(UnderscoredNamingConvention.Instance)
				.Build();

			var config = deserializer.Deserialize<ChangelogConfiguration>(yamlContent);

			// Validate that changelog.yml values conform to ChangelogConfiguration defaults
			var defaultConfig = ChangelogConfiguration.Default;
			var validProductIds = configurationContext.ProductsConfiguration.Products.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);

			// Validate available_types
			foreach (var type in config.AvailableTypes.Where(t => !defaultConfig.AvailableTypes.Contains(t)))
			{
				collector.EmitError(finalConfigPath, $"Type '{type}' in changelog.yml is not in the list of available types. Available types: {string.Join(", ", defaultConfig.AvailableTypes)}");
				return null;
			}

			// Validate available_subtypes
			foreach (var subtype in config.AvailableSubtypes.Where(s => !defaultConfig.AvailableSubtypes.Contains(s)))
			{
				collector.EmitError(finalConfigPath, $"Subtype '{subtype}' in changelog.yml is not in the list of available subtypes. Available subtypes: {string.Join(", ", defaultConfig.AvailableSubtypes)}");
				return null;
			}

			// Validate available_lifecycles
			foreach (var lifecycle in config.AvailableLifecycles.Where(l => !defaultConfig.AvailableLifecycles.Contains(l)))
			{
				collector.EmitError(finalConfigPath, $"Lifecycle '{lifecycle}' in changelog.yml is not in the list of available lifecycles. Available lifecycles: {string.Join(", ", defaultConfig.AvailableLifecycles)}");
				return null;
			}

			// Validate available_products (if specified) - must be from products.yml
			if (config.AvailableProducts != null && config.AvailableProducts.Count > 0)
			{
				foreach (var product in config.AvailableProducts)
				{
					var normalizedProductId = product.Replace('_', '-');
					if (!validProductIds.Contains(normalizedProductId))
					{
						var availableProducts = string.Join(", ", validProductIds.OrderBy(p => p));
						collector.EmitError(finalConfigPath, $"Product '{product}' in changelog.yml is not in the list of available products from config/products.yml. Available products: {availableProducts}");
						return null;
					}
				}
			}

			return config;
		}
		catch (IOException ex)
		{
			collector.EmitError(finalConfigPath, $"I/O error loading changelog configuration: {ex.Message}", ex);
			return null;
		}
		catch (UnauthorizedAccessException ex)
		{
			collector.EmitError(finalConfigPath, $"Access denied loading changelog configuration: {ex.Message}", ex);
			return null;
		}
		catch (YamlException ex)
		{
			collector.EmitError(finalConfigPath, $"YAML parsing error in changelog configuration: {ex.Message}", ex);
			return null;
		}
	}

	private static ChangelogData BuildChangelogData(ChangelogInput input)
	{
		// Title and Type are guaranteed to be non-null at this point due to validation above
		var data = new ChangelogData
		{
			Title = input.Title!,
			Type = input.Type!,
			Subtype = input.Subtype,
			Description = input.Description,
			Impact = input.Impact,
			Action = input.Action,
			FeatureId = input.FeatureId,
			Highlight = input.Highlight,
			Pr = input.Pr,
			Products = input.Products
		};

		if (input.Areas.Length > 0)
		{
			data.Areas = input.Areas.ToList();
		}

		if (input.Issues.Length > 0)
		{
			data.Issues = input.Issues.ToList();
		}

		return data;
	}

	private string GenerateYaml(ChangelogData data, ChangelogConfiguration config)
	{
		// Ensure areas is null if empty to omit it from YAML
		if (data.Areas != null && data.Areas.Count == 0)
			data.Areas = null;

		// Ensure issues is null if empty to omit it from YAML
		if (data.Issues != null && data.Issues.Count == 0)
			data.Issues = null;

		var serializer = new StaticSerializerBuilder(new ChangelogYamlStaticContext())
			.WithNamingConvention(UnderscoredNamingConvention.Instance)
			.ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull | DefaultValuesHandling.OmitEmptyCollections)
			.Build();

		var yaml = serializer.Serialize(data);

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

	private static string SanitizeFilename(string input)
	{
		var sanitized = input.ToLowerInvariant()
			.Replace(" ", "-")
			.Replace("/", "-")
			.Replace("\\", "-")
			.Replace(":", "")
			.Replace("'", "")
			.Replace("\"", "");

		// Limit length
		if (sanitized.Length > 50)
			sanitized = sanitized[..50];

		return sanitized;
	}

	private async Task<GitHubPrInfo?> TryFetchPrInfoAsync(string? prUrl, string? owner, string? repo, Cancel ctx)
	{
		if (string.IsNullOrWhiteSpace(prUrl) || _githubPrService == null)
		{
			return null;
		}

		try
		{
			var prInfo = await _githubPrService.FetchPrInfoAsync(prUrl, owner, repo, ctx);
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

	private static string? MapLabelsToType(string[] labels, Dictionary<string, string> labelToTypeMapping) => labels
			.Select(label => labelToTypeMapping.TryGetValue(label, out var mappedType) ? mappedType : null)
			.FirstOrDefault(mappedType => mappedType != null);

	private static List<string> MapLabelsToAreas(string[] labels, Dictionary<string, string> labelToAreasMapping)
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

	public async Task<bool> BundleChangelogs(
		IDiagnosticsCollector collector,
		ChangelogBundleInput input,
		Cancel ctx
	)
	{
		try
		{
			// Validate input
			if (string.IsNullOrWhiteSpace(input.Directory))
			{
				collector.EmitError(string.Empty, "Directory is required");
				return false;
			}

			if (!_fileSystem.Directory.Exists(input.Directory))
			{
				collector.EmitError(input.Directory, "Directory does not exist");
				return false;
			}

			// Validate filter options
			var filterCount = 0;
			if (input.All)
				filterCount++;
			if (input.Products != null && input.Products.Count > 0)
				filterCount++;
			if (input.Prs != null && input.Prs.Length > 0)
				filterCount++;
			if (!string.IsNullOrWhiteSpace(input.PrsFile))
				filterCount++;

			if (filterCount == 0)
			{
				collector.EmitError(string.Empty, "At least one filter option must be specified: --all, --products, --prs, or --prs-file");
				return false;
			}

			if (filterCount > 1)
			{
				collector.EmitError(string.Empty, "Only one filter option can be specified at a time: --all, --products, --prs, or --prs-file");
				return false;
			}

			// Load PRs from file if specified
			var prsToMatch = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			if (!string.IsNullOrWhiteSpace(input.PrsFile))
			{
				if (!_fileSystem.File.Exists(input.PrsFile))
				{
					collector.EmitError(input.PrsFile, "PRs file does not exist");
					return false;
				}

				var prsFileContent = await _fileSystem.File.ReadAllTextAsync(input.PrsFile, ctx);
				var prsFromFile = prsFileContent
					.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
					.Where(p => !string.IsNullOrWhiteSpace(p))
					.ToArray();

				if (input.Prs != null && input.Prs.Length > 0)
				{
					foreach (var pr in input.Prs)
					{
						_ = prsToMatch.Add(pr);
					}
				}

				foreach (var pr in prsFromFile)
				{
					_ = prsToMatch.Add(pr);
				}
			}
			else if (input.Prs != null && input.Prs.Length > 0)
			{
				foreach (var pr in input.Prs)
				{
					_ = prsToMatch.Add(pr);
				}
			}

			// Build set of product/version combinations to filter by
			var productsToMatch = new HashSet<(string product, string version)>();
			if (input.Products != null && input.Products.Count > 0)
			{
				foreach (var product in input.Products)
				{
					var version = product.Target ?? string.Empty;
					_ = productsToMatch.Add((product.Product.ToLowerInvariant(), version));
				}
			}

			// Determine output path to exclude it from input files
			var outputPath = input.Output ?? _fileSystem.Path.Combine(input.Directory, "changelog-bundle.yaml");
			var outputFileName = _fileSystem.Path.GetFileName(outputPath);

			// Read all YAML files from directory (exclude bundle files and output file)
			var yamlFiles = _fileSystem.Directory.GetFiles(input.Directory, "*.yaml", SearchOption.TopDirectoryOnly)
				.Concat(_fileSystem.Directory.GetFiles(input.Directory, "*.yml", SearchOption.TopDirectoryOnly))
				.Where(f =>
				{
					var fileName = _fileSystem.Path.GetFileName(f);
					// Exclude bundle files and the output file
					return !fileName.Contains("changelog-bundle", StringComparison.OrdinalIgnoreCase) &&
						!fileName.Equals(outputFileName, StringComparison.OrdinalIgnoreCase) &&
						!fileName.Contains("-bundle", StringComparison.OrdinalIgnoreCase);
				})
				.ToList();

			if (yamlFiles.Count == 0)
			{
				collector.EmitError(input.Directory, "No YAML files found in directory");
				return false;
			}

			_logger.LogInformation("Found {Count} YAML files in directory", yamlFiles.Count);

			// Deserialize and filter changelog files
			var deserializer = new StaticDeserializerBuilder(new ChangelogYamlStaticContext())
				.WithNamingConvention(UnderscoredNamingConvention.Instance)
				.Build();

			var changelogEntries = new List<(ChangelogData data, string filePath, string fileName, string checksum)>();
			var matchedPrs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			foreach (var filePath in yamlFiles)
			{
				try
				{
					var fileName = _fileSystem.Path.GetFileName(filePath);
					var fileContent = await _fileSystem.File.ReadAllTextAsync(filePath, ctx);

					// Compute checksum (SHA1)
					var checksum = ComputeSha1(fileContent);

					// Deserialize YAML (skip comment lines)
					var yamlLines = fileContent.Split('\n');
					var yamlWithoutComments = string.Join('\n', yamlLines.Where(line => !line.TrimStart().StartsWith('#')));

					// Normalize "version:" to "target:" in products section for compatibility
					// Some changelog files may use "version" instead of "target"
					// Match "version:" with various indentation levels
					var normalizedYaml = VersionToTargetRegex().Replace(yamlWithoutComments, "$1target:");

					var data = deserializer.Deserialize<ChangelogData>(normalizedYaml);

					if (data == null)
					{
						_logger.LogWarning("Skipping file {FileName}: failed to deserialize", fileName);
						continue;
					}

					// Apply filters
					if (input.All)
					{
						// Include all
					}
					else if (productsToMatch.Count > 0)
					{
						// Filter by products
						var matches = data.Products.Any(p =>
						{
							var version = p.Target ?? string.Empty;
							return productsToMatch.Contains((p.Product.ToLowerInvariant(), version));
						});

						if (!matches)
						{
							continue;
						}
					}
					else if (prsToMatch.Count > 0)
					{
						// Filter by PRs
						var matches = false;
						if (!string.IsNullOrWhiteSpace(data.Pr))
						{
							// Normalize PR for comparison
							var normalizedPr = NormalizePrForComparison(data.Pr, input.Owner, input.Repo);
							foreach (var pr in prsToMatch)
							{
								var normalizedPrToMatch = NormalizePrForComparison(pr, input.Owner, input.Repo);
								if (normalizedPr == normalizedPrToMatch)
								{
									matches = true;
									_ = matchedPrs.Add(pr);
									break;
								}
							}
						}

						if (!matches)
						{
							continue;
						}
					}

					changelogEntries.Add((data, filePath, fileName, checksum));
				}
				catch (YamlException ex)
				{
					_logger.LogWarning(ex, "Failed to parse YAML file {FilePath}", filePath);
					collector.EmitError(filePath, $"Failed to parse YAML: {ex.Message}");
					continue;
				}
				catch (Exception ex) when (ex is not (OutOfMemoryException or StackOverflowException or ThreadAbortException))
				{
					_logger.LogWarning(ex, "Error processing file {FilePath}", filePath);
					collector.EmitError(filePath, $"Error processing file: {ex.Message}");
					continue;
				}
			}

			// Warn about unmatched PRs if filtering by PRs
			if (prsToMatch.Count > 0)
			{
				var unmatchedPrs = prsToMatch.Where(pr => !matchedPrs.Contains(pr)).ToList();
				if (unmatchedPrs.Count > 0)
				{
					foreach (var unmatchedPr in unmatchedPrs)
					{
						collector.EmitWarning(string.Empty, $"No changelog file found for PR: {unmatchedPr}");
					}
				}
			}

			if (changelogEntries.Count == 0)
			{
				collector.EmitError(string.Empty, "No changelog entries matched the filter criteria");
				return false;
			}

			_logger.LogInformation("Found {Count} matching changelog entries", changelogEntries.Count);

			// Build bundled data
			var bundledData = new BundledChangelogData();

			// Extract unique products/versions
			// If --products filter was used, only include those specific product-versions
			if (productsToMatch.Count > 0)
			{
				bundledData.Products = productsToMatch
					.OrderBy(pv => pv.product)
					.ThenBy(pv => pv.version)
					.Select(pv => new BundledProduct
					{
						Product = pv.product,
						Version = pv.version
					})
					.ToList();
			}
			else
			{
				var productVersions = new HashSet<(string product, string version)>();
				foreach (var (data, _, _, _) in changelogEntries)
				{
					foreach (var product in data.Products)
					{
						var version = product.Target ?? string.Empty;
						_ = productVersions.Add((product.Product, version));
					}
				}

				bundledData.Products = productVersions
					.OrderBy(pv => pv.product)
					.ThenBy(pv => pv.version)
					.Select(pv => new BundledProduct
					{
						Product = pv.product,
						Version = pv.version
					})
					.ToList();
			}

			// Check for products with same product ID but different versions
			var productsByProductId = bundledData.Products.GroupBy(p => p.Product, StringComparer.OrdinalIgnoreCase)
				.Where(g => g.Count() > 1)
				.ToList();

			foreach (var productGroup in productsByProductId)
			{
				var versions = productGroup.Select(p => string.IsNullOrWhiteSpace(p.Version) ? "(no version)" : p.Version).ToList();
				collector.EmitWarning(string.Empty, $"Product '{productGroup.Key}' has multiple versions in bundle: {string.Join(", ", versions)}");
			}

			// Build entries - only include file information
			bundledData.Entries = changelogEntries
				.Select(e => new BundledEntry
				{
					File = new BundledFile
					{
						Name = e.fileName,
						Checksum = e.checksum
					}
				})
				.ToList();

			// Generate bundled YAML
			var bundleSerializer = new StaticSerializerBuilder(new ChangelogYamlStaticContext())
				.WithNamingConvention(UnderscoredNamingConvention.Instance)
				.ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull | DefaultValuesHandling.OmitEmptyCollections)
				.Build();

			var bundledYaml = bundleSerializer.Serialize(bundledData);

			// Output path was already determined above when filtering files
			var outputDir = _fileSystem.Path.GetDirectoryName(outputPath);
			if (!string.IsNullOrWhiteSpace(outputDir) && !_fileSystem.Directory.Exists(outputDir))
			{
				_ = _fileSystem.Directory.CreateDirectory(outputDir);
			}

			// If output file already exists, generate a unique filename
			if (_fileSystem.File.Exists(outputPath))
			{
				var directory = _fileSystem.Path.GetDirectoryName(outputPath) ?? string.Empty;
				var fileNameWithoutExtension = _fileSystem.Path.GetFileNameWithoutExtension(outputPath);
				var extension = _fileSystem.Path.GetExtension(outputPath);
				var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
				var uniqueFileName = $"{fileNameWithoutExtension}-{timestamp}{extension}";
				outputPath = _fileSystem.Path.Combine(directory, uniqueFileName);
				_logger.LogInformation("Output file already exists, using unique filename: {OutputPath}", outputPath);
			}

			// Write bundled file
			await _fileSystem.File.WriteAllTextAsync(outputPath, bundledYaml, ctx);
			_logger.LogInformation("Created bundled changelog: {OutputPath}", outputPath);

			return true;
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (IOException ioEx)
		{
			collector.EmitError(string.Empty, $"IO error bundling changelogs: {ioEx.Message}", ioEx);
			return false;
		}
		catch (UnauthorizedAccessException uaEx)
		{
			collector.EmitError(string.Empty, $"Access denied bundling changelogs: {uaEx.Message}", uaEx);
			return false;
		}
	}

	[System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA5350:Do not use insecure cryptographic algorithm SHA1", Justification = "SHA1 is required for compatibility with existing changelog bundle format")]
	private static string ComputeSha1(string content)
	{
		var bytes = Encoding.UTF8.GetBytes(content);
		var hash = SHA1.HashData(bytes);
		return Convert.ToHexString(hash).ToLowerInvariant();
	}

	[GeneratedRegex(@"(\s+)version:", RegexOptions.Multiline)]
	private static partial Regex VersionToTargetRegex();

	private static string NormalizePrForComparison(string pr, string? defaultOwner, string? defaultRepo)
	{
		// Parse PR using the same logic as GitHubPrService.ParsePrUrl
		// Return a normalized format (owner/repo#number) for comparison

		// Handle full URL: https://github.com/owner/repo/pull/123
		if (pr.StartsWith("https://github.com/", StringComparison.OrdinalIgnoreCase) ||
			pr.StartsWith("http://github.com/", StringComparison.OrdinalIgnoreCase))
		{
			try
			{
				var uri = new Uri(pr);
				var segments = uri.Segments;
				if (segments.Length >= 5 && segments[3].Equals("pull/", StringComparison.OrdinalIgnoreCase))
				{
					var owner = segments[1].TrimEnd('/');
					var repo = segments[2].TrimEnd('/');
					var prNum = segments[4].Trim();
					return $"{owner}/{repo}#{prNum}".ToLowerInvariant();
				}
			}
			catch (UriFormatException)
			{
				// Invalid URI, fall through
			}
		}

		// Handle short format: owner/repo#123
		var hashIndex = pr.LastIndexOf('#');
		if (hashIndex > 0 && hashIndex < pr.Length - 1)
		{
			return pr.ToLowerInvariant();
		}

		// Handle just a PR number when owner/repo are provided
		if (int.TryParse(pr, out var prNumber) &&
			!string.IsNullOrWhiteSpace(defaultOwner) && !string.IsNullOrWhiteSpace(defaultRepo))
		{
			return $"{defaultOwner}/{defaultRepo}#{prNumber}".ToLowerInvariant();
		}

		// Return as-is for comparison (fallback)
		return pr.ToLowerInvariant();
	}
}

