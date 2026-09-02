// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Changelog.GitHub;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Changelog;
using Elastic.Documentation.Configuration.ReleaseNotes;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.FileSystems;
using Elastic.Documentation.ReleaseNotes;
using Elastic.Documentation.Services;
using Microsoft.Extensions.Logging;
using YamlDotNet.Core;

namespace Elastic.Changelog.Evaluation;

/// <summary>
/// Service implementing the <c>changelog validate-entries</c> gate.
/// Validates changelog entry files that a PR added or modified: schema, config membership, PR existence, hygiene.
/// </summary>
public class ChangelogEntryValidationService(
	ILoggerFactory logFactory,
	IConfigurationContext configurationContext,
	IGitHubPrService gitHubPrService,
	IRunnerTempFileSystem fileSystem,
	IEnvironmentVariables? env = null
) : IService
{
	private readonly ILogger _logger = logFactory.CreateLogger<ChangelogEntryValidationService>();
	private readonly ChangelogConfigurationLoader _configLoader = new(logFactory, configurationContext, fileSystem);
	private readonly GithubDecisionMetadataWriter _metadataWriter = new(logFactory, fileSystem);

	/// <summary>
	/// Validates changelog entry files touched by the PR.
	/// Returns true when all entry-level checks pass (warnings do not block).
	/// </summary>
	public async Task<bool> ValidateEntries(IDiagnosticsCollector collector, ValidateEntriesArguments input, Cancel ctx)
	{
		var config = await _configLoader.LoadChangelogConfiguration(collector, input.ConfigFile, ctx) ?? ChangelogConfiguration.Default;
		var changelogDir = config.Bundle?.Directory ?? "docs/changelog";
		var defaultBranch = config.Bundle?.Branch ?? "main";

		// ── Resolve label-derived type ─────────────────────────────────────────────────────────
		ChangelogEntryType? labelDerivedType = null;
		if (config.LabelToType is { Count: > 0 })
		{
			var matchingLabels = input.PrLabels.Where(l => config.LabelToType.ContainsKey(l)).ToList();
			if (matchingLabels.Count == 1 && config.LabelToType.TryGetValue(matchingLabels[0], out var typeStr))
			{
				if (ChangelogEntryTypeExtensions.TryParse(typeStr, out var t, ignoreCase: true, allowMatchingMetadataAttribute: true))
					labelDerivedType = t;
			}
		}

		// ── Discover files ─────────────────────────────────────────────────────────────────────
		IReadOnlyList<string> filesToValidate;
		if (input.Files is { Length: > 0 })
		{
			filesToValidate = FilterChangelogFiles(input.Files, changelogDir);
		}
		else
		{
			var changedFiles = await gitHubPrService.FetchChangedFilesAsync(input.Owner, input.Repo, input.PrNumber, ctx);
			if (changedFiles is null)
			{
				collector.EmitError(
					string.Empty,
					$"Failed to fetch changed files for PR #{input.PrNumber} from GitHub API. Cannot validate changelog entries."
				);
				return false;
			}
			filesToValidate = FilterChangelogFiles(changedFiles, changelogDir);
		}

		if (filesToValidate.Count == 0)
		{
			_logger.LogInformation("No changelog entry files to validate for PR #{PrNumber}", input.PrNumber);
			if (input.RequireChangelogFile)
			{
				var expectedPath = $"{changelogDir}/{input.PrNumber}.yaml";
				collector.EmitError(
					string.Empty,
					$"No changelog entry file found for PR #{input.PrNumber}. " +
						$"Add one at {expectedPath}, or run 'docs-builder changelog pr' to generate it."
				);
				await WriteMetadataAsync(input, "missing-entry", null, defaultBranch, ctx);
				return false;
			}
			await WriteMetadataAsync(input, "ok", null, defaultBranch, ctx);
			return true;
		}

		_logger.LogInformation("Validating {Count} changelog entry file(s) for PR #{PrNumber}", filesToValidate.Count, input.PrNumber);

		// ── Known products ────────────────────────────────────────────────────────────────────
		IReadOnlySet<string>? knownProducts = null;
		var availableProducts = configurationContext.ProductsConfiguration.Products;
		if (availableProducts is { Count: > 0 })
			knownProducts = new HashSet<string>(availableProducts.Keys.Select(k => k.Replace('_', '-')), StringComparer.OrdinalIgnoreCase);

		// ── Parse files, validate filenames, run field-level rules ───────────────────────────
		var allFindings = new List<EntryFileFinding>();
		var entriesByFile = new Dictionary<string, ChangelogEntryDto?>(StringComparer.OrdinalIgnoreCase);
		var filenamePrNumbers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		foreach (var relPath in filesToValidate)
		{
			// Filename validation (must be <digits>[-<name>].yaml)
			allFindings.AddRange(ChangelogEntryValidator.ValidateFilename(relPath));
			if (ChangelogEntryValidator.TryParseFilenameAsPrNumber(relPath, out var fnPrNum))
				filenamePrNumbers[relPath] = fnPrNum;

			var fullPath = fileSystem.Path.GetFullPath(relPath);
			if (!fileSystem.File.Exists(fullPath))
			{
				// File is listed in the PR diff but not present on disk (deleted? wrong cwd?).
				_logger.LogWarning("Changelog file {Path} listed in PR diff but not found on disk; skipping", relPath);
				continue;
			}

			var rawYaml = await fileSystem.File.ReadAllTextAsync(fullPath, ctx);
			ChangelogEntryDto? dto = null;
			try
			{
				var normalized = ReleaseNotesSerialization.NormalizeYaml(rawYaml);
				dto = ReleaseNotesSerialization.GetEntryDeserializer().Deserialize<ChangelogEntryDto>(normalized);
				if (dto is not null)
				{
					var fnNum = filenamePrNumbers.TryGetValue(relPath, out var n) ? n : (int?)null;
					var findings = ChangelogEntryValidator.Validate(relPath, dto, config, labelDerivedType, knownProducts, fnNum);
					allFindings.AddRange(findings);
				}
			}
			catch (YamlException ex)
			{
				allFindings.Add(new EntryFileFinding(relPath, FindingSeverity.Error, $"YAML parse error: {ex.Message}"));
			}

			entriesByFile[relPath] = dto;
		}

		// ── Collect own-repo PR numbers for existence check (from filenames) ──────────────────
		var ownRepoNumbers = new HashSet<int>(filenamePrNumbers.Values);

		// ── Batch PR existence check ───────────────────────────────────────────────────────────
		var existenceResults = ownRepoNumbers.Count > 0
			? await gitHubPrService.CheckPullRequestsExistAsync(input.Owner, input.Repo, [.. ownRepoNumbers], ctx)
			: new Dictionary<int, bool>();

		// ── Filename PR existence check ───────────────────────────────────────────────────────
		foreach (var (relPath, prNum) in filenamePrNumbers)
		{
			if (existenceResults.TryGetValue(prNum, out var exists) && !exists)
				allFindings.Add(
					new EntryFileFinding(relPath, FindingSeverity.Error, $"PR #{prNum} does not exist in {input.Owner}/{input.Repo}")
				);
		}

		// ── Presence check ────────────────────────────────────────────────────────────────────
		if (input.RequireChangelogFile && !ownRepoNumbers.Contains(input.PrNumber))
		{
			var expectedPath = $"{changelogDir}/{input.PrNumber}.yaml";
			allFindings.Add(
				new EntryFileFinding(
					string.Empty,
					FindingSeverity.Error,
					$"No changelog entry file references PR #{input.PrNumber}. " +
						$"Add one at {expectedPath}, or run 'docs-builder changelog pr' to generate it."
				)
			);
		}

		// ── Write metadata and return ─────────────────────────────────────────────────────────
		var hasErrors = allFindings.Any(f => f.Severity == FindingSeverity.Error);

		if (hasErrors)
		{
			foreach (var finding in allFindings.Where(f => f.Severity == FindingSeverity.Error))
				collector.EmitError(finding.File, finding.Message);
			foreach (var finding in allFindings.Where(f => f.Severity == FindingSeverity.Warning))
				collector.EmitWarning(finding.File, finding.Message);
		}
		else
		{
			foreach (var finding in allFindings.Where(f => f.Severity == FindingSeverity.Warning))
				collector.EmitWarning(finding.File, finding.Message);
		}

		var entryFindings = allFindings.Count > 0 ? allFindings : null;
		await WriteMetadataAsync(input, hasErrors ? "entries-invalid" : "ok", entryFindings, defaultBranch, ctx);

		return !hasErrors;
	}

	/// <summary>Filters a list of file paths to changelog entry files in the changelog dir (top level only, no note-* files).</summary>
	private static IReadOnlyList<string> FilterChangelogFiles(IEnumerable<string> files, string changelogDir)
	{
		var normalizedDir = changelogDir.TrimEnd('/');
		return files.Where(f =>
		{
			var trimmed = f.TrimStart('/');
			// Must be directly in the changelog dir, not a subdirectory
			if (!trimmed.StartsWith(normalizedDir + "/", StringComparison.OrdinalIgnoreCase))
				return false;
			var remainder = trimmed[(normalizedDir.Length + 1)..];
			// Top-level only — no subdirectories
			if (remainder.Contains('/'))
				return false;
			// Must be yaml
			if (
				!remainder.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase)
				&& !remainder.EndsWith(".yml", StringComparison.OrdinalIgnoreCase)
			)
				return false;
			// Skip note- files
			if (remainder.StartsWith("note-", StringComparison.OrdinalIgnoreCase))
				return false;
			return true;
		}).ToList();
	}

	private async Task WriteMetadataAsync(
		ValidateEntriesArguments input,
		string status,
		List<EntryFileFinding>? findings,
		string defaultBranch,
		Cancel ctx
	)
	{
		if (env?.IsRunningOnCI != true || input.PrNumber <= 0)
			return;

		var entryFindings = findings?.Select(
			f => new EntryFinding { File = f.File, Severity = f.Severity.ToString(), Message = f.Message }
		).ToList();

		var metadata = new GithubDecisionMetadata
		{
			Gate = ValidationGate.Entries,
			PrNumber = input.PrNumber,
			HeadRef = input.HeadRef,
			HeadSha = input.HeadSha,
			Status = status,
			IsFork = input.IsFork,
			CanCommit = input.CanCommit,
			MaintainerCanModify = input.MaintainerCanModify,
			HeadRepo = input.HeadRepo,
			ConfigFile = input.ConfigFile,
			DefaultBranch = defaultBranch,
			EntryFindings = entryFindings
		};

		await _metadataWriter.WriteAsync(metadata, ctx);
	}
}
