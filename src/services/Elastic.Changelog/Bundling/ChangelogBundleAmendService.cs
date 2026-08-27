// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;
using System.IO.Abstractions;
using System.Text;
using System.Text.RegularExpressions;
using Elastic.Changelog.Utilities;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Configuration.Changelog;
using Elastic.Documentation.Configuration.ReleaseNotes;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Extensions;
using Elastic.Documentation.FileSystems;
using Elastic.Documentation.ReleaseNotes;
using Elastic.Documentation.Services;
using Microsoft.Extensions.Logging;

namespace Elastic.Changelog.Bundling;

/// <summary>
/// Arguments for the AmendBundle method
/// </summary>
public record AmendBundleArguments
{
	/// <summary>
	/// Path to the original bundle file to amend
	/// </summary>
	public required string BundlePath { get; init; }

	/// <summary>
	/// Paths to changelog YAML files to add to the bundle
	/// </summary>
	public IReadOnlyList<string> AddFiles { get; init; } = [];

	/// <summary>
	/// Paths to changelog YAML files to remove from the effective bundle
	/// </summary>
	public IReadOnlyList<string> RemoveFiles { get; init; } = [];

	/// <summary>
	/// Remove by file name when the bundle checksum does not match the sourced changelog.
	/// </summary>
	public bool Force { get; init; }

	/// <summary>
	/// Force local entry sourcing for this run (CLI <c>--force-local</c>).
	/// </summary>
	public bool ForceLocal { get; init; }

	/// <summary>
	/// Preview changes without writing an amend file.
	/// </summary>
	public bool DryRun { get; init; }
}

