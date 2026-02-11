// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Changelog.Configuration;
using Elastic.Changelog.GitHub;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Changelog;
using Elastic.Documentation.Configuration.Inference;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Services;
using Microsoft.Extensions.Logging;

namespace Elastic.Changelog.Creation;

/// <summary>
/// Arguments for the CreateChangelog method
/// </summary>
public record CreateChangelogArguments
{
	public string? Title { get; init; }
	public string? Type { get; init; }
	public required IReadOnlyList<ProductArgument> Products { get; init; }
	public string? Subtype { get; init; }
	public string[]? Areas { get; init; }
	public string[]? Prs { get; init; }
	public string? Owner { get; init; }
	public string? Repo { get; init; }
	public string[]? Issues { get; init; }
	public string? Description { get; init; }
	public string? Impact { get; init; }
	public string? Action { get; init; }
	public string? FeatureId { get; init; }
	public bool? Highlight { get; init; }
	public string? Output { get; init; }
	public string? Config { get; init; }
	public bool UsePrNumber { get; init; }
	public bool StripTitlePrefix { get; init; }
	public bool ExtractReleaseNotes { get; init; }
	public bool ExtractIssues { get; init; }
}

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
	private readonly CreateChangelogArgumentsValidator _validator = new(configurationContext);
	private readonly PrInfoProcessor _prProcessor = new(githubPrService, logFactory.CreateLogger<PrInfoProcessor>());
	private readonly ChangelogFileWriter _fileWriter = new(fileSystem ?? new FileSystem(), logFactory.CreateLogger<ChangelogFileWriter>());
	private readonly ProductInferService _productInferService = new(
		configurationContext.ProductsConfiguration);

	public async Task<bool> CreateChangelog(IDiagnosticsCollector collector, CreateChangelogArguments input, Cancel ctx)
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

			// Apply config defaults to input (for extract_release_notes, extract_issues)
			input = ApplyConfigDefaults(input, config);

			// If no products provided, try to infer from config defaults or repository
			if (input.Products.Count == 0)
			{
				var inferredProducts = InferProducts(config.ProductsConfiguration);
				if (inferredProducts != null)
					input = input with { Products = inferredProducts };
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

	private static CreateChangelogArguments ApplyConfigDefaults(CreateChangelogArguments input, ChangelogConfiguration _) =>
		// Config defaults are already handled by CLI layer, but this ensures service layer has proper defaults too
		input;

	/// <summary>
	/// Infers products from configuration defaults or git repository name.
	/// </summary>
	private IReadOnlyList<ProductArgument>? InferProducts(ProductsConfig? productsConfig)
	{
		// First, try config defaults
		if (productsConfig?.Default is { Count: > 0 })
		{
			var products = productsConfig.Default.Select(d => new ProductArgument
			{
				Product = d.Product,
				Lifecycle = d.Lifecycle
			}).ToList();
			_logger.LogInformation("Using default products from config: {Products}",
				string.Join(", ", products.Select(p => $"{p.Product} ({p.Lifecycle})")));
			return products;
		}

		// Second, try generic product inference from current git repo
		var product = _productInferService.InferProductFromCurrentRepository();
		if (product == null)
		{
			_logger.LogDebug("Could not infer product from repository");
			return null;
		}

		_logger.LogInformation("Inferred product '{ProductId}' from repository", product.Id);
		return [new ProductArgument { Product = product.Id, Lifecycle = "ga" }];
	}

	private async Task<bool> CreateChangelogsForMultiplePrsAsync(
		IDiagnosticsCollector collector,
		CreateChangelogArguments input,
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
		CreateChangelogArguments input,
		ChangelogConfiguration config,
		Cancel ctx)
	{
		// Get the PR URL if Prs is provided (for single PR processing)
		var prUrl = input.Prs is { Length: > 0 } ? input.Prs[0] : null;
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
				input = ApplyDerivedFields(input, prResult.DerivedFields);
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

	private static CreateChangelogArguments CreateInputForSinglePr(CreateChangelogArguments input, string prUrl) =>
		input with { Prs = [prUrl] };

	private static CreateChangelogArguments ApplyDerivedFields(CreateChangelogArguments input, DerivedPrFields derived) =>
		input with
		{
			Title = derived.Title != null && string.IsNullOrWhiteSpace(input.Title) ? derived.Title : input.Title,
			Type = derived.Type != null && string.IsNullOrWhiteSpace(input.Type) ? derived.Type : input.Type,
			Description = derived.Description != null && string.IsNullOrWhiteSpace(input.Description) ? derived.Description : input.Description,
			Areas = derived.Areas != null && (input.Areas == null || input.Areas.Length == 0) ? derived.Areas : input.Areas,
			Highlight = derived.Highlight ?? input.Highlight,
			Issues = derived.Issues != null && (input.Issues == null || input.Issues.Length == 0) ? derived.Issues : input.Issues
		};
}
