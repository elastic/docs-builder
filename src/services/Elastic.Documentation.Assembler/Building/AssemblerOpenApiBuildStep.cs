// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using System.Diagnostics;
using Elastic.ApiExplorer;
using Elastic.ApiExplorer.Export;
using Elastic.ApiExplorer.Landing;
using Elastic.ApiExplorer.Model;
using Elastic.Documentation;
using Elastic.Documentation.Assembler.Navigation;
using Elastic.Documentation.Diagnostics;
using Elastic.Markdown;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Assembler.Building;

/// <summary>
/// Renders ApiExplorer HTML into the assembler output tree (e.g. <c>/docs/api/</c>).
/// </summary>
public static class AssemblerOpenApiBuildStep
{
	public static async Task<IReadOnlyList<string>> BuildAsync(
		ILoggerFactory logFactory,
		AssembleContext assembleContext,
		AssembleSources assembleSources,
		Cancel ctx)
	{
		var logger = logFactory.CreateLogger(typeof(AssemblerOpenApiBuildStep));
		var env = assembleContext.Environment;
		var features = env.ToFeatureFlags();

		if (!features.AssemblerApiExplorerEnabled)
		{
			logger.LogInformation("Skipping OpenAPI generation: assembler-api-explorer feature flag is disabled");
			return [];
		}

		var owners = DiscoverApiOwners(assembleSources.AssembleSets, assembleContext.Collector);
		if (owners.Count == 0)
		{
			logger.LogInformation("Skipping OpenAPI generation: no API declarations found in assembled docsets");
			return [];
		}

		var stopwatch = Stopwatch.StartNew();
		var catalogEntries = new List<ApiCatalogEntry>();
		var generatedUrls = new List<string>();
		using var versionIndexClient = new VersionIndexClient();

		foreach (var owner in owners)
		{
			ApplyFeatureFlags(owner.Set, env.FeatureFlags);
			var generator = new DocumentationGenerator(owner.Set.DocumentationSet, logFactory);
			var openApiGenerator = new OpenApiGenerator(
				logFactory,
				owner.Set.BuildContext,
				generator.MarkdownStringRenderer,
				versionIndexClient);
			var entries = await openApiGenerator.GenerateProducts(ctx).ConfigureAwait(false);
			catalogEntries.AddRange(entries);
			generatedUrls.AddRange(openApiGenerator.GeneratedPageUrls);
		}

		if (catalogEntries.Count > 0)
		{
			var catalogContext = owners[0].Set.BuildContext;
			var catalogGenerator = new OpenApiGenerator(
				logFactory,
				catalogContext,
				new DocumentationGenerator(owners[0].Set.DocumentationSet, logFactory).MarkdownStringRenderer,
				versionIndexClient);
			await catalogGenerator.GenerateCatalog(catalogEntries, ctx).ConfigureAwait(false);
			generatedUrls.AddRange(catalogGenerator.GeneratedPageUrls);
		}

		stopwatch.Stop();
		logger.LogInformation(
			"Finished generating OpenAPI pages under {OutputDirectory} in {DurationMs} ms",
			assembleContext.OutputWithPathPrefixDirectory.FullName,
			stopwatch.ElapsedMilliseconds);
		return generatedUrls;
	}

	public static IReadOnlyList<OpenApiExportSource> DiscoverExportSources(
		FrozenDictionary<string, AssemblerDocumentationSet> assembleSets,
		IDiagnosticsCollector collector)
	{
		var sources = new List<OpenApiExportSource>();
		foreach (var owner in DiscoverApiOwners(assembleSets, collector))
		{
			var apiConfigurations = owner.Set.BuildContext.Configuration.ApiConfigurations;
			if (apiConfigurations is null)
				continue;

			foreach (var (apiKey, apiConfig) in apiConfigurations)
				sources.Add(new OpenApiExportSource(apiKey, apiConfig, owner.Set.BuildContext.Git));
		}

		return sources;
	}

	internal static IReadOnlyList<AssemblerApiOwner> DiscoverApiOwners(
		FrozenDictionary<string, AssemblerDocumentationSet> assembleSets,
		IDiagnosticsCollector collector)
	{
		var keyOwners = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		var owners = new List<AssemblerApiOwner>();

		foreach (var set in assembleSets.Values)
		{
			var apiConfigurations = set.BuildContext.Configuration.ApiConfigurations;
			if (apiConfigurations is null || apiConfigurations.Count == 0)
				continue;

			foreach (var apiKey in apiConfigurations.Keys)
			{
				if (keyOwners.TryGetValue(apiKey, out var existingRepository))
				{
					collector.EmitGlobalError(
						$"Duplicate API key '{apiKey}' declared in {existingRepository} and {set.Checkout.Repository.Name}");
					continue;
				}

				keyOwners[apiKey] = set.Checkout.Repository.Name;
			}

			owners.Add(new AssemblerApiOwner(set));
		}

		return owners;
	}

	private static void ApplyFeatureFlags(
		AssemblerDocumentationSet set,
		IReadOnlyDictionary<string, bool> featureFlags)
	{
		foreach (var (key, value) in featureFlags)
			set.BuildContext.Configuration.Features.Set(key, value);
	}
}

internal sealed record AssemblerApiOwner(AssemblerDocumentationSet Set);