/// <summary>
/// Service for amending changelog bundles with additional entries
/// </summary>
public partial class ChangelogBundleAmendService(
	ILoggerFactory logFactory,
	IChangelogFileSystem fileSystem,
	IConfigurationContext? configurationContext = null,
	CdnChangelogEntryFetcher? entryFetcher = null) : IService
{
	/// <summary>
	/// UTF-8 encoding without BOM for writing YAML files.
	/// </summary>
	private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

	private readonly ILogger _logger = logFactory.CreateLogger<ChangelogBundleAmendService>();
	private readonly IChangelogFileSystem _fileSystem = fileSystem;
	private readonly CdnChangelogEntryFetcher _entryFetcher = entryFetcher ?? new CdnChangelogEntryFetcher(logFactory);
	private readonly ChangelogConfigurationLoader? _configLoader = configurationContext != null
		? new ChangelogConfigurationLoader(logFactory, configurationContext, fileSystem)
		: null;

	[GeneratedRegex(@"\.amend-(\d+)\.ya?ml$", RegexOptions.IgnoreCase)]
	private static partial Regex AmendFileRegex();

	/// <summary>
	/// Amends a bundle with additional or excluded changelog entries, creating a new immutable amend file.
	/// </summary>
	public async Task<bool> AmendBundle(IDiagnosticsCollector collector, AmendBundleArguments input, Cancel ctx)
	{
		try
		{
			if (!_fileSystem.File.Exists(input.BundlePath))
			{
				var currentDir = _fileSystem.Directory.GetCurrentDirectory();
				collector.EmitError(
					input.BundlePath,
					$"Bundle file does not exist. Current directory: {currentDir}"
				);
				return false;
			}

			if (input.AddFiles.Count == 0 && input.RemoveFiles.Count == 0)
			{
				collector.EmitError(string.Empty, "At least one file must be specified with --add or --remove");
				return false;
			}

			var (parentOk, parentBundle) = await TryDeserializeParentBundleAsync(
				input.BundlePath,
				collector,
				ctx);
			if (!parentOk || parentBundle == null)
				return false;

			ChangelogConfiguration? changelogConfig = null;
			if (_configLoader != null)
			{
				changelogConfig = await _configLoader.LoadChangelogConfiguration(collector, null, ctx);
				if (changelogConfig is null)
					return false;
			}

			var useLocalChangelogs = (changelogConfig?.Bundle?.UseLocalChangelogs ?? false) || input.ForceLocal;
			var authoringRepo = ChangelogRepoOwnerResolver.NormalizeRepo(
				changelogConfig?.Bundle?.Repo ?? (parentBundle.Products.Count > 0 ? parentBundle.Products[0].Repo : null));
			var useCdn = ChangelogEntrySourcing.ShouldSourceFromCdn(authoringRepo, useLocalChangelogs: useLocalChangelogs);

			IReadOnlyDictionary<string, string>? cdnContents = null;
			if (useCdn)
			{
				var fetched = await FetchCdnContentsAsync(
					collector,
					new CdnAmendSourceRequest(
						parentBundle,
						changelogConfig,
						authoringRepo!,
						input.AddFiles,
						input.RemoveFiles,
						input.Force),
					ctx);
				if (fetched is null)
					return false;
				cdnContents = fetched;
			}

			var addSources = await SourceInputFilesAsync(
				collector,
				input.AddFiles,
				"--add",
				useCdn,
				cdnContents,
				requireContent: true,
				force: false,
				ctx);
			if (addSources is null)
				return false;

			var removeSources = await SourceInputFilesAsync(
				collector,
				input.RemoveFiles,
				"--remove",
				useCdn,
				cdnContents,
				requireContent: false,
				force: input.Force,
				ctx);
			if (removeSources is null)
				return false;

			var (amendsOk, existingAmendBundles) = await LoadExistingAmendBundlesAsync(
				input.BundlePath,
				collector,
				ctx);
			if (!amendsOk)
				return false;

			var effectiveEntries = BundleAmendMerger.MergeEntries(parentBundle.Entries, existingAmendBundles);
			var appliedExclusionKeys = BundleAmendMerger.CollectAppliedExclusionKeys(existingAmendBundles);

			var excludeEntries = new List<BundledEntry>();
			foreach (var removeSource in removeSources)
			{
				var exclusion = BuildExclusionEntry(
					collector,
					removeSource,
					effectiveEntries,
					appliedExclusionKeys,
					input.Force);
				if (exclusion == null)
					return false;
				if (exclusion is RemoveExclusionResult.Skip)
					continue;

				var entry = ((RemoveExclusionResult.Add)exclusion).Entry;
				excludeEntries.Add(entry);
				_ = appliedExclusionKeys.Add(BundleAmendMerger.BuildExclusionKey(entry));
			}

			var linkAllowRepos = changelogConfig?.Bundle?.LinkAllowRepos;
			var linkAllowlistActive = linkAllowRepos != null;

			var entries = new List<BundledEntry>();
			if (addSources.Count > 0)
			{
				if (linkAllowlistActive)
				{
					var owner = parentBundle.Products.Count > 0 ? parentBundle.Products[0].Owner ?? "elastic" : "elastic";
					var repo = parentBundle.Products.Count > 0 ? parentBundle.Products[0].Repo : null;
					if (!LinkAllowlistSanitizer.TryApplyBundle(
						collector,
						parentBundle,
						linkAllowRepos!,
						owner,
						repo,
						out _,
						out var parentHadAllowlistChanges))
						return false;

					if (parentHadAllowlistChanges)
					{
						collector.EmitError(
							string.Empty,
							"bundle.link_allow_repos requires the parent bundle to already reflect filtered PR/issue references. " +
							"Re-create the parent bundle with the same bundle.link_allow_repos, " +
							"or remove bundle.link_allow_repos for amend.");
						return false;
					}
				}

				foreach (var addSource in addSources)
				{
					var entry = LoadChangelogContent(collector, addSource);
					if (entry == null)
						return false;
					entries.Add(entry);
				}
			}

			if (excludeEntries.Count == 0 && entries.Count == 0)
			{
				collector.EmitWarning(string.Empty, "No changes to apply; amend file was not created.");
				return true;
			}

			if (input.DryRun)
			{
				_logger.LogInformation(
					"Dry run: would exclude {ExcludeCount} and add {AddCount} entries",
					excludeEntries.Count,
					entries.Count);
				return true;
			}

			var nextAmendNumber = GetNextAmendNumber(input.BundlePath);
			var amendFilePath = GenerateAmendFilePath(input.BundlePath, nextAmendNumber);

			_logger.LogInformation(
				"Creating amend file: {AmendFilePath} (exclude={ExcludeCount}, add={AddCount})",
				amendFilePath,
				excludeEntries.Count,
				entries.Count);

			// Copy the parent's complete products (target, repo, owner) so the amend is self-contained:
			// upload destination discovery, the registry's per-product target, and :version:-filtered
			// CDN fetches all derive from a bundle file's own products.
			var amendBundle = new Bundle
			{
				Products = parentBundle.Products,
				ExcludeEntries = excludeEntries,
				Entries = entries
			};

			var bundleForWrite = amendBundle;
			if (entries.Count > 0 && linkAllowRepos != null)
			{
				var owner = parentBundle.Products.Count > 0 ? parentBundle.Products[0].Owner ?? "elastic" : "elastic";
				var repo = parentBundle.Products.Count > 0 ? parentBundle.Products[0].Repo : null;

				if (!LinkAllowlistSanitizer.TryApplyBundle(
					collector,
					amendBundle,
					linkAllowRepos,
					owner,
					repo,
					out var sanitized,
					out _))
					return false;
				bundleForWrite = sanitized;

				if (configurationContext != null && linkAllowRepos.Count > 0)
				{
					try
					{
						var assemblyYaml = configurationContext.ConfigurationFileProvider.AssemblerFile.ReadToEnd();
						var assembly = AssemblyConfiguration.Deserialize(assemblyYaml, skipPrivateRepositories: false);
						LinkAllowlistSanitizer.EmitAssemblerDiagnostics(collector, linkAllowRepos, assembly);
					}
					catch (Exception ex) when (ex is not (OutOfMemoryException or StackOverflowException))
					{
						collector.EmitWarning(
							string.Empty,
							$"Could not load assembler.yml for bundle.link_allow_repos diagnostics: {ex.Message}");
					}
				}
			}

			var yaml = ReleaseNotesSerialization.SerializeBundle(bundleForWrite);

			var outputDir = _fileSystem.Path.GetDirectoryName(amendFilePath);
			if (!string.IsNullOrWhiteSpace(outputDir) && !_fileSystem.Directory.Exists(outputDir))
				_ = _fileSystem.Directory.CreateDirectory(outputDir);

			var normalizedYaml = ChangelogUtf8Normalization.StripLeadingUtf8BomChar(yaml);
			await _fileSystem.File.WriteAllTextAsync(amendFilePath, normalizedYaml, Utf8NoBom, ctx);
			_logger.LogInformation(
				"Created amend file: {AmendFilePath} with {ExcludeCount} exclusions and {AddCount} additions",
				amendFilePath,
				excludeEntries.Count,
				entries.Count);

			return true;
		}
		catch (IOException ioEx)
		{
			collector.EmitError(string.Empty, $"IO error creating amend file: {ioEx.Message}", ioEx);
			return false;
		}
		catch (UnauthorizedAccessException uaEx)
		{
			collector.EmitError(string.Empty, $"Access denied creating amend file: {uaEx.Message}", uaEx);
			return false;
		}
	}

	private readonly record struct CdnAmendSourceRequest(
		Bundle ParentBundle,
		ChangelogConfiguration? ChangelogConfig,
		string AuthoringRepo,
		IReadOnlyList<string> AddPaths,
		IReadOnlyList<string> RemovePaths,
		bool ForceRemove);

	private readonly record struct CdnPoolLocation(Uri BaseUri, string Owner, string Repo, string Branch);

	private async Task<IReadOnlyDictionary<string, string>?> FetchCdnContentsAsync(
		IDiagnosticsCollector collector,
		CdnAmendSourceRequest request,
		Cancel ctx)
	{
		var parentOwner = request.ParentBundle.Products.Count > 0 ? request.ParentBundle.Products[0].Owner : null;
		var owner = ChangelogRepoOwnerResolver.ResolveOwner(
			request.ChangelogConfig?.Bundle?.Owner,
			request.ChangelogConfig?.Bundle?.Repo,
			parentOwner) ?? ChangelogEntrySourcing.DefaultOwner;
		var configuredBranch = request.ChangelogConfig?.Bundle?.Branch;
		var branch = string.IsNullOrWhiteSpace(configuredBranch)
			? ChangelogEntrySourcing.DefaultBranch
			: configuredBranch;

		var baseUri = ChangelogCdn.ResolveBaseUri();
		if (baseUri is null)
		{
			collector.EmitError(string.Empty,
				$"No valid changelog CDN base URL is configured. Set the {ChangelogCdn.BaseUrlEnvironmentVariable} environment variable to an absolute http(s) URL.");
			return null;
		}

		var pool = new CdnPoolLocation(baseUri, owner, request.AuthoringRepo, branch);
		var contents = new Dictionary<string, string>(StringComparer.Ordinal);
		var addNames = DistinctFileNames(request.AddPaths);
		if (addNames.Count > 0)
		{
			var adds = await FetchNamedRequiredAsync(collector, pool, addNames, ctx).ConfigureAwait(false);
			if (adds is null)
				return null;
			foreach (var entry in adds)
				contents[entry.FileName] = entry.Content;
		}

		var removeNames = DistinctFileNames(request.RemovePaths);
		if (removeNames.Count == 0)
			return contents;

		if (!request.ForceRemove)
		{
			var removes = await FetchNamedRequiredAsync(collector, pool, removeNames, ctx).ConfigureAwait(false);
			if (removes is null)
				return null;
			foreach (var entry in removes)
				contents[entry.FileName] = entry.Content;
			return contents;
		}

		foreach (var name in removeNames)
		{
			if (contents.ContainsKey(name))
				continue;

			var captured = new List<string>();
			var result = await _entryFetcher.FetchNamedAsync(
				pool.BaseUri,
				pool.Owner,
				pool.Repo,
				pool.Branch,
				[name],
				captured.Add,
				ctx).ConfigureAwait(false);
			if (result is not null)
			{
				foreach (var entry in result)
					contents[entry.FileName] = entry.Content;
				continue;
			}

			if (captured.TrueForAll(IsNamedFetchNotFound))
				continue;

			foreach (var msg in captured)
				collector.EmitError(string.Empty, msg);
			return null;
		}

		return contents;
	}

	private async Task<IReadOnlyList<CdnChangelogEntry>?> FetchNamedRequiredAsync(
		IDiagnosticsCollector collector,
		CdnPoolLocation pool,
		IReadOnlyList<string> names,
		Cancel ctx)
	{
		var fatal = false;
		var result = await _entryFetcher.FetchNamedAsync(
			pool.BaseUri,
			pool.Owner,
			pool.Repo,
			pool.Branch,
			names,
			msg =>
			{
				fatal = true;
				collector.EmitError(string.Empty, msg);
			},
			ctx).ConfigureAwait(false);
		return fatal || result is null ? null : result;
	}

	private List<string> DistinctFileNames(IReadOnlyList<string> paths) =>
		paths
			.Select(p => _fileSystem.Path.GetFileName(p))
			.Where(n => !string.IsNullOrWhiteSpace(n))
			.Distinct(StringComparer.Ordinal)
			.ToList();

	private static bool IsNamedFetchNotFound(string message) =>
		message.Contains("404", StringComparison.Ordinal);

	private async Task<IReadOnlyList<SourcedChangelog>?> SourceInputFilesAsync(
		IDiagnosticsCollector collector,
		IReadOnlyList<string> files,
		string optionName,
		bool useCdn,
		IReadOnlyDictionary<string, string>? cdnContents,
		bool requireContent,
		bool force,
		Cancel ctx)
	{
		if (files.Count == 0)
			return [];

		var sourced = new List<SourcedChangelog>();
		foreach (var file in files)
		{
			var fileName = _fileSystem.Path.GetFileName(file);
			if (useCdn)
			{
				if (cdnContents is not null && cdnContents.TryGetValue(fileName, out var cdnYaml))
				{
					sourced.Add(new SourcedChangelog(fileName, cdnYaml, file));
					continue;
				}

				if (!requireContent && force)
				{
					sourced.Add(new SourcedChangelog(fileName, Content: null, file));
					continue;
				}

				collector.EmitError(
					file,
					requireContent
						? $"Changelog '{fileName}' was not found in the CDN pool. Ensure the entry was uploaded (changelog upload), or pass --force-local to read a local file."
						: $"Changelog '{fileName}' was not found in the CDN pool. Ensure the entry was uploaded (changelog upload), pass --force-local to read a local file, or pass --force to exclude by file name.");
				return null;
			}

			if (_fileSystem.File.Exists(file))
			{
				var content = await _fileSystem.File.ReadAllTextAsync(file, ctx).ConfigureAwait(false);
				sourced.Add(new SourcedChangelog(fileName, content, file));
				continue;
			}

			if (!requireContent && force)
			{
				sourced.Add(new SourcedChangelog(fileName, Content: null, file));
				continue;
			}

			var currentDir = _fileSystem.Directory.GetCurrentDirectory();
			collector.EmitError(
				file,
				$"File does not exist. Current directory: {currentDir}. " +
				$"Tip: Repeat {optionName} for each file, or use comma-separated values (e.g., {optionName} \"file1.yaml,file2.yaml\"). " +
				"Paths support tilde (~) expansion and can be relative or absolute. " +
				"When sourcing from the CDN, paths are matched by file name and do not need to exist locally.");
			return null;
		}

		return sourced;
	}

	private async Task<(bool Ok, List<Bundle> Bundles)> LoadExistingAmendBundlesAsync(
		string bundlePath,
		IDiagnosticsCollector collector,
		Cancel ctx)
	{
		var amendPaths = DiscoverAmendFiles(_fileSystem, bundlePath);
		var amendBundles = new List<Bundle>();
		foreach (var amendPath in amendPaths)
		{
			try
			{
				var content = await _fileSystem.File.ReadAllTextAsync(amendPath, ctx);
				amendBundles.Add(ReleaseNotesSerialization.DeserializeBundle(content));
			}
			catch (Exception ex) when (ex is not (OutOfMemoryException or StackOverflowException or ThreadAbortException))
			{
				collector.EmitError(
					amendPath,
					$"Failed to deserialize amend file: {ex.Message}",
					ex);
				return (false, []);
			}
		}
		return (true, amendBundles);
	}

	private RemoveExclusionResult? BuildExclusionEntry(
		IDiagnosticsCollector collector,
		SourcedChangelog source,
		IReadOnlyList<BundledEntry> effectiveEntries,
		HashSet<string> appliedExclusionKeys,
		bool force)
	{
		var fileName = source.FileName;
		var fileChecksum = source.Content is null
			? string.Empty
			: ChangelogBundlingService.ComputeSha1(source.Content);

		var strictExclusion = new BundledEntry
		{
			File = new BundledFile
			{
				Name = fileName,
				Checksum = fileChecksum
			}
		};

		var exclusionKey = BundleAmendMerger.BuildExclusionKey(strictExclusion);
		if (!string.IsNullOrEmpty(fileChecksum) && appliedExclusionKeys.Contains(exclusionKey))
		{
			collector.EmitWarning(
				source.DisplayPath,
				$"Changelog '{fileName}' is already excluded by a prior amend file; skipping.");
			return RemoveExclusionResult.Skip.Instance;
		}

		var matchedEntry = source.Content is null
			? null
			: effectiveEntries.FirstOrDefault(entry => BundleAmendMerger.EntryMatchesExclusion(entry, strictExclusion));

		if (matchedEntry == null)
		{
			var nameOnlyExclusion = new BundledEntry
			{
				File = new BundledFile
				{
					Name = fileName,
					Checksum = string.Empty
				}
			};

			var nameMatches = effectiveEntries
				.Where(entry => BundleAmendMerger.EntryMatchesExclusion(entry, nameOnlyExclusion))
				.ToList();

			if (nameMatches.Count == 0)
			{
				collector.EmitError(
					source.DisplayPath,
					$"Changelog '{fileName}' was not found in the effective bundle (parent plus existing amend files).");
				return null;
			}

			if (!force)
			{
				collector.EmitError(
					source.DisplayPath,
					$"Bundle contains '{fileName}' but with a different checksum than the sourced changelog. " +
					"Re-create the bundle or use --force to remove by file name only.");
				return null;
			}

			matchedEntry = nameMatches[0];
		}

		var exclusionChecksum = matchedEntry.File?.Checksum ?? fileChecksum;
		var appliedKey = BundleAmendMerger.BuildExclusionKey(new BundledEntry
		{
			File = new BundledFile
			{
				Name = fileName,
				Checksum = exclusionChecksum
			}
		});
		if (appliedExclusionKeys.Contains(appliedKey))
		{
			collector.EmitWarning(
				source.DisplayPath,
				$"Changelog '{fileName}' is already excluded by a prior amend file; skipping.");
			return RemoveExclusionResult.Skip.Instance;
		}

		return new RemoveExclusionResult.Add(new BundledEntry
		{
			File = new BundledFile
			{
				Name = fileName,
				Checksum = exclusionChecksum
			}
		});
	}

	private abstract record RemoveExclusionResult
	{
		public sealed record Add(BundledEntry Entry) : RemoveExclusionResult;
		public sealed record Skip : RemoveExclusionResult
		{
			public static readonly Skip Instance = new();
			private Skip() { }
		}
	}

	private async Task<(bool Ok, Bundle? Bundle)> TryDeserializeParentBundleAsync(
		string bundlePath,
		IDiagnosticsCollector collector,
		Cancel ctx)
	{
		try
		{
			var text = await _fileSystem.File.ReadAllTextAsync(bundlePath, ctx);
			var bundle = ReleaseNotesSerialization.DeserializeBundle(text);
			return (true, bundle);
		}
		catch (Exception ex) when (ex is not (OutOfMemoryException or StackOverflowException))
		{
			collector.EmitError(
				bundlePath,
				$"Failed to parse parent bundle YAML: {ex.Message}",
				ex);
			return (false, null);
		}
	}

	private int GetNextAmendNumber(string bundlePath)
	{
		var directory = _fileSystem.Path.GetDirectoryName(bundlePath) ?? string.Empty;
		var baseName = _fileSystem.Path.GetFileNameWithoutExtension(bundlePath);

		var existingAmendFiles = _fileSystem.Directory.GetFiles(directory, $"{baseName}.amend-*.y*ml");

		var maxNumber = existingAmendFiles
			.Select(file => AmendFileRegex().Match(file))
			.Where(match => match.Success && int.TryParse(match.Groups[1].Value, out _))
			.Select(match => int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture))
			.DefaultIfEmpty(0)
			.Max();

		return maxNumber + 1;
	}

	private string GenerateAmendFilePath(string bundlePath, int amendNumber)
	{
		var directory = _fileSystem.Path.GetDirectoryName(bundlePath) ?? string.Empty;
		var baseName = _fileSystem.Path.GetFileNameWithoutExtension(bundlePath);
		var extension = _fileSystem.Path.GetExtension(bundlePath);

		return _fileSystem.Path.Join(directory, $"{baseName}.amend-{amendNumber}{extension}");
	}

	private BundledEntry? LoadChangelogContent(
		IDiagnosticsCollector collector,
		SourcedChangelog source)
	{
		try
		{
			if (source.Content is null)
			{
				collector.EmitError(source.DisplayPath, "Cannot add a changelog without sourced YAML content.");
				return null;
			}

			var checksum = ChangelogBundlingService.ComputeSha1(source.Content);
			var normalizedYaml = ReleaseNotesSerialization.NormalizeYaml(source.Content);
			var entry = ReleaseNotesSerialization.DeserializeEntry(normalizedYaml);

			return new BundledEntry
			{
				File = new BundledFile
				{
					Name = source.FileName,
					Checksum = checksum
				},
				Type = entry.Type,
				Title = entry.Title,
				Products = entry.Products,
				Description = entry.Description,
				Impact = entry.Impact,
				Action = entry.Action,
				FeatureId = entry.FeatureId,
				Highlight = entry.Highlight,
				Subtype = entry.Subtype,
				Areas = entry.Areas,
				Prs = entry.Prs,
				Issues = entry.Issues
			};
		}
		catch (Exception ex) when (ex is not (OutOfMemoryException or StackOverflowException))
		{
			collector.EmitError(source.DisplayPath, $"Failed to load changelog file: {ex.Message}", ex);
			return null;
		}
	}

	private readonly record struct SourcedChangelog(string FileName, string? Content, string DisplayPath);

	/// <summary>
	/// Discovers amend files for a bundle
	/// </summary>
	public static IReadOnlyList<string> DiscoverAmendFiles(IFileSystem fileSystem, string bundlePath)
	{
		var directory = fileSystem.Path.GetDirectoryName(bundlePath) ?? string.Empty;
		var baseName = fileSystem.Path.GetFileNameWithoutExtension(bundlePath);

		if (!fileSystem.Directory.Exists(directory))
			return [];

		var amendFiles = fileSystem.Directory.GetFiles(directory, $"{baseName}.amend-*.y*ml")
			.OrderBy(BundleAmendMerger.GetAmendFileNumber)
			.ToList();

		return amendFiles;
	}
}
