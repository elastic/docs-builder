// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using System.Diagnostics;
using System.Text;
using Elastic.ApiExplorer;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Landing;
using Elastic.ApiExplorer.Model;
using Elastic.Documentation;
using Elastic.Documentation.Assembler.Navigation;
using Elastic.Documentation.Configuration.Builder;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Diagnostics;
using Elastic.Markdown;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Assembler.Building;

/// <summary>
/// Renders ApiExplorer HTML into the assembler output tree (e.g. <c>/docs/api/</c>).
/// </summary>
public static class AssemblerOpenApiBuildStep
{
	public static async Task<IReadOnlyList<ApiCatalogEntry>> BuildAsync(
		ILoggerFactory logFactory,
		AssembleContext assembleContext,
		AssemblerApiSections apiSections,
		TopNavRenderModel? siteTopNav,
		Cancel ctx
	)
	{
		var logger = logFactory.CreateLogger(typeof(AssemblerOpenApiBuildStep));
		var owners = apiSections.Owners;
		if (owners.Count == 0)
		{
			logger.LogInformation(
				"Skipping OpenAPI generation: the assembler-api-explorer feature flag is disabled or no API declarations were found in assembled docsets"
			);
			return [];
		}

		var stopwatch = Stopwatch.StartNew();
		var catalogEntries = new List<ApiCatalogEntry>();

		// The build service creates the generators before the docsets resolve their trees. A standalone call creates them here.
		var planned = apiSections.EnsureGenerators(logFactory, assembleContext, siteTopNav);

		// The roots of the site navigation exist already. This fills their children from the trees the pages render from below.
		await apiSections.ResolveAsync(ctx).ConfigureAwait(false);

		var hubEntries = new List<ApiCatalogEntry>();
		var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (var (_, generator) in planned)
		{
			foreach (var entry in await generator.ResolveCatalogEntries(ctx).ConfigureAwait(false))
			{
				if (seenKeys.Add(entry.Key))
					hubEntries.Add(entry);
			}
		}

		foreach (var (_, generator) in planned)
		{
			var entries = await generator.GenerateProducts(hubEntries: hubEntries, ctx).ConfigureAwait(false);
			catalogEntries.AddRange(entries);
		}

		if (catalogEntries.Count > 0)
		{
			var catalogContext = owners[0].Set.BuildContext;
			var catalogGenerator = new OpenApiGenerator(
				logFactory,
				catalogContext,
				new DocumentationGenerator(owners[0].Set.DocumentationSet, logFactory).MarkdownStringRenderer,
				apiSections.VersionIndexClient
			)
			{ SiteTopNav = siteTopNav };
			await catalogGenerator.GenerateCatalog(catalogEntries, ctx).ConfigureAwait(false);
			await WriteApiLlmsTxt(assembleContext, catalogEntries, ctx).ConfigureAwait(false);
		}

		stopwatch.Stop();
		logger.LogInformation(
			"Finished generating OpenAPI pages under {OutputDirectory} in {DurationMs} ms",
			assembleContext.OutputWithPathPrefixDirectory.FullName,
			stopwatch.ElapsedMilliseconds
		);
		return catalogEntries;
	}

	private static async Task WriteApiLlmsTxt(AssembleContext assembleContext, IReadOnlyList<ApiCatalogEntry> catalogEntries, Cancel ctx)
	{
		var canonicalBaseUrl = new Uri(assembleContext.Environment.Uri);
		var content = new LlmsNavigationEnhancer().GenerateApiHubIndex(catalogEntries, canonicalBaseUrl);
		var directory = assembleContext.WriteFileSystem.Path.Join(assembleContext.OutputWithPathPrefixDirectory.FullName, "api");
		_ = assembleContext.WriteFileSystem.Directory.CreateDirectory(directory);
		var path = assembleContext.WriteFileSystem.Path.Join(directory, "llms.txt");
		await assembleContext.WriteFileSystem.File.WriteAllTextAsync(path, content, Encoding.UTF8, ctx).ConfigureAwait(false);
	}

	internal static IReadOnlyList<AssemblerApiOwner> DiscoverApiOwners(
		FrozenDictionary<string, AssemblerDocumentationSet> assembleSets,
		IDiagnosticsCollector collector
	)
	{
		var keyOwners = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		var owners = new List<AssemblerApiOwner>();

		foreach (var set in assembleSets.Values)
		{
			var apiConfigurations = set.BuildContext.Configuration.ApiConfigurations;
			if (apiConfigurations is null || apiConfigurations.Count == 0)
				continue;

			var publishesApi = false;
			foreach (var apiKey in apiConfigurations.Keys)
			{
				if (IsolatedApiAliases.IsFixtureKey(apiKey))
					continue;

				publishesApi = true;
				if (keyOwners.TryGetValue(apiKey, out var existingRepository))
				{
					collector.EmitGlobalError(
						$"Duplicate API key '{apiKey}' declared in {existingRepository} and {set.Checkout.Repository.Name}"
					);
					continue;
				}

				keyOwners[apiKey] = set.Checkout.Repository.Name;
			}

			if (publishesApi)
				owners.Add(new AssemblerApiOwner(set));
		}

		return owners;
	}
}

internal sealed record AssemblerApiOwner(AssemblerDocumentationSet Set);
