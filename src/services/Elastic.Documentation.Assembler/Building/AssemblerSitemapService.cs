// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Actions.Core.Services;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Search;
using Elastic.Documentation.Services;
using Elastic.Markdown.Exporters.Elasticsearch;
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
		IFileSystem fileSystem,
		string? endpoint = null,
		string? environment = null,
		string? apiKey = null,
		string? username = null,
		string? password = null,
		bool? debugMode = null,
		string? proxyAddress = null,
		string? proxyPassword = null,
		string? proxyUsername = null,
		bool? disableSslVerification = null,
		string? certificateFingerprint = null,
		string? certificatePath = null,
		bool? certificateNotRoot = null,
		Cancel ctx = default
	)
	{
		var githubEnvironmentInput = githubActionsService.GetInput("environment");
		environment ??= !string.IsNullOrEmpty(githubEnvironmentInput) ? githubEnvironmentInput : "dev";

		_logger.LogInformation("Generating sitemap from ES index for environment {Environment}", environment);

		var assembleContext = new AssembleContext(
			assemblyConfiguration, configurationContext, environment, collector,
			fileSystem, fileSystem, null, null
		);

		var cfg = configurationContext.Endpoints.Elasticsearch;
		var options = new ElasticsearchIndexOptions
		{
			Endpoint = endpoint,
			ApiKey = apiKey,
			Username = username,
			Password = password,
			DebugMode = debugMode,
			ProxyAddress = proxyAddress,
			ProxyPassword = proxyPassword,
			ProxyUsername = proxyUsername,
			DisableSslVerification = disableSslVerification,
			CertificateFingerprint = certificateFingerprint,
			CertificatePath = certificatePath,
			CertificateNotRoot = certificateNotRoot
		};
		await ElasticsearchEndpointConfigurator.ApplyAsync(cfg, options, collector, fileSystem, ctx);

		if (collector.Errors > 0)
			return false;

		var transport = ElasticsearchTransportFactory.Create(cfg);

		var indexName = DocumentationMappingContext.DocumentationDocument
			.CreateContext(type: "assembler", env: environment)
			.ResolveReadTarget();

		_logger.LogInformation("Querying index {Index} for sitemap entries", indexName);

		var reader = new EsSitemapReader(transport, _logger, indexName);
		var entries = new Dictionary<string, DateTimeOffset>();

		await foreach (var entry in reader.ReadAllAsync(ctx))
			entries[entry.Url] = entry.LastUpdated;

		_logger.LogInformation("Fetched {Count} sitemap entries from ES", entries.Count);

		if (entries.Count == 0)
		{
			collector.EmitGlobalError("No documents found in ES index — cannot generate sitemap");
			return false;
		}

		SitemapBuilder.Generate(entries, assembleContext.WriteFileSystem, assembleContext.OutputWithPathPrefixDirectory);

		_logger.LogInformation("Sitemap written to {Path}", assembleContext.OutputWithPathPrefixDirectory.FullName);
		return true;
	}
}
