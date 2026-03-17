// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;
using System.IO.Abstractions;
using Actions.Core.Services;
using Elastic.Changelog.Configuration;
using Elastic.Changelog.Creation;
using Elastic.Changelog.GitHub;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Changelog;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.ReleaseNotes;
using Elastic.Documentation.Services;
using Microsoft.Extensions.Logging;

namespace Elastic.Changelog.Evaluation;

/// <summary>Service implementing the changelog evaluate-pr CI command.</summary>
public class ChangelogPrEvaluationService(
	ILoggerFactory logFactory,
	IConfigurationContext configurationContext,
	IGitHubPrService gitHubPrService,
	ICoreService coreService,
	IFileSystem? fileSystem = null
) : IService
{
	private readonly ILogger _logger = logFactory.CreateLogger<ChangelogPrEvaluationService>();
	private readonly IFileSystem _fileSystem = fileSystem ?? new FileSystem();
	private readonly ChangelogConfigurationLoader _configLoader = new(logFactory, configurationContext, fileSystem ?? new FileSystem());

	public async Task<bool> EvaluatePr(IDiagnosticsCollector collector, EvaluatePrArguments input, Cancel ctx)
	{
		// Body-only edit check
		if (input.EventAction == "edited" && !input.TitleChanged)
		{
			_logger.LogInformation("Skipping: body-only edit (title unchanged)");
			return await SetOutputs(PrEvaluationResult.Skipped);
		}

		var config = await _configLoader.LoadChangelogConfiguration(collector, input.Config, ctx)
			?? ChangelogConfiguration.Default;
		var changelogDir = config.Bundle?.Directory ?? "docs/changelog";

		// Commit bot loop detection
		if (input.EventAction == "synchronize")
		{
			var commitAuthor = await gitHubPrService.FetchCommitAuthorAsync(input.Owner, input.Repo, input.HeadSha, ctx);
			if (string.Equals(commitAuthor, input.BotName, StringComparison.OrdinalIgnoreCase))
			{
				_logger.LogInformation("Skipping: last commit by bot {BotName}", input.BotName);
				return await SetOutputs(PrEvaluationResult.Skipped);
			}
		}

		// Find existing changelog file for this PR (handles all filename strategies)
		var existingFilename = FindExistingChangelog(changelogDir, input.PrNumber);
		var changelogFilePath = existingFilename != null
			? $"{changelogDir}/{existingFilename}"
			: null;

		// Manual edit detection (only if a file exists)
		if (changelogFilePath != null)
		{
			var fileAuthor = await gitHubPrService.FetchLastFileCommitAuthorAsync(
				input.Owner, input.Repo, changelogFilePath, input.HeadRef, ctx
			);
			if (!string.IsNullOrEmpty(fileAuthor) && !string.Equals(fileAuthor, input.BotName, StringComparison.OrdinalIgnoreCase))
			{
				_logger.LogInformation("Skipping: changelog file {File} manually edited by {Author}", changelogFilePath, fileAuthor);
				return await SetOutputs(PrEvaluationResult.ManuallyEdited);
			}
		}

		// Label-based skip check
		if (PrInfoProcessor.AreAllProductsBlocked(input.PrLabels, config.Rules?.Create))
		{
			_logger.LogInformation("Skipping: all products blocked by label rules");
			return await SetOutputs(PrEvaluationResult.Skipped);
		}

		// Resolve title
		var title = input.PrTitle;
		if (input.StripTitlePrefix)
			title = ChangelogTextUtilities.StripSquareBracketPrefix(title);

		if (string.IsNullOrWhiteSpace(title))
		{
			_logger.LogWarning("PR has no title after processing");
			return await SetOutputs(PrEvaluationResult.NoTitle);
		}

		// Resolve type
		string? resolvedType = null;
		if (config.LabelToType is { Count: > 0 })
			resolvedType = PrInfoProcessor.MapLabelsToType(input.PrLabels, config.LabelToType);

		if (resolvedType == null)
		{
			_logger.LogInformation("No type label found on PR");
			return await SetOutputs(PrEvaluationResult.NoLabel, title, labelTable: BuildLabelTable(config.LabelToType));
		}

		_logger.LogInformation("PR evaluation complete: title={Title}, type={Type}, existingFile={File}", title, resolvedType, existingFilename);
		return await SetOutputs(PrEvaluationResult.Success, title, resolvedType, existingFilename: existingFilename);
	}

	/// <summary>The evaluate-pr output value when evaluation succeeds and generation should proceed.</summary>
	internal const string ProceedStatus = "proceed";

	private async Task<bool> SetOutputs(
		PrEvaluationResult status,
		string? resolvedTitle = null,
		string? resolvedType = null,
		string? labelTable = null,
		string? existingFilename = null)
	{
		// evaluate-pr outputs "proceed" (not "success") to signal the generate step should run
		var statusString = status == PrEvaluationResult.Success
			? ProceedStatus
			: status.ToStringFast(true);

		var shouldGenerate = status == PrEvaluationResult.Success;
		var shouldUpload = status is PrEvaluationResult.Success or PrEvaluationResult.NoLabel or PrEvaluationResult.NoTitle;

		await coreService.SetOutputAsync("status", statusString);
		await coreService.SetOutputAsync("should-generate", shouldGenerate ? "true" : "false");
		await coreService.SetOutputAsync("should-upload", shouldUpload ? "true" : "false");

		if (resolvedTitle != null)
			await coreService.SetOutputAsync("title", resolvedTitle);
		if (resolvedType != null)
			await coreService.SetOutputAsync("type", resolvedType);
		if (labelTable != null)
			await coreService.SetOutputAsync("label-table", labelTable);
		if (existingFilename != null)
			await coreService.SetOutputAsync("existing-changelog-filename", existingFilename);

		return true;
	}

	/// <summary>
	/// Finds an existing changelog file for the given PR in the changelog directory.
	/// Returns the filename (not the full path) if found, or null.
	/// </summary>
	internal string? FindExistingChangelog(string changelogDir, int prNumber)
	{
		if (!_fileSystem.Directory.Exists(changelogDir))
			return null;

		var prFilename = $"{prNumber}.yaml";
		if (_fileSystem.File.Exists(_fileSystem.Path.Combine(changelogDir, prFilename)))
			return prFilename;

		var prString = prNumber.ToString(CultureInfo.InvariantCulture);
		foreach (var filePath in _fileSystem.Directory.GetFiles(changelogDir, "*.yaml"))
		{
			var content = _fileSystem.File.ReadAllText(filePath);
			if (ContentReferencesPr(content, prString))
				return _fileSystem.Path.GetFileName(filePath);
		}

		return null;
	}

	internal static bool ContentReferencesPr(string content, string prNumber) =>
		content.Contains($"/pull/{prNumber}", StringComparison.Ordinal) ||
		content.Contains($"- \"{prNumber}\"", StringComparison.Ordinal) ||
		content.Contains($"- '{prNumber}'", StringComparison.Ordinal);

	internal static string BuildLabelTable(IReadOnlyDictionary<string, string>? labelToType)
	{
		if (labelToType is not { Count: > 0 })
			return "";

		var lines = new List<string> { "| Label | Type |", "| --- | --- |" };
		foreach (var (label, type) in labelToType)
			lines.Add($"| `{label}` | {type} |");

		return string.Join("\n", lines);
	}
}
