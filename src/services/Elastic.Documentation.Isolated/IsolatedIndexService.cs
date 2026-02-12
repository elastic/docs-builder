// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Actions.Core.Services;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using Microsoft.Extensions.Logging;
using static Elastic.Documentation.Exporter;

namespace Elastic.Documentation.Isolated;

public class IsolatedIndexService(
	ILoggerFactory logFactory,
	IConfigurationContext configurationContext,
	ICoreService githubActionsService
) : IsolatedBuildService(logFactory, configurationContext, githubActionsService)
{
	private readonly IConfigurationContext _configurationContext = configurationContext;

	/// <summary>
	/// Index documentation to Elasticsearch, calls `docs-builder assembler build --exporters elasticsearch`. Exposes more options
	/// </summary>
	/// <param name="collector"></param>
	/// <param name="fileSystem"></param>
	/// <param name="path">path to the documentation folder, defaults to pwd.</param>
	/// <param name="endpoint">Elasticsearch endpoint, alternatively set env DOCUMENTATION_ELASTIC_URL</param>
	/// <param name="apiKey">Elasticsearch API key, alternatively set env DOCUMENTATION_ELASTIC_APIKEY</param>
	/// <param name="username">Elasticsearch username (basic auth), alternatively set env DOCUMENTATION_ELASTIC_USERNAME</param>
	/// <param name="password">Elasticsearch password (basic auth), alternatively set env DOCUMENTATION_ELASTIC_PASSWORD</param>
	/// <param name="noSemantic">Index without semantic fields</param>
	/// <param name="enableAiEnrichment">Enable AI enrichment of documents using LLM-generated metadata</param>
	/// <param name="searchNumThreads">The number of search threads the inference endpoint should use. Defaults: 8</param>
	/// <param name="indexNumThreads">The number of index threads the inference endpoint should use. Defaults: 8</param>
	/// <param name="noEis">Do not use the Elastic Inference Service, bootstrap inference endpoint</param>
	/// <param name="bootstrapTimeout">Timeout in minutes for the inference endpoint creation. Defaults: 4</param>
	/// <param name="indexNamePrefix">The prefix for the computed index/alias names. Defaults: semantic-docs</param>
	/// <param name="forceReindex">Force reindex strategy to semantic index</param>
	/// <param name="bufferSize">The number of documents to send to ES as part of the bulk. Defaults: 100</param>
	/// <param name="maxRetries">The number of times failed bulk items should be retried. Defaults: 3</param>
	/// <param name="debugMode">Buffer ES request/responses for better error messages and pass ?pretty to all requests</param>
	/// <param name="proxyAddress">Route requests through a proxy server</param>
	/// <param name="proxyPassword">Proxy server password</param>
	/// <param name="proxyUsername">Proxy server username</param>
	/// <param name="disableSslVerification">Disable SSL certificate validation (EXPERT OPTION)</param>
	/// <param name="certificateFingerprint">Pass a self-signed certificate fingerprint to validate the SSL connection</param>
	/// <param name="certificatePath">Pass a self-signed certificate to validate the SSL connection</param>
	/// <param name="certificateNotRoot">If the certificate is not root but only part of the validation chain pass this</param>
	/// <param name="ctx"></param>
	/// <returns></returns>
	public async Task<bool> Index(IDiagnosticsCollector collector,
		FileSystem fileSystem,
		string? path = null,
		string? endpoint = null,
		string? apiKey = null,
		string? username = null,
		string? password = null,
		// inference options
		bool? noSemantic = null,
		bool? enableAiEnrichment = null,
		int? searchNumThreads = null,
		int? indexNumThreads = null,
		bool? noEis = null,
		int? bootstrapTimeout = null,
		// index options
		string? indexNamePrefix = null,
		bool? forceReindex = null,
		// channel buffer options
		int? bufferSize = null,
		int? maxRetries = null,
		// connection options
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
		var cfg = _configurationContext.Endpoints.Elasticsearch;
		var options = new ElasticsearchIndexOptions
		{
			Endpoint = endpoint,
			ApiKey = apiKey,
			Username = username,
			Password = password,
			NoSemantic = noSemantic,
			EnableAiEnrichment = enableAiEnrichment,
			SearchNumThreads = searchNumThreads,
			IndexNumThreads = indexNumThreads,
			NoEis = noEis,
			BootstrapTimeout = bootstrapTimeout,
			IndexNamePrefix = indexNamePrefix,
			ForceReindex = forceReindex,
			BufferSize = bufferSize,
			MaxRetries = maxRetries,
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

		var exporters = new HashSet<Exporter> { Elasticsearch };

		return await Build(collector, fileSystem,
			metadataOnly: true, strict: false, path: path, output: null, pathPrefix: null,
			force: true, allowIndexing: null, exporters: exporters, canonicalBaseUrl: null,
		ctx: ctx);
	}
}
