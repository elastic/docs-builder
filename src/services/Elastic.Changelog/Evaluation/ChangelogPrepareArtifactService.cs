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
		var status = ResolveStatus(input.EvaluateStatus, input.GenerateOutcome);
		_logger.LogInformation("Resolved artifact status: {Status} (evaluate={Evaluate}, generate={Generate})",
			status, input.EvaluateStatus, input.GenerateOutcome);

		_ = _fileSystem.Directory.CreateDirectory(input.OutputDir);

		string? changelogFilename = null;
		if (status == PrEvaluationResult.Success)
		{
			var sourceYaml = FindStagingYaml(input.StagingDir);

			if (sourceYaml != null)
			{
				changelogFilename = input.ExistingChangelogFilename != null
					? _fileSystem.Path.GetFileName(input.ExistingChangelogFilename)
					: _fileSystem.Path.GetFileName(sourceYaml);

				if (input.ExistingChangelogFilename != null)
					_logger.LogInformation("Reusing existing filename {Filename} for stable path on branch", changelogFilename);

				var destYaml = _fileSystem.Path.Combine(input.OutputDir, changelogFilename);
				_fileSystem.File.Copy(sourceYaml, destYaml, overwrite: true);
				_logger.LogInformation("Copied changelog YAML: {Source} → {Dest}", sourceYaml, destYaml);
			}
			else
			{
				collector.EmitError(input.StagingDir, "No generated changelog YAML found in staging directory");
				status = PrEvaluationResult.Error;
			}
		}

		var config = await _configLoader.LoadChangelogConfiguration(collector, input.Config, ctx);
		var createRules = config?.Rules?.Create;
		var changelogDir = config?.Bundle?.Directory ?? "docs/changelog";

		var statusString = status.ToStringFast(true);
		var metadata = new ChangelogArtifactMetadata
		{
			PrNumber = input.PrNumber,
			HeadRef = input.HeadRef,
			HeadSha = input.HeadSha,
			Status = statusString,
			IsFork = input.IsFork,
			HeadRepo = input.HeadRepo,
			CanCommit = input.CanCommit,
			MaintainerCanModify = input.MaintainerCanModify,
			LabelTable = input.LabelTable,
			ProductLabelTable = input.ProductLabelTable,
			SkipLabels = input.SkipLabels,
			ConfigFile = input.Config,
			ChangelogDir = changelogDir,
			ChangelogFilename = changelogFilename,
			CreateRules = createRules
		};

		var metadataPath = _fileSystem.Path.Combine(input.OutputDir, "metadata.json");
		var json = JsonSerializer.Serialize(metadata, ChangelogArtifactMetadataJsonContext.Default.ChangelogArtifactMetadata);
		await _fileSystem.File.WriteAllTextAsync(metadataPath, json, ctx);
		_logger.LogInformation("Wrote artifact metadata to {Path}", metadataPath);

		await coreService.SetOutputAsync("status", statusString);

		return true;
	}

	private string? FindStagingYaml(string stagingDir)
	{
		if (!_fileSystem.Directory.Exists(stagingDir))
			return null;

		var yamlFiles = _fileSystem.Directory.GetFiles(stagingDir, "*.yaml");
		if (yamlFiles.Length == 0)
			return null;

		if (yamlFiles.Length > 1)
		{
			_logger.LogError("Multiple YAML files found in staging directory: {Files}", string.Join(", ", yamlFiles));
			return null;
		}

		return yamlFiles[0];
	}

	internal static PrEvaluationResult ResolveStatus(string evaluateStatus, string generateOutcome)
	{
		if (string.Equals(evaluateStatus, ChangelogPrEvaluationService.ProceedStatus, StringComparison.OrdinalIgnoreCase))
		{
			return generateOutcome.Equals("success", StringComparison.OrdinalIgnoreCase)
				? PrEvaluationResult.Success
				: PrEvaluationResult.Error;
		}

		return PrEvaluationResultExtensions.TryParse(evaluateStatus, out var parsed, ignoreCase: true, allowMatchingMetadataAttribute: true)
			? parsed
			: PrEvaluationResult.Error;
	}
}
