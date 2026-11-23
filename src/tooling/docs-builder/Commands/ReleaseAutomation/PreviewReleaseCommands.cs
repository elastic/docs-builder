// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Net.Http.Headers;
using Actions.Core.Services;
using Elastic.Changelog.Caching;
using Elastic.Changelog.Serverless;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using Microsoft.Extensions.Logging;
using Nullean.Argh;
using Nullean.Argh.Documentation;

namespace Documentation.Builder.Commands.ReleaseAutomation;

/// <summary>Commands for the release notes preview site: every product that previews its release notes before they ship.</summary>
internal sealed class PreviewReleaseCommands(
	ILoggerFactory logFactory,
	IDiagnosticsCollector collector,
	IConfigurationContext configurationContext,
	ICoreService githubActionsService,
	IEnvironmentVariables environmentVariables
)
{
	private const string GitOpsRepository = "elastic/serverless-gitops";

	private readonly ILogger _logger = logFactory.CreateLogger<PreviewReleaseCommands>();

	private UnifiedReleaseCommands Unified => new(logFactory, collector, configurationContext, githubActionsService, environmentVariables);

	/// <summary>Generate the changelog bundles of everything the preview site shows.</summary>
	/// <remarks>
	/// <para>
	/// The DRA products are bundled like <c>release unified bundle preview</c>, for the next patch and minor versions.
	/// </para>
	/// <para>
	/// Each serverless service (Kibana, Elasticsearch, and the Elastic Cloud Hosted <c>frontend-adminconsole</c>) is bundled twice,
	/// as the commits since the version in production: up to the version on its staging slice (<c>staging</c>), and up to the head
	/// of its base branch (<c>head</c>). A release without changes has no bundle. The bundles are written to the folder of the
	/// product of the service: <c>cloud-serverless</c> for Kibana and Elasticsearch, <c>cloud-hosted</c> for the hosted service.
	/// </para>
	/// <para>Requires <c>GITHUB_TOKEN</c> with read access to <c>elastic/serverless-gitops</c> and the repositories of the services.</para>
	/// </remarks>
	/// <param name="outputDir">Root directory for bundle output. Defaults to <c>./bundles</c>. Each product writes to a <c>&lt;product-id&gt;/</c> subdirectory.</param>
	/// <param name="changelogConfigs">Optional directory of prototype <c>&lt;product-id&gt;.changelog.yml</c> files for the DRA products.</param>
	/// <param name="products">Limit the DRA products to these (repeatable). All DRA products are bundled by default.</param>
	/// <param name="services">Limit the serverless services to these (repeatable): <c>kibana</c>, <c>elasticsearch</c>, <c>frontend-adminconsole</c>. All are bundled by default.</param>
	/// <param name="strict">Fail when a DRA product is not onboarded. By default it is skipped with a warning.</param>
	/// <param name="cacheDir">Directory of the cache that remembers, between runs, which pull request each commit of a range belongs to. Defaults to the application data of docs-builder.</param>
	/// <param name="noCache">Do not read or write the cache of commit ranges.</param>
	[Hidden(Schema = true)]
	[NoOptionsInjection]
	public async Task<int> Bundle(
		[ExpandUserProfile] DirectoryInfo? outputDir = null,
		[ExpandUserProfile] DirectoryInfo? changelogConfigs = null,
		string[]? products = null,
		string[]? services = null,
		bool strict = false,
		[ExpandUserProfile] DirectoryInfo? cacheDir = null,
		bool noCache = false,
		CancellationToken ctx = default
	)
	{
		var selected = SelectServices(services);
		if (selected is null)
			return 1;

		var rootOutput = outputDir ?? new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), "bundles"));
		var dra = await Unified.Bundle("preview", rootOutput, changelogConfigs, products, strict, cacheDir, noCache, ctx);
		if (dra != 0)
			return dra;

		var token = environmentVariables.GetEnvironmentVariable("GITHUB_TOKEN");
		if (string.IsNullOrWhiteSpace(token))
		{
			collector.EmitError(string.Empty, "GITHUB_TOKEN is required to read elastic/serverless-gitops and the service repositories.");
			return 1;
		}

		using var http = new HttpClient { BaseAddress = new Uri("https://api.github.com/") };
		http.DefaultRequestHeaders.UserAgent.ParseAdd("docs-builder/1.0");
		http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

		var cache = ChangelogCache.Create(cacheDir, noCache, _logger);
		var bundler = new ServerlessBundler(logFactory, collector, configurationContext, http, cache);
		foreach (var service in selected)
		{
			if (!await BundleServiceAsync(bundler, service, rootOutput, ctx))
				return 1;
			if (cache is not null)
				await cache.SaveAsync(ctx);
		}

		return 0;
	}

	/// <summary>Build the preview site from the bundles that <c>release preview bundle</c> wrote.</summary>
	/// <remarks>
	/// The site has a page per version for every DRA product, and the <c>staging</c> and <c>head</c> pages of the serverless products.
	/// A product without bundles is left out of the site, and the build fails only when no product has any bundle.
	/// The site is built from the bundles on disk: the release schedule is not read.
	/// </remarks>
	/// <param name="bundlesDir">Folder that holds one sub-folder of bundle files per product. Defaults to <c>./bundles</c>.</param>
	/// <param name="output">Destination for the generated site. Defaults to <c>./site</c>. The folder is cleaned first.</param>
	/// <param name="pathPrefix">URL path prefix prepended to every generated link.</param>
	/// <param name="canonicalBaseUrl">Base URL written into canonical link tags.</param>
	[Hidden(Schema = true)]
	[NoOptionsInjection]
	public async Task<int> Build(
		[ExpandUserProfile] DirectoryInfo? bundlesDir = null,
		[ExpandUserProfile] DirectoryInfo? output = null,
		string? pathPrefix = null,
		Uri? canonicalBaseUrl = null,
		CancellationToken ctx = default
	)
	{
		string[] serverlessProducts = [.. ServerlessPromotion.Services.Values.Select(s => s.Product).Distinct(StringComparer.Ordinal)];
		return await Unified.BuildSiteAsync(
			null,
			new UnifiedSiteOptions(bundlesDir, output, pathPrefix, canonicalBaseUrl),
			serverlessProducts,
			ctx
		);
	}

	private IReadOnlyList<ServerlessService>? SelectServices(string[]? requested)
	{
		if (requested is not { Length: > 0 })
			return [.. ServerlessPromotion.Services.Values];

		var unknown = requested.Where(r => !ServerlessPromotion.Services.ContainsKey(r)).ToArray();
		if (unknown.Length > 0)
		{
			collector.EmitError(
				string.Empty,
				$"--services names no serverless service: {string.Join(", ", unknown)}. Supported: {string.Join(", ", ServerlessPromotion.Services.Keys)}."
			);
			return null;
		}

		return [.. requested.Select(r => ServerlessPromotion.Services[r]).Distinct()];
	}

	private async Task<bool> BundleServiceAsync(
		ServerlessBundler bundler,
		ServerlessService svc,
		DirectoryInfo rootOutput,
		CancellationToken ctx
	)
	{
		var versionsYaml = await bundler.GetRawAsync($"repos/{GitOpsRepository}/contents/{svc.VersionsPath}", ctx);
		if (versionsYaml is null)
		{
			collector.EmitError(string.Empty, $"Could not read {svc.VersionsPath} from {GitOpsRepository}.");
			return false;
		}

		var head = await ResolveBranchHeadAsync(bundler, svc, ctx);
		if (head is null)
			return false;

		RemoveSupersededBundles(svc, rootOutput);
		var targets = ServerlessPromotion.PlanPreview(versionsYaml, svc, head);
		if (targets.Count == 0)
		{
			_logger.LogInformation(
				"'{Service}' has nothing to preview: no {Slice} version, or no changes since it.",
				svc.Id,
				svc.FinalSlice
			);
			return true;
		}

		var productOutput = new DirectoryInfo(Path.Combine(rootOutput.FullName, svc.Product));
		_ = Directory.CreateDirectory(productOutput.FullName);
		foreach (var target in targets)
		{
			_logger.LogInformation(
				"Previewing '{Service}' as {Label}: {Start}..{End}",
				svc.Id,
				target.Label,
				target.StartRef,
				target.EndRef
			);
			var result = await bundler.BundleAsync(
				new ServerlessBundleRequest(svc, target.StartRef, target.EndRef, target.Label, productOutput),
				ctx
			);
			if (!result.Success)
				return false;
		}

		// A product that wrote nothing leaves no folder behind.
		if (!productOutput.EnumerateFileSystemInfos().Any())
			productOutput.Delete();
		return true;
	}

	// A reused output folder may hold the bundles of an earlier run, such as a Staging release that production has caught up with.
	private void RemoveSupersededBundles(ServerlessService svc, DirectoryInfo rootOutput)
	{
		foreach (var name in ServerlessPromotion.PreviewBundleFileNames(svc))
		{
			var path = Path.Combine(rootOutput.FullName, svc.Product, name);
			if (!File.Exists(path))
				continue;
			_logger.LogInformation("Removing the bundle of an earlier run: {Path}", path);
			File.Delete(path);
		}
	}

	private async Task<string?> ResolveBranchHeadAsync(ServerlessBundler bundler, ServerlessService svc, CancellationToken ctx)
	{
		using var json = await bundler.GetJsonAsync($"repos/elastic/{svc.Repository}/commits/{svc.BaseBranch}", ctx);
		var sha = json?.RootElement.GetProperty("sha").GetString();
		if (string.IsNullOrEmpty(sha))
			collector.EmitError(string.Empty, $"Could not read the head of {svc.BaseBranch} in elastic/{svc.Repository}.");
		return sha;
	}
}
