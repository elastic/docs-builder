// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.Text.Json;
using Actions.Core.Services;
using Elastic.Changelog.Bundling;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Changelog;
using Elastic.Documentation.Configuration.Products;
using Elastic.Documentation.Configuration.ReleaseSchedule;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.FileSystems;
using Elastic.Documentation.Isolated;
using Elastic.Documentation.Services;
using Microsoft.Extensions.Logging;
using Nullean.Argh;
using Nullean.Argh.Documentation;
using YamlDotNet.Core;

namespace Documentation.Builder.Commands.ReleaseAutomation;

/// <summary>Unified release automation commands.</summary>
internal sealed class UnifiedReleaseCommands(
	ILoggerFactory logFactory,
	IDiagnosticsCollector collector,
	IConfigurationContext configurationContext,
	ICoreService githubActionsService,
	IEnvironmentVariables environmentVariables
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
	/// <para>
	/// The release notes of a version cover the commits between two build manifests. The end is the product's
	/// commit in the manifest of the target. The start is its commit in the manifest of the previous release:
	/// the previous patch of the same minor, or the previous minor's <c>.0</c> for a <c>x.y.0</c> release.
	/// A product whose commit is the same in both manifests has no changes and gets no bundle.
	/// The pull requests in that range come from the GitHub GraphQL API, so <c>GITHUB_TOKEN</c> must be set.
	/// The first profile of each product's <c>changelog.yml</c> shapes the bundle and must use <c>product:</c>
	/// or <c>output_products:</c>. A <c>products:</c> pattern cannot be combined with a commit range.
	/// </para>
	/// </remarks>
	/// <param name="versionOrKeyword">
	/// A version string (e.g. <c>9.5.5</c>), <c>"current"</c>, or <c>"preview"</c>.
	/// </param>
	/// <param name="outputDir">
	/// Root directory for bundle output. Defaults to <c>./bundles</c>. Each product writes to a
	/// <c>&lt;product-id&gt;/</c> subdirectory.
	/// </param>
	/// <param name="products">
	/// Limit the run to these products (repeatable). Every product with <c>release-notes: dra</c> is bundled by default.
	/// Use it to bundle only the products that have a changelog entry pool, or to try one product.
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
		string[]? products = null,
		CancellationToken ctx = default
	)
	{
		var (selected, unknown) = UnifiedBundlePlanner.FilterProducts(
			UnifiedBundlePlanner.SelectDraProducts(configurationContext.ProductsConfiguration.Products.Values),
			products
		);
		if (unknown.Count > 0)
		{
			var message = $"--products names no product with 'release-notes: dra' in products.yml: {string.Join(", ", unknown)}.";
			_logger.LogError("{Message}", message);
			collector.EmitError(string.Empty, message);
			return 1;
		}

		var prestageProducts = selected;

		if (prestageProducts.Count == 0)
		{
			_logger.LogWarning("No products with 'release-notes: dra' found in products.yml. Nothing to bundle.");
			return 0;
		}

		_logger.LogInformation(
			"Found {Count} DRA product(s): {Products}",
			prestageProducts.Count,
			string.Join(", ", prestageProducts.Select(p => p.Id))
		);

		var rootOutput = outputDir ?? new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), "bundles"));
		// Entries are read from the repository and bundles are written to the output folder, which may
		// live outside it: the scope covers the git root and that folder, like the documentation build does.
		var fileSystem = ChangelogFileSystem.FromWorkingDirectory(extraRoots: [rootOutput.FullName]);

		if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("GITHUB_TOKEN")))
		{
			const string message = "release unified bundle finds the pull requests between two releases through the GitHub GraphQL API, "
				+ "which does not accept anonymous requests. Set the GITHUB_TOKEN environment variable.";
			_logger.LogError("{Message}", message);
			collector.EmitError(string.Empty, message);
			return 1;
		}

		if (!rootOutput.Exists)
			rootOutput.Create();

		_logger.LogInformation("Output root: {OutputDir}", rootOutput.FullName);

		using var http = new HttpClient();
		http.DefaultRequestHeaders.UserAgent.ParseAdd("docs-builder/1.0");

		var bundleService = new ChangelogBundlingService(logFactory, fileSystem, configurationContext);

		var targets = await ResolveTargetsAsync(http, versionOrKeyword, ctx);
		if (targets is null)
			return 1;

		if (targets.Count == 0)
		{
			_logger.LogInformation("No bundle targets resolved. Nothing to do.");
			return 0;
		}

		var past = await FetchRequiredJsonAsync(http, PastReleasesUrl, ReleaseScheduleJsonContext.Default.PastReleasesResponse, ctx);
		if (past is null)
			return 1;

		// Plan every target before bundling anything: a product that blocks the plan fails the run
		// up front, not after minutes of bundling the others.
		var planned = new List<(ReleaseTarget Target, PastRelease Previous, UnifiedBundlePlan Plan)>();
		foreach (var target in targets)
		{
			_logger.LogInformation(
				"Planning target: version={Version} source={Source} manifest={Manifest}",
				target.Version,
				target.Source,
				target.ManifestUrl
			);

			var manifest = await FetchManifestAsync(http, target.ManifestUrl, ctx);
			if (manifest is null)
				return 1;

			var previous = ReleaseTargetResolver.ResolvePreviousRelease(past, target.Version);
			if (previous is null)
			{
				var message = $"No earlier GA release was found for {target.Version}, so the range of its release notes has no start.";
				_logger.LogError("{Message}", message);
				collector.EmitError(string.Empty, message);
				return 1;
			}

			var previousManifest = await FetchManifestAsync(http, previous.Manifest!, ctx);
			if (previousManifest is null)
				return 1;

			_logger.LogInformation("Release notes for {Version} start at the manifest of {Previous}", target.Version, previous.Version);

			var plan = UnifiedBundlePlanner.Plan(prestageProducts, target.Version, manifest, previousManifest);
			ReportProblems(target, previous, plan);
			planned.Add((target, previous, plan));
		}

		var blocked = planned.SelectMany(p => p.Plan.Problems.Select(problem => $"{problem.ProductId}@{p.Target.Version}")).ToArray();
		if (blocked.Length > 0)
		{
			_logger.LogError(
				"{Count} product(s) cannot be bundled because a manifest has no commit for them, so nothing was bundled: {Products}",
				blocked.Length,
				string.Join(", ", blocked)
			);
			return 1;
		}

		foreach (var (target, previous, plan) in planned)
		{
			foreach (var id in plan.Unchanged)
				_logger.LogInformation(
					"'{Product}' is unchanged between {Previous} and {Version}. No bundle is needed.",
					id,
					previous.Version,
					target.Version
				);

			foreach (var range in plan.Ranges)
			{
				_logger.LogInformation(
					"Bundling '{Product}' (repo: '{RepoKey}', range: {Start}..{End})...",
					range.ProductId,
					range.RepoKey,
					range.StartCommit[..8],
					range.EndCommit[..8]
				);

				var productOutput = Path.Combine(rootOutput.FullName, range.ProductId);
				_ = Directory.CreateDirectory(productOutput);

				var result = await BundleProductAsync(http, bundleService, range, productOutput, changelogConfigs?.FullName, ctx);
				if (result != 0)
					return result;

				_logger.LogInformation("Bundled '{Product}' @ {Version} successfully.", range.ProductId, target.Version);
			}
		}

		return 0;
	}

	/// <summary>
	/// Build the unified release notes site from the bundles that <c>release unified bundle</c> wrote.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The docset is created in memory from <c>products.yml</c>. Every product with
	/// <c>features.release-notes: dra</c> gets a release notes, known issues, breaking changes and
	/// deprecations page. No documentation folder is needed.
	/// </para>
	/// <para>
	/// The command resolves the argument like <c>release unified bundle</c>. A product without bundles is left
	/// out of the site, and the build fails only when no product has any bundle.
	/// </para>
	/// </remarks>
	/// <param name="versionOrKeyword">
	/// A version string (e.g. <c>9.5.5</c>), <c>"current"</c>, or <c>"preview"</c>.
	/// </param>
	/// <param name="bundlesDir">
	/// Folder that holds one sub-folder of bundle files per product. Defaults to <c>./bundles</c>.
	/// </param>
	/// <param name="output">Destination for the generated site. Defaults to <c>./site</c>. The folder is cleaned first.</param>
	/// <param name="pathPrefix">URL path prefix prepended to every generated link.</param>
	/// <param name="canonicalBaseUrl">Base URL written into canonical link tags.</param>
	[Hidden]
	[NoOptionsInjection]
	public async Task<int> Build(
		[Argument] string versionOrKeyword,
		[ExpandUserProfile] DirectoryInfo? bundlesDir = null,
		[ExpandUserProfile] DirectoryInfo? output = null,
		string? pathPrefix = null,
		Uri? canonicalBaseUrl = null,
		CancellationToken ctx = default
	)
	{
		using var http = new HttpClient();
		http.DefaultRequestHeaders.UserAgent.ParseAdd("docs-builder/1.0");

		var targets = await ResolveTargetsAsync(http, versionOrKeyword, ctx);
		if (targets is null)
			return 1;

		if (targets.Count == 0)
		{
			_logger.LogInformation("No release targets resolved. Nothing to build.");
			return 0;
		}

		var options = new UnifiedReleaseNotesBuildOptions
		{
			BundlesDirectory = bundlesDir ?? new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), "bundles")),
			Output = output ?? new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), "site")),
			ExpectedVersions = [.. targets.Select(t => t.Version)],
			PathPrefix = pathPrefix,
			CanonicalBaseUrl = canonicalBaseUrl
		};

		var service = new UnifiedReleaseNotesBuildService(logFactory, configurationContext, githubActionsService, environmentVariables);
		return await service.Build(collector, options, ctx) ? 0 : 1;
	}

	private async Task<IReadOnlyList<ReleaseTarget>?> ResolveTargetsAsync(HttpClient http, string versionOrKeyword, CancellationToken ctx)
	{
		if (versionOrKeyword.Equals("current", StringComparison.OrdinalIgnoreCase))
			return await ResolveCurrentTargetAsync(http, ctx);

		if (versionOrKeyword.Equals("preview", StringComparison.OrdinalIgnoreCase))
			return await ResolvePreviewTargetsAsync(http, ctx);

		// Explicit version — guard against GA then require a BC.
		var past = await FetchRequiredJsonAsync(http, PastReleasesUrl, ReleaseScheduleJsonContext.Default.PastReleasesResponse, ctx);
		if (past is null)
			return null;

		if (ReleaseTargetResolver.IsGa(past, versionOrKeyword))
		{
			_logger.LogInformation("Version {Version} is already GA. No bundle needed.", versionOrKeyword);
			return [];
		}

		var future = await FetchRequiredJsonAsync(http, FutureReleasesUrl, ReleaseScheduleJsonContext.Default.FutureReleasesResponse, ctx);
		if (future is null)
			return null;

		var release = future.Releases.FirstOrDefault(r => r.Version.Equals(versionOrKeyword, StringComparison.OrdinalIgnoreCase));

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
		var past = await FetchRequiredJsonAsync(http, PastReleasesUrl, ReleaseScheduleJsonContext.Default.PastReleasesResponse, ctx);
		if (past is null)
			return null;

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

	private async Task<IReadOnlyList<ReleaseTarget>?> ResolvePreviewTargetsAsync(HttpClient http, CancellationToken ctx)
	{
		// The release schedule is required. A snapshot pointer may legitimately not exist (404), but a
		// failed request or an unreadable response must not turn into a "nothing to do" success.
		var past = await FetchRequiredJsonAsync(http, PastReleasesUrl, ReleaseScheduleJsonContext.Default.PastReleasesResponse, ctx);
		var future = await FetchRequiredJsonAsync(http, FutureReleasesUrl, ReleaseScheduleJsonContext.Default.FutureReleasesResponse, ctx);
		if (past is null || future is null)
			return null;

		var currentGa = ReleaseTargetResolver.ResolveCurrentGa(past);
		var currentGaVersion = currentGa?.Version;
		_logger.LogInformation("Current GA (for preview context): {Version}", currentGaVersion ?? "unknown");

		// Determine the patch-branch snapshot URL from the current GA (e.g. 9.2.json for 9.2.x).
		LatestBuildPointer? patchPointer = null;
		if (currentGaVersion is not null && ReleaseTargetResolver.TryParseVersion(currentGaVersion) is { } gaVersion)
		{
			var patchUrl = $"https://snapshots.elastic.co/latest/{gaVersion.Major}.{gaVersion.Minor}.json";
			var patch = await TryFetchJsonAsync(http, patchUrl, ReleaseScheduleJsonContext.Default.LatestBuildPointer, ctx);
			if (patch.Failed)
				return null;
			patchPointer = patch.Value;
		}

		var minor = await TryFetchJsonAsync(http, MasterSnapshotUrl, ReleaseScheduleJsonContext.Default.LatestBuildPointer, ctx);
		if (minor.Failed)
			return null;
		var minorPointer = minor.Value;

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

	private async Task<ElasticBuildManifest?> FetchManifestAsync(HttpClient http, string url, CancellationToken ctx) =>
		await FetchRequiredJsonAsync(http, url, ReleaseScheduleJsonContext.Default.ElasticBuildManifest, ctx);

	private void ReportProblems(ReleaseTarget target, PastRelease previous, UnifiedBundlePlan plan)
	{
		foreach (var problem in plan.Problems)
		{
			var message = problem.Kind == PlanProblemKind.MissingFromManifest
				? $"Product '{problem.ProductId}': artifact key '{problem.ArtifactKey}' has no commit_hash in the build manifest for {target.Version} ({target.ManifestUrl})."
				: $"Product '{problem.ProductId}': artifact key '{problem.ArtifactKey}' has no commit_hash in the manifest of {previous.Version} ({previous.Manifest}), "
					+ $"so the range of the release notes for {target.Version} has no start.";
			_logger.LogError("{Message}", message);
			collector.EmitError(string.Empty, message);
		}
	}

	private async Task<int> BundleProductAsync(
		HttpClient http,
		ChangelogBundlingService bundleService,
		ProductBundleRange range,
		string outputDirectory,
		string? changelogConfigsDir,
		CancellationToken ctx
	)
	{
		var (productId, repoKey, _, _, commitHash) = range;
		var resolver = new ChangelogConfigResolver(http, new FileSystem(), _logger, collector);
		var config = await resolver.ResolveAsync(productId, repoKey, commitHash, changelogConfigsDir, ctx);
		if (config.Failed)
			return 1;

		var changelogYaml = config.Content;
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
			var arguments = UnifiedBundleArguments.Create(range, profileName, tempConfig, outputDirectory);

			var success = await bundleService.BundleChangelogs(collector, arguments, ctx);
			return success ? 0 : 1;
		}
		finally
		{
			File.Delete(tempConfig);
		}
	}

	private sealed record FetchResult<T>(T? Value, bool Failed) where T : class;

	/// <summary>
	/// Fetches JSON and tells a missing document (404, <c>Failed = false</c>, no value) from a failure
	/// (other status, network error, unreadable body: <c>Failed = true</c>, logged and reported).
	/// </summary>
	private async Task<FetchResult<T>> TryFetchJsonAsync<T>(
		HttpClient http,
		string url,
		System.Text.Json.Serialization.Metadata.JsonTypeInfo<T> typeInfo,
		CancellationToken ctx
	) where T : class
	{
		try
		{
			using var response = await http.GetAsync(url, ctx);
			if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
			{
				_logger.LogInformation("{Url} does not exist (404).", url);
				return new FetchResult<T>(null, false);
			}

			if (!response.IsSuccessStatusCode)
			{
				Fail($"Request to {url} returned {(int)response.StatusCode}.");
				return new FetchResult<T>(null, true);
			}

			var stream = await response.Content.ReadAsStreamAsync(ctx);
			var value = await JsonSerializer.DeserializeAsync(stream, typeInfo, ctx);
			if (value is null)
			{
				Fail($"The response from {url} was empty.");
				return new FetchResult<T>(null, true);
			}

			return new FetchResult<T>(value, false);
		}
		catch (Exception ex) when (ex is HttpRequestException or JsonException or IOException or TaskCanceledException)
		{
			if (ctx.IsCancellationRequested)
				throw;
			Fail($"Could not read {url}: {ex.Message}");
			return new FetchResult<T>(null, true);
		}

		void Fail(string message)
		{
			_logger.LogError("{Message}", message);
			collector.EmitError(string.Empty, message);
		}
	}

	/// <summary>Fetches a document the run cannot do without: a 404 is a failure too.</summary>
	private async Task<T?> FetchRequiredJsonAsync<T>(
		HttpClient http,
		string url,
		System.Text.Json.Serialization.Metadata.JsonTypeInfo<T> typeInfo,
		CancellationToken ctx
	) where T : class
	{
		var result = await TryFetchJsonAsync(http, url, typeInfo, ctx);
		if (result is { Value: null, Failed: false })
			collector.EmitError(string.Empty, $"Required document {url} was not found (404).");
		return result.Value;
	}
}
