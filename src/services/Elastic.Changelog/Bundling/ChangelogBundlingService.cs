// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Elastic.Changelog;
using Elastic.Changelog.Configuration;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Services;
using Microsoft.Extensions.Logging;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Elastic.Changelog.Bundling;

/// <summary>
/// Service for bundling changelog files
/// </summary>
public partial class ChangelogBundlingService(
	ILoggerFactory logFactory,
	IFileSystem? fileSystem = null
) : IService
{
	private readonly ILogger _logger = logFactory.CreateLogger<ChangelogBundlingService>();
	private readonly IFileSystem _fileSystem = fileSystem ?? new FileSystem();

	[GeneratedRegex(@"(\s+)version:", RegexOptions.Multiline)]
	internal static partial Regex VersionToTargetRegex();

	[GeneratedRegex(@"github\.com/([^/]+)/([^/]+)/pull/(\d+)", RegexOptions.IgnoreCase)]
	internal static partial Regex GitHubPrUrlRegex();

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
			var specifiedFilters = new List<string>();
			if (input.All)
				specifiedFilters.Add("--all");
			if (input.InputProducts is { Count: > 0 })
				specifiedFilters.Add("--input-products");
			if (input.Prs is { Length: > 0 })
				specifiedFilters.Add("--prs");

			if (specifiedFilters.Count == 0)
			{
				collector.EmitError(string.Empty, "At least one filter option must be specified: --all, --input-products, or --prs");
				return false;
			}

			if (specifiedFilters.Count > 1)
			{
				collector.EmitError(string.Empty, $"Multiple filter options cannot be specified together. You specified: {string.Join(", ", specifiedFilters)}. Please use only one filter option: --all, --input-products, or --prs");
				return false;
			}

			// Build product filter patterns (with wildcard support)
			var productFilters = new List<(string? productPattern, string? targetPattern, string? lifecyclePattern)>();
			if (input.InputProducts is { Count: > 0 })
			{
				foreach (var product in input.InputProducts)
				{
					productFilters.Add((
						product.Product == "*" ? null : product.Product,
						product.Target == "*" ? null : product.Target,
						product.Lifecycle == "*" ? null : product.Lifecycle
					));
				}
			}

			// Load PRs - check if --prs contains a file path or a list of PRs
			var prsToMatch = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			var nonExistentFiles = new List<string>();
			if (input.Prs is { Length: > 0 })
			{
				// If there's exactly one value, check if it's a file path
				if (input.Prs.Length == 1)
				{
					var singleValue = input.Prs[0];

					// Check if it's a URL - URLs should always be treated as PRs, not file paths
					var isUrl = singleValue.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
						singleValue.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

					if (!isUrl && _fileSystem.File.Exists(singleValue))
					{
						// File exists, read PRs from it
						var fileContent = await _fileSystem.File.ReadAllTextAsync(singleValue, ctx);
						var prsFromFile = fileContent
							.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
							.Where(p => !string.IsNullOrWhiteSpace(p))
							.ToArray();

						foreach (var pr in prsFromFile)
						{
							_ = prsToMatch.Add(pr);
						}
					}
					else if (!isUrl)
					{
						// Check for short PR format (owner/repo#number) first
						var isShortPrFormat = false;
						var hashIndex = singleValue.LastIndexOf('#');
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
								// Doesn't look like a file path, treat as PR identifier
								_ = prsToMatch.Add(singleValue);
							}
						}
					}
					else
					{
						// URL, treat as PR identifier
						_ = prsToMatch.Add(singleValue);
					}
				}
				else
				{
					// Multiple values - process all values first, then check for errors
					foreach (var value in input.Prs)
					{
						var isUrl = value.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
							value.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

						if (!isUrl && _fileSystem.File.Exists(value))
						{
							// File exists, read PRs from it
							var fileContent = await _fileSystem.File.ReadAllTextAsync(value, ctx);
							var prsFromFile = fileContent
								.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
								.Where(p => !string.IsNullOrWhiteSpace(p))
								.ToArray();

							foreach (var pr in prsFromFile)
							{
								_ = prsToMatch.Add(pr);
							}
						}
						else if (isUrl)
						{
							// URL, treat as PR identifier
							_ = prsToMatch.Add(value);
						}
						else
						{
							// Check for short PR format (owner/repo#number)
							var isShortPrFormat = false;
							var hashIndex = value.LastIndexOf('#');
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
			var seenChangelogs = new HashSet<string>(); // For deduplication (using checksum)

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
					var normalizedYaml = VersionToTargetRegex().Replace(yamlWithoutComments, "$1target:");

					var data = deserializer.Deserialize<ChangelogData>(normalizedYaml);

					if (data == null)
					{
						_logger.LogWarning("Skipping file {FileName}: failed to deserialize", fileName);
						continue;
					}

					// Check for duplicates (using checksum)
					if (seenChangelogs.Contains(checksum))
					{
						_logger.LogDebug("Skipping duplicate changelog: {FileName} (checksum: {Checksum})", fileName, checksum);
						continue;
					}

					// Apply filters
					if (input.All)
					{
						// Include all - no filtering needed
					}
					else if (productFilters.Count > 0)
					{
						// Filter by products with wildcard support
						var matches = false;
						foreach (var (productPattern, targetPattern, lifecyclePattern) in productFilters)
						{
							// Check if any product in the changelog matches this filter
							foreach (var changelogProduct in data.Products)
							{
								var productMatches = MatchesPattern(changelogProduct.Product, productPattern);
								var targetMatches = MatchesPattern(changelogProduct.Target, targetPattern);
								var lifecycleMatches = MatchesPattern(changelogProduct.Lifecycle, lifecyclePattern);

								if (productMatches && targetMatches && lifecycleMatches)
								{
									matches = true;
									break;
								}
							}

							if (matches)
								break;
						}

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

					// Add to seen set and entries list
					_ = seenChangelogs.Add(checksum);
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
			List<BundledProduct> bundledProducts;
			List<BundledEntry> bundledEntries;

			// Set products array in output
			if (input.OutputProducts is { Count: > 0 })
			{
				bundledProducts = input.OutputProducts
					.OrderBy(p => p.Product)
					.ThenBy(p => p.Target ?? string.Empty)
					.ThenBy(p => p.Lifecycle ?? string.Empty)
					.Select(p => new BundledProduct
					{
						Product = p.Product,
						Target = p.Target == "*" ? null : p.Target,
						Lifecycle = p.Lifecycle == "*" ? null : p.Lifecycle
					})
					.ToList();
			}
			else if (changelogEntries.Count > 0)
			{
				var productVersions = new HashSet<(string product, string version, string? lifecycle)>();
				foreach (var (data, _, _, _) in changelogEntries)
				{
					foreach (var product in data.Products)
					{
						var version = product.Target ?? string.Empty;
						_ = productVersions.Add((product.Product, version, product.Lifecycle));
					}
				}

				bundledProducts = productVersions
					.OrderBy(pv => pv.product)
					.ThenBy(pv => pv.version)
					.ThenBy(pv => pv.lifecycle ?? string.Empty)
					.Select(pv => new BundledProduct
					{
						Product = pv.product,
						Target = string.IsNullOrWhiteSpace(pv.version) ? null : pv.version,
						Lifecycle = pv.lifecycle
					})
					.ToList();
			}
			else
			{
				bundledProducts = [];
			}

			// Check if we should allow empty result
			if (changelogEntries.Count == 0)
			{
				collector.EmitError(string.Empty, "No changelog entries matched the filter criteria");
				return false;
			}

			// Check for products with same product ID but different versions
			var productsByProductId = bundledProducts.GroupBy(p => p.Product, StringComparer.OrdinalIgnoreCase)
				.Where(g => g.Count() > 1)
				.ToList();

			foreach (var productGroup in productsByProductId)
			{
				var targets = productGroup.Select(p =>
				{
					var target = string.IsNullOrWhiteSpace(p.Target) ? "(no target)" : p.Target;
					if (!string.IsNullOrWhiteSpace(p.Lifecycle))
					{
						target = $"{target} {p.Lifecycle}";
					}
					return target;
				}).ToList();
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
						Products = data.Products.ToList(),
						Description = data.Description,
						Impact = data.Impact,
						Action = data.Action,
						FeatureId = data.FeatureId,
						Highlight = data.Highlight,
						Subtype = data.Subtype,
						Areas = data.Areas?.ToList(),
						Pr = data.Pr,
						Issues = data.Issues?.ToList()
					});
				}

				bundledEntries = resolvedEntries;
			}
			else
			{
				// Only include file information
				bundledEntries = changelogEntries
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

			var bundledData = new BundledChangelogData
			{
				Products = bundledProducts,
				Entries = bundledEntries
			};

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

	private static bool MatchesPattern(string? value, string? pattern)
	{
		if (pattern == null)
			return true; // Wildcard matches anything (including null/empty)

		if (value == null)
			return false; // Non-wildcard pattern doesn't match null

		// If pattern ends with *, do prefix match
		if (pattern.EndsWith('*'))
		{
			var prefix = pattern[..^1];
			return value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
		}

		// Exact match (case-insensitive)
		return string.Equals(value, pattern, StringComparison.OrdinalIgnoreCase);
	}

	[System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA5350:Do not use insecure cryptographic algorithm SHA1", Justification = "SHA1 is required for compatibility with existing changelog bundle format")]
	internal static string ComputeSha1(string content)
	{
		var bytes = Encoding.UTF8.GetBytes(content);
		var hash = SHA1.HashData(bytes);
		return Convert.ToHexString(hash).ToLowerInvariant();
	}

	internal static string NormalizePrForComparison(string pr, string? defaultOwner, string? defaultRepo)
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
}
