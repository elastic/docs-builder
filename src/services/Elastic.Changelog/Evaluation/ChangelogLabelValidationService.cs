// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Actions.Core.Services;
using Elastic.Changelog.Creation;
using Elastic.Changelog.Utilities;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Changelog;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.FileSystems;
using Elastic.Documentation.Services;
using Microsoft.Extensions.Logging;

namespace Elastic.Changelog.Evaluation;

/// <summary>
/// Service implementing the changelog validate-labels CI command.
/// Validates only label/type/product resolution — no GitHub API access, no title, no entry-pool lookup.
/// Suitable as a label-only gate on <c>pull_request</c> events.
/// </summary>
public class ChangelogLabelValidationService(
	ILoggerFactory logFactory,
	IConfigurationContext configurationContext,
	ICoreService coreService,
	IRunnerTempFileSystem fileSystem,
	IEnvironmentVariables? env = null
) : IService
{
	private readonly ILogger _logger = logFactory.CreateLogger<ChangelogLabelValidationService>();
	private readonly ChangelogConfigurationLoader _configLoader = new(logFactory, configurationContext, fileSystem);
	private readonly GithubDecisionMetadataWriter _metadataWriter = new(logFactory, fileSystem);

	/// <summary>
	/// Validates that the PR's labels contain a recognised type label, optionally with product labels.
	/// Exits non-zero only on <c>no-label</c>; all other paths (skipped, ok) return zero.
	/// When running on CI (<c>GITHUB_ACTIONS</c> is set), writes a <see cref="GithubDecisionMetadata"/>
	/// file for the downstream <c>changelog github-comment</c> command to pick up.
	/// </summary>
	public async Task<bool> ValidateLabels(IDiagnosticsCollector collector, ValidateLabelsArguments input, Cancel ctx)
	{
		var config = await _configLoader.LoadChangelogConfiguration(collector, input.Config, ctx) ?? ChangelogConfiguration.Default;
		var defaultBranch = config.Bundle?.Branch ?? "main";

		// Label-based skip check: all products blocked → skipped
		var skipLabels = ChangelogPrEvaluationService.CollectExcludeLabels(config.Rules?.Create);
		if (PrInfoProcessor.AreAllProductsBlocked(input.PrLabels, config.Rules?.Create))
		{
			_logger.LogInformation("All products blocked by label rules; skipping");
			await Finish("skipped", skipLabels: skipLabels);
			return true;
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
			await Finish(
				"no-label",
				ambiguousTypeLabels: ambiguousTypeLabels,
				skipLabels: skipLabels,
				labelKeys: ChangelogPrEvaluationService.BuildLabelKeys(config.LabelToType)
			);
			return false;
		}

		// Resolve products
		string? resolvedProducts = null;
		string? productLabelTable = null;
		if (config.LabelToProducts is { Count: > 0 } labelToProducts)
		{
			var products = PrInfoProcessor.MapLabelsToProducts(input.PrLabels, labelToProducts);
			if (products.Count > 0)
			{
				resolvedProducts = ProductArgument.FormatProductSpecs(products);
			}
			else
			{
				var distinctSpecs = labelToProducts.Values.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
				if (distinctSpecs.Count == 1)
					resolvedProducts = ProductArgument.FormatProductSpecs(ProductArgument.ParseProductSpecs(distinctSpecs[0]));
				else
					productLabelTable = ChangelogPrEvaluationService.BuildProductLabelTable(labelToProducts);
			}
		}

		if (resolvedType == null)
		{
			_logger.LogInformation("No type label found on PR");
			collector.EmitError(
				string.Empty,
				"No matching changelog type label found on this PR. Add a label from your changelog.yml pivot.types, or a skip label."
			);
			var labelTable = ChangelogPrEvaluationService.BuildLabelTable(config.LabelToType);
			var labelKeys = ChangelogPrEvaluationService.BuildLabelKeys(config.LabelToType);
			await Finish(
				"no-label",
				labelTable: labelTable,
				labelKeys: labelKeys,
				productLabelTable: productLabelTable,
				skipLabels: skipLabels
			);
			return false;
		}

		if (productLabelTable != null && (config.ProductsConfiguration?.Default is null or { Count: 0 }))
		{
			_logger.LogInformation("Multiple products configured but no matching product label on PR");
			collector.EmitError(
				string.Empty,
				"No matching product label found on this PR. Add a label from your changelog.yml pivot.products."
			);
			await Finish("no-label", productLabelTable: productLabelTable, skipLabels: skipLabels);
			return false;
		}

		_logger.LogInformation("Label validation complete: type={Type}, products={Products}", resolvedType, resolvedProducts);
		await Finish("ok", type: resolvedType, products: resolvedProducts, skipLabels: skipLabels);
		return true;

		async Task Finish(
			string status,
			string? type = null,
			string? products = null,
			string? labelTable = null,
			string? labelKeys = null,
			string? productLabelTable = null,
			string? skipLabels = null,
			string? ambiguousTypeLabels = null
		)
		{
			_ = await SetOutputs(status, type, products, labelTable, productLabelTable, skipLabels, ambiguousTypeLabels);

			if (env?.IsRunningOnCI == true && input.PrNumber > 0)
			{
				var metadata = new GithubDecisionMetadata
				{
					Gate = ValidationGate.Labels,
					PrNumber = input.PrNumber,
					HeadRef = input.HeadRef,
					HeadSha = input.HeadSha,
					Status = status,
					IsFork = input.IsFork,
					CanCommit = input.CanCommit,
					MaintainerCanModify = input.MaintainerCanModify,
					HeadRepo = input.HeadRepo,
					LabelTable = labelKeys ?? labelTable,
					ProductLabelTable = productLabelTable,
					SkipLabels = skipLabels,
					ConfigFile = input.ConfigFile,
					AmbiguousTypeLabels = ambiguousTypeLabels,
					DefaultBranch = defaultBranch
				};
				await _metadataWriter.WriteAsync(metadata, ctx);
			}
		}
	}

	private async Task<bool> SetOutputs(
		string status,
		string? type = null,
		string? products = null,
		string? labelTable = null,
		string? productLabelTable = null,
		string? skipLabels = null,
		string? ambiguousTypeLabels = null
	)
	{
		await coreService.SetOutputAsync("status", status);
		if (type != null)
			await coreService.SetOutputAsync("type", OutputSanitizer.SanitizeForOutput(type, OutputSanitizer.TypeMaxLength));
		if (products != null)
			await coreService.SetOutputAsync("products", OutputSanitizer.SanitizeForOutput(products, OutputSanitizer.LabelsMaxLength));
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
		if (skipLabels != null)
			await coreService.SetOutputAsync("skip-labels", OutputSanitizer.SanitizeForOutput(skipLabels, OutputSanitizer.LabelsMaxLength));
		if (ambiguousTypeLabels != null)
			await coreService.SetOutputAsync(
				"ambiguous-type-labels",
				OutputSanitizer.SanitizeForOutput(ambiguousTypeLabels, OutputSanitizer.LabelsMaxLength)
			);
		return true;
	}
}
