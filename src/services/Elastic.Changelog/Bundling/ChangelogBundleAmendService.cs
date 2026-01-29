// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;
using System.IO.Abstractions;
using System.Text;
using System.Text.RegularExpressions;
using Elastic.Changelog.Serialization;
using Elastic.Documentation.Diagnostics;
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
	/// Whether to resolve (copy contents) the added entries
	/// </summary>
	public bool Resolve { get; init; }
}

/// <summary>
/// Service for amending changelog bundles with additional entries
/// </summary>
public partial class ChangelogBundleAmendService(ILoggerFactory logFactory, IFileSystem? fileSystem = null) : IService
{
	private readonly ILogger _logger = logFactory.CreateLogger<ChangelogBundleAmendService>();
	private readonly IFileSystem _fileSystem = fileSystem ?? new FileSystem();

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
				collector.EmitError(input.BundlePath, "Bundle file does not exist");
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
					collector.EmitError(addFile, "File does not exist");
					return false;
				}
				addFilePaths.Add(addFile);
			}

			// Determine the next amend file number
			var nextAmendNumber = GetNextAmendNumber(input.BundlePath);
			var amendFilePath = GenerateAmendFilePath(input.BundlePath, nextAmendNumber);

			_logger.LogInformation("Creating amend file: {AmendFilePath}", amendFilePath);

			// Load and process the files to add
			var entries = new List<BundledEntry>();
			foreach (var filePath in addFilePaths)
			{
				var entry = await LoadChangelogFileAsync(collector, filePath, input.Resolve, ctx);
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

			// Serialize and write the amend file
			var yaml = ChangelogYamlSerialization.SerializeBundle(amendBundle);

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

		return _fileSystem.Path.Combine(directory, $"{baseName}.amend-{amendNumber}{extension}");
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
			// Filter out comment lines
			var yamlLines = content.Split('\n');
			var yamlWithoutComments = string.Join('\n', yamlLines.Where(line => !line.TrimStart().StartsWith('#')));

			// Normalize "version:" to "target:" in products section
			var normalizedYaml = ChangelogBundlingService.VersionToTargetRegex().Replace(yamlWithoutComments, "$1target:");

			var entry = ChangelogYamlSerialization.DeserializeEntry(normalizedYaml);

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
				Pr = entry.Pr,
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
