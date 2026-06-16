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
using Elastic.Documentation.ReleaseNotes;
using Elastic.Documentation.Services;
using Microsoft.Extensions.Logging;
using Nullean.ScopedFileSystem;

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
	/// Whether to resolve (copy contents) the added entries.
	/// When null, inferred from the original bundle.
	/// </summary>
	public bool? Resolve { get; init; }

	/// <summary>
	/// Remove by file name when the bundle checksum does not match the file on disk.
	/// </summary>
	public bool Force { get; init; }

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
	ScopedFileSystem? fileSystem = null,
	IConfigurationContext? configurationContext = null) : IService
{
	/// <summary>
	/// UTF-8 encoding without BOM for writing YAML files.
	/// </summary>
	private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

	private readonly ILogger _logger = logFactory.CreateLogger<ChangelogBundleAmendService>();
	private readonly IFileSystem _fileSystem = fileSystem ?? FileSystemFactory.RealRead;
	private readonly ChangelogConfigurationLoader? _configLoader = configurationContext != null
		? new ChangelogConfigurationLoader(logFactory, configurationContext, fileSystem ?? FileSystemFactory.RealRead)
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

			var addFilePaths = ValidateInputFiles(collector, input.AddFiles, "--add");
			if (addFilePaths == null)
				return false;

			var removeFilePaths = ValidateInputFiles(collector, input.RemoveFiles, "--remove");
			if (removeFilePaths == null)
				return false;

			var (parentOk, parentBundle) = await TryDeserializeParentBundleAsync(
				input.BundlePath,
				collector,
				emitParseErrorToCollector: true,
				ctx);
			if (!parentOk || parentBundle == null)
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
			foreach (var removeFilePath in removeFilePaths!)
			{
				var exclusion = await BuildExclusionEntryAsync(
					collector,
					removeFilePath,
					effectiveEntries,
					appliedExclusionKeys,
					input.Force,
					ctx);
				if (exclusion == null)
					return false;
				if (exclusion is RemoveExclusionResult.Skip)
					continue;

				var entry = ((RemoveExclusionResult.Add)exclusion).Entry;
				excludeEntries.Add(entry);
				_ = appliedExclusionKeys.Add(BundleAmendMerger.BuildExclusionKey(entry));
			}

			bool shouldResolve;
			if (input.Resolve.HasValue)
				shouldResolve = input.Resolve.Value;
			else
			{
				shouldResolve = parentBundle.IsResolved;
				_logger.LogInformation("Inferred resolve={Resolve} from original bundle", shouldResolve);
			}
			var entries = new List<BundledEntry>();
			if (addFilePaths!.Count > 0)
			{
				ChangelogConfiguration? changelogConfig = null;
				if (_configLoader != null)
					changelogConfig = await _configLoader.LoadChangelogConfiguration(collector, null, ctx);

				var linkAllowRepos = changelogConfig?.Bundle?.LinkAllowRepos;
				var linkAllowlistActive = linkAllowRepos != null;

				if (linkAllowlistActive && !parentBundle.IsResolved)
				{
					collector.EmitError(
						string.Empty,
						"bundle.link_allow_repos requires the parent bundle to be resolved (inline entry content). " +
						"Re-create the bundle with resolve enabled, or remove bundle.link_allow_repos.");
					return false;
				}

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
							"Re-create the parent bundle with the same bundle.link_allow_repos and resolve enabled, " +
							"or remove bundle.link_allow_repos for amend.");
						return false;
					}
				}

				if (linkAllowlistActive && !shouldResolve)
				{
					collector.EmitError(
						string.Empty,
						"bundle.link_allow_repos requires resolved amend content. Use --resolve or ensure the original bundle is resolved, or remove bundle.link_allow_repos.");
					return false;
				}

				foreach (var filePath in addFilePaths)
				{
					var entry = await LoadChangelogFileAsync(collector, filePath, shouldResolve, ctx);
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
				"Creating amend file: {AmendFilePath} (exclude={ExcludeCount}, add={AddCount}, resolve={Resolve})",
				amendFilePath,
				excludeEntries.Count,
				entries.Count,
				shouldResolve);

			var amendBundle = new Bundle
			{
				Products = [],
				ExcludeEntries = excludeEntries,
				Entries = entries
			};

			var bundleForWrite = amendBundle;
			if (entries.Count > 0 && shouldResolve && _configLoader != null)
			{
				var changelogConfig = await _configLoader.LoadChangelogConfiguration(collector, null, ctx);
				var linkAllowRepos = changelogConfig?.Bundle?.LinkAllowRepos;
				if (linkAllowRepos != null)
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

	private List<string>? ValidateInputFiles(
		IDiagnosticsCollector collector,
		IReadOnlyList<string> files,
		string optionName)
	{
		if (files.Count == 0)
			return [];

		var validatedPaths = new List<string>();
		foreach (var file in files)
		{
			if (!_fileSystem.File.Exists(file))
			{
				var currentDir = _fileSystem.Directory.GetCurrentDirectory();
				collector.EmitError(
					file,
					$"File does not exist. Current directory: {currentDir}. " +
					$"Tip: Repeat {optionName} for each file, or use comma-separated values (e.g., {optionName} \"file1.yaml,file2.yaml\"). " +
					"Paths support tilde (~) expansion and can be relative or absolute."
				);
				return null;
			}
			validatedPaths.Add(file);
		}

		return validatedPaths;
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

	private async Task<RemoveExclusionResult?> BuildExclusionEntryAsync(
		IDiagnosticsCollector collector,
		string removeFilePath,
		IReadOnlyList<BundledEntry> effectiveEntries,
		HashSet<string> appliedExclusionKeys,
		bool force,
		Cancel ctx)
	{
		var fileName = _fileSystem.Path.GetFileName(removeFilePath);
		var content = await _fileSystem.File.ReadAllTextAsync(removeFilePath, ctx);
		var fileChecksum = ChangelogBundlingService.ComputeSha1(content);

		var strictExclusion = new BundledEntry
		{
			File = new BundledFile
			{
				Name = fileName,
				Checksum = fileChecksum
			}
		};

		var exclusionKey = BundleAmendMerger.BuildExclusionKey(strictExclusion);
		if (appliedExclusionKeys.Contains(exclusionKey))
		{
			collector.EmitWarning(
				removeFilePath,
				$"Changelog '{fileName}' is already excluded by a prior amend file; skipping.");
			return RemoveExclusionResult.Skip.Instance;
		}

		var strictMatches = effectiveEntries
			.Where(entry => BundleAmendMerger.EntryMatchesExclusion(entry, strictExclusion))
			.ToList();

		var matchedEntry = strictMatches.Count > 0 ? strictMatches[0] : null;

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
					removeFilePath,
					$"Changelog '{fileName}' was not found in the effective bundle (parent plus existing amend files).");
				return null;
			}

			if (!force)
			{
				collector.EmitError(
					removeFilePath,
					$"Bundle contains '{fileName}' but with a different checksum than the file on disk. " +
					"Re-create the bundle or use --force to remove by file name only.");
				return null;
			}

			matchedEntry = nameMatches[0];
		}

		var exclusionChecksum = matchedEntry.File?.Checksum ?? fileChecksum;
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
		bool emitParseErrorToCollector,
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
			if (emitParseErrorToCollector)
			{
				collector.EmitError(
					bundlePath,
					$"Failed to parse parent bundle YAML: {ex.Message}",
					ex);
			}
			else
				_logger.LogWarning(ex, "Could not read original bundle to infer resolve; defaulting to false");

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

	private async Task<BundledEntry?> LoadChangelogFileAsync(
		IDiagnosticsCollector collector,
		string filePath,
		bool resolve,
		Cancel ctx)
	{
		try
		{
			var fileName = _fileSystem.Path.GetFileName(filePath);
			var content = await _fileSystem.File.ReadAllTextAsync(filePath, ctx);

			var checksum = ChangelogBundlingService.ComputeSha1(content);

			if (!resolve)
			{
				return new BundledEntry
				{
					File = new BundledFile
					{
						Name = fileName,
						Checksum = checksum
					}
				};
			}

			var normalizedYaml = ReleaseNotesSerialization.NormalizeYaml(content);
			var entry = ReleaseNotesSerialization.DeserializeEntry(normalizedYaml);

			return new BundledEntry
			{
				File = new BundledFile
				{
					Name = fileName,
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
			collector.EmitError(filePath, $"Failed to load changelog file: {ex.Message}", ex);
			return null;
		}
	}

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
