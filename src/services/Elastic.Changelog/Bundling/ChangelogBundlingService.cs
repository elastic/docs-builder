// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Services;
using Microsoft.Extensions.Logging;
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
			if (!ValidateInput(collector, input))
				return false;

			// Load PR filter values
			var prFilterLoader = new PrFilterLoader(_fileSystem);
			var prFilterResult = await prFilterLoader.LoadPrsAsync(collector, input.Prs, input.Owner, input.Repo, ctx);
			if (!prFilterResult.IsValid)
				return false;

			// Determine output path
			var outputPath = input.Output ?? _fileSystem.Path.Combine(input.Directory, "changelog-bundle.yaml");

			// Discover changelog files
			var fileDiscovery = new ChangelogFileDiscovery(_fileSystem, _logger);
			var yamlFiles = await fileDiscovery.DiscoverChangelogFilesAsync(input.Directory, outputPath, ctx);

			if (yamlFiles.Count == 0)
			{
				collector.EmitError(input.Directory, "No YAML files found in directory");
				return false;
			}

			_logger.LogInformation("Found {Count} YAML files in directory", yamlFiles.Count);

			// Build filter criteria
			var filterCriteria = BuildFilterCriteria(input, prFilterResult.PrsToMatch);

			// Match changelog entries
			var deserializer = new StaticDeserializerBuilder(new ChangelogYamlStaticContext())
				.WithNamingConvention(UnderscoredNamingConvention.Instance)
				.Build();

			var entryMatcher = new ChangelogEntryMatcher(_fileSystem, deserializer, _logger);
			var matchResult = await entryMatcher.MatchChangelogsAsync(collector, yamlFiles, filterCriteria, ctx);

			_logger.LogInformation("Found {Count} matching changelog entries", matchResult.Entries.Count);

			if (matchResult.Entries.Count == 0)
			{
				collector.EmitError(string.Empty, "No changelog entries matched the filter criteria");
				return false;
			}

			// Build bundle
			var bundleBuilder = new BundleBuilder();
			var buildResult = bundleBuilder.BuildBundle(collector, matchResult.Entries, input.OutputProducts, input.Resolve);

			if (!buildResult.IsValid || buildResult.Data == null)
				return false;

			// Write bundle file
			await WriteBundleFileAsync(buildResult.Data, outputPath, ctx);

			return true;
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

	private bool ValidateInput(IDiagnosticsCollector collector, ChangelogBundleInput input)
	{
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

		return true;
	}

	private static ChangelogFilterCriteria BuildFilterCriteria(ChangelogBundleInput input, HashSet<string> prsToMatch)
	{
		var productFilters = new List<ProductFilter>();
		if (input.InputProducts is { Count: > 0 })
		{
			foreach (var product in input.InputProducts)
			{
				productFilters.Add(new ProductFilter
				{
					ProductPattern = product.Product == "*" ? null : product.Product,
					TargetPattern = product.Target == "*" ? null : product.Target,
					LifecyclePattern = product.Lifecycle == "*" ? null : product.Lifecycle
				});
			}
		}

		return new ChangelogFilterCriteria
		{
			IncludeAll = input.All,
			ProductFilters = productFilters,
			PrsToMatch = prsToMatch,
			DefaultOwner = input.Owner,
			DefaultRepo = input.Repo
		};
	}

	private async Task WriteBundleFileAsync(BundledChangelogData bundledData, string outputPath, Cancel ctx)
	{
		// Generate bundled YAML
		var bundleSerializer = new StaticSerializerBuilder(new ChangelogYamlStaticContext())
			.WithNamingConvention(UnderscoredNamingConvention.Instance)
			.ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull | DefaultValuesHandling.OmitEmptyCollections)
			.Build();

		var bundledYaml = bundleSerializer.Serialize(bundledData);

		// Ensure output directory exists
		var outputDir = _fileSystem.Path.GetDirectoryName(outputPath);
		if (!string.IsNullOrWhiteSpace(outputDir) && !_fileSystem.Directory.Exists(outputDir))
			_ = _fileSystem.Directory.CreateDirectory(outputDir);

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
