// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Actions.Core.Services;
using Elastic.ApiExplorer;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Links.CrossLinks;
using Elastic.Documentation.Services;
using Elastic.Markdown;
using Elastic.Markdown.Exporters;
using Elastic.Markdown.IO;
using Microsoft.Extensions.Logging;
using static System.StringComparison;

namespace Elastic.Documentation.Isolated;

public class IsolatedBuildService(
	ILoggerFactory logFactory,
	IConfigurationContext configurationContext,
	ICoreService githubActionsService
) : IService
{
	private readonly ILogger _logger = logFactory.CreateLogger<IsolatedBuildService>();

	public bool IsStrict(bool? strict)
	{
		if (bool.TryParse(githubActionsService.GetInput("strict"), out var strictValue) && strictValue)
			return strictValue;
		return strict.HasValue && strict.Value;
	}

	public async Task<bool> Build(
		IDiagnosticsCollector collector,
		IFileSystem fileSystem,
		string? path = null,
		string? output = null,
		string? pathPrefix = null,
		bool? force = null,
		bool? strict = null,
		bool? allowIndexing = null,
		bool? metadataOnly = null,
		IReadOnlySet<Exporter>? exporters = null,
		string? canonicalBaseUrl = null,
		Cancel ctx = default
	)
	{
		strict = IsStrict(strict);

		if (bool.TryParse(githubActionsService.GetInput("metadata-only"), out var metaValue) && metaValue)
			metadataOnly ??= metaValue;

		exporters ??= metadataOnly.GetValueOrDefault(false) ? ExportOptions.MetadataOnly : ExportOptions.Default;

		pathPrefix ??= githubActionsService.GetInput("prefix");

		var runningOnCi = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));
		BuildContext context;

		Uri? canonicalBaseUri;

		if (runningOnCi)
		{
			_logger.LogInformation("Build running on CI, forcing a full rebuild of the destination folder");
			force = true;
		}

		if (canonicalBaseUrl is null)
			canonicalBaseUri = new Uri("https://docs-v3-preview.elastic.dev");
		else if (!Uri.TryCreate(canonicalBaseUrl, UriKind.Absolute, out canonicalBaseUri))
			throw new ArgumentException($"The canonical base url '{canonicalBaseUrl}' is not a valid absolute uri");

		try
		{
			context = new BuildContext(collector, fileSystem, fileSystem, configurationContext, exporters, path, output)
			{
				UrlPathPrefix = pathPrefix,
				Force = force ?? false,
				AllowIndexing = allowIndexing ?? false,
				CanonicalBaseUrl = canonicalBaseUri
			};
		}
		// On CI, we are running on a merge commit which may have changes against an older
		// docs folder (this can happen on out-of-date PR's).
		// At some point in the future we can remove this try catch
		catch (Exception e) when (runningOnCi && e.Message.StartsWith("Can not locate docset.yml file in", OrdinalIgnoreCase))
		{
			var outputDirectory = !string.IsNullOrWhiteSpace(output)
				? fileSystem.DirectoryInfo.New(output)
				: fileSystem.DirectoryInfo.New(Path.Combine(Paths.WorkingDirectoryRoot.FullName, ".artifacts/docs/html"));
			// we temporarily do not error when pointed to a non-documentation folder.
			_ = fileSystem.Directory.CreateDirectory(outputDirectory.FullName);

			_logger.LogInformation("Skipping build as we are running on a merge commit and the docs folder is out of date and has no docset.yml. {Message}",
				e.Message);

			await githubActionsService.SetOutputAsync("skip", "true");
			return true;
		}

		if (runningOnCi)
			await githubActionsService.SetOutputAsync("skip", "false");

		var crossLinkFetcher = new DocSetConfigurationCrossLinkFetcher(logFactory, context.Configuration);
		var crossLinks = await crossLinkFetcher.FetchCrossLinks(ctx);
		var crossLinkResolver = new CrossLinkResolver(crossLinks);

		// always delete output folder on CI
		var set = new DocumentationSet(context, logFactory, crossLinkResolver);
		if (runningOnCi)
			set.ClearOutputDirectory();

		var markdownExporters = exporters.CreateMarkdownExporters(logFactory, context);

		var generator = new DocumentationGenerator(set, logFactory, null, null, markdownExporters.ToArray());
		_ = await generator.GenerateAll(ctx);

		var openApiGenerator = new OpenApiGenerator(logFactory, context, generator.MarkdownStringRenderer);
		await openApiGenerator.Generate(ctx);

		if (runningOnCi)
			await githubActionsService.SetOutputAsync("landing-page-path", set.FirstInterestingUrl);

		await collector.StopAsync(ctx);
		return strict.Value ? context.Collector.Errors + context.Collector.Warnings == 0 : context.Collector.Errors == 0;
	}
}
