// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using Elastic.Documentation.FileSystems;
using Microsoft.Extensions.Logging;

namespace Elastic.Changelog.Evaluation;

/// <summary>
/// Writes <see cref="GithubDecisionMetadata"/> to the conventional decision-artifact location
/// (<c>.artifacts/changelog-decision/metadata.json</c>) so a downstream <c>workflow_run</c> job can
/// pick it up for comment rendering.
/// <para>
/// The path is relative to the working root managed by <see cref="RunnerTempFileSystem"/>.
/// <c>.artifacts</c> is already allow-listed as a hidden folder by that file system, so no additional
/// scope configuration is needed.
/// </para>
/// </summary>
internal class GithubDecisionMetadataWriter(ILoggerFactory logFactory, IRunnerTempFileSystem fileSystem)
{
	/// <summary>
	/// Conventional artifact directory, relative to the checkout root.
	/// Consumers (action.yml files, docs) should reference this constant rather than hard-coding the path.
	/// </summary>
	internal const string ArtifactDir = ".artifacts/changelog-decision";

	/// <summary>The metadata filename within <see cref="ArtifactDir"/>.</summary>
	internal const string MetadataFilename = "metadata.json";

	private readonly ILogger _logger = logFactory.CreateLogger<GithubDecisionMetadataWriter>();

	/// <summary>
	/// Serialises <paramref name="metadata"/> to <c>.artifacts/changelog-decision/metadata.json</c>
	/// in the current working directory.
	/// </summary>
	internal async Task WriteAsync(GithubDecisionMetadata metadata, Cancel ctx)
	{
		var dir = fileSystem.Path.GetFullPath(ArtifactDir);
		_ = fileSystem.Directory.CreateDirectory(dir);

		var path = fileSystem.Path.Combine(dir, MetadataFilename);
		var json = JsonSerializer.Serialize(metadata, GithubDecisionMetadataJsonContext.Default.GithubDecisionMetadata);
		await fileSystem.File.WriteAllTextAsync(path, json, ctx);
		_logger.LogInformation("Wrote decision metadata to {Path}", path);
	}

	/// <summary>
	/// Reads the metadata file from the conventional location and returns the deserialised record,
	/// or <c>null</c> when the file is absent or cannot be parsed.
	/// </summary>
	internal async Task<GithubDecisionMetadata?> ReadAsync(string metadataPath, Cancel ctx)
	{
		try
		{
			var json = await fileSystem.File.ReadAllTextAsync(metadataPath, ctx);
			return JsonSerializer.Deserialize(json, GithubDecisionMetadataJsonContext.Default.GithubDecisionMetadata);
		}
		catch (FileNotFoundException)
		{
			_logger.LogInformation("Decision metadata not found at {Path}", metadataPath);
			return null;
		}
		catch (DirectoryNotFoundException)
		{
			_logger.LogInformation("Decision metadata not found at {Path}", metadataPath);
			return null;
		}
		catch (Exception ex) when (ex is IOException or JsonException)
		{
			_logger.LogWarning(ex, "Failed to read decision metadata from {Path}", metadataPath);
			return null;
		}
	}
}
