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
/// Validates that every product registered with <c>features.release-notes: prestage</c> or
/// <c>on-release</c> in <c>products.yml</c> has the required onboarding files in its repository.
/// A product without them would silently be skipped or fail at bundle time.
/// </summary>
public class ChangelogOnboardingValidationService(
	ILoggerFactory logFactory,
	IConfigurationContext configurationContext,
	GitHubApiTransport? transport = null
) : IService
{
	/// <summary>
	/// Workflow files a Prestage repository must carry. Covers both the legacy
	/// <c>changelog-*.yml</c> callers (existing onboarded repos) and the new
	/// <c>release-notes.yml</c> shape introduced by the shared workflow consolidation.
	/// Legacy names are checked first; the new names are the forward target.
	/// </summary>
	internal static readonly string[] RequiredWorkflowsPrestage =
	[
		".github/workflows/changelog-validate.yml",
		".github/workflows/changelog-submit.yml",
		".github/workflows/changelog-upload.yml",
		".github/workflows/changelog-bundle-stage.yml"
	];

	/// <summary>
	/// Workflow files a Prestage repository must carry using the new shared-workflow shape.
	/// Checked when the legacy files are absent (i.e. the repo has been migrated).
	/// </summary>
	internal static readonly string[] RequiredWorkflowsPrestageNew =
	[
		".github/workflows/release-notes.yml",
		".github/workflows/release-notes-changelog-file.yml",
		".github/workflows/changelog-bundle-stage.yml"
	];

	/// <summary>Workflow files an On-release repository must carry.</summary>
	internal static readonly string[] RequiredWorkflowsOnRelease = [".github/workflows/release-notes.yml"];

	/// <summary>Accepted changelog configuration locations, in discovery order.</summary>
	internal static readonly string[] ChangelogConfigCandidates = ["docs/changelog.yml", "changelog.yml"];

	// Keep the legacy property name for test compatibility
	internal static string[] RequiredWorkflows => RequiredWorkflowsPrestage;

	private readonly ILogger _logger = logFactory.CreateLogger<ChangelogOnboardingValidationService>();
	private readonly GitHubApiTransport _transport = transport ?? new GitHubApiTransport();

	public async Task<bool> ValidateOnboardingAsync(IDiagnosticsCollector collector, ValidateOnboardingArguments args, Cancel ctx)
	{
		var managedProducts = configurationContext
			.ProductsConfiguration
			.Products
			.Values
			.Where(p => p.Features.ReleaseNotes is ReleaseNotesPath.Prestage or ReleaseNotesPath.OnRelease)
			.OrderBy(p => p.Id, StringComparer.Ordinal)
			.ToList();

		if (managedProducts.Count == 0)
		{
			_logger.LogInformation(
				"No products declare 'features.release-notes: prestage' or 'on-release' in products.yml; nothing to validate."
			);
			return true;
		}

		_logger.LogInformation("Validating release-notes onboarding for {Count} product(s)", managedProducts.Count);

		var valid = true;
		foreach (var product in managedProducts)
		{
			ctx.ThrowIfCancellationRequested();
			var repo = product.Repository ?? product.Id;
			if (!await ValidateProduct(collector, product.Id, product.Features.ReleaseNotes, args.Owner, repo, ctx))
				valid = false;
		}

		return valid;
	}

	private async Task<bool> ValidateProduct(
		IDiagnosticsCollector collector,
		string productId,
		ReleaseNotesPath path,
		string owner,
		string repo,
		Cancel ctx
	)
	{
		var missing = new List<string>();

		// Choose the workflow set to check based on path and whether legacy or new shape is present
		string[] workflowsToCheck;
		if (path == ReleaseNotesPath.OnRelease)
		{
			workflowsToCheck = RequiredWorkflowsOnRelease;
		}
		else
		{
			// Prestage: check new shape first; fall back to legacy names if legacy names are present
			var legacyPrimaryExists = await FileExistsAsync(collector, owner, repo, RequiredWorkflowsPrestage[0], ctx);
			if (legacyPrimaryExists == null)
				return false; // probe error already emitted
			workflowsToCheck = legacyPrimaryExists == true ? RequiredWorkflowsPrestage : RequiredWorkflowsPrestageNew;
		}

		foreach (var workflow in workflowsToCheck)
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
			var pathLabel = path == ReleaseNotesPath.Prestage ? "prestage" : "on-release";
			collector.EmitError(
				string.Empty,
				$"Product '{productId}' declares 'features.release-notes: {pathLabel}' but {owner}/{repo} is missing required onboarding file(s): {string.Join(", ", missing)}. " +
					"See the release-notes onboarding documentation, or change the product's release-notes path in products.yml."
			);
			return false;
		}

		_logger.LogInformation("Product '{ProductId}' ({Owner}/{Repo}): {Path} onboarding files present", productId, owner, repo, path);
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
