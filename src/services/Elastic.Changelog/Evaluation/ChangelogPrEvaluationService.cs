// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;
using System.IO.Abstractions;
using Actions.Core.Services;
using Elastic.Changelog.Creation;
using Elastic.Changelog.GitHub;
using Elastic.Changelog.Utilities;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Changelog;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.FileSystems;
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
	IRunnerTempFileSystem fileSystem,
	IEnvironmentVariables? env = null
) : IService
{
	private readonly ILogger _logger = logFactory.CreateLogger<ChangelogPrEvaluationService>();
	private readonly IRunnerTempFileSystem _fileSystem = fileSystem;
	private readonly ChangelogConfigurationLoader _configLoader = new(logFactory, configurationContext, fileSystem);
	private readonly GithubDecisionMetadataWriter _metadataWriter = new(logFactory, fileSystem);

	public async Task<bool> EvaluatePr(IDiagnosticsCollector collector, EvaluatePrArguments input, Cancel ctx)
	{
		// Body-only edit check: skip when neither title nor body changed
		if (input is { EventAction: "edited", TitleChanged: false, BodyChanged: false })
		{
			_logger.LogInformation("Skipping: edit with no title or body change");
			return await SetOutputs(PrEvaluationResult.Skipped);
		}

		var config = await _configLoader.LoadChangelogConfiguration(collector, input.Config, ctx) ?? ChangelogConfiguration.Default;
		var changelogDir = config.Bundle?.Directory ?? "docs/changelog";
		var defaultBranch = config.Bundle?.Branch ?? "main";

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
		var changelogFilePath = existingFilename != null ? $"{changelogDir}/{existingFilename}" : null;

		// Manual edit detection (only if a file exists)
		if (changelogFilePath != null)
		{
			var fileAuthor = await gitHubPrService.FetchLastFileCommitAuthorAsync(
				input.Owner,
				input.Repo,
				changelogFilePath,
				input.HeadRef,
				ctx
			);
			if (!string.IsNullOrEmpty(fileAuthor) && !string.Equals(fileAuthor, input.BotName, StringComparison.OrdinalIgnoreCase))
			{
				_logger.LogInformation("Skipping: changelog file {File} manually edited by {Author}", changelogFilePath, fileAuthor);
				return await SetOutputs(PrEvaluationResult.ManuallyEdited);
			}
		}

		// Label-based skip check
		var skipLabels = CollectExcludeLabels(config.Rules?.Create);
		if (PrInfoProcessor.AreAllProductsBlocked(input.PrLabels, config.Rules?.Create))
		{
			_logger.LogInformation("Skipping: all products blocked by label rules");
			return await SetOutputs(PrEvaluationResult.Skipped, skipLabels: skipLabels);
		}

		// Resolve title from PR title only (release note text is never used as title)
		var prTitle = input.PrTitle;
		if (input.StripTitlePrefix)
			prTitle = ChangelogTextUtilities.StripSquareBracketPrefix(prTitle);

		string? description = null;
		if (config.Extract.ReleaseNotes && !string.IsNullOrWhiteSpace(input.PrBody))
		{
			var releaseNote = ReleaseNotesExtractor.FindReleaseNote(input.PrBody);
			if (releaseNote != null)
			{
				description = releaseNote;
				_logger.LogInformation("Using extracted release note as description (length: {Length} characters)", description.Length);
			}
		}

		var title = prTitle;

		if (string.IsNullOrWhiteSpace(title))
		{
			_logger.LogWarning("PR has no title after processing");
			collector.EmitError(string.Empty, "PR has no title. Cannot generate a changelog entry.");
			_ = await SetOutputs(PrEvaluationResult.NoTitle);
			return false;
		}

		// Resolve type — detect multiple matching labels before picking one
		string? resolvedType = null;
		string? ambiguousTypeLabels = null;
		if (config.LabelToType is { Count: > 0 })
		{
			var matching = PrInfoProcessor.MatchingTypeLabels(input.PrLabels, config.LabelToType);
			if (matching.Count > 1)
				ambiguousTypeLabels = string.Join(",", matching);
			else
				resolvedType = matching.Count == 1 ? config.LabelToType[matching[0]] : null;
		}

		if (ambiguousTypeLabels != null)
		{
			_logger.LogInformation("Multiple type labels found on PR: {Labels}", ambiguousTypeLabels);
			collector.EmitError(string.Empty, $"Multiple type labels found: {ambiguousTypeLabels}. Remove all but one.");
			_ = await SetOutputs(PrEvaluationResult.NoLabel, skipLabels: skipLabels);
			await WriteDecisionMetadataAsync(
				input,
				"no-label",
				ambiguousTypeLabels: ambiguousTypeLabels,
				skipLabels: skipLabels,
				defaultBranch: defaultBranch,
				ctx: ctx
			);
			return false;
		}

		// Resolve products from labels
		string? resolvedProducts = null;
		string? productLabelTable = null;
		if (config.LabelToProducts is { Count: > 0 } labelToProducts)
		{
			var products = PrInfoProcessor.MapLabelsToProducts(input.PrLabels, labelToProducts);
			if (products.Count > 0)
			{
				resolvedProducts = ProductArgument.FormatProductSpecs(products);
				_logger.LogInformation("Mapped PR labels to products: {Products}", resolvedProducts);
			}
			else
			{
				// When only one distinct product is configured, assigning it implicitly is
				// unambiguous — no point requiring contributors to add a redundant label.
				var distinctSpecs = labelToProducts.Values.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
				if (distinctSpecs.Count == 1)
				{
					resolvedProducts = ProductArgument.FormatProductSpecs(ProductArgument.ParseProductSpecs(distinctSpecs[0]));
					_logger.LogInformation("Single product configured; assigning implicitly: {Products}", resolvedProducts);
				}
				else
					productLabelTable = BuildProductLabelTable(labelToProducts);
			}
		}

		if (resolvedType == null)
		{
			_logger.LogInformation("No type label found on PR");
			collector.EmitError(
				string.Empty,
				"No matching changelog type label found on this PR. Add a label from your changelog.yml pivot.types, or a skip label."
			);
			var noTypeLabelTable = BuildLabelTable(config.LabelToType);
			_ = await SetOutputs(
				PrEvaluationResult.NoLabel,
				title,
				resolvedDescription: description,
				labelTable: noTypeLabelTable,
				productLabelTable: productLabelTable,
				skipLabels: skipLabels
			);
			await WriteDecisionMetadataAsync(
				input,
				"no-label",
				labelTable: BuildLabelKeys(config.LabelToType),
				productLabelTable: productLabelTable,
				skipLabels: skipLabels,
				defaultBranch: defaultBranch,
				ctx: ctx
			);
			return false;
		}

		// Multiple products are configured via labels but none matched, and no defaults are
		// available to fill in via inference at 'changelog add' time. Surface this as a
		// missing-label failure so the contributor sees an actionable hint instead of a hard
		// error later in the workflow.
		if (productLabelTable != null && (config.ProductsConfiguration?.Default is null or { Count: 0 }))
		{
			_logger.LogInformation("Multiple products configured but no matching product label on PR; no default products configured");
			collector.EmitError(
				string.Empty,
				"No matching product label found on this PR. Add a label from your changelog.yml pivot.products."
			);
			_ = await SetOutputs(
				PrEvaluationResult.NoLabel,
				title,
				resolvedDescription: description,
				productLabelTable: productLabelTable,
				skipLabels: skipLabels
			);
			await WriteDecisionMetadataAsync(
				input,
				"no-label",
				productLabelTable: productLabelTable,
				skipLabels: skipLabels,
				defaultBranch: defaultBranch,
				ctx: ctx
			);
			return false;
		}

		// Entry-required gate: fail when the flag is set and no file exists for this PR
		if (input.RequireChangelogFile && existingFilename == null)
		{
			var expectedPath = $"{changelogDir}/{input.PrNumber}.yaml";
			_logger.LogInformation("Missing changelog file for PR #{PrNumber}; require-changelog-file is set", input.PrNumber);
			collector.EmitError(
				string.Empty,
				$"No changelog entry file found for PR #{input.PrNumber}. " + $"Expected: {expectedPath}. " +
					"Add a changelog entry file to the PR or disable the require-changelog-file gate."
			);
			_ = await SetOutputs(PrEvaluationResult.MissingEntry, changelogDir: changelogDir);
			// Write metadata so the github-comment step can render a missing-entry body.
			await WriteDecisionMetadataAsync(input, "missing-entry", changelogDir: changelogDir, defaultBranch: defaultBranch, ctx: ctx);
			return false;
		}

		_logger.LogInformation(
			"PR evaluation complete: title={Title}, type={Type}, products={Products}, existingFile={File}",
			title,
			resolvedType,
			resolvedProducts,
			existingFilename
		);
		await WriteDecisionMetadataAsync(
			input,
			ProceedStatus,
			changelogDir: changelogDir,
			changelogFilename: existingFilename,
			skipLabels: skipLabels,
			defaultBranch: defaultBranch,
			ctx: ctx
		);
		return await SetOutputs(
			PrEvaluationResult.Success,
			title,
			resolvedType,
			resolvedDescription: description,
			resolvedProducts: resolvedProducts,
			productLabelTable: productLabelTable,
			changelogDir: changelogDir,
			existingFilename: existingFilename
		);
	}

	/// <summary>The evaluate-pr output value when evaluation succeeds and generation should proceed.</summary>
	internal const string ProceedStatus = "proceed";

	/// <summary>
	/// Writes <see cref="GithubDecisionMetadata"/> when running on CI.
	/// No-ops when <c>GITHUB_ACTIONS</c> is unset or <c>PrNumber</c> is zero (local runs).
	/// Failures are logged as warnings; they never affect the command exit code.
	/// </summary>
	private async Task WriteDecisionMetadataAsync(
		EvaluatePrArguments input,
		string status,
		Cancel ctx,
		string? labelTable = null,
		string? productLabelTable = null,
		string? skipLabels = null,
		string? changelogDir = null,
		string? changelogFilename = null,
		string? ambiguousTypeLabels = null,
		string? defaultBranch = null
	)
	{
		if (env?.IsRunningOnCI != true || input.PrNumber <= 0)
			return;

		var metadata = new GithubDecisionMetadata
		{
			Gate = ValidationGate.File,
			PrNumber = input.PrNumber,
			HeadRef = input.HeadRef,
			HeadSha = input.HeadSha,
			Status = status,
			IsFork = input.IsFork,
			CanCommit = input.CanCommit,
			MaintainerCanModify = input.MaintainerCanModify,
			HeadRepo = input.HeadRepo,
			LabelTable = labelTable,
			ProductLabelTable = productLabelTable,
			SkipLabels = skipLabels,
			ChangelogDir = changelogDir,
			ChangelogFilename = changelogFilename,
			AmbiguousTypeLabels = ambiguousTypeLabels,
			DefaultBranch = defaultBranch
		};
		await _metadataWriter.WriteAsync(metadata, ctx);
	}

	private async Task<bool> SetOutputs(
		PrEvaluationResult status,
		string? resolvedTitle = null,
		string? resolvedType = null,
		string? resolvedDescription = null,
		string? resolvedProducts = null,
		string? labelTable = null,
		string? productLabelTable = null,
		string? changelogDir = null,
		string? existingFilename = null,
		string? skipLabels = null
	)
	{
		var statusString = status == PrEvaluationResult.Success ? ProceedStatus : status.ToStringFast(true);

		var shouldGenerate = status == PrEvaluationResult.Success;

		await coreService.SetOutputAsync("status", statusString);
		await coreService.SetOutputAsync("should-generate", shouldGenerate ? "true" : "false");

		// All PR-derived outputs flow through OutputSanitizer to strip
		// control characters and enforce per-field length caps before they
		// cross the GITHUB_OUTPUT boundary. See elastic/docs-eng-team#491.
		if (resolvedTitle != null)
			await coreService.SetOutputAsync("title", OutputSanitizer.SanitizeForOutput(resolvedTitle, OutputSanitizer.TitleMaxLength));
		if (resolvedDescription != null)
			await coreService.SetOutputAsync(
				"description",
				OutputSanitizer.SanitizeForOutput(resolvedDescription, OutputSanitizer.DescriptionMaxLength)
			);
		if (resolvedType != null)
			await coreService.SetOutputAsync("type", OutputSanitizer.SanitizeForOutput(resolvedType, OutputSanitizer.TypeMaxLength));
		if (resolvedProducts != null)
			await coreService.SetOutputAsync(
				"products",
				OutputSanitizer.SanitizeForOutput(resolvedProducts, OutputSanitizer.LabelsMaxLength)
			);
		if (labelTable != null)
			await coreService.SetOutputAsync(
				"label-table",
				OutputSanitizer.SanitizeForOutput(labelTable, OutputSanitizer.LabelTableMaxLength)
			);
		if (productLabelTable != null)
			await coreService.SetOutputAsync(
				"product-label-table",
				OutputSanitizer.SanitizeForOutput(productLabelTable, OutputSanitizer.LabelTableMaxLength)
			);
		if (changelogDir != null)
			await coreService.SetOutputAsync(
				"changelog-dir",
				OutputSanitizer.SanitizeForOutput(changelogDir, OutputSanitizer.PathMaxLength)
			);
		if (existingFilename != null)
			await coreService.SetOutputAsync(
				"existing-changelog-filename",
				OutputSanitizer.SanitizeForOutput(existingFilename, OutputSanitizer.PathMaxLength)
			);
		if (skipLabels != null)
			await coreService.SetOutputAsync("skip-labels", OutputSanitizer.SanitizeForOutput(skipLabels, OutputSanitizer.LabelsMaxLength));

		return true;
	}

	/// <summary>
	/// Collects all exclude-mode labels from global and per-product create rules.
	/// Returns a comma-separated string of unique labels, or null when none are configured.
	/// </summary>
	internal static string? CollectExcludeLabels(CreateRules? createRules)
	{
		if (createRules == null)
			return null;

		var labels = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		if (createRules is { Mode: FieldMode.Exclude, Labels.Count: > 0 })
		{
			foreach (var label in createRules.Labels)
				_ = labels.Add(label);
		}

		if (createRules.ByProduct is { Count: > 0 })
		{
			foreach (var (_, productRules) in createRules.ByProduct)
			{
				if (productRules is { Mode: FieldMode.Exclude, Labels.Count: > 0 })
				{
					foreach (var label in productRules.Labels)
						_ = labels.Add(label);
				}
			}
		}

		return labels.Count > 0 ? string.Join(",", labels) : null;
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
		if (_fileSystem.File.Exists(_fileSystem.Path.Join(changelogDir, prFilename)))
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
		content.Contains($"/pull/{prNumber}", StringComparison.Ordinal)
			|| content.Contains($"- \"{prNumber}\"", StringComparison.Ordinal)
			|| content.Contains($"- '{prNumber}'", StringComparison.Ordinal);

	internal static string BuildLabelTable(IReadOnlyDictionary<string, string>? labelToType) =>
		ChangelogTableRenderers.BuildLabelTable(labelToType);

	internal static string BuildLabelKeys(IReadOnlyDictionary<string, string>? labelToType) =>
		ChangelogTableRenderers.BuildLabelKeys(labelToType);

	internal static string BuildProductLabelTable(IReadOnlyDictionary<string, string>? labelToProducts) =>
		ChangelogTableRenderers.BuildProductLabelTable(labelToProducts);

	internal static string BuildMappingTable(IReadOnlyDictionary<string, string>? mapping, string keyHeader, string valueHeader) =>
		ChangelogTableRenderers.BuildMappingTable(mapping, keyHeader, valueHeader);
}
