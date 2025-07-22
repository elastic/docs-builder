// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Concurrent;
using System.IO.Abstractions;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Configuration.Diagram;

/// <summary>
/// Information about a diagram that needs to be cached
/// </summary>
/// <param name="LocalSvgPath">Local SVG path relative to output directory</param>
/// <param name="EncodedUrl">Encoded Kroki URL for downloading</param>
/// <param name="OutputDirectory">Full path to output directory</param>
public record DiagramCacheInfo(string LocalSvgPath, string EncodedUrl, string OutputDirectory);

/// <summary>
/// Registry to track active diagrams and manage cleanup of outdated cached files
/// </summary>
/// <param name="writeFileSystem">File system for write/delete operations during cleanup</param>
public class DiagramRegistry(IFileSystem writeFileSystem) : IDisposable
{
	private readonly ConcurrentDictionary<string, bool> _activeDiagrams = new();
	private readonly ConcurrentDictionary<string, DiagramCacheInfo> _diagramsToCache = new();
	private readonly IFileSystem _writeFileSystem = writeFileSystem;
	private readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(30) };

	/// <summary>
	/// Register a diagram for caching (collects info for later batch processing)
	/// </summary>
	/// <param name="localSvgPath">The local SVG path relative to output directory</param>
	/// <param name="encodedUrl">The encoded Kroki URL for downloading</param>
	/// <param name="outputDirectory">The full path to output directory</param>
	public void RegisterDiagramForCaching(string localSvgPath, string encodedUrl, string outputDirectory)
	{
		if (string.IsNullOrEmpty(localSvgPath) || string.IsNullOrEmpty(encodedUrl))
			return;

		_ = _activeDiagrams.TryAdd(localSvgPath, true);
		_ = _diagramsToCache.TryAdd(localSvgPath, new DiagramCacheInfo(localSvgPath, encodedUrl, outputDirectory));
	}

	/// <summary>
	/// Clear all registered diagrams (called at start of build)
	/// </summary>
	public void Clear()
	{
		_activeDiagrams.Clear();
		_diagramsToCache.Clear();
	}

	/// <summary>
	/// Create cached diagram files by downloading from Kroki in parallel
	/// </summary>
	/// <param name="logger">Logger for reporting download activity</param>
	/// <param name="readFileSystem">File system for checking existing files</param>
	/// <returns>Number of diagrams downloaded</returns>
	public async Task<int> CreateDiagramCachedFiles(ILogger logger, IFileSystem readFileSystem)
	{
		if (_diagramsToCache.IsEmpty)
			return 0;

		var downloadCount = 0;

		await Parallel.ForEachAsync(_diagramsToCache.Values, new ParallelOptions
		{
			MaxDegreeOfParallelism = Environment.ProcessorCount,
			CancellationToken = CancellationToken.None
		}, async (diagramInfo, ct) =>
		{
			try
			{
				var fullPath = _writeFileSystem.Path.Combine(diagramInfo.OutputDirectory, diagramInfo.LocalSvgPath);

				// Skip if file already exists
				if (readFileSystem.File.Exists(fullPath))
					return;

				// Create directory if needed
				var directory = _writeFileSystem.Path.GetDirectoryName(fullPath);
				if (directory != null && !_writeFileSystem.Directory.Exists(directory))
				{
					_ = _writeFileSystem.Directory.CreateDirectory(directory);
				}

				// Download SVG content
				var svgContent = await _httpClient.GetStringAsync(diagramInfo.EncodedUrl, ct);

				// Validate SVG content
				if (string.IsNullOrWhiteSpace(svgContent) || !svgContent.Contains("<svg", StringComparison.OrdinalIgnoreCase))
				{
					logger.LogWarning("Invalid SVG content received for diagram {LocalPath}", diagramInfo.LocalSvgPath);
					return;
				}

				// Write atomically using temp file
				var tempPath = fullPath + ".tmp";
				await _writeFileSystem.File.WriteAllTextAsync(tempPath, svgContent, ct);
				_writeFileSystem.File.Move(tempPath, fullPath);

				_ = Interlocked.Increment(ref downloadCount);
				logger.LogDebug("Downloaded diagram: {LocalPath}", diagramInfo.LocalSvgPath);
			}
			catch (HttpRequestException ex)
			{
				logger.LogWarning("Failed to download diagram {LocalPath}: {Error}", diagramInfo.LocalSvgPath, ex.Message);
			}
			catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
			{
				logger.LogWarning("Timeout downloading diagram {LocalPath}", diagramInfo.LocalSvgPath);
			}
			catch (Exception ex)
			{
				logger.LogWarning("Unexpected error downloading diagram {LocalPath}: {Error}", diagramInfo.LocalSvgPath, ex.Message);
			}
		});

		if (downloadCount > 0)
		{
			logger.LogInformation("Downloaded {DownloadCount} diagram files from Kroki", downloadCount);
		}

		return downloadCount;
	}

	/// <summary>
	/// Clean up unused diagram files from the cache directory
	/// </summary>
	/// <param name="outputDirectory">The output directory containing cached diagrams</param>
	/// <returns>Number of files cleaned up</returns>
	public int CleanupUnusedDiagrams(IDirectoryInfo outputDirectory)
	{
		var graphsDir = _writeFileSystem.Path.Combine(outputDirectory.FullName, "images", "generated-graphs");
		if (!_writeFileSystem.Directory.Exists(graphsDir))
			return 0;

		var existingFiles = _writeFileSystem.Directory.GetFiles(graphsDir, "*.svg", SearchOption.AllDirectories);
		var cleanedCount = 0;

		try
		{
			foreach (var file in existingFiles)
			{
				var relativePath = _writeFileSystem.Path.GetRelativePath(outputDirectory.FullName, file);
				var normalizedPath = relativePath.Replace(_writeFileSystem.Path.DirectorySeparatorChar, '/');

				if (!_activeDiagrams.ContainsKey(normalizedPath))
				{
					try
					{
						_writeFileSystem.File.Delete(file);
						cleanedCount++;
					}
					catch
					{
						// Silent failure - cleanup is opportunistic
					}
				}
			}

			// Clean up empty directories
			CleanupEmptyDirectories(graphsDir);
		}
		catch
		{
			// Silent failure - cleanup is opportunistic
		}

		return cleanedCount;
	}

	private void CleanupEmptyDirectories(string directory)
	{
		try
		{
			foreach (var subDir in _writeFileSystem.Directory.GetDirectories(directory))
			{
				CleanupEmptyDirectories(subDir);

				if (!_writeFileSystem.Directory.EnumerateFileSystemEntries(subDir).Any())
				{
					try
					{
						_writeFileSystem.Directory.Delete(subDir);
					}
					catch
					{
						// Silent failure - cleanup is opportunistic
					}
				}
			}
		}
		catch
		{
			// Silent failure - cleanup is opportunistic
		}
	}

	/// <summary>
	/// Dispose of resources, including the HttpClient
	/// </summary>
	public void Dispose()
	{
		_httpClient.Dispose();
		GC.SuppressFinalize(this);
	}
}
