// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Concurrent;
using System.IO.Abstractions;
using Elastic.Documentation.Extensions;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Configuration.Diagram;

/// <summary>
/// Information about a diagram that needs to be cached
/// </summary>
/// <param name="OutputFile">The intended cache output file location</param>
/// <param name="EncodedUrl">Encoded Kroki URL for downloading</param>
public record DiagramCacheInfo(IFileInfo OutputFile, string EncodedUrl);

/// Registry to track active diagrams and manage cleanup of outdated cached files
public class DiagramRegistry(ILoggerFactory logFactory, BuildContext context) : IDisposable
{
	private readonly ILogger<DiagramRegistry> _logger = logFactory.CreateLogger<DiagramRegistry>();
	private readonly ConcurrentDictionary<string, DiagramCacheInfo> _diagramsToCache = new();
	private readonly IFileSystem _writeFileSystem = context.WriteFileSystem;
	private readonly IFileSystem _readFileSystem = context.ReadFileSystem;
	private readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(30) };

	/// <summary>
	/// Register a diagram for caching (collects info for later batch processing)
	/// </summary>
	/// <param name="localSvgPath">The local SVG path relative to the output directory</param>
	/// <param name="encodedUrl">The encoded Kroki URL for downloading</param>
	/// <param name="outputDirectory">The full path to the output directory</param>
	public void RegisterDiagramForCaching(IFileInfo outputFile, string encodedUrl)
	{
		if (string.IsNullOrEmpty(encodedUrl))
			return;

		if (!outputFile.IsSubPathOf(context.DocumentationOutputDirectory))
			return;

		_ = _diagramsToCache.TryAdd(outputFile.FullName, new DiagramCacheInfo(outputFile, encodedUrl));
	}

	/// <summary>
	/// Create cached diagram files by downloading from Kroki in parallel
	/// </summary>
	/// <returns>Number of diagrams downloaded</returns>
	public async Task<int> CreateDiagramCachedFiles(Cancel ctx)
	{
		if (_diagramsToCache.IsEmpty)
			return 0;

		var downloadCount = 0;

		await Parallel.ForEachAsync(_diagramsToCache.Values, new ParallelOptions
		{
			MaxDegreeOfParallelism = Environment.ProcessorCount,
			CancellationToken = ctx
		}, async (diagramInfo, ct) =>
		{
			var localPath = _readFileSystem.Path.GetRelativePath(context.DocumentationOutputDirectory.FullName, diagramInfo.OutputFile.FullName);

			try
			{
				if (!diagramInfo.OutputFile.IsSubPathOf(context.DocumentationOutputDirectory))
					return;

				// Skip if the file already exists
				if (_readFileSystem.File.Exists(diagramInfo.OutputFile.FullName))
					return;

				// Create the directory if needed
				var directory = _writeFileSystem.Path.GetDirectoryName(diagramInfo.OutputFile.FullName);
				if (directory != null && !_writeFileSystem.Directory.Exists(directory))
					_ = _writeFileSystem.Directory.CreateDirectory(directory);

				// Download SVG content
				var svgContent = await _httpClient.GetStringAsync(diagramInfo.EncodedUrl, ct);

				// Validate SVG content
				if (string.IsNullOrWhiteSpace(svgContent) || !svgContent.Contains("<svg", StringComparison.OrdinalIgnoreCase))
				{
					_logger.LogWarning("Invalid SVG content received for diagram {LocalPath}", localPath);
					return;
				}

				// Write atomically using a temp file
				var tempPath = $"{diagramInfo.OutputFile.FullName}.tmp";
				await _writeFileSystem.File.WriteAllTextAsync(tempPath, svgContent, ct);
				_writeFileSystem.File.Move(tempPath, diagramInfo.OutputFile.FullName);

				_ = Interlocked.Increment(ref downloadCount);
				_logger.LogDebug("Downloaded diagram: {LocalPath}", localPath);
			}
			catch (HttpRequestException ex)
			{
				_logger.LogWarning("Failed to download diagram {LocalPath}: {Error}", localPath, ex.Message);
			}
			catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
			{
				_logger.LogWarning("Timeout downloading diagram {LocalPath}", localPath);
			}
			catch (Exception ex)
			{
				_logger.LogWarning("Unexpected error downloading diagram {LocalPath}: {Error}", localPath, ex.Message);
			}
		});

		if (downloadCount > 0)
			_logger.LogInformation("Downloaded {DownloadCount} diagram files from Kroki", downloadCount);

		return downloadCount;
	}

	/// <summary>
	/// Clean up unused diagram files from the cache directory
	/// </summary>
	/// <returns>Number of files cleaned up</returns>
	public int CleanupUnusedDiagrams()
	{
		if (!_readFileSystem.Directory.Exists(context.DocumentationOutputDirectory.FullName))
			return 0;
		var folders = _writeFileSystem.Directory.GetDirectories(context.DocumentationOutputDirectory.FullName, "generated-graphs", SearchOption.AllDirectories);
		var existingFiles = folders
			.Select(f => (Folder: f, Files: _writeFileSystem.Directory.GetFiles(f, "*.svg", SearchOption.TopDirectoryOnly)))
			.ToArray();
		if (existingFiles.Length == 0)
			return 0;
		var cleanedCount = 0;

		try
		{
			foreach (var (folder, files) in existingFiles)
			{
				foreach (var file in files)
				{
					if (_diagramsToCache.ContainsKey(file))
						continue;
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
				// Clean up empty directories
				CleanupEmptyDirectories(folder);
			}
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
			var folder = _writeFileSystem.DirectoryInfo.New(directory);
			if (!folder.IsSubPathOf(context.DocumentationOutputDirectory))
				return;

			if (folder.Name != "generated-graphs")
				return;

			if (_writeFileSystem.Directory.EnumerateFileSystemEntries(folder.FullName).Any())
				return;

			_writeFileSystem.Directory.Delete(folder.FullName);

			var parentFolder = folder.Parent;
			if (parentFolder is null || parentFolder.Name != "images")
				return;

			if (_writeFileSystem.Directory.EnumerateFileSystemEntries(parentFolder.FullName).Any())
				return;

			_writeFileSystem.Directory.Delete(folder.FullName);
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
