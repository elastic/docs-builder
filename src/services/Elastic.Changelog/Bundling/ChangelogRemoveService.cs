// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.Linq;
using Elastic.Changelog.Configuration;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Changelog;
using Elastic.Documentation.Configuration.ReleaseNotes;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Services;
using Microsoft.Extensions.Logging;

namespace Elastic.Changelog.Bundling;

/// <summary>
/// Arguments for the <see cref="ChangelogRemoveService.RemoveChangelogs"/> method.
/// </summary>
public record ChangelogRemoveArguments
{
	public string? Directory { get; init; }
	public bool All { get; init; }
	public IReadOnlyList<ProductArgument>? Products { get; init; }
	public string[]? Prs { get; init; }
	public string[]? Issues { get; init; }
	public string? Owner { get; init; }
	public string? Repo { get; init; }
	public bool DryRun { get; init; }
	public string? BundlesDir { get; init; }
	public bool Force { get; init; }
	public string? Config { get; init; }

	/// <summary>Profile name from <c>bundle.profiles</c> in the changelog configuration.</summary>
	public string? Profile { get; init; }

	/// <summary>Version number or promotion report URL/path when using a profile.</summary>
	public string? ProfileArgument { get; init; }
}

/// <summary>
/// A discovered dependency between a changelog file and the bundle(s) that reference it.
/// </summary>
public record BundleDependency(string ChangelogFile, string BundleFile);

