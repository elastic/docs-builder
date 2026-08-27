// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Net;
using Elastic.Changelog.GitHub;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Products;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Services;
using Microsoft.Extensions.Logging;

namespace Elastic.Changelog.Onboarding;

/// <summary>Arguments for validating release-notes onboarding.</summary>
public record ValidateOnboardingArguments
{
	/// <summary>GitHub owner (org) the product repositories live under.</summary>
	public string Owner { get; init; } = "elastic";
}

/// <summary>
/// Validates that every product registered with <c>features.release-notes: prestage</c> in
/// <c>products.yml</c> actually has the scaffolding the Prestage path requires in its repository:
/// the changelog configuration plus the entry-generation, upload, and bundle-stage workflows.
/// A Prestage product without them would silently be skipped by the Prestage Release Orchestrator
/// (or fail at freeze), so drift is surfaced here as a CI-gateable error.
/// </summary>
public class ChangelogOnboardingValidationService(
	ILoggerFactory logFactory,
	IConfigurationContext configurationContext,
	GitHubApiTransport? transport = null
) : IService
{
	/// <summary>Workflow files every Prestage repository must carry (RFC onboarding steps).</summary>
	internal static readonly string[] RequiredWorkflows =
	[
		".github/workflows/changelog-validate.yml",
		".github/workflows/changelog-submit.yml",
		".github/workflows/changelog-upload.yml",
		".github/workflows/changelog-bundle-stage.yml"
	];

	/// <summary>Accepted changelog configuration locations, in discovery order.</summary>
	internal static readonly string[] ChangelogConfigCandidates = ["docs/changelog.yml", "changelog.yml"];

	private readonly ILogger _logger = logFactory.CreateLogger<ChangelogOnboardingValidationService>();
	private readonly GitHubApiTransport _transport = transport ?? new GitHubApiTransport();

	public async Task<bool> ValidateOnboardingAsync(IDiagnosticsCollector collector, ValidateOnboardingArguments args, Cancel ctx)
	{
		var prestageProducts = configurationContext.ProductsConfiguration
			.Products
			.Values
			.Where(p => p.Features.ReleaseNotes == ReleaseNotesPath.Prestage)
			.OrderBy(p => p.Id, StringComparer.Ordinal)
			.ToList();

		if (prestageProducts.Count == 0)
		{
			_logger.LogInformation("No products declare 'features.release-notes: prestage' in products.yml; nothing to validate.");
			return true;
		}

		_logger.LogInformation("Validating release-notes onboarding for {Count} Prestage product(s)", prestageProducts.Count);

		var valid = true;
		foreach (var product in prestageProducts)
		{
			ctx.ThrowIfCancellationRequested();
			var repo = product.Repository ?? product.Id;
			if (!await ValidateProduct(collector, product.Id, args.Owner, repo, ctx))
				valid = false;
		}

		return valid;
	}

	private async Task<bool> ValidateProduct(IDiagnosticsCollector collector, string productId, string owner, string repo, Cancel ctx)
	{
		var missing = new List<string>();
		foreach (var workflow in RequiredWorkflows)
		{
			var exists = await FileExistsAsync(collector, owner, repo, workflow, ctx);
			if (exists == null)
				return false;
			if (exists == false)
				missing.Add(workflow);
		}

		var hasConfig = false;
		foreach (var candidate in ChangelogConfigCandidates)
		{
			var exists = await FileExistsAsync(collector, owner, repo, candidate, ctx);
			if (exists == null)
				return false;
			if (exists == true)
			{
				hasConfig = true;
				break;
			}
		}

		if (!hasConfig)
			missing.Add(string.Join(" or ", ChangelogConfigCandidates));

		if (missing.Count > 0)
		{
			collector.EmitError(
				string.Empty,
				$"Product '{productId}' declares 'features.release-notes: prestage' but {owner}/{repo} is missing required onboarding file(s): {string.Join(", ", missing)}. " +
					"See the Prestage onboarding steps in the release-notes documentation, or change the product's release-notes path in products.yml."
			);
			return false;
		}

		_logger.LogInformation("Product '{ProductId}' ({Owner}/{Repo}): Prestage onboarding files present", productId, owner, repo);
		return true;
	}

	/// <summary>
	/// Probes a repository path via the GitHub contents API. Returns null after emitting an error
	/// for any response other than found/not-found — an unreadable repo (bad credentials, rate
	/// limiting) must fail the validation run rather than pass it vacuously.
	/// </summary>
	private async Task<bool?> FileExistsAsync(IDiagnosticsCollector collector, string owner, string repo, string path, Cancel ctx)
	{
		var url = $"https://api.github.com/repos/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(repo)}/contents/{path}";
		using var response = await _transport.GetAsync(url, ctx);
		if (response.StatusCode == HttpStatusCode.OK)
			return true;
		if (response.StatusCode == HttpStatusCode.NotFound)
			return false;

		collector.EmitError(
			string.Empty,
			$"Could not probe {owner}/{repo} for '{path}': {(int)response.StatusCode} {response.ReasonPhrase}. " +
				"Ensure GITHUB_TOKEN is set and can read the repository."
		);
		return null;
	}
}
