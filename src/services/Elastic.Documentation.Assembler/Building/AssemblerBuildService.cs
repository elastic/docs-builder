// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;
using System.IO.Abstractions;
using System.Text;
using Actions.Core.Services;
using Elastic.Documentation;
using Elastic.Documentation.Assembler.Navigation;
using Elastic.Documentation.Assembler.Sourcing;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.FileSystems;
using Elastic.Documentation.LegacyDocs;
using Elastic.Documentation.Navigation.Assembler;
using Elastic.Documentation.Services;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Assembler.Building;

public class AssemblerBuildService(
	ILoggerFactory logFactory,
	AssemblyConfiguration assemblyConfiguration,
	IConfigurationContext configurationContext,
	ICoreService githubActionsService,
	IEnvironmentVariables environmentVariables
) : IService
{
	private readonly ILogger _logger = logFactory.CreateLogger<AssemblerBuildService>();
	private readonly IEnvironmentVariables _env = environmentVariables;

	public async Task<bool> BuildAll(
		IDiagnosticsCollector collector,
		AssemblerBuildOptions options,
		CheckoutsFileSystem fileSystem,
		Cancel ctx
	)
	{
		var strict = options.Strict;
		var environment = options.Environment;
		var metadataOnly = options.MetadataOnly;
		var showHints = options.ShowHints;
		var exporters = options.Exporters;
		var assumeBuild = options.AssumeBuild;

		collector.NoHints = !showHints.GetValueOrDefault(false);
		strict ??= false;
		exporters ??= metadataOnly.GetValueOrDefault(false) ? ExportOptions.MetadataOnly : ExportOptions.Default;
		// ensure we never generate a documentation state for assembler builds
		if (exporters.Contains(Exporter.DocumentationState))
			exporters = new HashSet<Exporter>(exporters.Except([Exporter.DocumentationState]));

		var elasticsearchExportOnly = exporters.SetEquals([Exporter.Elasticsearch]);

		var githubEnvironmentInput = githubActionsService.GetInput("environment");
		environment ??= !string.IsNullOrEmpty(githubEnvironmentInput) ? githubEnvironmentInput : "dev";

		_logger.LogInformation("Building all repositories for environment {Environment}", environment);

		_logger.LogInformation("Creating assemble context");

		var assembleContext = new AssembleContext(assemblyConfiguration, configurationContext, environment, collector, fileSystem);

		// Explicit --assume-build on CI is not allowed: CI must produce fresh, reproducible output.
		if (assumeBuild == true && _env.IsRunningOnCI)
			throw new InvalidOperationException(
				"The --assume-build flag is not allowed on CI. CI builds must always produce fresh output to ensure reproducibility and prevent stale content."
			);

		// When no explicit choice is given, default to skipping locally and always building on CI.
		var effectiveAssumeBuild = assumeBuild ?? !_env.IsRunningOnCI;

		// Read checkout SHAs up front — needed both for the stamp and for the build proper.
		// This is a cheap, network-free operation (one git rev-parse per repo).
		_logger.LogInformation("Get all clone directory information");
		var cloner = new AssemblerRepositorySourcer(logFactory, assembleContext);
		var checkoutResult = cloner.GetAll();
		var checkouts = checkoutResult.Checkouts.ToArray();

		// Stamp-based staleness check: skip the build when code, config, and content are all unchanged.
		if (effectiveAssumeBuild && !elasticsearchExportOnly)
		{
			var stampPath = Path.Join(assembleContext.OutputDirectory.FullName, AssemblerBuildStampService.StampFileName);
			var existingStamp = await AssemblerBuildStampService.ReadAsync(stampPath, ctx);
			var currentStamp = AssemblerBuildStampService.Compute(
				environment,
				checkouts,
				configurationContext.ConfigurationFileProvider,
				exporters
			);
			var (isUpToDate, reason) = AssemblerBuildStampService.IsUpToDate(existingStamp, currentStamp);
			AssemblerBuildStampService.LogResult(_logger, isUpToDate, reason);
			if (isUpToDate)
				return true;
		}
		else if (effectiveAssumeBuild && elasticsearchExportOnly)
		{
			_logger.LogInformation("Elasticsearch export only — skipping stamp check");
		}

		if (assembleContext.OutputDirectory.Exists)
		{
			if (elasticsearchExportOnly)
			{
				_logger.LogInformation("Elasticsearch export only. Skipping clean up of target output directory");
			}
			else
			{
				_logger.LogInformation("Cleaning target output directory");
				assembleContext.OutputDirectory.Delete(true);
			}
		}

		if (checkouts.Length == 0)
			throw new Exception("No checkouts found");

		_logger.LogInformation("Preparing all assemble sources for build");
		var assembleSources = await AssembleSources.AssembleAsync(
			logFactory,
			assembleContext,
			checkouts,
			configurationContext,
			exporters,
			ctx
		);

		var navigationFileInfo = configurationContext.ConfigurationFileProvider.NavigationFile;
		var siteNavigationFile = SiteNavigationFile.Deserialize(await fileSystem.File.ReadAllTextAsync(navigationFileInfo.FullName, ctx));
		var documentationSets = assembleSources.AssembleSets.Values.Select(s => s.DocumentationSet.Navigation).ToArray();
		var navigationPreviewEnabled = assembleContext.Environment.ToFeatureFlags().NavigationPreviewEnabled;
		var navigation = new SiteNavigation(
			siteNavigationFile,
			assembleContext,
			documentationSets,
			assembleContext.Environment.PathPrefix,
			navigationPreviewEnabled
		);

		_logger.LogInformation("Validating navigation.yml does not contain colliding path prefixes");
		// this validates all path prefixes are unique, early exit if duplicates are detected
		if (
			!SiteNavigationFile.ValidatePathPrefixes(assembleContext.Collector, siteNavigationFile, navigationFileInfo)
			|| assembleContext.Collector.Errors > 0
		)
			return false;

		var pathProvider = new GlobalNavigationPathProvider(navigation, assembleSources, assembleContext);
		var htmlWriter = new GlobalNavigationHtmlWriter(logFactory, navigation, collector);
		var legacyPageChecker = new LegacyPageService(logFactory);
		var historyMapper = new PageLegacyUrlMapper(
			legacyPageChecker,
			assembleContext.VersionsConfiguration,
			assembleSources.LegacyUrlMappings
		);

		var builder = new AssemblerBuilder(logFactory, assembleContext, navigation, htmlWriter, pathProvider, historyMapper);

		await builder.BuildAllAsync(assembleSources.AssembleSets, exporters, ctx);

		if (exporters.Contains(Exporter.LinkMetadata))
			await cloner.WriteLinkRegistrySnapshot(checkoutResult.LinkRegistrySnapshot, ctx);

		var redirectsPath = Path.Join(assembleContext.OutputDirectory.FullName, "redirects.json");
		if (assembleContext.WriteFileSystem.File.Exists(redirectsPath))
			await githubActionsService.SetOutputAsync("redirects-artifact-path", redirectsPath);

		if (exporters.Contains(Exporter.Html))
		{
			var openApiStopwatch = Stopwatch.StartNew();
			await AssemblerOpenApiBuildStep.BuildAsync(logFactory, assembleContext, assembleSources, ctx);
			openApiStopwatch.Stop();
			_logger.LogInformation("OpenAPI build step completed in {DurationMs} ms", openApiStopwatch.ElapsedMilliseconds);

			// Build-time sitemap uses current date as placeholder for backwards compatibility.
			// Production sitemap with correct content_last_updated dates is generated via
			// `assembler sitemap` after ES indexing, which overwrites this file.
			var urls = navigation.NavigationItems.SelectMany(SitemapNavigationHelper.Flatten).Select(n => n.Url).Distinct();
			var now = DateTimeOffset.UtcNow;
			var entries = urls.ToDictionary(u => u, _ => now);

			if (entries.Count >= SitemapBuilder.WarningEntryThreshold)
				collector.EmitGlobalWarning(
					$"Sitemap has {entries.Count:N0} entries, approaching the {SitemapBuilder.MaxEntries:N0} URL protocol limit. " +
						"Consider implementing sitemap index files."
				);

			var sitemapResult = SitemapBuilder.Generate(
				entries,
				assembleContext.WriteFileSystem,
				assembleContext.OutputWithPathPrefixDirectory
			);

			if (sitemapResult.FileSizeBytes >= SitemapBuilder.WarningFileSizeBytes)
				collector.EmitGlobalWarning(
					$"Sitemap file size is {sitemapResult.FileSizeBytes / (1024.0 * 1024.0):F1} MB, approaching the 50 MB protocol limit. " +
						"Consider implementing sitemap index files."
				);
		}

		if (exporters.Contains(Exporter.LLMText))
		{
			_logger.LogInformation("Enhancing llms.txt with navigation structure");
			var llmsEnhancer = new LlmsNavigationEnhancer();
			await EnhanceLlmsTxtFile(assembleContext, navigation, llmsEnhancer, ctx);
		}

		await collector.StopAsync(ctx);

		_logger.LogInformation("Finished building and exporting exporters {Exporters}", exporters);

		var success = strict.Value ? collector.Errors + collector.Warnings == 0 : collector.Errors == 0;

		// Write the stamp only for local dev runs (effectiveAssumeBuild=true) so the next
		// local run can skip the build. Never write it on CI — stamps must not appear in
		// deployed output, and CI always does a full build anyway.
		if (success && !elasticsearchExportOnly && effectiveAssumeBuild)
		{
			var stampPath = Path.Join(assembleContext.OutputDirectory.FullName, AssemblerBuildStampService.StampFileName);
			var stamp = AssemblerBuildStampService.Compute(
				environment,
				checkouts,
				configurationContext.ConfigurationFileProvider,
				exporters
			);
			if (stamp is not null)
			{
				await AssemblerBuildStampService.WriteAsync(stampPath, stamp, ctx);
				_logger.LogInformation("Wrote build stamp to {StampPath}", stampPath);
			}
		}

		return success;
	}

	private static async Task EnhanceLlmsTxtFile(
		AssembleContext context,
		SiteNavigation navigation,
		LlmsNavigationEnhancer enhancer,
		Cancel ctx
	)
	{
		var pathPrefixedOutputFolder = context.OutputWithPathPrefixDirectory;
		var llmsTxtPath = context.ReadFileSystem.Path.Join(pathPrefixedOutputFolder.FullName, "llms.txt");

		if (!context.ReadFileSystem.File.Exists(llmsTxtPath))
			return; // No llms.txt file to enhance

		var existingContent = await context.ReadFileSystem.File.ReadAllTextAsync(llmsTxtPath, ctx);
		// Assembler always uses the production URL as canonical base URL
		var canonicalBaseUrl = new Uri(context.Environment.Uri);
		var navigationSections = enhancer.GenerateNavigationSections(navigation, canonicalBaseUrl);

		// Append the navigation sections to the existing boilerplate
		var enhancedContent = existingContent + Environment.NewLine + navigationSections;

		await context.WriteFileSystem.File.WriteAllTextAsync(llmsTxtPath, enhancedContent, Encoding.UTF8, ctx);
	}
}
