// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;

namespace Elastic.Markdown.Myst.Directives.Diagram;

/// <summary>
/// Registry to track active diagrams and manage cleanup of outdated cached files
/// </summary>
public static class DiagramRegistry
{
	private static readonly HashSet<string> ActiveDiagrams = [];
	private static readonly Lock Lock = new();

	/// <summary>
	/// Register a diagram as active during the current build
	/// </summary>
	/// <param name="localSvgPath">The local SVG path relative to output directory</param>
	public static void RegisterDiagram(string localSvgPath)
	{
		if (string.IsNullOrEmpty(localSvgPath))
			return;

		lock (Lock)
		{
			_ = ActiveDiagrams.Add(localSvgPath);
		}
	}

	/// <summary>
	/// Get all currently registered active diagrams
	/// </summary>
	/// <returns>Collection of active diagram paths</returns>
	public static IReadOnlyCollection<string> GetActiveDiagrams()
	{
		lock (Lock)
		{
			return ActiveDiagrams.ToArray();
		}
	}

	/// <summary>
	/// Clear all registered diagrams (typically called at start of build)
	/// </summary>
	public static void Clear()
	{
		lock (Lock)
		{
			ActiveDiagrams.Clear();
		}
	}

	/// <summary>
	/// Clean up unused diagram files from the output directory
	/// </summary>
	/// <param name="outputDirectory">The output directory path</param>
	/// <returns>Number of files cleaned up</returns>
	public static int CleanupUnusedDiagrams(string outputDirectory) =>
		CleanupUnusedDiagrams(outputDirectory, new FileSystem());

	/// <summary>
	/// Clean up unused diagram files from the output directory
	/// </summary>
	/// <param name="outputDirectory">The output directory path</param>
	/// <param name="fileSystem">File system abstraction for testing</param>
	/// <returns>Number of files cleaned up</returns>
	public static int CleanupUnusedDiagrams(string outputDirectory, IFileSystem fileSystem)
	{
		if (string.IsNullOrEmpty(outputDirectory))
			return 0;

		var graphsDir = fileSystem.Path.Combine(outputDirectory, "images", "generated-graphs");
		if (!fileSystem.Directory.Exists(graphsDir))
			return 0;

		var cleanedCount = 0;
		var activePaths = GetActiveDiagrams();

		try
		{
			var existingFiles = fileSystem.Directory.GetFiles(graphsDir, "*.svg", SearchOption.AllDirectories);

			foreach (var file in existingFiles)
			{
				var relativePath = fileSystem.Path.GetRelativePath(outputDirectory, file);

				// Convert to forward slashes for consistent comparison
				var normalizedPath = relativePath.Replace(fileSystem.Path.DirectorySeparatorChar, '/');

				if (!activePaths.Any(active => active.Replace(fileSystem.Path.DirectorySeparatorChar, '/') == normalizedPath))
				{
					try
					{
						fileSystem.File.Delete(file);
						cleanedCount++;
					}
					catch
					{
						// Silent failure - cleanup is opportunistic
					}
				}
			}

			// Clean up empty directories
			CleanupEmptyDirectories(graphsDir, fileSystem);
		}
		catch
		{
			// Silent failure - cleanup is opportunistic
		}

		return cleanedCount;
	}

	/// <summary>
	/// Remove empty directories recursively
	/// </summary>
	/// <param name="directory">Directory to clean up</param>
	/// <param name="fileSystem">File system abstraction</param>
	private static void CleanupEmptyDirectories(string directory, IFileSystem fileSystem)
	{
		try
		{
			if (!fileSystem.Directory.Exists(directory))
				return;

			// Clean up subdirectories first
			foreach (var subDir in fileSystem.Directory.GetDirectories(directory))
			{
				CleanupEmptyDirectories(subDir, fileSystem);
			}

			// Remove directory if it's empty
			if (!fileSystem.Directory.EnumerateFileSystemEntries(directory).Any())
			{
				fileSystem.Directory.Delete(directory);
			}
		}
		catch
		{
			// Silent failure - cleanup is opportunistic
		}
	}
}