/// <summary>
/// Service for removing changelog files based on the same filter options as <see cref="ChangelogBundlingService"/>.
/// </summary>
public class ChangelogRemoveService(
	ILoggerFactory logFactory,
	IConfigurationContext? configurationContext = null,
	IFileSystem? fileSystem = null)
	: IService
{
	private readonly ILogger _logger = logFactory.CreateLogger<ChangelogRemoveService>();
	private readonly IFileSystem _fileSystem = fileSystem ?? new FileSystem();
	private readonly ChangelogConfigurationLoader? _configLoader = configurationContext != null
		? new ChangelogConfigurationLoader(logFactory, configurationContext, fileSystem ?? new FileSystem())
		: null;

	public async Task<bool> RemoveChangelogs(IDiagnosticsCollector collector, ChangelogRemoveArguments input, Cancel ctx)
	{
		try
		{
			// Load changelog configuration
			ChangelogConfiguration? config = null;
			if (!string.IsNullOrWhiteSpace(input.Profile))
			{
				// Profile mode requires the config file to exist — no fallback to defaults.
				if (_configLoader == null)
				{
					collector.EmitError(string.Empty, "Changelog configuration loader is required for profile-based removal.");
					return false;
				}
				// When an explicit config path is provided, load it (required, no fallback).
				// Otherwise, discover from CWD: ./changelog.yml then ./docs/changelog.yml.
				config = string.IsNullOrWhiteSpace(input.Config)
					? await _configLoader.LoadChangelogConfigurationForProfileMode(collector, ctx)
					: await _configLoader.LoadChangelogConfigurationRequired(collector, input.Config, ctx);
				if (config == null)
					return false;
			}
			else if (_configLoader != null)
				config = await _configLoader.LoadChangelogConfiguration(collector, input.Config, ctx);

			// Handle profile-based removal (same ordering as ChangelogBundlingService)
			if (!string.IsNullOrWhiteSpace(input.Profile))
			{
				var filterResult = await ProfileFilterResolver.ResolveAsync(
					collector,
					input.Profile,
					input.ProfileArgument,
					config,
					_fileSystem,
					_logger,
					ctx
				);

				if (filterResult == null)
					return false;

				input = input with
				{
					Products = filterResult.Products,
					Prs = filterResult.Prs,
					All = false
				};
			}

			input = ApplyConfigDefaults(input, config);

			if (!ValidateInput(collector, input))
				return false;

			var prsToMatch = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			var issuesToMatch = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			if (input.Prs is { Length: > 0 })
			{
				var loader = new PrFilterLoader(_fileSystem);
				var result = await loader.LoadPrsAsync(collector, input.Prs, input.Owner, input.Repo, ctx);
				if (!result.IsValid)
					return false;
				prsToMatch = result.PrsToMatch;
			}
			else if (input.Issues is { Length: > 0 })
			{
				var loader = new IssueFilterLoader(_fileSystem);
				var result = await loader.LoadIssuesAsync(collector, input.Issues, input.Owner, input.Repo, ctx);
				if (!result.IsValid)
					return false;
				issuesToMatch = result.IssuesToMatch;
			}

			// A placeholder output path is passed to discovery so the bundle file itself is excluded.
			// Directory is non-null here: ApplyConfigDefaults ensures a value and ValidateInput enforces non-empty.
			var placeholderOutput = _fileSystem.Path.Combine(input.Directory!, "changelog-bundle.yaml");
			var fileDiscovery = new ChangelogFileDiscovery(_fileSystem, _logger);
			var yamlFiles = await fileDiscovery.DiscoverChangelogFilesAsync(input.Directory!, placeholderOutput, ctx);

			if (yamlFiles.Count == 0)
			{
				collector.EmitError(input.Directory!, "No changelog YAML files found in directory");
				return false;
			}

			var filterCriteria = BuildFilterCriteria(input, prsToMatch, issuesToMatch);
			var entryMatcher = new ChangelogEntryMatcher(_fileSystem, ReleaseNotesSerialization.GetEntryDeserializer(), _logger);
			var matchResult = await entryMatcher.MatchChangelogsAsync(collector, yamlFiles, filterCriteria, ctx);

			if (matchResult.Entries.Count == 0)
			{
				collector.EmitError(string.Empty, "No changelog entries matched the filter criteria");
				return false;
			}

			var filesToRemove = matchResult.Entries
				.Select(e => e.FilePath)
				.ToList();

			// Check bundle dependencies before deleting
			var dependencies = await FindBundleDependenciesAsync(input, filesToRemove, config, ctx);

			if (dependencies.Count > 0)
			{
				foreach (var dep in dependencies)
				{
					var proceedHint = input.Force ? "" : " To proceed anyway, use --force.";
					var message =
						$"Changelog file '{_fileSystem.Path.GetFileName(dep.ChangelogFile)}' is referenced by " +
						$"unresolved bundle '{dep.BundleFile}'." +
						$" Removing it will cause the {{changelog}} directive to fail when loading that bundle." +
						$" To make the bundle self-contained, re-run: docs-builder changelog bundle --resolve ..." +
						$"{proceedHint}";

					if (input.Force)
						collector.EmitWarning(dep.ChangelogFile, message);
					else
						collector.EmitError(dep.ChangelogFile, message);
				}

				if (!input.Force)
					return false;
			}

			if (input.DryRun)
			{
				_logger.LogInformation("[dry-run] Would remove {Count} changelog file(s):", filesToRemove.Count);
				foreach (var file in filesToRemove)
					_logger.LogInformation("[dry-run]   {File}", file);
				return true;
			}

			foreach (var file in filesToRemove)
			{
				_fileSystem.File.Delete(file);
				_logger.LogInformation("Removed: {File}", file);
			}

			_logger.LogInformation("Removed {Count} changelog file(s).", filesToRemove.Count);
			return true;
		}
		catch (IOException ioEx)
		{
			collector.EmitError(string.Empty, $"IO error removing changelogs: {ioEx.Message}", ioEx);
			return false;
		}
		catch (UnauthorizedAccessException uaEx)
		{
			collector.EmitError(string.Empty, $"Access denied removing changelogs: {uaEx.Message}", uaEx);
			return false;
		}
	}

	private ChangelogRemoveArguments ApplyConfigDefaults(ChangelogRemoveArguments input, ChangelogConfiguration? config)
	{
		var directory = input.Directory ?? config?.Bundle?.Directory ?? _fileSystem.Directory.GetCurrentDirectory();
		return input with { Directory = directory };
	}

	private bool ValidateInput(IDiagnosticsCollector collector, ChangelogRemoveArguments input)
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

		var specified = new List<string>();
		if (input.All)
			specified.Add("--all");
		if (input.Products is { Count: > 0 })
			specified.Add("--products");
		if (input.Prs is { Length: > 0 })
			specified.Add("--prs");
		if (input.Issues is { Length: > 0 })
			specified.Add("--issues");

		if (specified.Count == 0)
		{
			collector.EmitError(string.Empty, "At least one filter option must be specified: --all, --products, --prs, or --issues");
			return false;
		}

		if (specified.Count > 1)
		{
			collector.EmitError(string.Empty,
				$"Multiple filter options cannot be specified together. You specified: {string.Join(", ", specified)}. Please use only one filter option: --all, --products, --prs, or --issues");
			return false;
		}

		return true;
	}

	private static ChangelogFilterCriteria BuildFilterCriteria(
		ChangelogRemoveArguments input,
		HashSet<string> prsToMatch,
		HashSet<string> issuesToMatch)
	{
		var productFilters = new List<ProductFilter>();
		if (input.Products is { Count: > 0 })
		{
			foreach (var product in input.Products)
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
			IssuesToMatch = issuesToMatch,
			DefaultOwner = input.Owner,
			DefaultRepo = input.Repo
		};
	}

	/// <summary>
	/// Discovers which files to be removed are referenced by unresolved bundles.
	/// Bundle locations are discovered automatically unless overridden by <see cref="ChangelogRemoveArguments.BundlesDir"/>.
	/// </summary>
	private async Task<IReadOnlyList<BundleDependency>> FindBundleDependenciesAsync(
		ChangelogRemoveArguments input,
		IReadOnlyList<string> filesToRemove,
		ChangelogConfiguration? config,
		Cancel ctx)
	{
		var bundlesDir = ResolveBundlesDirectory(input, config);
		if (bundlesDir is null)
			return [];

		var bundleFiles = _fileSystem.Directory
			.GetFiles(bundlesDir, "*.yaml", SearchOption.AllDirectories)
			.Concat(_fileSystem.Directory.GetFiles(bundlesDir, "*.yml", SearchOption.AllDirectories))
			.ToList();

		if (bundleFiles.Count == 0)
			return [];

		// Build a set of file names to remove (just basenames, since bundle entries store basenames)
		var toRemoveNames = new HashSet<string>(
			filesToRemove.Select(f => _fileSystem.Path.GetFileName(f)),
			StringComparer.OrdinalIgnoreCase);

		var dependencies = new List<BundleDependency>();

		foreach (var bundleFile in bundleFiles)
		{
			try
			{
				var content = await _fileSystem.File.ReadAllTextAsync(bundleFile, ctx);
				var bundle = ReleaseNotesSerialization.DeserializeBundle(content);

				// Only treat as unresolved when the entry would need to load from file.
				// Resolved entries have inline data (Title+Type) and don't need the file even if they have a File block.
				var entryFileNames = bundle.Entries
					.Where(entry =>
						!string.IsNullOrWhiteSpace(entry.File?.Name) &&
						(string.IsNullOrWhiteSpace(entry.Title) || entry.Type == null))
					.Select(entry => NormalizeEntryFileName(entry.File!.Name));

				foreach (var entryFileName in entryFileNames.Where(entryFileName => toRemoveNames.Contains(entryFileName)))
				{
					// bundle entry.File.Name is relative to the changelog directory (parent of bundles dir)
					// Normalize to just the base filename for comparison

					// Find the full path from filesToRemove that matches this entry
					var matchingFile = filesToRemove
						.FirstOrDefault(f => string.Equals(
							_fileSystem.Path.GetFileName(f),
							entryFileName,
							StringComparison.OrdinalIgnoreCase));

					if (matchingFile is not null)
						dependencies.Add(new BundleDependency(matchingFile, bundleFile));
				}
			}
			catch (Exception ex) when (ex is not (OutOfMemoryException or StackOverflowException or ThreadAbortException))
			{
				_logger.LogWarning(ex, "Could not parse bundle file {BundleFile} for dependency check", bundleFile);
			}
		}

		return dependencies;
	}

	/// <summary>
	/// Resolves the bundles directory using: explicit override, then config, then fallbacks.
	/// Returns null if no bundles directory can be found.
	/// </summary>
	private string? ResolveBundlesDirectory(ChangelogRemoveArguments input, ChangelogConfiguration? config)
	{
		// 1. Explicit override
		if (!string.IsNullOrWhiteSpace(input.BundlesDir))
		{
			if (_fileSystem.Directory.Exists(input.BundlesDir))
				return input.BundlesDir;
			_logger.LogWarning("Specified --bundles-dir '{BundlesDir}' does not exist, skipping dependency check", input.BundlesDir);
			return null;
		}

		// 2. Config bundle.output_directory (resolve relative paths against config file location)
		var outputDir = config?.Bundle?.OutputDirectory;
		if (!string.IsNullOrWhiteSpace(outputDir))
		{
			var resolvedOutputDir = ResolveOutputDirectory(outputDir, input.Config);
			if (_fileSystem.Directory.Exists(resolvedOutputDir))
				return resolvedOutputDir;
		}

		// 3. {directory}/bundles
		// Directory is guaranteed non-null at this point (ApplyConfigDefaults + ValidateInput).
		var sibling = _fileSystem.Path.Combine(input.Directory!, "bundles");
		if (_fileSystem.Directory.Exists(sibling))
			return sibling;

		// 4. {directory}/../bundles
		var dirParent = _fileSystem.Path.GetDirectoryName(input.Directory!);
		if (!string.IsNullOrWhiteSpace(dirParent))
		{
			var parentBundles = _fileSystem.Path.Combine(dirParent, "bundles");
			if (_fileSystem.Directory.Exists(parentBundles))
				return parentBundles;
		}

		return null;
	}

	private string ResolveOutputDirectory(string outputDirectory, string? configPath)
	{
		if (_fileSystem.Path.IsPathRooted(outputDirectory))
			return outputDirectory;

		if (string.IsNullOrWhiteSpace(configPath))
			return _fileSystem.Path.GetFullPath(outputDirectory);

		var configDir = _fileSystem.Path.GetDirectoryName(configPath);
		if (string.IsNullOrWhiteSpace(configDir))
			return _fileSystem.Path.GetFullPath(outputDirectory);

		var repoRoot = _fileSystem.Path.GetDirectoryName(configDir);
		if (string.IsNullOrWhiteSpace(repoRoot))
			return _fileSystem.Path.GetFullPath(outputDirectory);

		return _fileSystem.Path.GetFullPath(_fileSystem.Path.Combine(repoRoot, outputDirectory));
	}

	private static string NormalizeEntryFileName(string entryFileName)
	{
		// Entry names can be paths like "subdir/file.yaml" — take just the file name for comparison
		var normalized = entryFileName.Replace('\\', '/');
		var slashIdx = normalized.LastIndexOf('/');
		return slashIdx >= 0 ? normalized[(slashIdx + 1)..] : normalized;
	}
}
