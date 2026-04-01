// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;
using System.IO.Abstractions;
using System.Text;
using System.Text.RegularExpressions;
using Elastic.Changelog.Configuration;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Configuration.Changelog;
using Elastic.Documentation.Configuration.ReleaseNotes;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Extensions;
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
	public required IReadOnlyList<string> AddFiles { get; init; }

	/// <summary>
	/// Whether to resolve (copy contents) the added entries.
	/// When null, inferred from the original bundle.
	/// </summary>
	public bool? Resolve { get; init; }
}

/// <summary>
/// Service for amending changelog bundles with additional entries
/// </summary>
public partial class ChangelogBundleAmendService(
	ILoggerFactory logFactory,
	IFileSystem? fileSystem = null,
	IConfigurationContext? configurationContext = null) : IService
{
	private readonly ILogger _logger = logFactory.CreateLogger<ChangelogBundleAmendService>();
	private readonly IFileSystem _fileSystem = fileSystem ?? new FileSystem();
	private readonly ChangelogConfigurationLoader? _configLoader = configurationContext != null
		? new ChangelogConfigurationLoader(logFactory, configurationContext, fileSystem ?? new FileSystem())
		: null;

	[GeneratedRegex(@"\.amend-(\d+)\.ya?ml$", RegexOptions.IgnoreCase)]
	private static partial Regex AmendFileRegex();

	/// <summary>
	/// Amends a bundle with additional changelog entries, creating a new immutable amend file.
	/// </summary>
	public async Task<bool> AmendBundle(IDiagnosticsCollector collector, AmendBundleArguments input, Cancel ctx)
	{
		try
		{
			// Validate bundle file exists
			if (!_fileSystem.File.Exists(input.BundlePath))
			{
				var currentDir = _fileSystem.Directory.GetCurrentDirectory();
				collector.EmitError(
					input.BundlePath,
					$"Bundle file does not exist. Current directory: {currentDir}"
				);
				return false;
			}

			// Validate add files
			if (input.AddFiles.Count == 0)
			{
				collector.EmitError(string.Empty, "At least one file must be specified with --add");
				return false;
			}

			// Validate all add files exist
			var addFilePaths = new List<string>();
			foreach (var addFile in input.AddFiles)
			{
				if (!_fileSystem.File.Exists(addFile))
				{
					var currentDir = _fileSystem.Directory.GetCurrentDirectory();
					collector.EmitError(
						addFile,
						$"File does not exist. Current directory: {currentDir}. " +
						"Tip: Specify multiple files as comma-separated values (e.g., --add \"file1.yaml,file2.yaml\"). " +
						"Paths support tilde (~) expansion and can be relative or absolute."
					);
					return false;
				}
				addFilePaths.Add(addFile);
			}

			// Resolve flag: explicit CLI wins; otherwise infer from parent bundle (single read + deserialize).
			Bundle? parentBundleFromInfer = null;
			bool shouldResolve;
			if (input.Resolve.HasValue)
				shouldResolve = input.Resolve.Value;
			else
			{
				var (ok, inferredBundle) = await TryDeserializeParentBundleAsync(
					input.BundlePath,
					collector,
					emitParseErrorToCollector: false,
					ctx);
				if (!ok)
					shouldResolve = false;
				else
				{
					parentBundleFromInfer = inferredBundle;
					shouldResolve = inferredBundle!.IsResolved;
					_logger.LogInformation("Inferred resolve={Resolve} from original bundle", shouldResolve);
				}
			}

			// Determine the next amend file number
			var nextAmendNumber = GetNextAmendNumber(input.BundlePath);
			var amendFilePath = GenerateAmendFilePath(input.BundlePath, nextAmendNumber);

			_logger.LogInformation("Creating amend file: {AmendFilePath} (resolve={Resolve})", amendFilePath, shouldResolve);

			ChangelogConfiguration? changelogConfig = null;
			if (_configLoader != null)
				changelogConfig = await _configLoader.LoadChangelogConfiguration(collector, null, ctx);

			var sanitizePrivateLinks = changelogConfig?.Bundle?.SanitizePrivateLinks == true;
			Bundle? parentBundleForSanitize = null;
			AssemblyConfiguration? assemblyForSanitize = null;

			if (sanitizePrivateLinks)
			{
				if (configurationContext == null)
				{
					collector.EmitError(
						string.Empty,
						"Private link sanitization requires assembler configuration. Run docs-builder with a valid configuration context.");
					return false;
				}

				if (parentBundleFromInfer != null)
					parentBundleForSanitize = parentBundleFromInfer;
				else
				{
					var (ok, loaded) = await TryDeserializeParentBundleAsync(
						input.BundlePath,
						collector,
						emitParseErrorToCollector: true,
						ctx);
					if (!ok)
						return false;
					ArgumentNullException.ThrowIfNull(loaded);
					parentBundleForSanitize = loaded;
				}

				ArgumentNullException.ThrowIfNull(parentBundleForSanitize);
				if (!parentBundleForSanitize.IsResolved)
				{
					collector.EmitError(
						string.Empty,
						"Private link sanitization requires the parent bundle to be resolved (inline entry content). " +
						"Re-create the bundle with resolve enabled, or disable bundle.sanitize_private_links.");
					return false;
				}

				var assemblyYaml = configurationContext.ConfigurationFileProvider.AssemblerFile.ReadToEnd();
				try
				{
					assemblyForSanitize = AssemblyConfiguration.Deserialize(assemblyYaml, skipPrivateRepositories: false);
				}
				catch (Exception ex) when (ex is not (OutOfMemoryException or StackOverflowException))
				{
					collector.EmitError(
						string.Empty,
						$"Failed to parse assembler configuration YAML: {ex.Message}",
						ex);
					return false;
				}

				var owner = parentBundleForSanitize.Products.Count > 0 ? parentBundleForSanitize.Products[0].Owner ?? "elastic" : "elastic";
				var repo = parentBundleForSanitize.Products.Count > 0 ? parentBundleForSanitize.Products[0].Repo : null;
				if (!PrivateChangelogLinkSanitizer.TrySanitizeBundle(
					collector,
					parentBundleForSanitize,
					assemblyForSanitize,
					owner,
					repo,
					out _,
					out var parentHadUnsanitizedLinks))
					return false;

				if (parentHadUnsanitizedLinks)
				{
					collector.EmitError(
						string.Empty,
						"Private link sanitization requires the parent bundle to already reflect sanitized PR/issue references. " +
						"Re-create the parent bundle with bundle.sanitize_private_links enabled and resolve enabled, " +
						"or disable bundle.sanitize_private_links for amend.");
					return false;
				}
			}

			if (sanitizePrivateLinks && !shouldResolve)
			{
				collector.EmitError(
					string.Empty,
					"Private link sanitization requires resolved amend content. Use --resolve or ensure the original bundle is resolved, or disable bundle.sanitize_private_links.");
				return false;
			}

			// Load and process the files to add
			var entries = new List<BundledEntry>();
			foreach (var filePath in addFilePaths)
			{
				var entry = await LoadChangelogFileAsync(collector, filePath, shouldResolve, ctx);
				if (entry == null)
					return false;
				entries.Add(entry);
			}

			// Create the amend bundle
			var amendBundle = new Bundle
			{
				Products = [], // Amend files don't have products, they inherit from the original bundle
				Entries = entries
			};

			var bundleForWrite = amendBundle;
			if (sanitizePrivateLinks && shouldResolve)
			{
				ArgumentNullException.ThrowIfNull(parentBundleForSanitize);
				ArgumentNullException.ThrowIfNull(assemblyForSanitize);
				var owner = parentBundleForSanitize.Products.Count > 0 ? parentBundleForSanitize.Products[0].Owner ?? "elastic" : "elastic";
				var repo = parentBundleForSanitize.Products.Count > 0 ? parentBundleForSanitize.Products[0].Repo : null;

				if (!PrivateChangelogLinkSanitizer.TrySanitizeBundle(
					collector,
					amendBundle,
					assemblyForSanitize,
					owner,
					repo,
					out var sanitized,
					out _))
					return false;
				bundleForWrite = sanitized;
			}

			// Serialize and write the amend file
			var yaml = ReleaseNotesSerialization.SerializeBundle(bundleForWrite);

			// Ensure output directory exists
			var outputDir = _fileSystem.Path.GetDirectoryName(amendFilePath);
			if (!string.IsNullOrWhiteSpace(outputDir) && !_fileSystem.Directory.Exists(outputDir))
				_ = _fileSystem.Directory.CreateDirectory(outputDir);

			await _fileSystem.File.WriteAllTextAsync(amendFilePath, yaml, Encoding.UTF8, ctx);
			_logger.LogInformation("Created amend file: {AmendFilePath} with {Count} entries", amendFilePath, entries.Count);

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

		// Find existing amend files
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

			// Compute checksum
			var checksum = ChangelogBundlingService.ComputeSha1(content);

			if (!resolve)
			{
				// Just return file reference
				return new BundledEntry
				{
					File = new BundledFile
					{
						Name = fileName,
						Checksum = checksum
					}
				};
			}

			// Parse the changelog file and include full entry data
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
			.OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
			.ToList();

		return amendFiles;
	}
}
