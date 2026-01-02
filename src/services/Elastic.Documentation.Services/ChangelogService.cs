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
			_logger.LogInformation("Created changelog: {FilePath}", filePath);

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
			if (input.InputProducts is { Count: > 0 })
				filterCount++;
			if (input.Prs is { Length: > 0 })
				filterCount++;
			if (!string.IsNullOrWhiteSpace(input.PrsFile))
				filterCount++;

			if (filterCount == 0)
			{
				collector.EmitError(string.Empty, "At least one filter option must be specified: --all, --input-products, --prs, or --prs-file");
				return false;
			}

			if (filterCount > 1)
			{
				collector.EmitError(string.Empty, "Only one filter option can be specified at a time: --all, --input-products, --prs, or --prs-file");
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
			if (input.InputProducts != null && input.InputProducts.Count > 0)
			{
				foreach (var product in input.InputProducts)
				{
					var version = product.Target ?? string.Empty;
					_ = productsToMatch.Add((product.Product.ToLowerInvariant(), version));
				}
			}

			// Determine output path to exclude it from input files
			var outputPath = input.Output ?? _fileSystem.Path.Combine(input.Directory, "changelog-bundle.yaml");
			var outputFileName = _fileSystem.Path.GetFileName(outputPath);

			// Read all YAML files from directory (exclude bundle files and output file)
			var allYamlFiles = _fileSystem.Directory.GetFiles(input.Directory, "*.yaml", SearchOption.TopDirectoryOnly)
				.Concat(_fileSystem.Directory.GetFiles(input.Directory, "*.yml", SearchOption.TopDirectoryOnly))
				.ToList();

			var yamlFiles = new List<string>();
			foreach (var filePath in allYamlFiles)
			{
				var fileName = _fileSystem.Path.GetFileName(filePath);

				// Exclude the output file
				if (fileName.Equals(outputFileName, StringComparison.OrdinalIgnoreCase))
					continue;

				// Check if file is a bundle file by looking for "entries:" key (unique to bundle files)
				try
				{
					var fileContent = await _fileSystem.File.ReadAllTextAsync(filePath, ctx);
					// Bundle files have "entries:" at root level, changelog files don't
					if (fileContent.Contains("entries:", StringComparison.Ordinal) &&
						fileContent.Contains("products:", StringComparison.Ordinal))
					{
						_logger.LogDebug("Skipping bundle file: {FileName}", fileName);
						continue;
					}
				}
				catch (Exception ex) when (ex is not (OutOfMemoryException or StackOverflowException or ThreadAbortException))
				{
					// If we can't read the file, skip it
					_logger.LogWarning(ex, "Failed to read file {FileName} for bundle detection", fileName);
					continue;
				}

				yamlFiles.Add(filePath);
			}

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

			// Set products array in output
			// If --output-products was specified, use those values (override any from changelogs)
			if (input.OutputProducts != null && input.OutputProducts.Count > 0)
			{
				bundledData.Products = input.OutputProducts
					.OrderBy(p => p.Product)
					.ThenBy(p => p.Target ?? string.Empty)
					.Select(p => new BundledProduct
					{
						Product = p.Product,
						Target = p.Target
					})
					.ToList();
			}
			// If --input-products filter was used, only include those specific product-versions
			else if (productsToMatch.Count > 0)
			{
				bundledData.Products = productsToMatch
					.OrderBy(pv => pv.product)
					.ThenBy(pv => pv.version)
					.Select(pv => new BundledProduct
					{
						Product = pv.product,
						Target = string.IsNullOrWhiteSpace(pv.version) ? null : pv.version
					})
					.ToList();
			}
			// Otherwise, extract unique products/versions from changelog entries
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
						Target = string.IsNullOrWhiteSpace(pv.version) ? null : pv.version
					})
					.ToList();
			}

			// Check for products with same product ID but different versions
			var productsByProductId = bundledData.Products.GroupBy(p => p.Product, StringComparer.OrdinalIgnoreCase)
				.Where(g => g.Count() > 1)
				.ToList();

			foreach (var productGroup in productsByProductId)
			{
				var targets = productGroup.Select(p => string.IsNullOrWhiteSpace(p.Target) ? "(no target)" : p.Target).ToList();
				collector.EmitWarning(string.Empty, $"Product '{productGroup.Key}' has multiple targets in bundle: {string.Join(", ", targets)}");
			}

			// Build entries
			if (input.Resolve)
			{
				// When resolving, include changelog contents and validate required fields
				var resolvedEntries = new List<BundledEntry>();
				foreach (var (data, filePath, fileName, checksum) in changelogEntries)
				{
					// Validate required fields
					if (string.IsNullOrWhiteSpace(data.Title))
					{
						collector.EmitError(filePath, "Changelog file is missing required field: title");
						return false;
					}

					if (string.IsNullOrWhiteSpace(data.Type))
					{
						collector.EmitError(filePath, "Changelog file is missing required field: type");
						return false;
					}

					if (data.Products == null || data.Products.Count == 0)
					{
						collector.EmitError(filePath, "Changelog file is missing required field: products");
						return false;
					}

					// Validate products have required fields
					if (data.Products.Any(product => string.IsNullOrWhiteSpace(product.Product)))
					{
						collector.EmitError(filePath, "Changelog file has product entry missing required field: product");
						return false;
					}

					resolvedEntries.Add(new BundledEntry
					{
						File = new BundledFile
						{
							Name = fileName,
							Checksum = checksum
						},
						Type = data.Type,
						Title = data.Title,
						Products = data.Products,
						Description = data.Description,
						Impact = data.Impact,
						Action = data.Action,
						FeatureId = data.FeatureId,
						Highlight = data.Highlight,
						Subtype = data.Subtype,
						Areas = data.Areas,
						Pr = data.Pr,
						Issues = data.Issues
					});
				}

				bundledData.Entries = resolvedEntries;
			}
			else
			{
				// Only include file information
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
			}

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

	public async Task<bool> RenderChangelogs(
		IDiagnosticsCollector collector,
		ChangelogRenderInput input,
		Cancel ctx
	)
	{
		try
		{
			// Validate input
			if (input.Bundles == null || input.Bundles.Count == 0)
			{
				collector.EmitError(string.Empty, "At least one bundle file is required. Use --input to specify bundle files.");
				return false;
			}

			var deserializer = new StaticDeserializerBuilder(new ChangelogYamlStaticContext())
				.WithNamingConvention(UnderscoredNamingConvention.Instance)
				.Build();

			// Validation phase: Load and validate all bundles before merging
			var bundleDataList = new List<(BundledChangelogData data, BundleInput input, string directory)>();
			var seenFileNames = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase); // filename -> list of bundle files
			var seenPrs = new Dictionary<string, List<string>>(); // PR -> list of bundle files
			var defaultRepo = "elastic";

			foreach (var bundleInput in input.Bundles)
			{
				if (string.IsNullOrWhiteSpace(bundleInput.BundleFile))
				{
					collector.EmitError(string.Empty, "Bundle file path is required for each --input");
					return false;
				}

				if (!_fileSystem.File.Exists(bundleInput.BundleFile))
				{
					collector.EmitError(bundleInput.BundleFile, "Bundle file does not exist");
					return false;
				}

				// Load bundle file
				var bundleContent = await _fileSystem.File.ReadAllTextAsync(bundleInput.BundleFile, ctx);

				// Validate bundle structure - check for unexpected fields by deserializing
				BundledChangelogData? bundledData;
				try
				{
					bundledData = deserializer.Deserialize<BundledChangelogData>(bundleContent);
				}
				catch (YamlException yamlEx)
				{
					collector.EmitError(bundleInput.BundleFile, $"Failed to deserialize bundle file: {yamlEx.Message}", yamlEx);
					return false;
				}

				if (bundledData == null)
				{
					collector.EmitError(bundleInput.BundleFile, "Failed to deserialize bundle file");
					return false;
				}

				// Validate bundle has required structure
				if (bundledData.Products == null)
				{
					collector.EmitError(bundleInput.BundleFile, "Bundle file is missing required field: products");
					return false;
				}

				if (bundledData.Entries == null)
				{
					collector.EmitError(bundleInput.BundleFile, "Bundle file is missing required field: entries");
					return false;
				}

				// Determine directory for resolving file references
				var bundleDirectory = bundleInput.Directory ?? _fileSystem.Path.GetDirectoryName(bundleInput.BundleFile) ?? Directory.GetCurrentDirectory();

				// Validate all referenced files exist and check for duplicates
				var fileNamesInThisBundle = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
				foreach (var entry in bundledData.Entries)
				{
					// Track file names for duplicate detection
					if (!string.IsNullOrWhiteSpace(entry.File?.Name))
					{
						var fileName = entry.File.Name;

						// Check for duplicates within the same bundle
						if (!fileNamesInThisBundle.Add(fileName))
						{
							collector.EmitWarning(bundleInput.BundleFile, $"Changelog file '{fileName}' appears multiple times in the same bundle");
						}

						// Track across bundles
						if (!seenFileNames.TryGetValue(fileName, out var bundleList))
						{
							bundleList = [];
							seenFileNames[fileName] = bundleList;
						}
						bundleList.Add(bundleInput.BundleFile);
					}

					// If entry has resolved data, validate it
					if (!string.IsNullOrWhiteSpace(entry.Title) && !string.IsNullOrWhiteSpace(entry.Type))
					{

						if (entry.Products == null || entry.Products.Count == 0)
						{
							collector.EmitError(bundleInput.BundleFile, $"Entry '{entry.Title}' in bundle is missing required field: products");
							return false;
						}

						// Track PRs for duplicate detection
						if (!string.IsNullOrWhiteSpace(entry.Pr))
						{
							var normalizedPr = NormalizePrForComparison(entry.Pr, null, null);
							if (!seenPrs.TryGetValue(normalizedPr, out var prBundleList))
							{
								prBundleList = [];
								seenPrs[normalizedPr] = prBundleList;
							}
							prBundleList.Add(bundleInput.BundleFile);
						}
					}
					else
					{
						// Entry only has file reference - validate file exists
						if (string.IsNullOrWhiteSpace(entry.File?.Name))
						{
							collector.EmitError(bundleInput.BundleFile, "Entry in bundle is missing required field: file.name");
							return false;
						}

						if (string.IsNullOrWhiteSpace(entry.File.Checksum))
						{
							collector.EmitError(bundleInput.BundleFile, $"Entry for file '{entry.File.Name}' in bundle is missing required field: file.checksum");
							return false;
						}

						var filePath = _fileSystem.Path.Combine(bundleDirectory, entry.File.Name);
						if (!_fileSystem.File.Exists(filePath))
						{
							collector.EmitError(bundleInput.BundleFile, $"Referenced changelog file '{entry.File.Name}' does not exist at path: {filePath}");
							return false;
						}

						// Validate the changelog file can be deserialized
						try
						{
							var fileContent = await _fileSystem.File.ReadAllTextAsync(filePath, ctx);
							var checksum = ComputeSha1(fileContent);
							if (checksum != entry.File.Checksum)
							{
								collector.EmitWarning(bundleInput.BundleFile, $"Checksum mismatch for file {entry.File.Name}. Expected {entry.File.Checksum}, got {checksum}");
							}

							// Deserialize YAML (skip comment lines) to validate structure
							var yamlLines = fileContent.Split('\n');
							var yamlWithoutComments = string.Join('\n', yamlLines.Where(line => !line.TrimStart().StartsWith('#')));

							// Normalize "version:" to "target:" in products section
							var normalizedYaml = VersionToTargetRegex().Replace(yamlWithoutComments, "$1target:");

							var entryData = deserializer.Deserialize<ChangelogData>(normalizedYaml);
							if (entryData == null)
							{
								collector.EmitError(bundleInput.BundleFile, $"Failed to deserialize changelog file '{entry.File.Name}'");
								return false;
							}

							// Validate required fields in changelog file
							if (string.IsNullOrWhiteSpace(entryData.Title))
							{
								collector.EmitError(filePath, "Changelog file is missing required field: title");
								return false;
							}

							if (string.IsNullOrWhiteSpace(entryData.Type))
							{
								collector.EmitError(filePath, "Changelog file is missing required field: type");
								return false;
							}

							if (entryData.Products == null || entryData.Products.Count == 0)
							{
								collector.EmitError(filePath, "Changelog file is missing required field: products");
								return false;
							}

							// Track PRs for duplicate detection
							if (!string.IsNullOrWhiteSpace(entryData.Pr))
							{
								var normalizedPr = NormalizePrForComparison(entryData.Pr, null, null);
								if (!seenPrs.TryGetValue(normalizedPr, out var prBundleList2))
								{
									prBundleList2 = [];
									seenPrs[normalizedPr] = prBundleList2;
								}
								prBundleList2.Add(bundleInput.BundleFile);
							}
						}
						catch (YamlException yamlEx)
						{
							collector.EmitError(filePath, $"Failed to parse changelog file: {yamlEx.Message}", yamlEx);
							return false;
						}
					}
				}

				bundleDataList.Add((bundledData, bundleInput, bundleDirectory));
			}

			// Check for duplicate file names across bundles
			foreach (var (fileName, bundleFiles) in seenFileNames.Where(kvp => kvp.Value.Count > 1))
			{
				var uniqueBundles = bundleFiles.Distinct().ToList();
				if (uniqueBundles.Count > 1)
				{
					collector.EmitWarning(string.Empty, $"Changelog file '{fileName}' appears in multiple bundles: {string.Join(", ", uniqueBundles)}");
				}
			}

			// Check for duplicate PRs
			foreach (var (pr, bundleFiles) in seenPrs.Where(kvp => kvp.Value.Count > 1))
			{
				var uniqueBundles = bundleFiles.Distinct().ToList();
				if (uniqueBundles.Count > 1)
				{
					collector.EmitWarning(string.Empty, $"PR '{pr}' appears in multiple bundles: {string.Join(", ", uniqueBundles)}");
				}
			}

			// If validation found errors, stop before merging
			if (collector.Errors > 0)
			{
				return false;
			}

			// Merge phase: Now that validation passed, load and merge all bundles
			var allResolvedEntries = new List<(ChangelogData entry, string repo)>();
			var allProducts = new HashSet<(string product, string target)>();

			foreach (var (bundledData, bundleInput, bundleDirectory) in bundleDataList)
			{
				// Collect products from this bundle
				foreach (var product in bundledData.Products)
				{
					var target = product.Target ?? string.Empty;
					_ = allProducts.Add((product.Product, target));
				}

				var repo = bundleInput.Repo ?? defaultRepo;

				// Resolve entries
				foreach (var entry in bundledData.Entries)
				{
					ChangelogData? entryData = null;

					// If entry has resolved data, use it
					if (!string.IsNullOrWhiteSpace(entry.Title) && !string.IsNullOrWhiteSpace(entry.Type))
					{
						entryData = new ChangelogData
						{
							Title = entry.Title,
							Type = entry.Type,
							Subtype = entry.Subtype,
							Description = entry.Description,
							Impact = entry.Impact,
							Action = entry.Action,
							FeatureId = entry.FeatureId,
							Highlight = entry.Highlight,
							Pr = entry.Pr,
							Products = entry.Products ?? [],
							Areas = entry.Areas,
							Issues = entry.Issues
						};
					}
					else
					{
						// Load from file (already validated to exist)
						var filePath = _fileSystem.Path.Combine(bundleDirectory, entry.File.Name);
						var fileContent = await _fileSystem.File.ReadAllTextAsync(filePath, ctx);

						// Deserialize YAML (skip comment lines)
						var yamlLines = fileContent.Split('\n');
						var yamlWithoutComments = string.Join('\n', yamlLines.Where(line => !line.TrimStart().StartsWith('#')));

						// Normalize "version:" to "target:" in products section
						var normalizedYaml = VersionToTargetRegex().Replace(yamlWithoutComments, "$1target:");

						entryData = deserializer.Deserialize<ChangelogData>(normalizedYaml);
					}

					if (entryData != null)
					{
						allResolvedEntries.Add((entryData, repo));
					}
				}
			}

			if (allResolvedEntries.Count == 0)
			{
				collector.EmitError(string.Empty, "No changelog entries to render");
				return false;
			}

			// Determine output directory
			var outputDir = input.Output ?? Directory.GetCurrentDirectory();
			if (!_fileSystem.Directory.Exists(outputDir))
			{
				_ = _fileSystem.Directory.CreateDirectory(outputDir);
			}

			// Extract version from products (use first product's target if available, or "unknown")
			var version = allProducts.Count > 0
				? allProducts.OrderBy(p => p.product).ThenBy(p => p.target).First().target
				: "unknown";

			if (string.IsNullOrWhiteSpace(version))
			{
				version = "unknown";
			}

			// Warn if --title was not provided and version defaults to "unknown"
			if (string.IsNullOrWhiteSpace(input.Title) && version == "unknown")
			{
				collector.EmitWarning(string.Empty, "No --title option provided and bundle files do not contain 'target' values. Output folder and markdown titles will default to 'unknown'. Consider using --title to specify a custom title.");
			}

			// Group entries by type (kind)
			var entriesByType = allResolvedEntries.Select(e => e.entry).GroupBy(e => e.Type).ToDictionary(g => g.Key, g => g.ToList());

			// Use title from input or default to version
			var title = input.Title ?? version;
			// Convert title to slug format for folder names and anchors (lowercase, dashes instead of spaces)
			var titleSlug = TitleToSlug(title);

			// Render markdown files (use first repo found, or default)
			var repoForRendering = allResolvedEntries.Count > 0 ? allResolvedEntries[0].repo : defaultRepo;

			// Render index.md (features, enhancements, bug fixes, security)
			await RenderIndexMarkdown(collector, outputDir, title, titleSlug, repoForRendering, allResolvedEntries.Select(e => e.entry).ToList(), entriesByType, input.Subsections, input.HidePrivateLinks, ctx);

			// Render breaking-changes.md
			await RenderBreakingChangesMarkdown(collector, outputDir, title, titleSlug, repoForRendering, allResolvedEntries.Select(e => e.entry).ToList(), entriesByType, input.Subsections, input.HidePrivateLinks, ctx);

			// Render deprecations.md
			await RenderDeprecationsMarkdown(collector, outputDir, title, titleSlug, repoForRendering, allResolvedEntries.Select(e => e.entry).ToList(), entriesByType, input.Subsections, input.HidePrivateLinks, ctx);

			_logger.LogInformation("Rendered changelog markdown files to {OutputDir}", outputDir);

			return true;
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (IOException ioEx)
		{
			collector.EmitError(string.Empty, $"IO error rendering changelogs: {ioEx.Message}", ioEx);
			return false;
		}
		catch (UnauthorizedAccessException uaEx)
		{
			collector.EmitError(string.Empty, $"Access denied rendering changelogs: {uaEx.Message}", uaEx);
			return false;
		}
		catch (YamlException yamlEx)
		{
			collector.EmitError(string.Empty, $"YAML parsing error: {yamlEx.Message}", yamlEx);
			return false;
		}
	}

	[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Parameters match interface pattern")]
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0058:Expression value is never used", Justification = "StringBuilder methods return builder for chaining")]
	private async Task RenderIndexMarkdown(
		IDiagnosticsCollector collector,
		string outputDir,
		string title,
		string titleSlug,
		string repo,
		List<ChangelogData> entries,
		Dictionary<string, List<ChangelogData>> entriesByType,
		bool subsections,
		bool hidePrivateLinks,
		Cancel ctx
	)
	{
		var features = entriesByType.GetValueOrDefault("feature", []);
		var enhancements = entriesByType.GetValueOrDefault("enhancement", []);
		var security = entriesByType.GetValueOrDefault("security", []);
		var bugFixes = entriesByType.GetValueOrDefault("bug-fix", []);

		if (features.Count == 0 && enhancements.Count == 0 && security.Count == 0 && bugFixes.Count == 0)
		{
			// Still create file with "no changes" message
		}

		var hasBreakingChanges = entriesByType.ContainsKey("breaking-change");
		var hasDeprecations = entriesByType.ContainsKey("deprecation");
		var hasKnownIssues = entriesByType.ContainsKey("known-issue");

		var otherLinks = new List<string>();
		if (hasKnownIssues)
		{
			otherLinks.Add("[Known issues](/release-notes/known-issues.md)");
		}
		if (hasBreakingChanges)
		{
			otherLinks.Add($"[Breaking changes](/release-notes/breaking-changes.md#{repo}-{titleSlug}-breaking-changes)");
		}
		if (hasDeprecations)
		{
			otherLinks.Add($"[Deprecations](/release-notes/deprecations.md#{repo}-{titleSlug}-deprecations)");
		}

		var sb = new StringBuilder();
		sb.AppendLine(CultureInfo.InvariantCulture, $"## {title} [{repo}-release-notes-{titleSlug}]");

		if (otherLinks.Count > 0)
		{
			var linksText = string.Join(" and ", otherLinks);
			sb.AppendLine(CultureInfo.InvariantCulture, $"_{linksText}._");
			sb.AppendLine();
		}

		if (features.Count > 0 || enhancements.Count > 0 || security.Count > 0 || bugFixes.Count > 0)
		{
			if (features.Count > 0 || enhancements.Count > 0)
			{
				sb.AppendLine(CultureInfo.InvariantCulture, $"### Features and enhancements [{repo}-{titleSlug}-features-enhancements]");
				var combined = features.Concat(enhancements).ToList();
				RenderEntriesByArea(sb, combined, repo, subsections, hidePrivateLinks);
			}

			if (security.Count > 0 || bugFixes.Count > 0)
			{
				sb.AppendLine();
				sb.AppendLine(CultureInfo.InvariantCulture, $"### Fixes [{repo}-{titleSlug}-fixes]");
				var combined = security.Concat(bugFixes).ToList();
				RenderEntriesByArea(sb, combined, repo, subsections, hidePrivateLinks);
			}
		}
		else
		{
			sb.AppendLine("_No new features, enhancements, or fixes._");
		}

		var indexPath = _fileSystem.Path.Combine(outputDir, titleSlug, "index.md");
		var indexDir = _fileSystem.Path.GetDirectoryName(indexPath);
		if (!string.IsNullOrWhiteSpace(indexDir) && !_fileSystem.Directory.Exists(indexDir))
		{
			_ = _fileSystem.Directory.CreateDirectory(indexDir);
		}

		await _fileSystem.File.WriteAllTextAsync(indexPath, sb.ToString(), ctx);
	}

	[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Parameters match interface pattern")]
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0058:Expression value is never used", Justification = "StringBuilder methods return builder for chaining")]
	private async Task RenderBreakingChangesMarkdown(
		IDiagnosticsCollector collector,
		string outputDir,
		string title,
		string titleSlug,
		string repo,
		List<ChangelogData> entries,
		Dictionary<string, List<ChangelogData>> entriesByType,
		bool subsections,
		bool hidePrivateLinks,
		Cancel ctx
	)
	{
		var breakingChanges = entriesByType.GetValueOrDefault("breaking-change", []);

		var sb = new StringBuilder();
		sb.AppendLine(CultureInfo.InvariantCulture, $"## {title} [{repo}-{titleSlug}-breaking-changes]");

		if (breakingChanges.Count > 0)
		{
			var groupedByArea = breakingChanges.GroupBy(e => GetComponent(e)).ToList();
			foreach (var areaGroup in groupedByArea)
			{
				if (subsections && !string.IsNullOrWhiteSpace(areaGroup.Key))
				{
					var header = FormatAreaHeader(areaGroup.Key);
					sb.AppendLine();
					sb.AppendLine(CultureInfo.InvariantCulture, $"**{header}**");
				}

				foreach (var entry in areaGroup)
				{
					sb.AppendLine();
					sb.AppendLine(CultureInfo.InvariantCulture, $"::::{{dropdown}} {Beautify(entry.Title)}");
					sb.AppendLine(entry.Description ?? "% Describe the functionality that changed");
					sb.AppendLine();
					if (hidePrivateLinks)
					{
						// When hiding private links, put them on separate lines as comments
						if (!string.IsNullOrWhiteSpace(entry.Pr))
						{
							sb.AppendLine(FormatPrLink(entry.Pr, repo, hidePrivateLinks));
						}
						if (entry.Issues != null && entry.Issues.Count > 0)
						{
							foreach (var issue in entry.Issues)
							{
								sb.AppendLine(FormatIssueLink(issue, repo, hidePrivateLinks));
							}
						}
						sb.AppendLine("For more information, check the pull request or issue above.");
					}
					else
					{
						sb.Append("For more information, check ");
						if (!string.IsNullOrWhiteSpace(entry.Pr))
						{
							sb.Append(FormatPrLink(entry.Pr, repo, hidePrivateLinks));
						}
						if (entry.Issues != null && entry.Issues.Count > 0)
						{
							foreach (var issue in entry.Issues)
							{
								sb.Append(' ');
								sb.Append(FormatIssueLink(issue, repo, hidePrivateLinks));
							}
						}
						sb.AppendLine(".");
					}
					sb.AppendLine();

					if (!string.IsNullOrWhiteSpace(entry.Impact))
					{
						sb.AppendLine("**Impact**<br>" + entry.Impact);
					}
					else
					{
						sb.AppendLine("% **Impact**<br>_Add a description of the impact_");
					}

					sb.AppendLine();

					if (!string.IsNullOrWhiteSpace(entry.Action))
					{
						sb.AppendLine("**Action**<br>" + entry.Action);
					}
					else
					{
						sb.AppendLine("% **Action**<br>_Add a description of the what action to take_");
					}

					sb.AppendLine("::::");
				}
			}
		}
		else
		{
			sb.AppendLine("_No breaking changes._");
		}

		var breakingPath = _fileSystem.Path.Combine(outputDir, titleSlug, "breaking-changes.md");
		var breakingDir = _fileSystem.Path.GetDirectoryName(breakingPath);
		if (!string.IsNullOrWhiteSpace(breakingDir) && !_fileSystem.Directory.Exists(breakingDir))
		{
			_ = _fileSystem.Directory.CreateDirectory(breakingDir);
		}

		await _fileSystem.File.WriteAllTextAsync(breakingPath, sb.ToString(), ctx);
	}

	[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Parameters match interface pattern")]
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0058:Expression value is never used", Justification = "StringBuilder methods return builder for chaining")]
	private async Task RenderDeprecationsMarkdown(
		IDiagnosticsCollector collector,
		string outputDir,
		string title,
		string titleSlug,
		string repo,
		List<ChangelogData> entries,
		Dictionary<string, List<ChangelogData>> entriesByType,
		bool subsections,
		bool hidePrivateLinks,
		Cancel ctx
	)
	{
		var deprecations = entriesByType.GetValueOrDefault("deprecation", []);

		var sb = new StringBuilder();
		sb.AppendLine(CultureInfo.InvariantCulture, $"## {title} [{repo}-{titleSlug}-deprecations]");

		if (deprecations.Count > 0)
		{
			var groupedByArea = deprecations.GroupBy(e => GetComponent(e)).ToList();
			foreach (var areaGroup in groupedByArea)
			{
				if (subsections && !string.IsNullOrWhiteSpace(areaGroup.Key))
				{
					var header = FormatAreaHeader(areaGroup.Key);
					sb.AppendLine();
					sb.AppendLine(CultureInfo.InvariantCulture, $"**{header}**");
				}

				foreach (var entry in areaGroup)
				{
					sb.AppendLine();
					sb.AppendLine(CultureInfo.InvariantCulture, $"::::{{dropdown}} {Beautify(entry.Title)}");
					sb.AppendLine(entry.Description ?? "% Describe the functionality that was deprecated");
					sb.AppendLine();
					if (hidePrivateLinks)
					{
						// When hiding private links, put them on separate lines as comments
						if (!string.IsNullOrWhiteSpace(entry.Pr))
						{
							sb.AppendLine(FormatPrLink(entry.Pr, repo, hidePrivateLinks));
						}
						if (entry.Issues != null && entry.Issues.Count > 0)
						{
							foreach (var issue in entry.Issues)
							{
								sb.AppendLine(FormatIssueLink(issue, repo, hidePrivateLinks));
							}
						}
						sb.AppendLine("For more information, check the pull request or issue above.");
					}
					else
					{
						sb.Append("For more information, check ");
						if (!string.IsNullOrWhiteSpace(entry.Pr))
						{
							sb.Append(FormatPrLink(entry.Pr, repo, hidePrivateLinks));
						}
						if (entry.Issues != null && entry.Issues.Count > 0)
						{
							foreach (var issue in entry.Issues)
							{
								sb.Append(' ');
								sb.Append(FormatIssueLink(issue, repo, hidePrivateLinks));
							}
						}
						sb.AppendLine(".");
					}
					sb.AppendLine();

					if (!string.IsNullOrWhiteSpace(entry.Impact))
					{
						sb.AppendLine("**Impact**<br>" + entry.Impact);
					}
					else
					{
						sb.AppendLine("% **Impact**<br>_Add a description of the impact_");
					}

					sb.AppendLine();

					if (!string.IsNullOrWhiteSpace(entry.Action))
					{
						sb.AppendLine("**Action**<br>" + entry.Action);
					}
					else
					{
						sb.AppendLine("% **Action**<br>_Add a description of the what action to take_");
					}

					sb.AppendLine("::::");
				}
			}
		}
		else
		{
			sb.AppendLine("_No deprecations._");
		}

		var deprecationsPath = _fileSystem.Path.Combine(outputDir, titleSlug, "deprecations.md");
		var deprecationsDir = _fileSystem.Path.GetDirectoryName(deprecationsPath);
		if (!string.IsNullOrWhiteSpace(deprecationsDir) && !_fileSystem.Directory.Exists(deprecationsDir))
		{
			_ = _fileSystem.Directory.CreateDirectory(deprecationsDir);
		}

		await _fileSystem.File.WriteAllTextAsync(deprecationsPath, sb.ToString(), ctx);
	}

	[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0058:Expression value is never used", Justification = "StringBuilder methods return builder for chaining")]
	private void RenderEntriesByArea(StringBuilder sb, List<ChangelogData> entries, string repo, bool subsections, bool hidePrivateLinks)
	{
		var groupedByArea = entries.GroupBy(e => GetComponent(e)).ToList();
		foreach (var areaGroup in groupedByArea)
		{
			if (subsections && !string.IsNullOrWhiteSpace(areaGroup.Key))
			{
				var header = FormatAreaHeader(areaGroup.Key);
				sb.AppendLine();
				sb.AppendLine(CultureInfo.InvariantCulture, $"**{header}**");
			}

			foreach (var entry in areaGroup)
			{
				sb.Append("* ");
				sb.Append(Beautify(entry.Title));

				var hasCommentedLinks = false;
				if (hidePrivateLinks)
				{
					// When hiding private links, put them on separate lines as comments with proper indentation
					if (!string.IsNullOrWhiteSpace(entry.Pr))
					{
						sb.AppendLine();
						sb.Append("  ");
						sb.Append(FormatPrLink(entry.Pr, repo, hidePrivateLinks));
						hasCommentedLinks = true;
					}

					if (entry.Issues != null && entry.Issues.Count > 0)
					{
						foreach (var issue in entry.Issues)
						{
							sb.AppendLine();
							sb.Append("  ");
							sb.Append(FormatIssueLink(issue, repo, hidePrivateLinks));
							hasCommentedLinks = true;
						}
					}

					// Add newline after the last link if there are commented links
					if (hasCommentedLinks)
					{
						sb.AppendLine();
					}
				}
				else
				{
					sb.Append(' ');
					if (!string.IsNullOrWhiteSpace(entry.Pr))
					{
						sb.Append(FormatPrLink(entry.Pr, repo, hidePrivateLinks));
						sb.Append(' ');
					}

					if (entry.Issues != null && entry.Issues.Count > 0)
					{
						foreach (var issue in entry.Issues)
						{
							sb.Append(FormatIssueLink(issue, repo, hidePrivateLinks));
							sb.Append(' ');
						}
					}
				}

				if (!string.IsNullOrWhiteSpace(entry.Description))
				{
					// Add blank line before description
					// When hidePrivateLinks is true and links exist, add an indented blank line
					if (hidePrivateLinks && hasCommentedLinks)
					{
						sb.AppendLine("  ");
					}
					else
					{
						sb.AppendLine();
					}
					var indented = Indent(entry.Description);
					sb.AppendLine(indented);
				}
				else
				{
					sb.AppendLine();
				}
			}
		}
	}

	private static string GetComponent(ChangelogData entry)
	{
		// Map areas (list) to component (string) - use first area or empty string
		if (entry.Areas != null && entry.Areas.Count > 0)
		{
			return entry.Areas[0];
		}
		return string.Empty;
	}

	private static string FormatAreaHeader(string area)
	{
		// Capitalize first letter and replace hyphens with spaces
		if (string.IsNullOrWhiteSpace(area))
			return string.Empty;

		var result = area.Length < 2
			? char.ToUpperInvariant(area[0]).ToString()
			: char.ToUpperInvariant(area[0]) + area[1..];
		return result.Replace("-", " ");
	}

	private static string Beautify(string text)
	{
		if (string.IsNullOrWhiteSpace(text))
			return string.Empty;

		// Capitalize first letter and ensure ends with period
		var result = text.Length < 2
			? char.ToUpperInvariant(text[0]).ToString()
			: char.ToUpperInvariant(text[0]) + text[1..];
		if (!result.EndsWith('.'))
		{
			result += ".";
		}
		return result;
	}

	private static string TitleToSlug(string title)
	{
		if (string.IsNullOrWhiteSpace(title))
			return string.Empty;

		// Convert to lowercase and replace spaces with dashes
		return title.ToLowerInvariant().Replace(' ', '-');
	}

	private static string Indent(string text)
	{
		// Indent each line with two spaces
		var lines = text.Split('\n');
		return string.Join("\n", lines.Select(line => "  " + line));
	}

	[GeneratedRegex(@"\d+$", RegexOptions.None)]
	private static partial Regex PrNumberRegex();

	[GeneratedRegex(@"\d+$", RegexOptions.None)]
	private static partial Regex IssueNumberRegex();

	private static string FormatPrLink(string pr, string repo, bool hidePrivateLinks)
	{
		// Extract PR number
		var match = PrNumberRegex().Match(pr);
		var prNumber = match.Success ? match.Value : pr;

		// Format as markdown link
		string link;
		if (pr.StartsWith("http", StringComparison.OrdinalIgnoreCase))
		{
			link = $"[#{prNumber}]({pr})";
		}
		else
		{
			var url = $"https://github.com/elastic/{repo}/pull/{prNumber}";
			link = $"[#{prNumber}]({url})";
		}

		// Comment out link if hiding private links
		if (hidePrivateLinks)
		{
			return $"% {link}";
		}

		return link;
	}

	private static string FormatIssueLink(string issue, string repo, bool hidePrivateLinks)
	{
		// Extract issue number
		var match = IssueNumberRegex().Match(issue);
		var issueNumber = match.Success ? match.Value : issue;

		// Format as markdown link
		string link;
		if (issue.StartsWith("http", StringComparison.OrdinalIgnoreCase))
		{
			link = $"[#{issueNumber}]({issue})";
		}
		else
		{
			var url = $"https://github.com/elastic/{repo}/issues/{issueNumber}";
			link = $"[#{issueNumber}]({url})";
		}

		// Comment out link if hiding private links
		if (hidePrivateLinks)
		{
			return $"% {link}";
		}

		return link;
	}
}

