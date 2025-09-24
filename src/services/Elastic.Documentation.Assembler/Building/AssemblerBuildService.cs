// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using System.IO.Abstractions;
using Actions.Core.Services;
using Elastic.Documentation.Assembler.Navigation;
using Elastic.Documentation.Assembler.Sourcing;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Configuration.Navigation;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.LegacyDocs;
using Elastic.Documentation.Services;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Assembler.Building;

public class AssemblerBuildService(
	ILoggerFactory logFactory,
	AssemblyConfiguration assemblyConfiguration,
	IConfigurationContext configurationContext,
	ICoreService githubActionsService
) : IService
{
	private readonly ILogger _logger = logFactory.CreateLogger<AssemblerBuildService>();

	public async Task<bool> BuildAll(
		IDiagnosticsCollector collector,
		bool? strict, string? environment,
		bool? metadataOnly,
		bool? showHints,
		IReadOnlySet<Exporter>? exporters,
		FileSystem fs,
		Cancel ctx
	)
	{
		collector.NoHints = !showHints.GetValueOrDefault(false);
		strict ??= false;
		exporters ??= metadataOnly.GetValueOrDefault(false) ? ExportOptions.MetadataOnly : ExportOptions.Default;
		// ensure we never generate a documentation state for assembler builds
		if (exporters.Contains(Exporter.DocumentationState))
			exporters = new HashSet<Exporter>(exporters.Except([Exporter.DocumentationState]));

		var githubEnvironmentInput = githubActionsService.GetInput("environment");
		environment ??= !string.IsNullOrEmpty(githubEnvironmentInput) ? githubEnvironmentInput : "dev";

		_logger.LogInformation("Building all repositories for environment {Environment}", environment);

		_logger.LogInformation("Creating assemble context");

		var assembleContext = new AssembleContext(assemblyConfiguration, configurationContext, environment, collector, fs, fs, null, null);

		if (assembleContext.OutputDirectory.Exists)
		{
			_logger.LogInformation("Cleaning target output directory");
			assembleContext.OutputDirectory.Delete(true);
		}

		_logger.LogInformation("Validating navigation.yml does not contain colliding path prefixes");
		// this validates all path prefixes are unique, early exit if duplicates are detected
		if (!GlobalNavigationFile.ValidatePathPrefixes(assembleContext.Collector, assembleContext.ConfigurationFileProvider, assemblyConfiguration)
			|| assembleContext.Collector.Errors > 0)
			return false;

		_logger.LogInformation("Get all clone directory information");
		var cloner = new AssemblerRepositorySourcer(logFactory, assembleContext);
		var checkoutResult = cloner.GetAll();
		var checkouts = checkoutResult.Checkouts.ToArray();

		if (checkouts.Length == 0)
			throw new Exception("No checkouts found");

		_logger.LogInformation("Preparing all assemble sources for build");
		var assembleSources = await AssembleSources.AssembleAsync(logFactory, assembleContext, checkouts, configurationContext, exporters, ctx);
		var navigationFile = new GlobalNavigationFile(collector, configurationContext.ConfigurationFileProvider, assemblyConfiguration, assembleSources.TocConfigurationMapping);

		_logger.LogInformation("Create global navigation");
		var navigation = new GlobalNavigation(assembleSources, navigationFile);

		var pathProvider = new GlobalNavigationPathProvider(navigationFile, assembleSources, assembleContext);
		var htmlWriter = new GlobalNavigationHtmlWriter(logFactory, navigation, collector);
		var legacyPageChecker = new LegacyPageService(logFactory);
		var historyMapper = new PageLegacyUrlMapper(legacyPageChecker, assembleSources.LegacyUrlMappings);

		var builder = new AssemblerBuilder(logFactory, assembleContext, navigation, htmlWriter, pathProvider, historyMapper);

		await builder.BuildAllAsync(assembleContext.Environment, assembleSources.AssembleSets, exporters, ctx);

		if (exporters.Contains(Exporter.LinkMetadata))
			await cloner.WriteLinkRegistrySnapshot(checkoutResult.LinkRegistrySnapshot, ctx);

		var redirectsPath = Path.Combine(assembleContext.OutputDirectory.FullName, "redirects.json");
		if (File.Exists(redirectsPath))
			await githubActionsService.SetOutputAsync("redirects-artifact-path", redirectsPath);

		if (exporters.Contains(Exporter.Html))
		{
			var sitemapBuilder = new SitemapBuilder(navigation.NavigationItems, assembleContext.WriteFileSystem, assembleContext.OutputDirectory);
			sitemapBuilder.Generate();
		}

		_logger.LogInformation("Finished building and exporting exporters {Exporters}", exporters);

		return strict.Value ? collector.Errors + collector.Warnings == 0 : collector.Errors == 0;
	}

}
