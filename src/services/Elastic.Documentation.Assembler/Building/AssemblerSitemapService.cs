// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Actions.Core.Services;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.FileSystems;
using Elastic.Documentation.Indexing.Exporters.Elasticsearch;
using Elastic.Documentation.Search;
using Elastic.Documentation.Search.Contract;
using Elastic.Documentation.Services;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Assembler.Building;

public class AssemblerSitemapService(
	ILoggerFactory logFactory,
	AssemblyConfiguration assemblyConfiguration,
	IConfigurationContext configurationContext,
	ICoreService githubActionsService
) : IService
{
	private readonly ILogger _logger = logFactory.CreateLogger<AssemblerSitemapService>();

	public async Task<bool> GenerateSitemapAsync(
		IDiagnosticsCollector collector,
		CheckoutsFileSystem fileSystem,
		ElasticsearchIndexOptions es,
		string? environment = null,
		Cancel ctx = default
	)
	{
		var githubEnvironmentInput = githubActionsService.GetInput("environment");
		environment ??= !string.IsNullOrEmpty(githubEnvironmentInput) ? githubEnvironmentInput : "dev";

		_logger.LogInformation("Generating sitemap from ES index for environment {Environment}", environment);

		var assembleContext = new AssembleContext(assemblyConfiguration, configurationContext, environment, collector, fileSystem);

		var cfg = configurationContext.Endpoints.Elasticsearch;
		await ElasticsearchEndpointConfigurator.ApplyAsync(cfg, es, collector, fileSystem, ctx);

		if (collector.Errors > 0)
			return false;

		var transport = ElasticsearchTransportFactory.Create(cfg);

		var indexName = DocumentationMappingContext
			.DocumentationDocumentSemantic
			.CreateContext(type: "assembler", env: environment)
			.ResolveReadTarget();

		_logger.LogInformation("Querying index {Index} for sitemap entries", indexName);

		var reader = new EsSitemapReader(transport, _logger, indexName);
		var output = SitemapOutput.For(assembleContext);
		var documents = new List<SitemapEntry>();
		var entries = new Dictionary<string, DateTimeOffset>();
		await foreach (var entry in reader.ReadAllAsync(ctx))
		{
			documents.Add(entry);
			if (entry.ApiTreeRoots.Count == 0 && !output.IsApiUrl(entry.Url))
				entries[entry.Url] = entry.LastUpdated;
		}

		_logger.LogInformation("Fetched {Count} narrative sitemap entries from ES", entries.Count);

		if (entries.Count == 0)
		{
			collector.EmitGlobalError("No documents found in ES index — cannot generate sitemap");
			return false;
		}

		// API pages are the ones `assembler build` listed; the index dates them.
		var buildPages = SitemapBuilder.ReadApiPages(output);
		if (ApiSitemapDates.BuildSitemapMissing(buildPages, documents, AssemblerApiSections.IsEnabled(assembleContext)))
			collector.EmitGlobalWarning(
				$"{indexName} has API documents, but {output.Folder.FullName} has no sitemap-api.xml from `assembler build`, so sitemap.xml will not list the API pages"
			);
		var api = ApiSitemapDates.Resolve(buildPages, documents, output.ApiRoot);
		if (api.Missing.Count > 0)
			collector.EmitGlobalWarning(
				$"{api.Missing.Count} API sitemap pages have no document in {indexName}, so they keep the build date: {string.Join(", ", api.Missing.Take(10))}"
			);
		foreach (var (url, date) in api.Entries)
			entries[url] = date;

		var files = SitemapBuilder.Generate(entries, output, collector);
		foreach (var file in files)
			_logger.LogInformation("Sitemap file {File}: {Count} URLs", file.Name, file.EntryCount);

		_logger.LogInformation("Sitemap written to {Path}", assembleContext.OutputWithPathPrefixDirectory.FullName);
		return true;
	}
}
