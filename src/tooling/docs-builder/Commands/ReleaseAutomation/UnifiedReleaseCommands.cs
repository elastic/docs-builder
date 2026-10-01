// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using Elastic.Changelog.Bundling;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Changelog;
using Elastic.Documentation.Configuration.Products;
using Elastic.Documentation.Configuration.ReleaseSchedule;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.FileSystems;
using Microsoft.Extensions.Logging;
using Nullean.Argh;
using Nullean.Argh.Documentation;
using YamlDotNet.Core;

namespace Documentation.Builder.Commands.ReleaseAutomation;

/// <summary>Unified release automation commands.</summary>
internal sealed class UnifiedReleaseCommands(
	ILoggerFactory logFactory,
	IDiagnosticsCollector collector,
	IConfigurationContext configurationContext
)
{
	private const string FutureReleasesUrl = "https://elastic-release-api.s3.us-west-2.amazonaws.com/public/future-releases.json";
	private const string PastReleasesUrl = "https://elastic-release-api.s3.us-west-2.amazonaws.com/public/past-releases.json";
	private const string MasterSnapshotUrl = "https://snapshots.elastic.co/latest/master.json";

	private readonly ILogger _logger = logFactory.CreateLogger<UnifiedReleaseCommands>();

	/// <summary>
	/// Generate changelog bundles for all DRA products at the given version, for the current GA,
	/// or for the next upcoming release(s).
	/// </summary>
	/// <remarks>
	/// <para>
	/// <list type="bullet">
	///   <item><c>"current"</c> — bundles the latest GA version from <c>past-releases.json</c>.</item>
	///   <item>
	///     <c>"preview"</c> — bundles both the next patch and next minor releases. Each uses its BC
	///     manifest when one has been cut; otherwise the corresponding SNAPSHOT is used.
	///   </item>
	///   <item>
	///     Explicit version (e.g. <c>9.5.5</c>) — bundles using the latest BC manifest. Errors when
	///     no BC has been cut yet, and skips gracefully when the version is already GA.
	///   </item>
	/// </list>
	/// </para>
	/// <para>
	/// Only products with <c>features.release-notes: dra</c> in <c>products.yml</c> are bundled.
	/// Bundle files are written to <c>&lt;outputDir&gt;/&lt;product-id&gt;/</c>.
	/// </para>
	/// </remarks>
	/// <param name="versionOrKeyword">
	/// A version string (e.g. <c>9.5.5</c>), <c>"current"</c>, or <c>"preview"</c>.
	/// </param>
	/// <param name="outputDir">
	/// Root directory for bundle output. Defaults to <c>./bundles</c>. Each product writes to a
	/// <c>&lt;product-id&gt;/</c> subdirectory.
	/// </param>
	/// <param name="changelogConfigs">
	/// Optional directory of prototype <c>&lt;product-id&gt;.changelog.yml</c> files. When present,
	/// a product's local file takes precedence over the file fetched from GitHub, allowing teams to
	/// iterate on the configuration ahead of landing it in their repository.
	/// </param>
	[Hidden]
	[NoOptionsInjection]
	public async Task<int> Bundle(
		[Argument] string versionOrKeyword,
		[ExpandUserProfile] DirectoryInfo? outputDir = null,
		[ExpandUserProfile] DirectoryInfo? changelogConfigs = null,
		CancellationToken ctx = default
	)
	{
		var prestageProducts = configurationContext
			.ProductsConfiguration
			.Products
			.Values
			.Where(p => p.Features.ReleaseNotes == ReleaseNotesPath.DailyReleasableArtifacts)
			.ToArray();

		if (prestageProducts.Length == 0)
		{
			_logger.LogWarning("No products with 'release-notes: dra' found in products.yml. Nothing to bundle.");
			return 0;
		}

		_logger.LogInformation(
			"Found {Count} DRA product(s): {Products}",
			prestageProducts.Length,
			string.Join(", ", prestageProducts.Select(p => p.Id))
		);

		var rootOutput = outputDir ?? new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), "bundles"));
		if (!rootOutput.Exists)
			rootOutput.Create();

		_logger.LogInformation("Output root: {OutputDir}", rootOutput.FullName);

		using var http = new HttpClient();
		http.DefaultRequestHeaders.UserAgent.ParseAdd("docs-builder/1.0");

		var fileSystem = ChangelogFileSystem.FromWorkingDirectory();
		var bundleService = new ChangelogBundlingService(logFactory, fileSystem, configurationContext);

		var targets = await ResolveTargetsAsync(http, versionOrKeyword, ctx);
		if (targets is null)
			return 1;

		if (targets.Count == 0)
		{
			_logger.LogInformation("No bundle targets resolved. Nothing to do.");
			return 0;
		}

		foreach (var target in targets)
		{
			_logger.LogInformation(
				"Bundling target: version={Version} source={Source} manifest={Manifest}",
				target.Version,
				target.Source,
				target.ManifestUrl
			);

			var manifest = await FetchManifestAsync(http, target.ManifestUrl, ctx);
			if (manifest is null)
			{
				_logger.LogError("Failed to fetch build manifest from {ManifestUrl}", target.ManifestUrl);
				collector.EmitError(string.Empty, $"Failed to fetch build manifest from {target.ManifestUrl}");
				return 1;
			}

			_logger.LogInformation(
				"Manifest contains {Count} project(s): {Projects}",
				manifest.Projects.Count,
				string.Join(", ", manifest.Projects.Keys)
			);

			foreach (var product in prestageProducts)
			{
				var artifactKey = (product.DraArtifact ?? product.Repository ?? product.Id).ToLowerInvariant();
				var repoKey = (product.Repository ?? product.Id).ToLowerInvariant();

				if (!manifest.Projects.TryGetValue(artifactKey, out var project) || string.IsNullOrEmpty(project.CommitHash))
				{
					_logger.LogWarning(
						"Skipping '{Product}': artifact key '{ArtifactKey}' not found in the build manifest.",
						product.Id,
						artifactKey
					);
					continue;
				}

				_logger.LogInformation(
					"Bundling '{Product}' (artifact: '{ArtifactKey}', repo: '{RepoKey}', commit: {Commit})...",
					product.Id,
					artifactKey,
					repoKey,
					project.CommitHash[..8]
				);

				var productOutput = Path.Combine(rootOutput.FullName, product.Id);
				_ = Directory.CreateDirectory(productOutput);

				var result = await BundleProductAsync(
					http,
					bundleService,
					product.Id,
					repoKey,
					project.CommitHash,
					target.Version,
					productOutput,
					changelogConfigs?.FullName,
					ctx
				);

				if (result != 0)
					return result;

				_logger.LogInformation("Bundled '{Product}' @ {Version} successfully.", product.Id, target.Version);
			}
		}

		return 0;
	}

	private async Task<IReadOnlyList<ReleaseTarget>?> ResolveTargetsAsync(HttpClient http, string versionOrKeyword, CancellationToken ctx)
	{
		if (versionOrKeyword.Equals("current", StringComparison.OrdinalIgnoreCase))
			return await ResolveCurrentTargetAsync(http, ctx);

		if (versionOrKeyword.Equals("preview", StringComparison.OrdinalIgnoreCase))
			return await ResolvePreviewTargetsAsync(http, ctx);

		// Explicit version — guard against GA then require a BC.
		var past = await FetchJsonAsync<PastReleasesResponse>(
			http,
			PastReleasesUrl,
			ReleaseScheduleJsonContext.Default.PastReleasesResponse,
			ctx
		);

		if (past?.Releases.Any(r => r.Version.Equals(versionOrKeyword, StringComparison.OrdinalIgnoreCase)) is true)
		{
			_logger.LogInformation("Version {Version} is already GA. No bundle needed.", versionOrKeyword);
			return [];
		}

		var future = await FetchJsonAsync<FutureReleasesResponse>(
			http,
			FutureReleasesUrl,
			ReleaseScheduleJsonContext.Default.FutureReleasesResponse,
			ctx
		);

		var release = future?.Releases.FirstOrDefault(r => r.Version.Equals(versionOrKeyword, StringComparison.OrdinalIgnoreCase));

		if (release is null)
		{
			_logger.LogError("Version {Version} not found in the release schedule ({Url})", versionOrKeyword, FutureReleasesUrl);
			collector.EmitError(string.Empty, $"Version '{versionOrKeyword}' not found in the release schedule ({FutureReleasesUrl}).");
			return null;
		}

		if (!release.HasBuildCandidate || release.LatestBuildCandidate is not { } bc)
		{
			_logger.LogError("No build candidate has been cut for {Version} yet", versionOrKeyword);
			collector.EmitError(
				string.Empty,
				$"No build candidate has been cut for {versionOrKeyword} yet. Use 'preview' to bundle against the latest SNAPSHOT."
			);
			return null;
		}

		return [new ReleaseTarget(versionOrKeyword, bc.ManifestUrl, ReleaseTargetSource.BuildCandidate)];
	}

	private async Task<IReadOnlyList<ReleaseTarget>?> ResolveCurrentTargetAsync(HttpClient http, CancellationToken ctx)
	{
		var past = await FetchJsonAsync<PastReleasesResponse>(
			http,
			PastReleasesUrl,
			ReleaseScheduleJsonContext.Default.PastReleasesResponse,
			ctx
		);

		var target = ReleaseTargetResolver.ResolveCurrentGa(past);
		if (target is null)
		{
			_logger.LogError("Could not resolve the current GA version from {Url}", PastReleasesUrl);
			collector.EmitError(string.Empty, $"Could not resolve the current GA version from {PastReleasesUrl}.");
			return null;
		}

		_logger.LogInformation("Current GA: {Version}", target.Version);
		return [target];
	}

	private async Task<IReadOnlyList<ReleaseTarget>> ResolvePreviewTargetsAsync(HttpClient http, CancellationToken ctx)
	{
		var past = await FetchJsonAsync<PastReleasesResponse>(
			http,
			PastReleasesUrl,
			ReleaseScheduleJsonContext.Default.PastReleasesResponse,
			ctx
		);

		var future = await FetchJsonAsync<FutureReleasesResponse>(
			http,
			FutureReleasesUrl,
			ReleaseScheduleJsonContext.Default.FutureReleasesResponse,
			ctx
		);

		var currentGa = ReleaseTargetResolver.ResolveCurrentGa(past);
		var currentGaVersion = currentGa?.Version;
		_logger.LogInformation("Current GA (for preview context): {Version}", currentGaVersion ?? "unknown");

		// Determine the patch-branch snapshot URL from the current GA (e.g. 9.2.json for 9.2.x).
		LatestBuildPointer? patchPointer = null;
		if (currentGaVersion is not null && ReleaseTargetResolver.TryParseVersion(currentGaVersion) is { } gaVersion)
		{
			var patchUrl = $"https://snapshots.elastic.co/latest/{gaVersion.Major}.{gaVersion.Minor}.json";
			patchPointer = await FetchJsonAsync<LatestBuildPointer>(
				http,
				patchUrl,
				ReleaseScheduleJsonContext.Default.LatestBuildPointer,
				ctx
			);
		}

		var minorPointer = await FetchJsonAsync<LatestBuildPointer>(
			http,
			MasterSnapshotUrl,
			ReleaseScheduleJsonContext.Default.LatestBuildPointer,
			ctx
		);

		var targets = ReleaseTargetResolver.ResolvePreviewTargets(past, future, patchPointer, minorPointer, currentGaVersion);

		if (targets.Count == 0)
			_logger.LogWarning("No preview targets resolved. Neither SNAPSHOT nor BC is available.");
		else
		{
			foreach (var t in targets)
				_logger.LogInformation("Preview target: {Version} ({Source})", t.Version, t.Source);
		}

		return targets;
	}

	private async Task<ElasticBuildManifest?> FetchManifestAsync(HttpClient http, string url, CancellationToken ctx)
	{
		try
		{
			return await FetchJsonAsync<ElasticBuildManifest>(http, url, ReleaseScheduleJsonContext.Default.ElasticBuildManifest, ctx);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to fetch manifest from {Url}", url);
			return null;
		}
	}

	private async Task<string?> ResolveChangelogConfigAsync(
		HttpClient http,
		string productId,
		string repoKey,
		string commitHash,
		string? changelogConfigsDir,
		CancellationToken ctx
	)
	{
		// Local prototype file wins over the GitHub-fetched config.
		if (changelogConfigsDir is not null)
		{
			var localPath = Path.Combine(changelogConfigsDir, $"{productId}.changelog.yml");
			if (File.Exists(localPath))
			{
				_logger.LogInformation("Using prototype changelog config for '{Product}': {Path}", productId, localPath);
				return await File.ReadAllTextAsync(localPath, ctx);
			}
		}

		// Fall back to the changelog.yml in the product's GitHub repo at the manifest commit.
		var candidates = new[]
		{
			$"https://raw.githubusercontent.com/elastic/{repoKey}/{commitHash}/docs/changelog.yml",
			$"https://raw.githubusercontent.com/elastic/{repoKey}/{commitHash}/changelog.yml",
		};

		foreach (var url in candidates)
		{
			try
			{
				var response = await http.GetAsync(url, ctx);
				if (response.IsSuccessStatusCode)
					return await response.Content.ReadAsStringAsync(ctx);
			}
			catch
			{
				// network error — fall through to next candidate
			}
		}

		return null;
	}

	private async Task<int> BundleProductAsync(
		HttpClient http,
		ChangelogBundlingService bundleService,
		string productId,
		string repoKey,
		string commitHash,
		string version,
		string outputDirectory,
		string? changelogConfigsDir,
		CancellationToken ctx
	)
	{
		var changelogYaml = await ResolveChangelogConfigAsync(http, productId, repoKey, commitHash, changelogConfigsDir, ctx);
		if (changelogYaml is null)
		{
			var msg = $"Product '{productId}' (repo: elastic/{repoKey}@{commitHash[..8]}) has no changelog.yml — "
				+ "add docs/changelog.yml or changelog.yml to the repository, or provide a prototype in --changelog-configs.";
			_logger.LogError("{Message}", msg);
			collector.EmitError(string.Empty, msg);
			return 1;
		}

		IReadOnlyList<string> profiles;
		try
		{
			profiles = ChangelogConfigurationLoader.ReadBundleProfileNames(changelogYaml);
		}
		catch (YamlException ex)
		{
			var msg = $"Product '{productId}' changelog.yml is not valid YAML: {ex.Message}";
			_logger.LogError("{Message}", msg);
			collector.EmitError(string.Empty, msg);
			return 1;
		}

		if (profiles.Count == 0)
		{
			var msg = $"Product '{productId}' (repo: elastic/{repoKey}@{commitHash[..8]}) changelog.yml has no bundle.profiles section. "
				+ "Add a bundle profile to the changelog.yml to opt into DRA bundling.";
			_logger.LogError("{Message}", msg);
			collector.EmitError(string.Empty, msg);
			return 1;
		}

		var profileName = profiles[0];
		if (profiles.Count > 1)
			_logger.LogWarning(
				"'{Product}' changelog.yml has {Count} profiles; using the first: '{Profile}'",
				productId,
				profiles.Count,
				profileName
			);

		var tempConfig = Path.Combine(outputDirectory, $"changelog-config-{repoKey}-{commitHash[..8]}.yml");
		await File.WriteAllTextAsync(tempConfig, changelogYaml, ctx);

		try
		{
			var arguments = new BundleChangelogsArguments
			{
				Profile = profileName,
				ProfileArgument = version,
				OutputDirectory = outputDirectory,
				Config = tempConfig,
			};

			var success = await bundleService.BundleChangelogs(collector, arguments, ctx);
			return success ? 0 : 1;
		}
		finally
		{
			File.Delete(tempConfig);
		}
	}

	private static async Task<T?> FetchJsonAsync<T>(
		HttpClient http,
		string url,
		System.Text.Json.Serialization.Metadata.JsonTypeInfo<T> typeInfo,
		CancellationToken ctx
	) where T : class
	{
		try
		{
			var response = await http.GetAsync(url, ctx);
			if (!response.IsSuccessStatusCode)
				return null;
			var stream = await response.Content.ReadAsStreamAsync(ctx);
			return await JsonSerializer.DeserializeAsync(stream, typeInfo, ctx);
		}
		catch
		{
			return null;
		}
	}
}
