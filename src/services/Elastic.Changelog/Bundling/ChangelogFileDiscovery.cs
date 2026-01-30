// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Microsoft.Extensions.Logging;

namespace Elastic.Changelog.Bundling;

/// <summary>
/// Service for discovering changelog YAML files in a directory
/// </summary>
public class ChangelogFileDiscovery(IFileSystem fileSystem, ILogger logger)
{
	/// <summary>
	/// Discovers changelog YAML files in the specified directory, excluding bundle files and the output file.
	/// </summary>
	public async Task<IReadOnlyList<string>> DiscoverChangelogFilesAsync(string directory, string outputPath, Cancel ctx)
	{
		var outputFileName = fileSystem.Path.GetFileName(outputPath);

		// Read all YAML files from directory
		var allYamlFiles = fileSystem.Directory.GetFiles(directory, "*.yaml", SearchOption.TopDirectoryOnly)
			.Concat(fileSystem.Directory.GetFiles(directory, "*.yml", SearchOption.TopDirectoryOnly))
			.ToList();

		var yamlFiles = new List<string>();
		foreach (var filePath in allYamlFiles)
		{
			var fileName = fileSystem.Path.GetFileName(filePath);

			// Exclude the output file
			if (fileName.Equals(outputFileName, StringComparison.OrdinalIgnoreCase))
				continue;

			// Check if file is a bundle file by looking for "entries:" key (unique to bundle files)
			if (await IsBundleFileAsync(filePath, fileName, ctx))
				continue;

			yamlFiles.Add(filePath);
		}

		return yamlFiles;
	}

	private async Task<bool> IsBundleFileAsync(string filePath, string fileName, Cancel ctx)
	{
		try
		{
			var fileContent = await fileSystem.File.ReadAllTextAsync(filePath, ctx);
			// Bundle files have "entries:" at root level, changelog files don't
			if (fileContent.Contains("entries:", StringComparison.Ordinal) &&
				fileContent.Contains("products:", StringComparison.Ordinal))
			{
				logger.LogDebug("Skipping bundle file: {FileName}", fileName);
				return true;
			}
		}
		catch (Exception ex) when (ex is not (OutOfMemoryException or StackOverflowException or ThreadAbortException))
		{
			// If we can't read the file, skip it
			logger.LogWarning(ex, "Failed to read file {FileName} for bundle detection", fileName);
			return true; // Skip files we can't read
		}

		return false;
	}
}
