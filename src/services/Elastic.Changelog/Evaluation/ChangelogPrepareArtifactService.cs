// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.Text.Json;
using Actions.Core.Services;
using Elastic.Changelog.Configuration;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Services;
using Microsoft.Extensions.Logging;

namespace Elastic.Changelog.Evaluation;

/// <summary>Service implementing the changelog prepare-artifact CI command.</summary>
public class ChangelogPrepareArtifactService(
	ILoggerFactory logFactory,
	IConfigurationContext configurationContext,
	ICoreService coreService,
	IFileSystem? fileSystem = null
) : IService
{
	private readonly ILogger _logger = logFactory.CreateLogger<ChangelogPrepareArtifactService>();
	private readonly IFileSystem _fileSystem = fileSystem ?? new FileSystem();
	private readonly ChangelogConfigurationLoader _configLoader = new(logFactory, configurationContext, fileSystem ?? new FileSystem());

	public async Task<bool> PrepareArtifact(IDiagnosticsCollector collector, PrepareArtifactArguments input, Cancel ctx)
	{
		// Resolve artifact status
		var status = ResolveStatus(input.EvaluateStatus, input.GenerateOutcome);
		_logger.LogInformation("Resolved artifact status: {Status} (evaluate={Evaluate}, generate={Generate})",
			status, input.EvaluateStatus, input.GenerateOutcome);

		_ = _fileSystem.Directory.CreateDirectory(input.OutputDir);

		// If we have a changelog to emit, copy it from staging to the artifact output directory
		if (string.Equals(status, "success", StringComparison.OrdinalIgnoreCase))
		{
			var sourceYaml = _fileSystem.Path.Combine(input.StagingDir, $"{input.PrNumber}.yaml");
			var destYaml = _fileSystem.Path.Combine(input.OutputDir, $"{input.PrNumber}.yaml");

			if (_fileSystem.File.Exists(sourceYaml))
			{
				_fileSystem.File.Copy(sourceYaml, destYaml, overwrite: true);
				_logger.LogInformation("Copied changelog YAML: {Source} → {Dest}", sourceYaml, destYaml);
			}
			else
			{
				collector.EmitError(sourceYaml, "Generated changelog YAML not found in staging directory");
				status = "error";
			}
		}

		var config = await _configLoader.LoadChangelogConfiguration(collector, input.Config, ctx);
		var createRules = config?.Rules?.Create;
		var changelogDir = config?.Bundle?.Directory ?? "docs/changelog";

		var metadata = new ChangelogArtifactMetadata
		{
			PrNumber = input.PrNumber,
			HeadRef = input.HeadRef,
			HeadSha = input.HeadSha,
			Status = status,
			LabelTable = input.LabelTable,
			ConfigFile = input.Config,
			ChangelogDir = changelogDir,
			CreateRules = createRules
		};

		var metadataPath = _fileSystem.Path.Combine(input.OutputDir, "metadata.json");
		var json = JsonSerializer.Serialize(metadata, ChangelogArtifactMetadataJsonContext.Default.ChangelogArtifactMetadata);
		await _fileSystem.File.WriteAllTextAsync(metadataPath, json, ctx);
		_logger.LogInformation("Wrote artifact metadata to {Path}", metadataPath);

		await coreService.SetOutputAsync("status", status);

		return true;
	}

	internal static string ResolveStatus(string evaluateStatus, string generateOutcome) =>
		evaluateStatus switch
		{
			"skipped" or "manually-edited" => evaluateStatus,
			"no-title" => "no-title",
			"no-label" => "no-label",
			"proceed" when generateOutcome.Equals("success", StringComparison.OrdinalIgnoreCase) => "success",
			_ => "error"
		};
}
