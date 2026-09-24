// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using Elastic.Changelog.Bundling;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Products;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.FileSystems;
using Microsoft.Extensions.Logging;
using Nullean.Argh;
using Nullean.Argh.Documentation;

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

	private readonly ILogger _logger = logFactory.CreateLogger<UnifiedReleaseCommands>();

	/// <summary>
	/// Generate changelog bundles for all prestage products at the given version or for the next upcoming release.
	/// </summary>
	/// <remarks>
	/// <para>
	/// When <paramref name="versionOrPreview"/> is <c>"preview"</c>, the command resolves the next upcoming
	/// version automatically:
	/// <list type="bullet">
	///   <item>If a build candidate has been cut (appears in <c>future-releases.json</c> with a non-empty
	///   <c>build_candidates</c> dict), the BC manifest anchors the git refs.</item>
	///   <item>Otherwise the latest SNAPSHOT from <c>snapshots.elastic.co/latest/master.json</c> is used,
	///   keeping a preview PR open well before the BC is cut.</item>
	/// </list>
	/// </para>
	/// <para>
	/// When a specific version is given (e.g. <c>9.5.5</c>), the command looks up the version in
	/// <c>future-releases.json</c> and uses the latest BC manifest. Exits with an error if no BC
	/// has been cut yet.
	/// </para>
	/// <para>
	/// Only products with <c>features.release-notes: dra</c> in <c>products.yml</c> are bundled.
	/// Bundle files are written to <paramref name="outputDir"/>.
	/// </para>
	/// </remarks>
	/// <param name="versionOrPreview">
	/// A specific version string (e.g. <c>9.5.5</c>) or the literal <c>"preview"</c> to target
	/// the next upcoming release.
	/// </param>
	/// <param name="outputDir">
	/// Directory where bundle YAML files are written.
	/// Defaults to <c>./bundles</c> relative to the current working directory.
	/// </param>
	[Hidden]
	[NoOptionsInjection]
	public async Task<int> Bundle(
		[Argument] string versionOrPreview,
		[ExpandUserProfile] DirectoryInfo? outputDir = null,
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
			"Found {Count} prestage product(s): {Products}",
			prestageProducts.Length,
			string.Join(", ", prestageProducts.Select(p => p.Id))
		);

		var isPreview = versionOrPreview.Equals("preview", StringComparison.OrdinalIgnoreCase);

		using var http = new HttpClient();
		http.DefaultRequestHeaders.UserAgent.ParseAdd("docs-builder/1.0");

		// For explicit versions, guard against bundling an already-GA'd release.
		// past-releases.json may lag by a few hours after GA, but this prevents
		// redundant workflow runs once it propagates.
		if (!isPreview && await IsReleasedAsync(http, versionOrPreview, ctx))
		{
			_logger.LogInformation("Version {Version} is already GA. No bundle needed.", versionOrPreview);
			return 0;
		}

		// Resolve the target version + BC manifest URL
		string resolvedVersion;
		string manifestUrl;

		if (isPreview)
		{
			(resolvedVersion, manifestUrl) = await ResolvePreviewAsync(http, ctx);
		}
		else
		{
			var result = await ResolveBcVersionAsync(http, versionOrPreview, ctx);
			if (result is null)
				return 1;
			(resolvedVersion, manifestUrl) = result.Value;
		}

		_logger.LogInformation("Resolved version: {Version}", resolvedVersion);
		_logger.LogInformation("Manifest: {ManifestUrl}", manifestUrl);

		// Fetch the manifest to get per-product commit hashes
		var manifest = await FetchManifestAsync(http, manifestUrl, ctx);
		if (manifest is null)
		{
			collector.EmitError(string.Empty, $"Failed to fetch build manifest from {manifestUrl}");
			return 1;
		}

		_logger.LogInformation(
			"Manifest contains {Count} project(s): {Projects}",
			manifest.Projects.Count,
			string.Join(", ", manifest.Projects.Keys)
		);

		var output = outputDir ?? new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), "bundles"));
		if (!output.Exists)
			output.Create();

		_logger.LogInformation("Output directory: {OutputDir}", output.FullName);

		var fileSystem = ChangelogFileSystem.FromWorkingDirectory();
		var bundleService = new ChangelogBundlingService(logFactory, fileSystem, configurationContext);

		var exitCode = 0;

		foreach (var product in prestageProducts)
		{
			// DRA manifest key: prefer explicit dra-artifact, then repository, then product id.
			var artifactKey = (product.DraArtifact ?? product.Repository ?? product.Id).ToLowerInvariant();
			// GitHub repo for changelog.yml lookup: prefer repository, then product id.
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

			var result = await BundleProductAsync(
				http,
				bundleService,
				product.Id,
				repoKey,
				project.CommitHash,
				resolvedVersion,
				output.FullName,
				ctx
			);

			if (result != 0)
				exitCode = result;
			else
				_logger.LogInformation("Bundled '{Product}' successfully.", product.Id);
		}

		return exitCode;
	}

	private async Task<(string version, string manifestUrl)> ResolvePreviewAsync(HttpClient http, CancellationToken ctx)
	{
		// Check if any future release for the master-tracked version already has a BC
		var future = await FetchJsonAsync<FutureReleasesResponse>(
			http,
			FutureReleasesUrl,
			ReleaseScheduleJsonContext.Default.FutureReleasesResponse,
			ctx
		);

		if (future is not null)
		{
			// The snapshot branch "master" tracks the next minor (currently the highest non-patch future version).
			// Find upcoming releases sorted ascending and pick the first one with a BC, if any.
			var withBc = future
				.Releases
				.Where(r => r.HasBuildCandidate)
				.OrderBy(r => r.Version, StringComparer.OrdinalIgnoreCase)
				.FirstOrDefault();

			if (withBc?.LatestBuildCandidate is { } bc)
			{
				_logger.LogInformation("Preview: BC found for {Version}, using BC manifest.", withBc.Version);
				return (withBc.Version, bc.ManifestUrl);
			}
		}

		// No BC yet — fall back to the latest SNAPSHOT on master
		_logger.LogInformation("Preview: no BC cut yet, using latest master SNAPSHOT.");
		var pointer = await FetchJsonAsync<LatestBuildPointer>(
			http,
			"https://snapshots.elastic.co/latest/master.json",
			ReleaseScheduleJsonContext.Default.LatestBuildPointer,
			ctx
		);

		return pointer is not null
			? (pointer.Version.Replace("-SNAPSHOT", ""), pointer.ManifestUrl)
			: throw new InvalidOperationException(
				"Could not resolve preview version: no BC in future-releases.json and " +
					"https://snapshots.elastic.co/latest/master.json returned no data."
			);
	}

	private async Task<(string version, string manifestUrl)?> ResolveBcVersionAsync(HttpClient http, string version, CancellationToken ctx)
	{
		var future = await FetchJsonAsync<FutureReleasesResponse>(
			http,
			FutureReleasesUrl,
			ReleaseScheduleJsonContext.Default.FutureReleasesResponse,
			ctx
		);

		var release = future?.Releases.FirstOrDefault(r => r.Version.Equals(version, StringComparison.OrdinalIgnoreCase));

		if (release is null)
		{
			collector.EmitError(string.Empty, $"Version '{version}' not found in the release schedule ({FutureReleasesUrl}).");
			return null;
		}

		if (!release.HasBuildCandidate || release.LatestBuildCandidate is not { } bc)
		{
			collector.EmitError(
				string.Empty,
				$"No build candidate has been cut for {version} yet. " + $"Use 'preview' to bundle against the latest SNAPSHOT."
			);
			return null;
		}

		return (version, bc.ManifestUrl);
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

	private async Task<string?> FetchChangelogConfigAsync(HttpClient http, string repoKey, string commitHash, CancellationToken ctx)
	{
		// Try docs/changelog.yml first, then changelog.yml at the root.
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
		CancellationToken ctx
	)
	{
		var changelogYaml = await FetchChangelogConfigAsync(http, repoKey, commitHash, ctx);
		if (changelogYaml is null)
		{
			collector.EmitError(
				string.Empty,
				$"Product '{productId}' (repo: elastic/{repoKey}@{commitHash[..8]}) has no changelog.yml. " +
					"Add docs/changelog.yml or changelog.yml to the repository to opt into DRA bundling."
			);
			return 1;
		}

		// Write the fetched config to a temp file so the bundling service can load it.
		var tempConfig = Path.Combine(Path.GetTempPath(), $"changelog-{repoKey}-{commitHash[..8]}.yml");
		await File.WriteAllTextAsync(tempConfig, changelogYaml, ctx);

		try
		{
			var arguments = new BundleChangelogsArguments
			{
				Profile = "dra-release",
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

	private async Task<bool> IsReleasedAsync(HttpClient http, string version, CancellationToken ctx)
	{
		var past = await FetchJsonAsync<PastReleasesResponse>(
			http,
			PastReleasesUrl,
			ReleaseScheduleJsonContext.Default.PastReleasesResponse,
			ctx
		);
		return past?.Releases.Any(r => r.Version.Equals(version, StringComparison.OrdinalIgnoreCase)) is true;
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
