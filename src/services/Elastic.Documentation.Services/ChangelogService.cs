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

	private static class ChangelogEntryTypes
	{
		public const string Feature = "feature";
		public const string Enhancement = "enhancement";
		public const string Security = "security";
		public const string BugFix = "bug-fix";
		public const string BreakingChange = "breaking-change";
		public const string Deprecation = "deprecation";
		public const string KnownIssue = "known-issue";
		public const string Docs = "docs";
		public const string Regression = "regression";
		public const string Other = "other";
	}

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

	internal async Task<ChangelogConfiguration?> LoadChangelogConfiguration(
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

			// If available_types is not specified or empty, use defaults
			if (config.AvailableTypes == null || config.AvailableTypes.Count == 0)
			{
				config.AvailableTypes = defaultConfig.AvailableTypes.ToList();
			}
			else
			{
				// Validate available_types - must be subset of defaults
				foreach (var type in config.AvailableTypes.Where(t => !defaultConfig.AvailableTypes.Contains(t)))
				{
					collector.EmitError(finalConfigPath, $"Type '{type}' in changelog.yml is not in the list of available types. Available types: {string.Join(", ", defaultConfig.AvailableTypes)}");
					return null;
				}
			}

			// If available_subtypes is not specified or empty, use defaults
			if (config.AvailableSubtypes == null || config.AvailableSubtypes.Count == 0)
			{
				config.AvailableSubtypes = defaultConfig.AvailableSubtypes.ToList();
			}
			else
			{
				// Validate available_subtypes - must be subset of defaults
				foreach (var subtype in config.AvailableSubtypes.Where(s => !defaultConfig.AvailableSubtypes.Contains(s)))
				{
					collector.EmitError(finalConfigPath, $"Subtype '{subtype}' in changelog.yml is not in the list of available subtypes. Available subtypes: {string.Join(", ", defaultConfig.AvailableSubtypes)}");
					return null;
				}
			}

			// If available_lifecycles is not specified or empty, use defaults
			if (config.AvailableLifecycles == null || config.AvailableLifecycles.Count == 0)
			{
				config.AvailableLifecycles = defaultConfig.AvailableLifecycles.ToList();
			}
			else
			{
				// Validate available_lifecycles - must be subset of defaults
				foreach (var lifecycle in config.AvailableLifecycles.Where(l => !defaultConfig.AvailableLifecycles.Contains(l)))
				{
					collector.EmitError(finalConfigPath, $"Lifecycle '{lifecycle}' in changelog.yml is not in the list of available lifecycles. Available lifecycles: {string.Join(", ", defaultConfig.AvailableLifecycles)}");
					return null;
				}
			}

			// Validate render_blockers types against available_types
			if (config.RenderBlockers != null)
			{
				foreach (var (productKey, blockersEntry) in config.RenderBlockers)
				{
					if (blockersEntry?.Types != null && blockersEntry.Types.Count > 0)
					{
						var invalidType = blockersEntry.Types.FirstOrDefault(type => !config.AvailableTypes.Contains(type));
						if (invalidType != null)
						{
							collector.EmitError(finalConfigPath, $"Type '{invalidType}' in render_blockers for '{productKey}' is not in the list of available types. Available types: {string.Join(", ", config.AvailableTypes)}");
							return null;
						}
					}
				}
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

			if (filterCount == 0)
			{
				collector.EmitError(string.Empty, "At least one filter option must be specified: --all, --input-products, or --prs");
				return false;
			}

			if (filterCount > 1)
			{
				collector.EmitError(string.Empty, "Only one filter option can be specified at a time: --all, --input-products, or --prs");
				return false;
			}

			// Load PRs - check if --prs contains a file path or a list of PRs
			var prsToMatch = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			if (input.Prs is { Length: > 0 })
			{
				// If there's exactly one value, check if it's a file path
				if (input.Prs.Length == 1)
				{
					var singleValue = input.Prs[0];

					// Check if it's a URL - URLs should always be treated as PRs, not file paths
					var isUrl = singleValue.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
						singleValue.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

					if (isUrl)
					{
						// Treat as PR identifier
						_ = prsToMatch.Add(singleValue);
					}
					else if (_fileSystem.File.Exists(singleValue))
					{
						// File exists, read PRs from it
						var prsFileContent = await _fileSystem.File.ReadAllTextAsync(singleValue, ctx);
						var prsFromFile = prsFileContent
							.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
							.Where(p => !string.IsNullOrWhiteSpace(p))
							.ToArray();

						foreach (var pr in prsFromFile)
						{
							_ = prsToMatch.Add(pr);
						}
					}
					else
					{
						// Check if it's in short PR format (owner/repo#number) before treating as file path
						var hashIndex = singleValue.LastIndexOf('#');
						var isShortPrFormat = false;
						if (hashIndex > 0 && hashIndex < singleValue.Length - 1)
						{
							var repoPart = singleValue[..hashIndex];
							var prPart = singleValue[(hashIndex + 1)..];
							var repoParts = repoPart.Split('/');
							// Check if it matches owner/repo#number format
							if (repoParts.Length == 2 && int.TryParse(prPart, out _))
							{
								isShortPrFormat = true;
								_ = prsToMatch.Add(singleValue);
							}
						}

						if (!isShortPrFormat)
						{
							// Check if it looks like a file path (contains path separators or has extension)
							var looksLikeFilePath = singleValue.Contains(_fileSystem.Path.DirectorySeparatorChar) ||
								singleValue.Contains(_fileSystem.Path.AltDirectorySeparatorChar) ||
								_fileSystem.Path.HasExtension(singleValue);

							if (looksLikeFilePath)
							{
								// File path doesn't exist - if there are no other PRs, return error; otherwise emit warning
								if (prsToMatch.Count == 0)
								{
									collector.EmitError(singleValue, $"File does not exist: {singleValue}");
									return false;
								}
								else
								{
									collector.EmitWarning(singleValue, $"File does not exist, skipping: {singleValue}");
								}
							}
							else
							{
								// Doesn't look like a file path, treat as PR identifier
								_ = prsToMatch.Add(singleValue);
							}
						}
					}
				}
				else
				{
					// Multiple values - process all values first, then check for errors
					var nonExistentFiles = new List<string>();
					foreach (var value in input.Prs)
					{
						// Check if it's a URL - URLs should always be treated as PRs
						var isUrl = value.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
							value.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

						if (isUrl)
						{
							// Treat as PR identifier
							_ = prsToMatch.Add(value);
						}
						else if (_fileSystem.File.Exists(value))
						{
							// File exists, read PRs from it
							var prsFileContent = await _fileSystem.File.ReadAllTextAsync(value, ctx);
							var prsFromFile = prsFileContent
								.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
								.Where(p => !string.IsNullOrWhiteSpace(p))
								.ToArray();

							foreach (var pr in prsFromFile)
							{
								_ = prsToMatch.Add(pr);
							}
						}
						else
						{
							// Check if it's in short PR format (owner/repo#number) before treating as file path
							var hashIndex = value.LastIndexOf('#');
							var isShortPrFormat = false;
							if (hashIndex > 0 && hashIndex < value.Length - 1)
							{
								var repoPart = value[..hashIndex];
								var prPart = value[(hashIndex + 1)..];
								var repoParts = repoPart.Split('/');
								// Check if it matches owner/repo#number format
								if (repoParts.Length == 2 && int.TryParse(prPart, out _))
								{
									isShortPrFormat = true;
									_ = prsToMatch.Add(value);
								}
							}

							if (!isShortPrFormat)
							{
								// Check if it looks like a file path
								var looksLikeFilePath = value.Contains(_fileSystem.Path.DirectorySeparatorChar) ||
									value.Contains(_fileSystem.Path.AltDirectorySeparatorChar) ||
									_fileSystem.Path.HasExtension(value);

								if (looksLikeFilePath)
								{
									// Track non-existent files to check later
									nonExistentFiles.Add(value);
								}
								else
								{
									// Doesn't look like a file path, treat as PR identifier
									_ = prsToMatch.Add(value);
								}
							}
						}
					}

					// After processing all values, handle non-existent files
					if (nonExistentFiles.Count > 0)
					{
						// If there are no valid PRs and we have non-existent files, return error
						if (prsToMatch.Count == 0)
						{
							collector.EmitError(nonExistentFiles[0], $"File does not exist: {nonExistentFiles[0]}");
							return false;
						}
						else
						{
							// Emit warnings for non-existent files since we have valid PRs
							foreach (var file in nonExistentFiles)
							{
								collector.EmitWarning(file, $"File does not exist, skipping: {file}");
							}
						}
					}
				}
			}

			// Validate that if any PR is just a number (not a URL and not in owner/repo#number format),
			// then owner and repo must be provided
			if (prsToMatch.Count > 0)
			{
				var hasNumericOnlyPr = false;
				foreach (var pr in prsToMatch)
				{
					// Check if it's a URL - URLs don't need owner/repo
					var isUrl = pr.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
						pr.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

					if (isUrl)
						continue;

					// Check if it's in owner/repo#number format - these don't need owner/repo
					var hashIndex = pr.LastIndexOf('#');
					if (hashIndex > 0 && hashIndex < pr.Length - 1)
					{
						var repoPart = pr[..hashIndex].Trim();
						var prPart = pr[(hashIndex + 1)..].Trim();
						var repoParts = repoPart.Split('/');
						// If it has a # and the part before # contains a /, it's likely owner/repo#number format
						if (repoParts.Length == 2 && int.TryParse(prPart, out _))
							continue;
					}

					// If it's just a number, it needs owner/repo
					if (int.TryParse(pr, out _))
					{
						hasNumericOnlyPr = true;
						break;
					}
				}

				if (hasNumericOnlyPr && (string.IsNullOrWhiteSpace(input.Owner) || string.IsNullOrWhiteSpace(input.Repo)))
				{
					collector.EmitError(string.Empty, "When --prs contains PR numbers (not URLs or owner/repo#number format), both --owner and --repo must be provided");
					return false;
				}
			}

			// Build set of product/version combinations to filter by
			var productsToMatch = new HashSet<(string product, string version)>();
			if (input.InputProducts is { Count: > 0 })
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

			_logger.LogInformation("Found {Count} matching changelog entries", changelogEntries.Count);

			// Build bundled data
			var bundledData = new BundledChangelogData();

			// Set products array in output
			// If --output-products was specified, use those values (override any from changelogs)
			if (input.OutputProducts is { Count: > 0 })
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
			else if (changelogEntries.Count > 0)
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
			else
			{
				// No entries and no products specified - initialize to empty list
				bundledData.Products = [];
			}

			// Check if we should allow empty result
			if (changelogEntries.Count == 0)
			{
				collector.EmitError(string.Empty, "No changelog entries matched the filter criteria");
				return false;
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
			if (changelogEntries.Count == 0)
			{
				// No entries - initialize to empty list
				bundledData.Entries = [];
			}
			else if (input.Resolve)
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

	[GeneratedRegex(@"github\.com/([^/]+)/([^/]+)/pull/(\d+)", RegexOptions.IgnoreCase)]
	private static partial Regex GitHubPrUrlRegex();

	private static string NormalizePrForComparison(string pr, string? defaultOwner, string? defaultRepo)
	{
		// Parse PR using the same logic as GitHubPrService.ParsePrUrl
		// Return a normalized format (owner/repo#number) for comparison

		// Trim whitespace first
		pr = pr.Trim();

		// Handle full URL: https://github.com/owner/repo/pull/123
		if (pr.StartsWith("https://github.com/", StringComparison.OrdinalIgnoreCase) ||
			pr.StartsWith("http://github.com/", StringComparison.OrdinalIgnoreCase))
		{
			// Use regex to parse URL more reliably
			var match = GitHubPrUrlRegex().Match(pr);
			if (match.Success && match.Groups.Count >= 4)
			{
				var owner = match.Groups[1].Value.Trim();
				var repo = match.Groups[2].Value.Trim();
				var prPart = match.Groups[3].Value.Trim();
				if (!string.IsNullOrWhiteSpace(owner) && !string.IsNullOrWhiteSpace(repo) &&
					int.TryParse(prPart, out var prNum))
				{
					return $"{owner}/{repo}#{prNum}".ToLowerInvariant();
				}
			}

			// Fallback to URI parsing if regex fails
			try
			{
				var uri = new Uri(pr);
				var segments = uri.Segments;
				// segments[0] is "/", segments[1] is "owner/", segments[2] is "repo/", segments[3] is "pull/", segments[4] is "123"
				if (segments.Length >= 5 && segments[3].Equals("pull/", StringComparison.OrdinalIgnoreCase))
				{
					var owner = segments[1].TrimEnd('/').Trim();
					var repo = segments[2].TrimEnd('/').Trim();
					var prPart = segments[4].TrimEnd('/').Trim();
					if (!string.IsNullOrWhiteSpace(owner) && !string.IsNullOrWhiteSpace(repo) &&
						int.TryParse(prPart, out var prNum))
					{
						return $"{owner}/{repo}#{prNum}".ToLowerInvariant();
					}
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
			var repoPart = pr[..hashIndex].Trim();
			var prPart = pr[(hashIndex + 1)..].Trim();
			if (int.TryParse(prPart, out var prNum))
			{
				var repoParts = repoPart.Split('/');
				if (repoParts.Length == 2)
				{
					var owner = repoParts[0].Trim();
					var repo = repoParts[1].Trim();
					if (!string.IsNullOrWhiteSpace(owner) && !string.IsNullOrWhiteSpace(repo))
					{
						return $"{owner}/{repo}#{prNum}".ToLowerInvariant();
					}
				}
			}
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
			var allResolvedEntries = new List<(ChangelogData entry, string repo, HashSet<string> bundleProductIds)>();
			var allProducts = new HashSet<(string product, string target)>();

			foreach (var (bundledData, bundleInput, bundleDirectory) in bundleDataList)
			{
				// Collect products from this bundle
				var bundleProductIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
				foreach (var product in bundledData.Products)
				{
					var target = product.Target ?? string.Empty;
					_ = allProducts.Add((product.Product, target));
					if (!string.IsNullOrWhiteSpace(product.Product))
					{
						_ = bundleProductIds.Add(product.Product);
					}
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
						allResolvedEntries.Add((entryData, repo, bundleProductIds));
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

			// Load changelog configuration to check for render_blockers
			var config = await LoadChangelogConfiguration(collector, input.Config, ctx);
			if (config == null)
			{
				collector.EmitError(string.Empty, "Failed to load changelog configuration");
				return false;
			}

			// Extract render blockers from configuration
			// RenderBlockers is a Dictionary<string, RenderBlockersEntry> where:
			// - Key can be a single product ID or comma-separated product IDs (e.g., "elasticsearch, cloud-serverless")
			// - Value is a RenderBlockersEntry containing areas and/or types that should be blocked for those products
			var renderBlockers = config.RenderBlockers;

			// Load feature IDs to hide - check if --hide-features contains a file path or a list of feature IDs
			var featureIdsToHide = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			if (input.HideFeatures is { Length: > 0 })
			{
				// If there's exactly one value, check if it's a file path
				if (input.HideFeatures.Length == 1)
				{
					var singleValue = input.HideFeatures[0];

					if (_fileSystem.File.Exists(singleValue))
					{
						// File exists, read feature IDs from it
						var featureIdsFileContent = await _fileSystem.File.ReadAllTextAsync(singleValue, ctx);
						var featureIdsFromFile = featureIdsFileContent
							.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
							.Where(f => !string.IsNullOrWhiteSpace(f))
							.ToArray();

						foreach (var featureId in featureIdsFromFile)
						{
							_ = featureIdsToHide.Add(featureId);
						}
					}
					else
					{
						// Check if it looks like a file path
						var looksLikeFilePath = singleValue.Contains(_fileSystem.Path.DirectorySeparatorChar) ||
							singleValue.Contains(_fileSystem.Path.AltDirectorySeparatorChar) ||
							_fileSystem.Path.HasExtension(singleValue);

						if (looksLikeFilePath)
						{
							// File path doesn't exist
							collector.EmitError(singleValue, $"File does not exist: {singleValue}");
							return false;
						}
						else
						{
							// Doesn't look like a file path, treat as feature ID
							_ = featureIdsToHide.Add(singleValue);
						}
					}
				}
				else
				{
					// Multiple values - process all values first, then check for errors
					var nonExistentFiles = new List<string>();
					foreach (var value in input.HideFeatures)
					{
						if (_fileSystem.File.Exists(value))
						{
							// File exists, read feature IDs from it
							var featureIdsFileContent = await _fileSystem.File.ReadAllTextAsync(value, ctx);
							var featureIdsFromFile = featureIdsFileContent
								.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
								.Where(f => !string.IsNullOrWhiteSpace(f))
								.ToArray();

							foreach (var featureId in featureIdsFromFile)
							{
								_ = featureIdsToHide.Add(featureId);
							}
						}
						else
						{
							// Check if it looks like a file path
							var looksLikeFilePath = value.Contains(_fileSystem.Path.DirectorySeparatorChar) ||
								value.Contains(_fileSystem.Path.AltDirectorySeparatorChar) ||
								_fileSystem.Path.HasExtension(value);

							if (looksLikeFilePath)
							{
								// Track non-existent files to check later
								nonExistentFiles.Add(value);
							}
							else
							{
								// Doesn't look like a file path, treat as feature ID
								_ = featureIdsToHide.Add(value);
							}
						}
					}

					// Report errors for non-existent files
					if (nonExistentFiles.Count > 0)
					{
						foreach (var filePath in nonExistentFiles)
						{
							collector.EmitError(filePath, $"File does not exist: {filePath}");
						}
						return false;
					}
				}
			}

			// Track hidden entries for warnings
			var hiddenEntries = new List<(string title, string featureId)>();
			foreach (var (entry, _, _) in allResolvedEntries)
			{
				if (!string.IsNullOrWhiteSpace(entry.FeatureId) && featureIdsToHide.Contains(entry.FeatureId))
				{
					hiddenEntries.Add((entry.Title ?? "Unknown", entry.FeatureId));
				}
			}

			// Emit warnings for hidden entries
			if (hiddenEntries.Count > 0)
			{
				foreach (var (entryTitle, featureId) in hiddenEntries)
				{
					collector.EmitWarning(string.Empty, $"Changelog entry '{entryTitle}' with feature-id '{featureId}' will be commented out in markdown output");
				}
			}

			// Check entries against render blockers and track blocked entries
			// render_blockers matches against bundle products, not individual entry products
			var blockedEntries = new List<(string title, List<string> reasons)>();
			foreach (var (entry, _, bundleProductIds) in allResolvedEntries)
			{
				var isBlocked = ShouldBlockEntry(entry, bundleProductIds, renderBlockers, out var blockReasons);
				if (isBlocked)
				{
					blockedEntries.Add((entry.Title ?? "Unknown", blockReasons));
				}
			}

			// Emit warnings for blocked entries
			if (blockedEntries.Count > 0)
			{
				foreach (var (entryTitle, reasons) in blockedEntries)
				{
					var reasonsText = string.Join(" and ", reasons);
					collector.EmitWarning(string.Empty, $"Changelog entry '{entryTitle}' will be commented out in markdown output because it matches render_blockers: {reasonsText}");
				}
			}

			// Check for unhandled changelog types
			var handledTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
			{
				ChangelogEntryTypes.Feature,
				ChangelogEntryTypes.Enhancement,
				ChangelogEntryTypes.Security,
				ChangelogEntryTypes.BugFix,
				ChangelogEntryTypes.BreakingChange,
				ChangelogEntryTypes.Deprecation,
				ChangelogEntryTypes.KnownIssue,
				ChangelogEntryTypes.Docs,
				ChangelogEntryTypes.Regression,
				ChangelogEntryTypes.Other
			};

			// config is never null at this point (checked above), and AvailableTypes is initialized in the class
			var availableTypes = config.AvailableTypes;
			var availableTypesSet = new HashSet<string>(availableTypes, StringComparer.OrdinalIgnoreCase);

			foreach (var entryType in entriesByType.Keys.Where(t => availableTypesSet.Contains(t) && !handledTypes.Contains(t)))
			{
				// Only warn if the type is valid according to config but not handled in rendering
				var entryCount = entriesByType[entryType].Count;
				collector.EmitWarning(string.Empty, $"Changelog type '{entryType}' is valid according to configuration but is not handled in rendering output. {entryCount} entry/entries of this type will not be included in the generated markdown files.");
			}

			// Create mapping from entries to their bundle product IDs for render_blockers checking
			// Use a custom comparer for reference equality since entries are objects
			var entryToBundleProducts = new Dictionary<ChangelogData, HashSet<string>>();
			foreach (var (entry, _, bundleProductIds) in allResolvedEntries)
			{
				entryToBundleProducts[entry] = bundleProductIds;
			}

			// Render files (use first repo found, or default)
			var repoForRendering = allResolvedEntries.Count > 0 ? allResolvedEntries[0].repo : defaultRepo;
			var fileType = input.FileType ?? "markdown";

			if (string.Equals(fileType, "asciidoc", StringComparison.OrdinalIgnoreCase))
			{
				// Render asciidoc file
				await RenderAsciidoc(collector, outputDir, title, titleSlug, repoForRendering, allResolvedEntries.Select(e => e.entry).ToList(), entriesByType, input.Subsections, input.HidePrivateLinks, featureIdsToHide, renderBlockers, entryToBundleProducts, ctx);
				_logger.LogInformation("Rendered changelog asciidoc file to {OutputDir}", outputDir);
			}
			else
			{
				// Render markdown files
				await RenderIndexMarkdown(collector, outputDir, title, titleSlug, repoForRendering, allResolvedEntries.Select(e => e.entry).ToList(), entriesByType, input.Subsections, input.HidePrivateLinks, featureIdsToHide, renderBlockers, entryToBundleProducts, ctx);

				// Render breaking-changes.md
				await RenderBreakingChangesMarkdown(collector, outputDir, title, titleSlug, repoForRendering, allResolvedEntries.Select(e => e.entry).ToList(), entriesByType, input.Subsections, input.HidePrivateLinks, featureIdsToHide, renderBlockers, entryToBundleProducts, ctx);

				// Render deprecations.md
				await RenderDeprecationsMarkdown(collector, outputDir, title, titleSlug, repoForRendering, allResolvedEntries.Select(e => e.entry).ToList(), entriesByType, input.Subsections, input.HidePrivateLinks, featureIdsToHide, renderBlockers, entryToBundleProducts, ctx);

				// Render known-issues.md
				await RenderKnownIssuesMarkdown(collector, outputDir, title, titleSlug, repoForRendering, allResolvedEntries.Select(e => e.entry).ToList(), entriesByType, input.Subsections, input.HidePrivateLinks, featureIdsToHide, renderBlockers, entryToBundleProducts, ctx);

				_logger.LogInformation("Rendered changelog markdown files to {OutputDir}", outputDir);
			}

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
		HashSet<string> featureIdsToHide,
		Dictionary<string, RenderBlockersEntry>? renderBlockers,
		Dictionary<ChangelogData, HashSet<string>> entryToBundleProducts,
		Cancel ctx
	)
	{
		var features = entriesByType.GetValueOrDefault(ChangelogEntryTypes.Feature, []);
		var enhancements = entriesByType.GetValueOrDefault(ChangelogEntryTypes.Enhancement, []);
		var security = entriesByType.GetValueOrDefault(ChangelogEntryTypes.Security, []);
		var bugFixes = entriesByType.GetValueOrDefault(ChangelogEntryTypes.BugFix, []);
		var docs = entriesByType.GetValueOrDefault(ChangelogEntryTypes.Docs, []);
		var regressions = entriesByType.GetValueOrDefault(ChangelogEntryTypes.Regression, []);
		var other = entriesByType.GetValueOrDefault(ChangelogEntryTypes.Other, []);

		var hasBreakingChanges = entriesByType.ContainsKey(ChangelogEntryTypes.BreakingChange);
		var hasDeprecations = entriesByType.ContainsKey(ChangelogEntryTypes.Deprecation);
		var hasKnownIssues = entriesByType.ContainsKey(ChangelogEntryTypes.KnownIssue);

		var otherLinks = new List<string>();
		if (hasKnownIssues)
		{
			otherLinks.Add($"[Known issues](/release-notes/known-issues.md#{repo}-{titleSlug}-known-issues)");
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

		var hasAnyEntries = features.Count > 0 || enhancements.Count > 0 || security.Count > 0 || bugFixes.Count > 0 || docs.Count > 0 || regressions.Count > 0 || other.Count > 0;

		if (hasAnyEntries)
		{
			if (features.Count > 0 || enhancements.Count > 0)
			{
				sb.AppendLine(CultureInfo.InvariantCulture, $"### Features and enhancements [{repo}-{titleSlug}-features-enhancements]");
				var combined = features.Concat(enhancements).ToList();
				RenderEntriesByArea(sb, combined, repo, subsections, hidePrivateLinks, featureIdsToHide, renderBlockers, entryToBundleProducts);
			}

			if (security.Count > 0 || bugFixes.Count > 0)
			{
				sb.AppendLine();
				sb.AppendLine(CultureInfo.InvariantCulture, $"### Fixes [{repo}-{titleSlug}-fixes]");
				var combined = security.Concat(bugFixes).ToList();
				RenderEntriesByArea(sb, combined, repo, subsections, hidePrivateLinks, featureIdsToHide, renderBlockers, entryToBundleProducts);
			}

			if (docs.Count > 0)
			{
				sb.AppendLine();
				sb.AppendLine(CultureInfo.InvariantCulture, $"### Documentation [{repo}-{titleSlug}-docs]");
				RenderEntriesByArea(sb, docs, repo, subsections, hidePrivateLinks, featureIdsToHide, renderBlockers, entryToBundleProducts);
			}

			if (regressions.Count > 0)
			{
				sb.AppendLine();
				sb.AppendLine(CultureInfo.InvariantCulture, $"### Regressions [{repo}-{titleSlug}-regressions]");
				RenderEntriesByArea(sb, regressions, repo, subsections, hidePrivateLinks, featureIdsToHide, renderBlockers, entryToBundleProducts);
			}

			if (other.Count > 0)
			{
				sb.AppendLine();
				sb.AppendLine(CultureInfo.InvariantCulture, $"### Other changes [{repo}-{titleSlug}-other]");
				RenderEntriesByArea(sb, other, repo, subsections, hidePrivateLinks, featureIdsToHide, renderBlockers, entryToBundleProducts);
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
		HashSet<string> featureIdsToHide,
		Dictionary<string, RenderBlockersEntry>? renderBlockers,
		Dictionary<ChangelogData, HashSet<string>> entryToBundleProducts,
		Cancel ctx
	)
	{
		var breakingChanges = entriesByType.GetValueOrDefault(ChangelogEntryTypes.BreakingChange, []);

		var sb = new StringBuilder();
		sb.AppendLine(CultureInfo.InvariantCulture, $"## {title} [{repo}-{titleSlug}-breaking-changes]");

		if (breakingChanges.Count > 0)
		{
			// Group by subtype if subsections is enabled, otherwise group by area
			var groupedEntries = subsections
				? breakingChanges.GroupBy(e => string.IsNullOrWhiteSpace(e.Subtype) ? string.Empty : e.Subtype).ToList()
				: breakingChanges.GroupBy(e => GetComponent(e)).ToList();

			foreach (var group in groupedEntries)
			{
				if (subsections && !string.IsNullOrWhiteSpace(group.Key))
				{
					var header = FormatSubtypeHeader(group.Key);
					sb.AppendLine();
					sb.AppendLine(CultureInfo.InvariantCulture, $"**{header}**");
				}

				foreach (var entry in group)
				{
					var bundleProductIds = entryToBundleProducts.GetValueOrDefault(entry, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
					var shouldHide = (!string.IsNullOrWhiteSpace(entry.FeatureId) && featureIdsToHide.Contains(entry.FeatureId)) ||
						ShouldBlockEntry(entry, bundleProductIds, renderBlockers, out _);

					sb.AppendLine();
					if (shouldHide)
					{
						sb.AppendLine("<!--");
					}
					sb.AppendLine(CultureInfo.InvariantCulture, $"::::{{dropdown}} {Beautify(entry.Title)}");
					sb.AppendLine(entry.Description ?? "% Describe the functionality that changed");
					sb.AppendLine();
					var hasPr = !string.IsNullOrWhiteSpace(entry.Pr);
					var hasIssues = entry.Issues != null && entry.Issues.Count > 0;
					if (hasPr || hasIssues)
					{
						if (hidePrivateLinks)
						{
							// When hiding private links, put them on separate lines as comments
							if (hasPr)
							{
								sb.AppendLine(FormatPrLink(entry.Pr!, repo, hidePrivateLinks));
							}
							if (hasIssues)
							{
								foreach (var issue in entry.Issues!)
								{
									sb.AppendLine(FormatIssueLink(issue, repo, hidePrivateLinks));
								}
							}
							sb.AppendLine("For more information, check the pull request or issue above.");
						}
						else
						{
							sb.Append("For more information, check ");
							if (hasPr)
							{
								sb.Append(FormatPrLink(entry.Pr!, repo, hidePrivateLinks));
							}
							if (hasIssues)
							{
								foreach (var issue in entry.Issues!)
								{
									sb.Append(' ');
									sb.Append(FormatIssueLink(issue, repo, hidePrivateLinks));
								}
							}
							sb.AppendLine(".");
						}
						sb.AppendLine();
					}

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
					if (shouldHide)
					{
						sb.AppendLine("-->");
					}
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
		HashSet<string> featureIdsToHide,
		Dictionary<string, RenderBlockersEntry>? renderBlockers,
		Dictionary<ChangelogData, HashSet<string>> entryToBundleProducts,
		Cancel ctx
	)
	{
		var deprecations = entriesByType.GetValueOrDefault(ChangelogEntryTypes.Deprecation, []);

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
					var bundleProductIds = entryToBundleProducts.GetValueOrDefault(entry, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
					var shouldHide = (!string.IsNullOrWhiteSpace(entry.FeatureId) && featureIdsToHide.Contains(entry.FeatureId)) ||
						ShouldBlockEntry(entry, bundleProductIds, renderBlockers, out _);

					sb.AppendLine();
					if (shouldHide)
					{
						sb.AppendLine("<!--");
					}
					sb.AppendLine(CultureInfo.InvariantCulture, $"::::{{dropdown}} {Beautify(entry.Title)}");
					sb.AppendLine(entry.Description ?? "% Describe the functionality that was deprecated");
					sb.AppendLine();
					var hasPr = !string.IsNullOrWhiteSpace(entry.Pr);
					var hasIssues = entry.Issues != null && entry.Issues.Count > 0;
					if (hasPr || hasIssues)
					{
						if (hidePrivateLinks)
						{
							// When hiding private links, put them on separate lines as comments
							if (hasPr)
							{
								sb.AppendLine(FormatPrLink(entry.Pr!, repo, hidePrivateLinks));
							}
							if (hasIssues)
							{
								foreach (var issue in entry.Issues!)
								{
									sb.AppendLine(FormatIssueLink(issue, repo, hidePrivateLinks));
								}
							}
							sb.AppendLine("For more information, check the pull request or issue above.");
						}
						else
						{
							sb.Append("For more information, check ");
							if (hasPr)
							{
								sb.Append(FormatPrLink(entry.Pr!, repo, hidePrivateLinks));
							}
							if (hasIssues)
							{
								foreach (var issue in entry.Issues!)
								{
									sb.Append(' ');
									sb.Append(FormatIssueLink(issue, repo, hidePrivateLinks));
								}
							}
							sb.AppendLine(".");
						}
						sb.AppendLine();
					}

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
					if (shouldHide)
					{
						sb.AppendLine("-->");
					}
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

	[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Parameters match interface pattern")]
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0058:Expression value is never used", Justification = "StringBuilder methods return builder for chaining")]
	private async Task RenderKnownIssuesMarkdown(
		IDiagnosticsCollector collector,
		string outputDir,
		string title,
		string titleSlug,
		string repo,
		List<ChangelogData> entries,
		Dictionary<string, List<ChangelogData>> entriesByType,
		bool subsections,
		bool hidePrivateLinks,
		HashSet<string> featureIdsToHide,
		Dictionary<string, RenderBlockersEntry>? renderBlockers,
		Dictionary<ChangelogData, HashSet<string>> entryToBundleProducts,
		Cancel ctx
	)
	{
		var knownIssues = entriesByType.GetValueOrDefault(ChangelogEntryTypes.KnownIssue, []);

		var sb = new StringBuilder();
		sb.AppendLine(CultureInfo.InvariantCulture, $"## {title} [{repo}-{titleSlug}-known-issues]");

		if (knownIssues.Count > 0)
		{
			var groupedByArea = knownIssues.GroupBy(e => GetComponent(e)).ToList();
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
					var bundleProductIds = entryToBundleProducts.GetValueOrDefault(entry, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
					var shouldHide = (!string.IsNullOrWhiteSpace(entry.FeatureId) && featureIdsToHide.Contains(entry.FeatureId)) ||
						ShouldBlockEntry(entry, bundleProductIds, renderBlockers, out _);

					sb.AppendLine();
					if (shouldHide)
					{
						sb.AppendLine("<!--");
					}
					sb.AppendLine(CultureInfo.InvariantCulture, $"::::{{dropdown}} {Beautify(entry.Title)}");
					sb.AppendLine(entry.Description ?? "% Describe the known issue");
					sb.AppendLine();
					var hasPr = !string.IsNullOrWhiteSpace(entry.Pr);
					var hasIssues = entry.Issues != null && entry.Issues.Count > 0;
					if (hasPr || hasIssues)
					{
						if (hidePrivateLinks)
						{
							// When hiding private links, put them on separate lines as comments
							if (hasPr)
							{
								sb.AppendLine(FormatPrLink(entry.Pr!, repo, hidePrivateLinks));
							}
							if (hasIssues)
							{
								foreach (var issue in entry.Issues!)
								{
									sb.AppendLine(FormatIssueLink(issue, repo, hidePrivateLinks));
								}
							}
							sb.AppendLine("For more information, check the pull request or issue above.");
						}
						else
						{
							sb.Append("For more information, check ");
							if (hasPr)
							{
								sb.Append(FormatPrLink(entry.Pr!, repo, hidePrivateLinks));
							}
							if (hasIssues)
							{
								foreach (var issue in entry.Issues!)
								{
									sb.Append(' ');
									sb.Append(FormatIssueLink(issue, repo, hidePrivateLinks));
								}
							}
							sb.AppendLine(".");
						}
						sb.AppendLine();
					}

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
					if (shouldHide)
					{
						sb.AppendLine("-->");
					}
				}
			}
		}
		else
		{
			sb.AppendLine("_No known issues._");
		}

		var knownIssuesPath = _fileSystem.Path.Combine(outputDir, titleSlug, "known-issues.md");
		var knownIssuesDir = _fileSystem.Path.GetDirectoryName(knownIssuesPath);
		if (!string.IsNullOrWhiteSpace(knownIssuesDir) && !_fileSystem.Directory.Exists(knownIssuesDir))
		{
			_ = _fileSystem.Directory.CreateDirectory(knownIssuesDir);
		}

		await _fileSystem.File.WriteAllTextAsync(knownIssuesPath, sb.ToString(), ctx);
	}

	[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0058:Expression value is never used", Justification = "StringBuilder methods return builder for chaining")]
	private void RenderEntriesByArea(StringBuilder sb, List<ChangelogData> entries, string repo, bool subsections, bool hidePrivateLinks, HashSet<string> featureIdsToHide, Dictionary<string, RenderBlockersEntry>? renderBlockers, Dictionary<ChangelogData, HashSet<string>> entryToBundleProducts)
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
				var bundleProductIds = entryToBundleProducts.GetValueOrDefault(entry, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
				var shouldHide = (!string.IsNullOrWhiteSpace(entry.FeatureId) && featureIdsToHide.Contains(entry.FeatureId)) ||
					ShouldBlockEntry(entry, bundleProductIds, renderBlockers, out _);

				if (shouldHide)
				{
					sb.Append("% ");
				}
				sb.Append("* ");
				sb.Append(Beautify(entry.Title));

				var hasCommentedLinks = false;
				if (hidePrivateLinks)
				{
					// When hiding private links, put them on separate lines as comments with proper indentation
					if (!string.IsNullOrWhiteSpace(entry.Pr))
					{
						sb.AppendLine();
						if (shouldHide)
						{
							sb.Append("% ");
						}
						sb.Append("  ");
						sb.Append(FormatPrLink(entry.Pr, repo, hidePrivateLinks));
						hasCommentedLinks = true;
					}

					if (entry.Issues != null && entry.Issues.Count > 0)
					{
						foreach (var issue in entry.Issues)
						{
							sb.AppendLine();
							if (shouldHide)
							{
								sb.Append("% ");
							}
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
					if (shouldHide)
					{
						// Comment out each line of the description
						var indentedLines = indented.Split('\n');
						foreach (var line in indentedLines)
						{
							sb.Append("% ");
							sb.AppendLine(line);
						}
					}
					else
					{
						sb.AppendLine(indented);
					}
				}
				else
				{
					sb.AppendLine();
				}
			}
		}
	}

	/// <summary>
	/// Checks if an entry should be blocked based on render_blockers configuration.
	/// RenderBlockers is a Dictionary where:
	/// - Key can be a single product ID or comma-separated product IDs (e.g., "elasticsearch, cloud-serverless")
	/// - Value is a RenderBlockersEntry containing areas and/or types that should be blocked for those products
	/// An entry is blocked if ANY product in the bundle matches ANY product key AND (ANY area matches OR ANY type matches).
	/// Note: render_blockers matches against bundle products, not individual entry products.
	/// </summary>
	private static bool ShouldBlockEntry(ChangelogData entry, HashSet<string> bundleProductIds, Dictionary<string, RenderBlockersEntry>? renderBlockers, out List<string> reasons)
	{
		reasons = [];
		if (renderBlockers == null || renderBlockers.Count == 0)
		{
			return false;
		}

		// Bundle must have products to be blocked
		if (bundleProductIds == null || bundleProductIds.Count == 0)
		{
			return false;
		}

		// Extract area values from entry (case-insensitive comparison)
		var entryAreas = entry.Areas != null && entry.Areas.Count > 0
			? entry.Areas
				.Where(a => !string.IsNullOrWhiteSpace(a))
				.Select(a => a!)
				.ToHashSet(StringComparer.OrdinalIgnoreCase)
			: new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		// Extract type from entry (case-insensitive comparison)
		var entryType = !string.IsNullOrWhiteSpace(entry.Type)
			? entry.Type
			: null;

		// Check each render_blockers entry
		foreach (var (productKey, blockersEntry) in renderBlockers)
		{
			if (blockersEntry == null)
			{
				continue;
			}

			// Parse product key - can be comma-separated (e.g., "elasticsearch, cloud-serverless")
			var productKeys = productKey
				.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
				.Where(p => !string.IsNullOrWhiteSpace(p))
				.ToHashSet(StringComparer.OrdinalIgnoreCase);

			// Check if any product in the bundle matches any product in the key
			var matchingProducts = bundleProductIds.Intersect(productKeys, StringComparer.OrdinalIgnoreCase).ToList();
			if (matchingProducts.Count == 0)
			{
				continue;
			}

			var isBlocked = false;
			var blockReasons = new List<string>();

			// Check areas if specified
			if (blockersEntry.Areas != null && blockersEntry.Areas.Count > 0 && entryAreas.Count > 0)
			{
				var matchingAreas = entryAreas.Intersect(blockersEntry.Areas, StringComparer.OrdinalIgnoreCase).ToList();
				if (matchingAreas.Count > 0)
				{
					isBlocked = true;
					var reasonsForProductsAndAreas = matchingProducts
						.SelectMany(product => matchingAreas
							.Select(area => $"product '{product}' with area '{area}'"))
						.Distinct();

					foreach (var reason in reasonsForProductsAndAreas.Where(reason => !blockReasons.Contains(reason)))
					{
						blockReasons.Add(reason);
					}
				}
			}

			// Check types if specified
			if (blockersEntry.Types != null && blockersEntry.Types.Count > 0 && !string.IsNullOrWhiteSpace(entryType))
			{
				var matchingTypes = blockersEntry.Types
					.Where(t => string.Equals(t, entryType, StringComparison.OrdinalIgnoreCase))
					.ToList();
				if (matchingTypes.Count > 0)
				{
					isBlocked = true;
					var reasonsForProducts = matchingProducts
						.SelectMany(product => matchingTypes
							.Select(type => $"product '{product}' with type '{type}'"))
						.Distinct();

					foreach (var reason in reasonsForProducts.Where(reason => !blockReasons.Contains(reason)))
					{
						blockReasons.Add(reason);
					}
				}
			}

			if (isBlocked)
			{
				reasons.AddRange(blockReasons);
				return true;
			}
		}

		return false;
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

	private static string FormatSubtypeHeader(string subtype)
	{
		// Capitalize first letter and replace hyphens with spaces
		if (string.IsNullOrWhiteSpace(subtype))
			return string.Empty;

		var result = subtype.Length < 2
			? char.ToUpperInvariant(subtype[0]).ToString()
			: char.ToUpperInvariant(subtype[0]) + subtype[1..];
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
	private static partial Regex TrailingNumberRegex();

	private static string FormatPrLink(string pr, string repo, bool hidePrivateLinks)
	{
		// Extract PR number
		var match = TrailingNumberRegex().Match(pr);
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
		var match = TrailingNumberRegex().Match(issue);
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

	private static string FormatPrLinkAsciidoc(string pr, string repo, bool hidePrivateLinks)
	{
		// Extract PR number
		var match = TrailingNumberRegex().Match(pr);
		var prNumber = match.Success ? match.Value : pr;

		// Format as asciidoc link attribute reference
		// Format: {repo-pull}PRNUMBER[#PRNUMBER]
		// Convert repo name to attribute format (e.g., "elastic-agent" -> "agent-pull", "fleet-server" -> "fleet-server-pull")
		var attributeName = ConvertRepoToAttributeName(repo, "pull");
		var link = $"{{{attributeName}}}{prNumber}[#{prNumber}]";

		// Comment out link if hiding private links
		if (hidePrivateLinks)
		{
			return $"// {link}";
		}

		return link;
	}

	private static string FormatIssueLinkAsciidoc(string issue, string repo, bool hidePrivateLinks)
	{
		// Extract issue number
		var match = TrailingNumberRegex().Match(issue);
		var issueNumber = match.Success ? match.Value : issue;

		// Format as asciidoc link attribute reference
		// Format: {repo-issue}ISSUENUMBER[#ISSUENUMBER]
		// Convert repo name to attribute format (e.g., "elastic-agent" -> "agent-issue", "fleet-server" -> "fleet-server-issue")
		var attributeName = ConvertRepoToAttributeName(repo, "issue");
		var link = $"{{{attributeName}}}{issueNumber}[#{issueNumber}]";

		// Comment out link if hiding private links
		if (hidePrivateLinks)
		{
			return $"// {link}";
		}

		return link;
	}

	private static string ConvertRepoToAttributeName(string repo, string suffix)
	{
		// Convert repo name to attribute format
		// Examples:
		// "elastic-agent" -> "agent-pull"
		// "fleet-server" -> "fleet-server-pull"
		// "elastic-agent-libs" -> "agent-libs-pull"
		// "elasticsearch" -> "es-pull"
		// "kibana" -> "kibana-pull"

		if (string.IsNullOrWhiteSpace(repo))
		{
			return $"repo-{suffix}";
		}

		// Handle common repo name patterns
		if (repo.Equals("elasticsearch", StringComparison.OrdinalIgnoreCase))
		{
			return $"es-{suffix}";
		}

		// Remove "elastic-" prefix if present
		var normalized = repo;
		if (normalized.StartsWith("elastic-", StringComparison.OrdinalIgnoreCase))
		{
			normalized = normalized.Substring("elastic-".Length);
		}

		// Return normalized name with suffix
		return $"{normalized}-{suffix}";
	}

	[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Parameters match interface pattern")]
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0058:Expression value is never used", Justification = "StringBuilder methods return builder for chaining")]
	private async Task RenderAsciidoc(
		IDiagnosticsCollector collector,
		string outputDir,
		string title,
		string titleSlug,
		string repo,
		List<ChangelogData> entries,
		Dictionary<string, List<ChangelogData>> entriesByType,
		bool subsections,
		bool hidePrivateLinks,
		HashSet<string> featureIdsToHide,
		Dictionary<string, RenderBlockersEntry>? renderBlockers,
		Dictionary<ChangelogData, HashSet<string>> entryToBundleProducts,
		Cancel ctx
	)
	{
		var sb = new StringBuilder();

		// Add anchor
		sb.AppendLine(CultureInfo.InvariantCulture, $"[[release-notes-{titleSlug}]]");
		sb.AppendLine(CultureInfo.InvariantCulture, $"== {title}");
		sb.AppendLine();

		// Group entries by type
		var security = entriesByType.GetValueOrDefault(ChangelogEntryTypes.Security, []);
		var bugFixes = entriesByType.GetValueOrDefault(ChangelogEntryTypes.BugFix, []);
		var features = entriesByType.GetValueOrDefault(ChangelogEntryTypes.Feature, []);
		var enhancements = entriesByType.GetValueOrDefault(ChangelogEntryTypes.Enhancement, []);
		var breakingChanges = entriesByType.GetValueOrDefault(ChangelogEntryTypes.BreakingChange, []);
		var deprecations = entriesByType.GetValueOrDefault(ChangelogEntryTypes.Deprecation, []);
		var knownIssues = entriesByType.GetValueOrDefault(ChangelogEntryTypes.KnownIssue, []);
		var docs = entriesByType.GetValueOrDefault(ChangelogEntryTypes.Docs, []);
		var regressions = entriesByType.GetValueOrDefault(ChangelogEntryTypes.Regression, []);
		var other = entriesByType.GetValueOrDefault(ChangelogEntryTypes.Other, []);

		// Render security updates
		if (security.Count > 0)
		{
			sb.AppendLine(CultureInfo.InvariantCulture, $"[[security-updates-{titleSlug}]]");
			sb.AppendLine("[float]");
			sb.AppendLine("=== Security updates");
			sb.AppendLine();
			RenderEntriesByAreaAsciidoc(sb, security, repo, subsections, hidePrivateLinks, featureIdsToHide, renderBlockers, entryToBundleProducts);
			sb.AppendLine();
		}

		// Render bug fixes
		if (bugFixes.Count > 0)
		{
			sb.AppendLine(CultureInfo.InvariantCulture, $"[[bug-fixes-{titleSlug}]]");
			sb.AppendLine("[float]");
			sb.AppendLine("=== Bug fixes");
			sb.AppendLine();
			RenderEntriesByAreaAsciidoc(sb, bugFixes, repo, subsections, hidePrivateLinks, featureIdsToHide, renderBlockers, entryToBundleProducts);
			sb.AppendLine();
		}

		// Render features and enhancements
		if (features.Count > 0 || enhancements.Count > 0)
		{
			sb.AppendLine(CultureInfo.InvariantCulture, $"[[features-enhancements-{titleSlug}]]");
			sb.AppendLine("[float]");
			sb.AppendLine("=== New features and enhancements");
			sb.AppendLine();
			var combined = features.Concat(enhancements).ToList();
			RenderEntriesByAreaAsciidoc(sb, combined, repo, subsections, hidePrivateLinks, featureIdsToHide, renderBlockers, entryToBundleProducts);
			sb.AppendLine();
		}

		// Render breaking changes
		if (breakingChanges.Count > 0)
		{
			sb.AppendLine(CultureInfo.InvariantCulture, $"[[breaking-changes-{titleSlug}]]");
			sb.AppendLine("[float]");
			sb.AppendLine("=== Breaking changes");
			sb.AppendLine();
			RenderBreakingChangesAsciidoc(sb, breakingChanges, repo, subsections, hidePrivateLinks, featureIdsToHide, renderBlockers, entryToBundleProducts);
			sb.AppendLine();
		}

		// Render deprecations
		if (deprecations.Count > 0)
		{
			sb.AppendLine(CultureInfo.InvariantCulture, $"[[deprecations-{titleSlug}]]");
			sb.AppendLine("[float]");
			sb.AppendLine("=== Deprecations");
			sb.AppendLine();
			RenderDeprecationsAsciidoc(sb, deprecations, repo, subsections, hidePrivateLinks, featureIdsToHide, renderBlockers, entryToBundleProducts);
			sb.AppendLine();
		}

		// Render known issues
		if (knownIssues.Count > 0)
		{
			sb.AppendLine(CultureInfo.InvariantCulture, $"[[known-issues-{titleSlug}]]");
			sb.AppendLine("[float]");
			sb.AppendLine("=== Known issues");
			sb.AppendLine();
			RenderKnownIssuesAsciidoc(sb, knownIssues, repo, subsections, hidePrivateLinks, featureIdsToHide, renderBlockers, entryToBundleProducts);
			sb.AppendLine();
		}

		// Render documentation changes
		if (docs.Count > 0)
		{
			sb.AppendLine(CultureInfo.InvariantCulture, $"[[docs-{titleSlug}]]");
			sb.AppendLine("[float]");
			sb.AppendLine("=== Documentation");
			sb.AppendLine();
			RenderEntriesByAreaAsciidoc(sb, docs, repo, subsections, hidePrivateLinks, featureIdsToHide, renderBlockers, entryToBundleProducts);
			sb.AppendLine();
		}

		// Render regressions
		if (regressions.Count > 0)
		{
			sb.AppendLine(CultureInfo.InvariantCulture, $"[[regressions-{titleSlug}]]");
			sb.AppendLine("[float]");
			sb.AppendLine("=== Regressions");
			sb.AppendLine();
			RenderEntriesByAreaAsciidoc(sb, regressions, repo, subsections, hidePrivateLinks, featureIdsToHide, renderBlockers, entryToBundleProducts);
			sb.AppendLine();
		}

		// Render other changes
		if (other.Count > 0)
		{
			sb.AppendLine(CultureInfo.InvariantCulture, $"[[other-{titleSlug}]]");
			sb.AppendLine("[float]");
			sb.AppendLine("=== Other changes");
			sb.AppendLine();
			RenderEntriesByAreaAsciidoc(sb, other, repo, subsections, hidePrivateLinks, featureIdsToHide, renderBlockers, entryToBundleProducts);
			sb.AppendLine();
		}

		// Write the asciidoc file
		var asciidocPath = _fileSystem.Path.Combine(outputDir, $"{titleSlug}.asciidoc");
		var asciidocDir = _fileSystem.Path.GetDirectoryName(asciidocPath);
		if (!string.IsNullOrWhiteSpace(asciidocDir) && !_fileSystem.Directory.Exists(asciidocDir))
		{
			_ = _fileSystem.Directory.CreateDirectory(asciidocDir);
		}

		await _fileSystem.File.WriteAllTextAsync(asciidocPath, sb.ToString(), ctx);
	}

	[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0058:Expression value is never used", Justification = "StringBuilder methods return builder for chaining")]
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Parameter matches interface pattern for consistency")]
	private void RenderEntriesByAreaAsciidoc(StringBuilder sb, List<ChangelogData> entries, string repo, bool subsections, bool hidePrivateLinks, HashSet<string> featureIdsToHide, Dictionary<string, RenderBlockersEntry>? renderBlockers, Dictionary<ChangelogData, HashSet<string>> entryToBundleProducts)
	{
		var groupedByArea = entries.GroupBy(e => GetComponent(e)).ToList();
		foreach (var areaGroup in groupedByArea)
		{
			var componentName = !string.IsNullOrWhiteSpace(areaGroup.Key) ? areaGroup.Key : "General";

			// Format component name (capitalize first letter, replace hyphens with spaces)
			var formattedComponent = FormatAreaHeader(componentName);

			sb.AppendLine(CultureInfo.InvariantCulture, $"{formattedComponent}::");
			sb.AppendLine();

			foreach (var entry in areaGroup)
			{
				var bundleProductIds = entryToBundleProducts.GetValueOrDefault(entry, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
				var shouldHide = (!string.IsNullOrWhiteSpace(entry.FeatureId) && featureIdsToHide.Contains(entry.FeatureId)) ||
					ShouldBlockEntry(entry, bundleProductIds, renderBlockers, out _);

				if (shouldHide)
				{
					sb.AppendLine("// ");
				}

				sb.Append("* ");
				sb.Append(Beautify(entry.Title));

				var hasPr = !string.IsNullOrWhiteSpace(entry.Pr);
				var hasIssues = entry.Issues != null && entry.Issues.Count > 0;

				if (hasPr || hasIssues)
				{
					sb.Append(' ');
					if (hasPr)
					{
						sb.Append(FormatPrLinkAsciidoc(entry.Pr!, repo, hidePrivateLinks));
						sb.Append(' ');
					}
					if (hasIssues)
					{
						foreach (var issue in entry.Issues!)
						{
							sb.Append(FormatIssueLinkAsciidoc(issue, repo, hidePrivateLinks));
							sb.Append(' ');
						}
					}
				}

				if (!string.IsNullOrWhiteSpace(entry.Description))
				{
					sb.AppendLine();
					var indented = Indent(entry.Description);
					if (shouldHide)
					{
						var indentedLines = indented.Split('\n');
						foreach (var line in indentedLines)
						{
							sb.AppendLine(CultureInfo.InvariantCulture, $"// {line}");
						}
					}
					else
					{
						sb.AppendLine(indented);
					}
				}

				sb.AppendLine();
			}
		}
	}

	[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0058:Expression value is never used", Justification = "StringBuilder methods return builder for chaining")]
	private void RenderBreakingChangesAsciidoc(StringBuilder sb, List<ChangelogData> breakingChanges, string repo, bool subsections, bool hidePrivateLinks, HashSet<string> featureIdsToHide, Dictionary<string, RenderBlockersEntry>? renderBlockers, Dictionary<ChangelogData, HashSet<string>> entryToBundleProducts)
	{
		// Group by subtype if subsections is enabled, otherwise group by area
		var groupedEntries = subsections
			? breakingChanges.GroupBy(e => string.IsNullOrWhiteSpace(e.Subtype) ? string.Empty : e.Subtype).ToList()
			: breakingChanges.GroupBy(e => GetComponent(e)).ToList();

		foreach (var group in groupedEntries)
		{
			if (subsections && !string.IsNullOrWhiteSpace(group.Key))
			{
				var header = FormatSubtypeHeader(group.Key);
				sb.AppendLine(CultureInfo.InvariantCulture, $"**{header}**");
				sb.AppendLine();
			}

			foreach (var entry in group)
			{
				var bundleProductIds = entryToBundleProducts.GetValueOrDefault(entry, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
				var shouldHide = (!string.IsNullOrWhiteSpace(entry.FeatureId) && featureIdsToHide.Contains(entry.FeatureId)) ||
					ShouldBlockEntry(entry, bundleProductIds, renderBlockers, out _);

				if (shouldHide)
				{
					sb.AppendLine("// ");
				}

				sb.Append("* ");
				sb.Append(Beautify(entry.Title));

				var hasPr = !string.IsNullOrWhiteSpace(entry.Pr);
				var hasIssues = entry.Issues != null && entry.Issues.Count > 0;

				if (hasPr || hasIssues)
				{
					sb.Append(' ');
					if (hasPr)
					{
						sb.Append(FormatPrLinkAsciidoc(entry.Pr!, repo, hidePrivateLinks));
						sb.Append(' ');
					}
					if (hasIssues)
					{
						foreach (var issue in entry.Issues!)
						{
							sb.Append(FormatIssueLinkAsciidoc(issue, repo, hidePrivateLinks));
							sb.Append(' ');
						}
					}
				}

				if (!string.IsNullOrWhiteSpace(entry.Description))
				{
					sb.AppendLine();
					var indented = Indent(entry.Description);
					if (shouldHide)
					{
						var indentedLines = indented.Split('\n');
						foreach (var line in indentedLines)
						{
							sb.AppendLine(CultureInfo.InvariantCulture, $"// {line}");
						}
					}
					else
					{
						sb.AppendLine(indented);
					}
				}

				if (!string.IsNullOrWhiteSpace(entry.Impact))
				{
					sb.AppendLine();
					sb.AppendLine(CultureInfo.InvariantCulture, $"**Impact:** {entry.Impact}");
				}

				if (!string.IsNullOrWhiteSpace(entry.Action))
				{
					sb.AppendLine();
					sb.AppendLine(CultureInfo.InvariantCulture, $"**Action:** {entry.Action}");
				}

				sb.AppendLine();
			}
		}
	}

	[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0058:Expression value is never used", Justification = "StringBuilder methods return builder for chaining")]
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Parameter matches interface pattern for consistency")]
	private void RenderDeprecationsAsciidoc(StringBuilder sb, List<ChangelogData> deprecations, string repo, bool subsections, bool hidePrivateLinks, HashSet<string> featureIdsToHide, Dictionary<string, RenderBlockersEntry>? renderBlockers, Dictionary<ChangelogData, HashSet<string>> entryToBundleProducts)
	{
		var groupedByArea = deprecations.GroupBy(e => GetComponent(e)).ToList();
		foreach (var areaGroup in groupedByArea)
		{
			var componentName = !string.IsNullOrWhiteSpace(areaGroup.Key) ? areaGroup.Key : "General";
			var formattedComponent = FormatAreaHeader(componentName);

			sb.AppendLine(CultureInfo.InvariantCulture, $"{formattedComponent}::");
			sb.AppendLine();

			foreach (var entry in areaGroup)
			{
				var bundleProductIds = entryToBundleProducts.GetValueOrDefault(entry, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
				var shouldHide = (!string.IsNullOrWhiteSpace(entry.FeatureId) && featureIdsToHide.Contains(entry.FeatureId)) ||
					ShouldBlockEntry(entry, bundleProductIds, renderBlockers, out _);

				if (shouldHide)
				{
					sb.AppendLine("// ");
				}

				sb.Append("* ");
				sb.Append(Beautify(entry.Title));

				var hasPr = !string.IsNullOrWhiteSpace(entry.Pr);
				var hasIssues = entry.Issues != null && entry.Issues.Count > 0;

				if (hasPr || hasIssues)
				{
					sb.Append(' ');
					if (hasPr)
					{
						sb.Append(FormatPrLinkAsciidoc(entry.Pr!, repo, hidePrivateLinks));
						sb.Append(' ');
					}
					if (hasIssues)
					{
						foreach (var issue in entry.Issues!)
						{
							sb.Append(FormatIssueLinkAsciidoc(issue, repo, hidePrivateLinks));
							sb.Append(' ');
						}
					}
				}

				if (!string.IsNullOrWhiteSpace(entry.Description))
				{
					sb.AppendLine();
					var indented = Indent(entry.Description);
					if (shouldHide)
					{
						var indentedLines = indented.Split('\n');
						foreach (var line in indentedLines)
						{
							sb.AppendLine(CultureInfo.InvariantCulture, $"// {line}");
						}
					}
					else
					{
						sb.AppendLine(indented);
					}
				}

				if (!string.IsNullOrWhiteSpace(entry.Impact))
				{
					sb.AppendLine();
					sb.AppendLine(CultureInfo.InvariantCulture, $"**Impact:** {entry.Impact}");
				}

				if (!string.IsNullOrWhiteSpace(entry.Action))
				{
					sb.AppendLine();
					sb.AppendLine(CultureInfo.InvariantCulture, $"**Action:** {entry.Action}");
				}

				sb.AppendLine();
			}
		}
	}

	[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0058:Expression value is never used", Justification = "StringBuilder methods return builder for chaining")]
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Parameter matches interface pattern for consistency")]
	private void RenderKnownIssuesAsciidoc(StringBuilder sb, List<ChangelogData> knownIssues, string repo, bool subsections, bool hidePrivateLinks, HashSet<string> featureIdsToHide, Dictionary<string, RenderBlockersEntry>? renderBlockers, Dictionary<ChangelogData, HashSet<string>> entryToBundleProducts)
	{
		var groupedByArea = knownIssues.GroupBy(e => GetComponent(e)).ToList();
		foreach (var areaGroup in groupedByArea)
		{
			var componentName = !string.IsNullOrWhiteSpace(areaGroup.Key) ? areaGroup.Key : "General";
			var formattedComponent = FormatAreaHeader(componentName);

			sb.AppendLine(CultureInfo.InvariantCulture, $"{formattedComponent}::");
			sb.AppendLine();

			foreach (var entry in areaGroup)
			{
				var bundleProductIds = entryToBundleProducts.GetValueOrDefault(entry, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
				var shouldHide = (!string.IsNullOrWhiteSpace(entry.FeatureId) && featureIdsToHide.Contains(entry.FeatureId)) ||
					ShouldBlockEntry(entry, bundleProductIds, renderBlockers, out _);

				if (shouldHide)
				{
					sb.AppendLine("// ");
				}

				sb.Append("* ");
				sb.Append(Beautify(entry.Title));

				var hasPr = !string.IsNullOrWhiteSpace(entry.Pr);
				var hasIssues = entry.Issues != null && entry.Issues.Count > 0;

				if (hasPr || hasIssues)
				{
					sb.Append(' ');
					if (hasPr)
					{
						sb.Append(FormatPrLinkAsciidoc(entry.Pr!, repo, hidePrivateLinks));
						sb.Append(' ');
					}
					if (hasIssues)
					{
						foreach (var issue in entry.Issues!)
						{
							sb.Append(FormatIssueLinkAsciidoc(issue, repo, hidePrivateLinks));
							sb.Append(' ');
						}
					}
				}

				if (!string.IsNullOrWhiteSpace(entry.Description))
				{
					sb.AppendLine();
					var indented = Indent(entry.Description);
					if (shouldHide)
					{
						var indentedLines = indented.Split('\n');
						foreach (var line in indentedLines)
						{
							sb.AppendLine(CultureInfo.InvariantCulture, $"// {line}");
						}
					}
					else
					{
						sb.AppendLine(indented);
					}
				}

				if (!string.IsNullOrWhiteSpace(entry.Impact))
				{
					sb.AppendLine();
					sb.AppendLine(CultureInfo.InvariantCulture, $"**Impact:** {entry.Impact}");
				}

				if (!string.IsNullOrWhiteSpace(entry.Action))
				{
					sb.AppendLine();
					sb.AppendLine(CultureInfo.InvariantCulture, $"**Action:** {entry.Action}");
				}

				sb.AppendLine();
			}
		}
	}
}

