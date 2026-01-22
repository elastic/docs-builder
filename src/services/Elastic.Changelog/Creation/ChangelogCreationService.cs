// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Changelog.Configuration;
using Elastic.Changelog.GitHub;
using Elastic.Documentation.Changelog;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Services;
using Microsoft.Extensions.Logging;

namespace Elastic.Changelog.Creation;

/// <summary>
/// Service for creating changelog entries
/// </summary>
public class ChangelogCreationService(
	ILoggerFactory logFactory,
	IConfigurationContext configurationContext,
	IGitHubPrService? githubPrService = null,
	IFileSystem? fileSystem = null
) : IService
{
	private readonly ILogger _logger = logFactory.CreateLogger<ChangelogCreationService>();
	private readonly ChangelogConfigurationLoader _configLoader = new(logFactory, configurationContext, fileSystem ?? new FileSystem());
	private readonly ChangelogInputValidator _validator = new(configurationContext);
	private readonly PrInfoProcessor _prProcessor = new(githubPrService, logFactory.CreateLogger<PrInfoProcessor>());
	private readonly ChangelogFileWriter _fileWriter = new(fileSystem ?? new FileSystem(), logFactory.CreateLogger<ChangelogFileWriter>());

	public async Task<bool> CreateChangelog(
		IDiagnosticsCollector collector,
		ChangelogInput input,
		Cancel ctx
	)
	{
		try
		{
			// Load changelog configuration
			var config = await _configLoader.LoadChangelogConfiguration(collector, input.Config, ctx);
			if (config == null)
			{
				collector.EmitError(string.Empty, "Failed to load changelog configuration");
				return false;
			}

			// Handle multiple PRs if provided (more than one PR)
			if (input.Prs != null && input.Prs.Length > 1)
				return await CreateChangelogsForMultiplePrsAsync(collector, input, config, ctx);

			// Single PR or no PR - use existing logic
			return await CreateSingleChangelogAsync(collector, input, config, ctx);
		}
		catch (IOException ioEx)
		{
			collector.EmitError(string.Empty, $"IO error creating changelog: {ioEx.Message}", ioEx);
			return false;
		}
		catch (UnauthorizedAccessException uaEx)
		{
			collector.EmitError(string.Empty, $"Access denied creating changelog: {uaEx.Message}", uaEx);
			return false;
		}
	}

	private async Task<bool> CreateChangelogsForMultiplePrsAsync(
		IDiagnosticsCollector collector,
		ChangelogInput input,
		ChangelogConfiguration config,
		Cancel ctx)
	{
		if (input.Prs == null || input.Prs.Length == 0)
			return false;

		// Validate PR format
		if (!_validator.ValidateMultiplePrFormat(collector, input.Prs, input.Owner, input.Repo))
			return false;

		var successCount = 0;
		var skippedCount = 0;

		foreach (var prTrimmed in input.Prs.Select(pr => pr.Trim()).Where(prTrimmed => !string.IsNullOrWhiteSpace(prTrimmed)))
		{
			// Check PR for blockers
			var (shouldSkip, _) = await _prProcessor.CheckPrForBlockersAsync(
				collector, prTrimmed, input.Owner, input.Repo, input.Products, config, ctx);

			if (shouldSkip)
			{
				skippedCount++;
				continue;
			}

			// Create a copy of input for this PR
			var prInput = CreateInputForSinglePr(input, prTrimmed);

			// Process this PR (treat as single PR)
			var result = await CreateSingleChangelogAsync(collector, prInput, config, ctx);
			if (result)
				successCount++;
		}

		if (successCount == 0 && skippedCount == 0)
			return false;

		_logger.LogInformation("Processed {SuccessCount} PR(s) successfully, skipped {SkippedCount} PR(s)", successCount, skippedCount);
		return successCount > 0;
	}

	private async Task<bool> CreateSingleChangelogAsync(
		IDiagnosticsCollector collector,
		ChangelogInput input,
		ChangelogConfiguration config,
		Cancel ctx)
	{
		// Get the PR URL if Prs is provided (for single PR processing)
		var prUrl = input.Prs != null && input.Prs.Length > 0 ? input.Prs[0] : null;
		var prFetchFailed = false;

		// Validate PR format
		if (!_validator.ValidatePrFormat(collector, prUrl, input.Owner, input.Repo))
			return false;

		// Process PR information if specified
		if (!string.IsNullOrWhiteSpace(prUrl))
		{
			var prResult = await _prProcessor.ProcessPrAsync(collector, input, config, prUrl, ctx);

			if (prResult.ShouldSkip)
				return true; // Return true but don't create changelog

			prFetchFailed = prResult.FetchFailed;

			// Apply derived fields if available
			if (prResult.DerivedFields != null)
			{
				ApplyDerivedFields(input, prResult.DerivedFields);
			}
			else if (!prFetchFailed)
			{
				// DerivedFields is null and fetch didn't fail means validation error occurred
				return false;
			}
		}

		// Validate required fields
		if (!_validator.ValidateRequiredFields(collector, input, prFetchFailed))
			return false;

		// Validate against configuration
		if (!_validator.ValidateAgainstConfiguration(collector, input, config))
			return false;

		// Write changelog file
		return await _fileWriter.WriteChangelogAsync(
			collector,
			input,
			config,
			prUrl,
			string.IsNullOrWhiteSpace(input.Title),
			string.IsNullOrWhiteSpace(input.Type),
			ctx);
	}

	private static ChangelogInput CreateInputForSinglePr(ChangelogInput input, string prUrl) =>
		new()
		{
			Title = input.Title,
			Type = input.Type,
			Products = input.Products,
			Subtype = input.Subtype,
			Areas = input.Areas,
			Prs = [prUrl],
			Owner = input.Owner,
			Repo = input.Repo,
			Issues = input.Issues,
			Description = input.Description,
			Impact = input.Impact,
			Action = input.Action,
			FeatureId = input.FeatureId,
			Highlight = input.Highlight,
			Output = input.Output,
			Config = input.Config,
			UsePrNumber = input.UsePrNumber,
			StripTitlePrefix = input.StripTitlePrefix,
			ExtractReleaseNotes = input.ExtractReleaseNotes
		};

	private static void ApplyDerivedFields(ChangelogInput input, DerivedPrFields derived)
	{
		if (derived.Title != null && string.IsNullOrWhiteSpace(input.Title))
			input.Title = derived.Title;

		if (derived.Type != null && string.IsNullOrWhiteSpace(input.Type))
			input.Type = derived.Type;

		if (derived.Description != null && string.IsNullOrWhiteSpace(input.Description))
			input.Description = derived.Description;

		if (derived.Areas != null && (input.Areas == null || input.Areas.Length == 0))
			input.Areas = derived.Areas;
	}
}
