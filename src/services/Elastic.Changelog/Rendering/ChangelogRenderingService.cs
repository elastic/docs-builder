// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Changelog.Bundling;
using Elastic.Changelog.Configuration;
using Elastic.Changelog.Rendering.Asciidoc;
using Elastic.Changelog.Rendering.Markdown;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Services;
using Microsoft.Extensions.Logging;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Elastic.Changelog.Rendering;

/// <summary>
/// Service for rendering changelog output (markdown or asciidoc)
/// </summary>
public class ChangelogRenderingService(
	ILoggerFactory logFactory,
	IConfigurationContext? configurationContext = null,
	IFileSystem? fileSystem = null
) : IService
{
	private readonly ILogger _logger = logFactory.CreateLogger<ChangelogRenderingService>();
	private readonly IFileSystem _fileSystem = fileSystem ?? new FileSystem();

	public async Task<bool> RenderChangelogs(
		IDiagnosticsCollector collector,
		ChangelogRenderInput input,
		Cancel ctx
	)
	{
		try
		{
			// Validate input
			if (input.Bundles.Count == 0)
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

				// Determine directory for resolving file references
				var bundleDirectory = bundleInput.Directory ?? _fileSystem.Path.GetDirectoryName(bundleInput.BundleFile) ?? _fileSystem.Directory.GetCurrentDirectory();

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
							collector.EmitWarning(bundleInput.BundleFile, $"Changelog file '{fileName}' appears multiple times in the same bundle");

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
							var normalizedPr = ChangelogBundlingService.NormalizePrForComparison(entry.Pr, null, null);
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
							var checksum = ChangelogBundlingService.ComputeSha1(fileContent);
							if (checksum != entry.File.Checksum)
								collector.EmitWarning(bundleInput.BundleFile, $"Checksum mismatch for file {entry.File.Name}. Expected {entry.File.Checksum}, got {checksum}");

							// Deserialize YAML (skip comment lines) to validate structure
							var yamlLines = fileContent.Split('\n');
							var yamlWithoutComments = string.Join('\n', yamlLines.Where(line => !line.TrimStart().StartsWith('#')));

							// Normalize "version:" to "target:" in products section
							var normalizedYaml = ChangelogBundlingService.VersionToTargetRegex().Replace(yamlWithoutComments, "$1target:");

							var entryData = deserializer.Deserialize<ChangelogData>(normalizedYaml);

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

							if (entryData.Products.Count == 0)
							{
								collector.EmitError(filePath, "Changelog file is missing required field: products");
								return false;
							}

							// Track PRs for duplicate detection
							if (!string.IsNullOrWhiteSpace(entryData.Pr))
							{
								var normalizedPr = ChangelogBundlingService.NormalizePrForComparison(entryData.Pr, null, null);
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
					collector.EmitWarning(string.Empty, $"Changelog file '{fileName}' appears in multiple bundles: {string.Join(", ", uniqueBundles)}");
			}

			// Check for duplicate PRs
			foreach (var (pr, bundleFiles) in seenPrs.Where(kvp => kvp.Value.Count > 1))
			{
				var uniqueBundles = bundleFiles.Distinct().ToList();
				if (uniqueBundles.Count > 1)
					collector.EmitWarning(string.Empty, $"PR '{pr}' appears in multiple bundles: {string.Join(", ", uniqueBundles)}");
			}

			// If validation found errors, stop before merging
			if (collector.Errors > 0)
				return false;

			// Merge phase: Now that validation passed, load and merge all bundles
			var allResolvedEntries = new List<(ChangelogData entry, string repo, HashSet<string> bundleProductIds, bool hideLinks)>();
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
						_ = bundleProductIds.Add(product.Product);
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
						var filePath = _fileSystem.Path.Combine(bundleDirectory, entry.File!.Name);
						var fileContent = await _fileSystem.File.ReadAllTextAsync(filePath, ctx);

						// Deserialize YAML (skip comment lines)
						var yamlLines = fileContent.Split('\n');
						var yamlWithoutComments = string.Join('\n', yamlLines.Where(line => !line.TrimStart().StartsWith('#')));

						// Normalize "version:" to "target:" in products section
						var normalizedYaml = ChangelogBundlingService.VersionToTargetRegex().Replace(yamlWithoutComments, "$1target:");

						entryData = deserializer.Deserialize<ChangelogData>(normalizedYaml);
					}

					allResolvedEntries.Add((entryData, repo, bundleProductIds, bundleInput.HideLinks));
				}
			}

			if (allResolvedEntries.Count == 0)
			{
				collector.EmitError(string.Empty, "No changelog entries to render");
				return false;
			}

			// Determine output directory
			var outputDir = input.Output ?? _fileSystem.Directory.GetCurrentDirectory();
			if (!_fileSystem.Directory.Exists(outputDir))
				_ = _fileSystem.Directory.CreateDirectory(outputDir);

			// Extract version from products (use first product's target if available, or "unknown")
			var version = allProducts.Count > 0
				? allProducts.OrderBy(p => p.product).ThenBy(p => p.target).First().target
				: "unknown";

			if (string.IsNullOrWhiteSpace(version))
				version = "unknown";

			// Warn if --title was not provided and version defaults to "unknown"
			if (string.IsNullOrWhiteSpace(input.Title) && version == "unknown")
				collector.EmitWarning(string.Empty, "No --title option provided and bundle files do not contain 'target' values. Output folder and markdown titles will default to 'unknown'. Consider using --title to specify a custom title.");

			// Group entries by type (kind)
			var entriesByType = allResolvedEntries.Select(e => e.entry).GroupBy(e => e.Type).ToDictionary(g => g.Key, g => g.ToList());

			// Use title from input or default to version
			var title = input.Title ?? version;
			// Convert title to slug format for folder names and anchors (lowercase, dashes instead of spaces)
			var titleSlug = ChangelogTextUtilities.TitleToSlug(title);

			// Load changelog configuration to check for render_blockers
			var configLoader = new ChangelogConfigurationLoader(logFactory, configurationContext!, _fileSystem);
			var config = await configLoader.LoadChangelogConfiguration(collector, input.Config, ctx);
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
							_ = featureIdsToHide.Add(featureId);
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

						// Doesn't look like a file path, treat as feature ID
						_ = featureIdsToHide.Add(singleValue);
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
								_ = featureIdsToHide.Add(featureId);
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
							collector.EmitError(filePath, $"File does not exist: {filePath}");
						return false;
					}
				}
			}

			// Track hidden entries for warnings
			var hiddenEntries = new List<(string title, string featureId)>();
			foreach (var (entry, _, _, _) in allResolvedEntries)
			{
				if (!string.IsNullOrWhiteSpace(entry.FeatureId) && featureIdsToHide.Contains(entry.FeatureId))
					hiddenEntries.Add((entry.Title, entry.FeatureId));
			}

			// Emit warnings for hidden entries
			if (hiddenEntries.Count > 0)
			{
				foreach (var (entryTitle, featureId) in hiddenEntries)
					collector.EmitWarning(string.Empty, $"Changelog entry '{entryTitle}' with feature-id '{featureId}' will be commented out in markdown output");
			}

			// Check entries against render blockers and track blocked entries
			// render_blockers matches against bundle products, not individual entry products
			var blockedEntries = new List<(string title, List<string> reasons)>();
			foreach (var (entry, _, bundleProductIds, _) in allResolvedEntries)
			{
				var isBlocked = ChangelogRenderUtilities.ShouldBlockEntry(entry, bundleProductIds, renderBlockers, out var blockReasons);
				if (isBlocked)
					blockedEntries.Add((entry.Title, blockReasons));
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
			foreach (var (entry, _, bundleProductIds, _) in allResolvedEntries)
				entryToBundleProducts[entry] = bundleProductIds;

			// Create mapping from entries to their repo for PR link formatting
			var entryToRepo = new Dictionary<ChangelogData, string>();
			foreach (var (entry, repo, _, _) in allResolvedEntries)
				entryToRepo[entry] = repo;

			// Create mapping from entries to their hideLinks setting for per-bundle link visibility
			var entryToHideLinks = new Dictionary<ChangelogData, bool>();
			foreach (var (entry, _, _, hideLinks) in allResolvedEntries)
				entryToHideLinks[entry] = hideLinks;

			// Render files (use first repo found for section anchors, or default)
			var repoForAnchors = allResolvedEntries.Count > 0 ? allResolvedEntries[0].repo : defaultRepo;

			// Create shared render context
			var context = new ChangelogRenderContext
			{
				OutputDir = outputDir,
				Title = title,
				TitleSlug = titleSlug,
				Repo = repoForAnchors,
				EntriesByType = entriesByType,
				Subsections = input.Subsections,
				FeatureIdsToHide = featureIdsToHide,
				RenderBlockers = renderBlockers,
				EntryToBundleProducts = entryToBundleProducts,
				EntryToRepo = entryToRepo,
				EntryToHideLinks = entryToHideLinks
			};

			switch (input.FileType)
			{
				case ChangelogFileType.Asciidoc:
					// Render asciidoc file
					var asciidocRenderer = new ChangelogAsciidocRenderer(_fileSystem);
					await asciidocRenderer.RenderAsciidoc(context, allResolvedEntries.Select(e => e.entry).ToList(), ctx);
					_logger.LogInformation("Rendered changelog asciidoc file to {OutputDir}", outputDir);
					break;
				case ChangelogFileType.Markdown:
					// Render markdown files using specialized renderers
					IChangelogMarkdownRenderer[] renderers =
					[
						new IndexMarkdownRenderer(_fileSystem),
						new BreakingChangesMarkdownRenderer(_fileSystem),
						new DeprecationsMarkdownRenderer(_fileSystem),
						new KnownIssuesMarkdownRenderer(_fileSystem)
					];

					foreach (var renderer in renderers)
						await renderer.RenderAsync(context, ctx);

					_logger.LogInformation("Rendered changelog markdown files to {OutputDir}", outputDir);
					break;
				default:
					throw new Exception($"Unknown changelog file type: {input.FileType}");
			}

			return true;
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
}
