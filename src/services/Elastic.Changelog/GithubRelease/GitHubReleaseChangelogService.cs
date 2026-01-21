// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.Security.Cryptography;
using System.Text;
using Elastic.Changelog.Bundling;
using Elastic.Changelog.Configuration;
using Elastic.Changelog.GitHub;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Services;
using Microsoft.Extensions.Logging;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Elastic.Changelog.GithubRelease;

/// <summary>
/// Service for creating changelogs from GitHub releases
/// </summary>
public class GitHubReleaseChangelogService(
	ILoggerFactory logFactory,
	IConfigurationContext configurationContext,
	IGitHubReleaseService? releaseService = null,
	IGitHubPrService? prService = null,
	IFileSystem? fileSystem = null
) : IService
{
	private readonly ILogger _logger = logFactory.CreateLogger<GitHubReleaseChangelogService>();
	private readonly IFileSystem _fileSystem = fileSystem ?? new FileSystem();
	private readonly ChangelogConfigurationLoader _configLoader = new(logFactory, configurationContext, fileSystem ?? new FileSystem());
	private readonly IGitHubReleaseService _releaseService = releaseService ?? new GitHubReleaseService(logFactory);
	private readonly IGitHubPrService _prService = prService ?? new GitHubPrService(logFactory);

	public async Task<bool> CreateChangelogsFromRelease(
		IDiagnosticsCollector collector,
		GitHubReleaseInput input,
		Cancel ctx
	)
	{
		try
		{
			// 1. Parse owner/repo from input
			var (owner, repo) = ChangelogTextUtilities.ParseRepository(input.Repository);
			if (string.IsNullOrWhiteSpace(owner))
			{
				// If no owner, assume "elastic" as default
				owner = "elastic";
				repo = input.Repository;
			}

			_logger.LogInformation("Processing GitHub release from {Owner}/{Repo}", owner, repo);

			// 2. Resolve product from repo name via products.yml
			var product = configurationContext.ProductsConfiguration.GetProductByRepositoryName(repo);
			if (product == null)
			{
				collector.EmitError(string.Empty,
					$"Could not find product for repository '{repo}' in products.yml. " +
					"Ensure the repository name matches a product ID or a product has 'repository: {repo}' configured.");
				return false;
			}

			_logger.LogInformation("Resolved product: {ProductId} ({ProductDisplay})", product.Id, product.DisplayName);

			// 3. Load changelog configuration
			var config = await _configLoader.LoadChangelogConfiguration(collector, input.Config, ctx);
			if (config == null)
			{
				collector.EmitError(string.Empty, "Failed to load changelog configuration");
				return false;
			}

			// 4. Fetch GitHub release
			var release = await _releaseService.FetchReleaseAsync(owner, repo, input.Version, ctx);
			if (release == null)
			{
				collector.EmitError(string.Empty,
					$"Failed to fetch release for {owner}/{repo}@{input.Version}. " +
					"Ensure the repository exists and the version tag is valid.");
				return false;
			}

			_logger.LogInformation("Fetched release: {TagName} ({Name})", release.TagName, release.Name);

			// 5. Parse release notes
			var parsedNotes = ReleaseNoteParser.Parse(release.Body);
			_logger.LogInformation("Detected format: {Format}, found {Count} PR references",
				parsedNotes.Format, parsedNotes.PrReferences.Count);

			if (parsedNotes.PrReferences.Count == 0)
			{
				collector.EmitWarning(string.Empty, "No PR references found in release notes. No changelogs will be created.");
				return true;
			}

			// 6. Infer lifecycle and target version from release tag
			var lifecycle = ChangelogTextUtilities.InferLifecycleFromVersion(release.TagName);
			var targetVersion = ChangelogTextUtilities.ExtractBaseVersion(release.TagName);

			_logger.LogInformation("Inferred lifecycle: {Lifecycle}, target version: {Target}", lifecycle, targetVersion);

			// Create product info with inferred values
			var productInfo = new ProductInfo
			{
				Product = product.Id,
				Target = targetVersion,
				Lifecycle = lifecycle
			};

			// 7. Process each PR and create changelog files
			var outputDir = input.Output ?? _fileSystem.Path.Combine(_fileSystem.Directory.GetCurrentDirectory(), "changelogs");
			if (!_fileSystem.Directory.Exists(outputDir))
				_ = _fileSystem.Directory.CreateDirectory(outputDir);

			var createdFiles = new List<string>();
			var successCount = 0;

			foreach (var prRef in parsedNotes.PrReferences)
			{
				var success = await ProcessPrReference(
					collector, config, owner, repo, prRef,
					productInfo, input, parsedNotes.Format, outputDir, createdFiles, ctx);
				if (success)
					successCount++;
			}

			_logger.LogInformation("Created {Count} changelog files from release {Tag}", successCount, release.TagName);

			// 8. Create bundle file if changelogs were created
			if (createdFiles.Count > 0)
			{
				var bundlePath = await CreateBundleFile(outputDir, createdFiles, productInfo, ctx);
				_logger.LogInformation("Created bundle file: {BundlePath}", bundlePath);
			}

			return successCount > 0 || parsedNotes.PrReferences.Count == 0;
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

	private async Task<bool> ProcessPrReference(
		IDiagnosticsCollector collector,
		ChangelogConfiguration config,
		string owner,
		string repo,
		ExtractedPrReference prRef,
		ProductInfo productInfo,
		GitHubReleaseInput input,
		ReleaseNoteFormat format,
		string outputDir,
		List<string> createdFiles,
		Cancel ctx)
	{
		var prUrl = $"https://github.com/{owner}/{repo}/pull/{prRef.PrNumber}";

		// Fetch PR labels
		var prInfo = await _prService.FetchPrInfoAsync(prUrl, owner, repo, ctx);

		// Derive type from labels
		string? labelDerivedType = null;
		List<string>? labelDerivedAreas = null;

		if (prInfo != null)
		{
			if (config.LabelToType != null && config.LabelToType.Count > 0)
				labelDerivedType = MapLabelsToType(prInfo.Labels.ToArray(), config.LabelToType);

			if (config.LabelToAreas != null && config.LabelToAreas.Count > 0)
				labelDerivedAreas = MapLabelsToAreas(prInfo.Labels.ToArray(), config.LabelToAreas);
		}
		else
		{
			collector.EmitWarning(prUrl, $"Failed to fetch PR info for #{prRef.PrNumber}. Using inferred type from release notes.");
		}

		// Determine final type (label-derived takes priority)
		var finalType = labelDerivedType ?? prRef.InferredType ?? ChangelogEntryTypes.Other;

		// Warn on type mismatch if Release Drafter format and warning enabled
		if (format == ReleaseNoteFormat.ReleaseDrafter &&
			input.WarnOnTypeMismatch &&
			labelDerivedType != null &&
			prRef.InferredType != null &&
			!string.Equals(labelDerivedType, prRef.InferredType, StringComparison.OrdinalIgnoreCase))
		{
			collector.EmitWarning(prUrl,
				$"Type mismatch for PR #{prRef.PrNumber}: " +
				$"section header suggests '{prRef.InferredType}' but labels suggest '{labelDerivedType}'. " +
				"Using label-derived type.");
		}

		// Build title
		var title = prRef.Title ?? prInfo?.Title ?? $"PR #{prRef.PrNumber}";
		if (input.StripTitlePrefix)
			title = ChangelogTextUtilities.StripSquareBracketPrefix(title);

		// Create changelog data
		var changelogData = new ChangelogData
		{
			Title = title,
			Type = finalType,
			Products = [productInfo],
			Areas = labelDerivedAreas,
			Pr = prUrl
		};

		// Generate YAML content
		var yamlContent = GenerateYaml(changelogData);

		// Write file with prettier name: <pr_number>-<type>-<slug>.yaml
		var slug = ChangelogTextUtilities.GenerateSlug(title);
		var filename = $"{prRef.PrNumber}-{finalType}-{slug}.yaml";
		var filePath = _fileSystem.Path.Combine(outputDir, filename);
		await _fileSystem.File.WriteAllTextAsync(filePath, yamlContent, ctx);

		createdFiles.Add(filename);
		_logger.LogDebug("Created changelog: {FilePath}", filePath);

		return true;
	}

	private string GenerateYaml(ChangelogData data)
	{
		var serializer = new StaticSerializerBuilder(new ChangelogYamlStaticContext())
			.WithNamingConvention(UnderscoredNamingConvention.Instance)
			.ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull | DefaultValuesHandling.OmitEmptyCollections)
			.Build();

		return serializer.Serialize(data);
	}

	private async Task<string> CreateBundleFile(
		string outputDir,
		List<string> createdFiles,
		ProductInfo productInfo,
		Cancel ctx)
	{
		var bundleEntries = new List<BundledEntry>();

		foreach (var filename in createdFiles)
		{
			var filePath = _fileSystem.Path.Combine(outputDir, filename);
			var content = await _fileSystem.File.ReadAllTextAsync(filePath, ctx);
			var checksum = ComputeChecksum(content);

			bundleEntries.Add(new BundledEntry
			{
				File = new BundledFile
				{
					Name = filename,
					Checksum = checksum
				}
			});
		}

		var bundleData = new BundledChangelogData
		{
			Products = [new BundledProduct
			{
				Product = productInfo.Product,
				Target = productInfo.Target,
				Lifecycle = productInfo.Lifecycle
			}],
			Entries = bundleEntries
		};

		var serializer = new StaticSerializerBuilder(new ChangelogYamlStaticContext())
			.WithNamingConvention(UnderscoredNamingConvention.Instance)
			.ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull | DefaultValuesHandling.OmitEmptyCollections)
			.Build();

		var yamlContent = serializer.Serialize(bundleData);

		// Create bundles subfolder
		var bundlesDir = _fileSystem.Path.Combine(outputDir, "bundles");
		if (!_fileSystem.Directory.Exists(bundlesDir))
			_ = _fileSystem.Directory.CreateDirectory(bundlesDir);

		// Name format: <version>-<product>-bundle.yml
		var bundleFilename = $"{productInfo.Target}-{productInfo.Product}-bundle.yml";
		var bundlePath = _fileSystem.Path.Combine(bundlesDir, bundleFilename);
		await _fileSystem.File.WriteAllTextAsync(bundlePath, yamlContent, ctx);

		return bundlePath;
	}

	private static string ComputeChecksum(string content)
	{
		var bytes = Encoding.UTF8.GetBytes(content);
		var hash = SHA256.HashData(bytes);
		return Convert.ToHexString(hash).ToLowerInvariant()[..16];
	}

	private static string? MapLabelsToType(string[] labels, IReadOnlyDictionary<string, string> labelToTypeMapping) =>
		labels
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
			_ = areas.Add(area);
		return areas.ToList();
	}
}
