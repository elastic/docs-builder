// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Changelog.Configuration;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;

namespace Elastic.Changelog.Creation;

/// <summary>
/// Service for validating changelog input against configuration
/// </summary>
public class CreateChangelogArgumentsValidator(IConfigurationContext configurationContext)
{
	/// <summary>
	/// Validates that if a PR is just a number, owner and repo must be provided.
	/// </summary>
	public bool ValidatePrFormat(IDiagnosticsCollector collector, string? prUrl, string? owner, string? repo)
	{
		if (!string.IsNullOrWhiteSpace(prUrl)
			&& int.TryParse(prUrl, out _)
			&& (string.IsNullOrWhiteSpace(owner) || string.IsNullOrWhiteSpace(repo)))
		{
			collector.EmitError(string.Empty, "When --prs is specified as just a number, both --owner and --repo must be provided");
			return false;
		}

		return true;
	}

	/// <summary>
	/// Validates that if all PRs are just numbers, owner and repo must be provided.
	/// </summary>
	public bool ValidateMultiplePrFormat(IDiagnosticsCollector collector, string[] prs, string? owner, string? repo)
	{
		var allAreNumbers = prs.All(pr => int.TryParse(pr.Trim(), out _));
		if (allAreNumbers && (string.IsNullOrWhiteSpace(owner) || string.IsNullOrWhiteSpace(repo)))
		{
			collector.EmitError(string.Empty, "When --prs contains only numbers, both --owner and --repo must be provided");
			return false;
		}

		return true;
	}

	/// <summary>
	/// Validates required fields after PR processing.
	/// </summary>
	public bool ValidateRequiredFields(
		IDiagnosticsCollector collector,
		CreateChangelogArguments input,
		bool prFetchFailed)
	{
		// Validate title
		if (string.IsNullOrWhiteSpace(input.Title))
		{
			if (prFetchFailed)
				collector.EmitWarning(string.Empty, "Title is missing. The changelog will be created with title commented out. Please manually update the title field.");
			else
			{
				collector.EmitError(string.Empty, "Title is required. Provide --title or specify --prs to derive it from the PR.");
				return false;
			}
		}

		// Validate type
		if (string.IsNullOrWhiteSpace(input.Type))
		{
			if (prFetchFailed)
				collector.EmitWarning(string.Empty, "Type is missing. The changelog will be created with type commented out. Please manually update the type field.");
			else
			{
				collector.EmitError(string.Empty, "Type is required. Provide --type or specify --prs to derive it from PR labels (requires label_to_type mapping in changelog.yml).");
				return false;
			}
		}

		// Validate products
		if (input.Products.Count == 0)
		{
			collector.EmitError(string.Empty, "At least one product is required");
			return false;
		}

		return true;
	}

	/// <summary>
	/// Validates input values against configuration.
	/// </summary>
	public bool ValidateAgainstConfiguration(
		IDiagnosticsCollector collector,
		CreateChangelogArguments input,
		ChangelogConfiguration config)
	{
		// Validate type is in allowed list (only if type is provided)
		if (!string.IsNullOrWhiteSpace(input.Type) && !config.Types.Contains(input.Type))
		{
			collector.EmitError(string.Empty, $"Type '{input.Type}' is not in the list of available types. Available types: {string.Join(", ", config.Types)}");
			return false;
		}

		// Validate subtype if provided
		if (!string.IsNullOrWhiteSpace(input.Subtype) && !config.SubTypes.Contains(input.Subtype))
		{
			collector.EmitError(string.Empty, $"Subtype '{input.Subtype}' is not in the list of available subtypes. Available subtypes: {string.Join(", ", config.SubTypes)}");
			return false;
		}

		// Validate areas if configuration provides available areas
		if (config.Areas != null && config.Areas.Count > 0 && input.Areas != null)
		{
			foreach (var area in input.Areas.Where(area => !config.Areas.Contains(area)))
			{
				collector.EmitError(string.Empty, $"Area '{area}' is not in the list of available areas. Available areas: {string.Join(", ", config.Areas)}");
				return false;
			}
		}

		// Validate products against products.yml
		var validProductIds = configurationContext.ProductsConfiguration.Products.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
		foreach (var product in input.Products)
		{
			// Normalize product ID (replace underscores with hyphens for comparison)
			var normalizedProductId = product.Product?.Replace('_', '-') ?? string.Empty;
			if (!validProductIds.Contains(normalizedProductId))
			{
				var availableProducts = string.Join(", ", validProductIds.OrderBy(p => p));
				collector.EmitError(string.Empty, $"Product '{product.Product}' is not in the list of available products from config/products.yml. Available products: {availableProducts}");
				return false;
			}
		}

		// Validate lifecycle values in products
		var availableLifecycleStrings = config.Lifecycles.Select(l => l.ToStringFast(true)).ToList();
		foreach (var product in input.Products.Where(product => !string.IsNullOrWhiteSpace(product.Lifecycle)))
		{
			if (!LifecycleExtensions.TryParse(product.Lifecycle, out _, ignoreCase: true, allowMatchingMetadataAttribute: true)
				|| !availableLifecycleStrings.Contains(product.Lifecycle, StringComparer.OrdinalIgnoreCase))
			{
				collector.EmitError(string.Empty, $"Lifecycle '{product.Lifecycle}' for product '{product.Product}' is not in the list of available lifecycles. Available lifecycles: {string.Join(", ", availableLifecycleStrings)}");
				return false;
			}
		}

		return true;
	}
}
