// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Configuration.Changelog;
using Elastic.Documentation.Configuration.ReleaseSchedule;
using Elastic.Documentation.Diagnostics;
using Microsoft.Extensions.Logging;
using SharpYaml;

namespace Elastic.Changelog.Bundling;

/// <summary>What happened to one product of <c>release unified bundle</c>.</summary>
public enum ProductBundleOutcome
{
	Bundled,

	/// <summary>The product is not onboarded to changelogs. A warning says why, and the run goes on.</summary>
	Skipped,

	/// <summary>The commit range has no changelog entries. The product released with no notes, and the run goes on.</summary>
	Empty,
	Failed
}

/// <summary>
/// Bundles the release notes of one product for <c>release unified bundle</c>: finds its <c>changelog.yml</c>, takes the
/// first bundle profile, and bundles the commit range. A product that is not onboarded is skipped with a warning, or
/// fails when <c>failWhenNotOnboarded</c> is set. Not onboarded means no <c>changelog.yml</c>, no bundle profile, or no
/// entry pool on the CDN. The bundler itself reports the last one, with its own owner, repo, branch and CDN or local
/// sourcing rules, so an invalid configuration still fails before the pool is looked at.
/// </summary>
public sealed class UnifiedProductBundler(
	ChangelogBundlingService bundleService,
	HttpClient http,
	IFileSystem fileSystem,
	ILogger logger,
	IDiagnosticsCollector collector
)
{
	public async Task<ProductBundleOutcome> BundleAsync(
		ProductBundleRange range,
		string outputDirectory,
		string? changelogConfigsDir,
		bool failWhenNotOnboarded,
		Cancel ctx
	)
	{
		var outcome = await BundleCoreAsync(range, outputDirectory, changelogConfigsDir, failWhenNotOnboarded, ctx);

		// A product that wrote nothing leaves no folder behind.
		if (
			outcome is ProductBundleOutcome.Skipped or ProductBundleOutcome.Empty
			&& fileSystem.Directory.Exists(outputDirectory)
			&& !fileSystem.Directory.EnumerateFileSystemEntries(outputDirectory).Any()
		)
			fileSystem.Directory.Delete(outputDirectory);

		return outcome;
	}

	private async Task<ProductBundleOutcome> BundleCoreAsync(
		ProductBundleRange range,
		string outputDirectory,
		string? changelogConfigsDir,
		bool failWhenNotOnboarded,
		Cancel ctx
	)
	{
		var (productId, repoKey, _, _, commitHash) = range;
		var resolver = new ChangelogConfigResolver(http, fileSystem, logger, collector);
		var config = await resolver.ResolveAsync(productId, repoKey, commitHash, changelogConfigsDir, ctx);
		if (config.Failed)
			return ProductBundleOutcome.Failed;

		var changelogYaml = config.Content;
		if (changelogYaml is null)
			return NotOnboarded(
				$"Product '{productId}' (repo: elastic/{repoKey}@{commitHash[..8]}) has no changelog.yml — " +
					"add docs/changelog.yml or changelog.yml to the repository, or provide a prototype in --changelog-configs.",
				failWhenNotOnboarded
			);

		IReadOnlyList<string> profiles;
		try
		{
			profiles = ChangelogConfigurationLoader.ReadBundleProfileNames(changelogYaml);
		}
		catch (YamlException ex)
		{
			var msg = $"Product '{productId}' changelog.yml is not valid YAML: {ex.Message}";
			logger.LogError("{Message}", msg);
			collector.EmitError(string.Empty, msg);
			return ProductBundleOutcome.Failed;
		}

		if (profiles.Count == 0)
			return NotOnboarded(
				$"Product '{productId}' (repo: elastic/{repoKey}@{commitHash[..8]}) changelog.yml has no bundle.profiles section. " +
					"Add a bundle profile to the changelog.yml to opt into DRA bundling.",
				failWhenNotOnboarded
			);

		var profileName = profiles[0];
		if (profiles.Count > 1)
			logger.LogWarning(
				"'{Product}' changelog.yml has {Count} profiles; using the first: '{Profile}'",
				productId,
				profiles.Count,
				profileName
			);

		var tempConfig = fileSystem.Path.Combine(outputDirectory, $"changelog-config-{repoKey}-{commitHash[..8]}.yml");
		await fileSystem.File.WriteAllTextAsync(tempConfig, changelogYaml, ctx);

		try
		{
			string? poolMissing = null;
			var emptyRange = false;
			var arguments = UnifiedBundleArguments.Create(range, profileName, tempConfig, outputDirectory) with
			{
				OnPoolMissing = failWhenNotOnboarded ? null : message => poolMissing = message,
				OnEmptyRange = () => emptyRange = true
			};

			var success = await bundleService.BundleChangelogs(collector, arguments, ctx);
			if (poolMissing is not null)
				return NotOnboarded(
					$"Product '{productId}' (repo: elastic/{repoKey}): {poolMissing} Onboard the repository to the changelog workflow so its entries are uploaded.",
					fail: false
				);

			if (emptyRange)
				return ProductBundleOutcome.Empty;

			return success ? ProductBundleOutcome.Bundled : ProductBundleOutcome.Failed;
		}
		finally
		{
			fileSystem.File.Delete(tempConfig);
		}
	}

	private ProductBundleOutcome NotOnboarded(string message, bool fail)
	{
		if (!fail)
		{
			logger.LogWarning("{Message} Skipping it.", message);
			return ProductBundleOutcome.Skipped;
		}

		logger.LogError("{Message}", message);
		collector.EmitError(string.Empty, message);
		return ProductBundleOutcome.Failed;
	}
}
