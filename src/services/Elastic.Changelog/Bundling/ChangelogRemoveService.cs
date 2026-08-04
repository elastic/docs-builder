// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Linq;
using Elastic.Changelog.GitHub;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Changelog;
using Elastic.Documentation.Configuration.ReleaseNotes;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Services;
using Microsoft.Extensions.Logging;
using Nullean.ScopedFileSystem;

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

	/// <summary>
	/// Explicit changelog YAML paths (or a path-list file) for the <c>--files</c> filter.
	/// Mutually exclusive with other filter sources.
	/// </summary>
	public string[]? Files { get; init; }

	public string? Owner { get; init; }
	public string? Repo { get; init; }
	public bool DryRun { get; init; }
	public string? Config { get; init; }

	/// <summary>Profile name from <c>bundle.profiles</c> in the changelog configuration.</summary>
	public string? Profile { get; init; }

	/// <summary>Version number or promotion report URL/path when using a profile.</summary>
	public string? ProfileArgument { get; init; }

	/// <summary>
	/// Optional third profile argument: a promotion report URL/path or URL list file to use as the
	/// PR/issue filter source when <see cref="ProfileArgument"/> is the version string.
	/// </summary>
	public string? ProfileReport { get; init; }

	/// <summary>
	/// Promotion report URL or file path for option-based removal (<c>--report</c>).
	/// When set, the report is parsed and the extracted PR URLs become the effective PR filter.
	/// </summary>
	public string? Report { get; init; }
}

/// <summary>
/// Service for removing changelog files based on the same filter options as <see cref="ChangelogBundlingService"/>.
/// </summary>
public class ChangelogRemoveService(
	ILoggerFactory logFactory,
	IConfigurationContext? configurationContext = null,
	ScopedFileSystem? fileSystem = null,
	IGitHubReleaseService? releaseService = null)
	: IService
{
	private readonly ILogger _logger = logFactory.CreateLogger<ChangelogRemoveService>();
	private readonly ScopedFileSystem _fileSystem = fileSystem ?? FileSystemFactory.RealRead;
	private readonly IGitHubReleaseService _releaseService = releaseService ?? new GitHubReleaseService(logFactory);
	private readonly ChangelogConfigurationLoader? _configLoader = configurationContext != null
		? new ChangelogConfigurationLoader(logFactory, configurationContext, fileSystem ?? FileSystemFactory.RealRead)
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
						ctx,
						input.ProfileReport,
						_releaseService
					);

				if (filterResult == null)
					return false;

				input = input with
				{
					Products = filterResult.Products,
					Prs = filterResult.Prs,
					Issues = filterResult.Issues,
					Files = filterResult.Files,
					All = false
				};
			}
			else if (!string.IsNullOrWhiteSpace(input.Report))
			{
				// Option-based mode with --report: parse report and populate Prs
				var parser = new PromotionReportParser(logFactory, _fileSystem);
				var prs = await parser.ParseReportToPrUrlsAsync(collector, input.Report, ctx);
				if (prs == null)
					return false;
				input = input with { Prs = prs };
			}

			input = ApplyConfigDefaults(input, config);

			if (!ValidateInput(collector, input))
				return false;

			var prsToMatch = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			var issuesToMatch = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			IReadOnlyList<string>? explicitFilePaths = null;

			if (input.Files is { Length: > 0 })
			{
				var loader = new FileFilterLoader(_fileSystem);
				var result = await loader.LoadFilesAsync(collector, input.Files, input.Directory, ctx);
				if (!result.IsValid)
					return false;
				explicitFilePaths = result.FilePaths;
			}
			else if (input.Prs is { Length: > 0 })
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

			var filterCriteria = BuildFilterCriteria(input, prsToMatch, issuesToMatch);
			var entryMatcher = new ChangelogEntryMatcher(_fileSystem, ReleaseNotesSerialization.GetEntryDeserializer(), _logger);
			ChangelogMatchResult matchResult;
			if (explicitFilePaths != null)
			{
				var filesCriteria = filterCriteria with { IncludeAll = true };
				matchResult = await entryMatcher.MatchChangelogsAsync(collector, explicitFilePaths, filesCriteria, ctx);
			}
			else
			{
				// A placeholder output path is passed to discovery so the bundle file itself is excluded.
				// Directory is non-null here: ApplyConfigDefaults ensures a value and ValidateInput enforces non-empty.
				var placeholderOutput = _fileSystem.Path.Join(input.Directory!, "changelog-bundle.yaml");
				var fileDiscovery = new ChangelogFileDiscovery(_fileSystem, _logger);
				var yamlFiles = await fileDiscovery.DiscoverChangelogFilesAsync(input.Directory!, placeholderOutput, ctx);

				if (yamlFiles.Count == 0)
				{
					collector.EmitError(input.Directory!, "No changelog YAML files found in directory");
					return false;
				}

				matchResult = await entryMatcher.MatchChangelogsAsync(collector, yamlFiles, filterCriteria, ctx);
			}

			if (matchResult.Entries.Count == 0)
			{
				collector.EmitError(string.Empty, "No changelog entries matched the filter criteria");
				return false;
			}

			var filesToRemove = matchResult.Entries
				.Select(e => e.FilePath)
				.ToList();

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

		// Apply repo/owner: CLI takes precedence; fall back to bundle-level config defaults.
		var repo = input.Repo ?? config?.Bundle?.Repo;
		var owner = input.Owner ?? config?.Bundle?.Owner;

		return input with { Directory = directory, Repo = repo, Owner = owner };
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
		if (input.Files is { Length: > 0 })
			specified.Add("--files");

		if (specified.Count == 0)
		{
			collector.EmitError(string.Empty, "At least one filter option must be specified: --all, --products, --prs, --issues, or --files");
			return false;
		}

		if (specified.Count > 1)
		{
			collector.EmitError(string.Empty,
				$"Multiple filter options cannot be specified together. You specified: {string.Join(", ", specified)}. Please use only one filter option: --all, --products, --prs, --issues, or --files");
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

}
