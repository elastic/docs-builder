// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.Text.Json;
using Actions.Core.Services;
using Elastic.ApiExplorer;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Builder;
using Elastic.Documentation.Configuration.Inference;
using Elastic.Documentation.Configuration.ReleaseNotes;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.FileSystems;
using Elastic.Documentation.LinkIndex;
using Elastic.Documentation.Links;
using Elastic.Documentation.Links.CrossLinks;
using Elastic.Documentation.Serialization;
using Elastic.Documentation.Services;
using Elastic.Markdown;
using Elastic.Markdown.Exporters;
using Elastic.Markdown.IO;
using Microsoft.Extensions.Logging;
using Nullean.ScopedFileSystem;
using static System.StringComparison;

namespace Elastic.Documentation.Isolated;

public class IsolatedBuildService(
	ILoggerFactory logFactory,
	IConfigurationContext configurationContext,
	ICoreService githubActionsService,
	IEnvironmentVariables environmentVariables
) : IService
{
	private readonly ILogger _logger = logFactory.CreateLogger<IsolatedBuildService>();
	private readonly IEnvironmentVariables _env = environmentVariables;

	public bool IsStrict(bool? strict)
	{
		if (bool.TryParse(githubActionsService.GetInput("strict"), out var strictValue) && strictValue)
			return strictValue;
		return strict.HasValue && strict.Value;
	}

	public async Task<bool> Build(
		IDiagnosticsCollector collector,
		IsolatedBuildOptions options,
		IFileSystem? writeFileSystem = null,
		Cancel ctx = default
	)
	{
		var path = options.Path?.FullName;
		var output = options.Output?.FullName;
		var pathPrefix = options.PathPrefix;
		var force = options.Force;
		var strict = options.Strict;
		var allowIndexing = options.AllowIndexing;
		var metadataOnly = options.MetadataOnly;
		var exporters = options.Exporters;
		var canonicalBaseUri = options.CanonicalBaseUrl;
		var skipOpenApi = options.SkipApi;
		var skipCrossLinks = options.SkipCrossLinks;

		strict = IsStrict(strict);

		if (bool.TryParse(githubActionsService.GetInput("metadata-only"), out var metaValue) && metaValue)
			metadataOnly ??= metaValue;

		exporters ??= metadataOnly.GetValueOrDefault(false) ? ExportOptions.MetadataOnly : ExportOptions.Default;

		pathPrefix ??= githubActionsService.GetInput("prefix");

		var runningOnCi = _env.IsRunningOnCI;
		BuildContext context;

		canonicalBaseUri ??= new Uri("https://docs-v3-preview.elastic.dev");

		if (runningOnCi)
		{
			_logger.LogInformation("Build running on CI, forcing a full rebuild of the destination folder");
			force = true;
		}

		try
		{
			var docFs = DocumentationFileSystem.Resolve(path, new DocumentationScopeOptions
			{
				Output = options.Output?.FullName,
				InnerWrite = writeFileSystem
			});
			context = new BuildContext(collector, docFs, configurationContext)
			{
				AvailableExporters = exporters,
				UrlPathPrefix = pathPrefix,
				Force = force ?? false,
				AllowIndexing = allowIndexing ?? false,
				CanonicalBaseUrl = canonicalBaseUri,
			};
		}
		// On CI, we are running on a merge commit which may have changes against an older
		// docs folder (this can happen on out-of-date PR's).
		// At some point in the future we can remove this try catch
		catch (DocumentationPathException e) when (runningOnCi)
		{
			// Derive the default output from `path` so it stays within the write FS scope.
			// Using Paths.WorkingDirectoryRoot would be wrong when --path points to a different repo.
			var rootFolder = !string.IsNullOrWhiteSpace(path) ? path : Paths.WorkingDirectoryRoot.FullName;
			var fallbackFs = writeFileSystem ?? new FileSystem();
			var outputDirectory = fallbackFs.DirectoryInfo.New(output ?? Path.Join(rootFolder, ".artifacts/docs/html"));
			// we temporarily do not error when pointed to a non-documentation folder.
			_ = fallbackFs.Directory.CreateDirectory(outputDirectory.FullName);

			// Surfaced as a warning (not swallowed at Information level) so that when the underlying
			// cause is a real bug — not the stale-merge-commit case this catch was written for —
			// the --git-dir remedy in e.Message actually reaches whoever is reading the failed run,
			// rather than being buried above a later, unrelated artifact-upload failure.
			_logger.LogWarning("Skipping build on CI: {Message} If the docs folder is not actually out of date on a stale merge commit, this indicates a real path-resolution issue.",
				e.Message);

			await githubActionsService.SetOutputAsync("skip", "true");
			return true;
		}

		if (runningOnCi)
			await githubActionsService.SetOutputAsync("skip", "false");

		ICrossLinkResolver crossLinkResolver;
		if (skipCrossLinks)
		{
			_logger.LogInformation("Skipping cross-link fetching for fast validation build");
			crossLinkResolver = NoopCrossLinkResolver.Instance;
		}
		else
		{
			using var codexReader = context.Configuration.Registry != DocSetRegistry.Public
				? new GitLinkIndexReader(context.Configuration.Registry.ToStringFast(true), new ApplicationDataFileSystem())
				: null;

			var crossLinkFetcher = new DocSetConfigurationCrossLinkFetcher(
				logFactory,
				context.Configuration,
				codexLinkIndexReader: codexReader);
			var crossLinks = await crossLinkFetcher.FetchCrossLinks(ctx);
			IUriEnvironmentResolver? uriResolver = crossLinks.CodexRepositories is not null
				? new CodexAwareUriResolver(crossLinks.CodexRepositories)
				: null;
			crossLinkResolver = new CrossLinkResolver(crossLinks, uriResolver);
		}

		// Prefetch CDN-hosted release notes for products declared under `release_notes` in docset.yml.
		var releaseNotesResolver = await ReleaseNotesFetcher.PrefetchAsync(context, logFactory, ctx);

		// always delete output folder on CI
		var set = new DocumentationSet(context, logFactory, crossLinkResolver, releaseNotesResolver);
		if (runningOnCi)
			set.ClearOutputDirectory();

		var documentInferrer = new DocumentInferrerService(
			context.ProductsConfiguration,
			context.VersionsConfiguration,
			context.LegacyUrlMappings,
			set.Configuration,
			context.Git);
		var markdownExporters = exporters.CreateMarkdownExporters(logFactory, context,
			branded: context.Configuration.Branding is not null);

		var tasks = markdownExporters.Select(async e => await e.StartAsync(ctx));
		await Task.WhenAll(tasks);


		var generator = new DocumentationGenerator(set, logFactory, set, null, null, markdownExporters.ToArray(), documentInferrer: documentInferrer);
		var result = await generator.GenerateAll(ctx);

		if (!skipOpenApi)
		{
			var openApiGenerator = new OpenApiGenerator(logFactory, context, generator.MarkdownStringRenderer);
			await openApiGenerator.Generate(ctx);
		}

		if (runningOnCi)
			await githubActionsService.SetOutputAsync("landing-page-path", set.FirstInterestingUrl);

		if (result.Redirects.Count > 0)
			await WriteRedirectsAsync(result.Redirects, context, ctx);

		var finishTasks = markdownExporters.Select(async e => await e.FinishExportAsync(context.OutputDirectory, ctx));
		_ = await Task.WhenAll(finishTasks);

		tasks = markdownExporters.Select(async e => await e.StopAsync(ctx));
		await Task.WhenAll(tasks);
		_logger.LogInformation("Finished building and exporting exporters {Exporters}", exporters);

		return strict.Value ? context.Collector.Errors + context.Collector.Warnings == 0 : context.Collector.Errors == 0;
	}

	private async Task WriteRedirectsAsync(IReadOnlyDictionary<string, LinkRedirect> redirects, BuildContext context, Cancel ctx)
	{
		var pathPrefix = (context.UrlPathPrefix ?? string.Empty).TrimEnd('/');
		var resolved = new Dictionary<string, string>();

		foreach (var (from, redirect) in redirects)
		{
			string? to = null;
			if (redirect.To is not null)
				to = redirect.To;
			else if (redirect.Many is { Length: > 0 })
				to = redirect.Many.FirstOrDefault(r => r.To is not null)?.To;

			if (to is null || to.Contains("://"))
				continue;

			var fromUrl = ToAbsoluteUrl(from, pathPrefix);
			var toUrl = ToAbsoluteUrl(to, pathPrefix);

			if (!string.IsNullOrEmpty(fromUrl) && !string.IsNullOrEmpty(toUrl)
				&& !fromUrl.TrimEnd('/').Equals(toUrl.TrimEnd('/'), OrdinalIgnoreCase))
			{
				resolved[fromUrl] = toUrl;
			}
		}

		if (resolved.Count == 0)
			return;

		var redirectsFile = context.WriteFileSystem.FileInfo.New(
			Path.Join(context.OutputDirectory.FullName, "redirects.json"));
		_logger.LogInformation("Writing {Count} resolved redirects to {Path}", resolved.Count, redirectsFile.FullName);

		var json = JsonSerializer.Serialize(resolved, SourceGenerationContext.Default.DictionaryStringString);
		await context.WriteFileSystem.File.WriteAllTextAsync(redirectsFile.FullName, json, ctx);
	}

	internal static string ToAbsoluteUrl(string path, string pathPrefix)
	{
		pathPrefix = pathPrefix.TrimEnd('/');

		if (path.EndsWith(".md", OrdinalIgnoreCase))
			path = path[..^3];

		if (path.EndsWith("/index", OrdinalIgnoreCase))
			path = path[..^6];
		else if (path.Equals("index", OrdinalIgnoreCase))
			return string.IsNullOrEmpty(pathPrefix) ? "/" : pathPrefix;

		if (string.IsNullOrEmpty(path))
			return string.IsNullOrEmpty(pathPrefix) ? "/" : pathPrefix;

		return $"{pathPrefix}/{path}";
	}
}
