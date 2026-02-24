// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Actions.Core.Services;
using ConsoleAppFramework;
using Elastic.Codex;
using Elastic.Codex.Indexing;
using Elastic.Codex.Sourcing;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Codex;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Isolated;
using Elastic.Documentation.LinkIndex;
using Elastic.Documentation.Services;
using Microsoft.Extensions.Logging;

namespace Documentation.Builder.Commands.Codex;

/// <summary>
/// Command for indexing codex documentation into Elasticsearch.
/// </summary>
internal sealed class CodexIndexCommand(
	ILoggerFactory logFactory,
	IDiagnosticsCollector collector,
	IConfigurationContext configurationContext,
	ICoreService githubActionsService
)
{
	/// <summary>
	/// Index codex documentation to Elasticsearch.
	/// </summary>
	/// <param name="config">Path to the codex configuration file.</param>
	/// <param name="endpoint">-es, Elasticsearch endpoint, alternatively set env DOCUMENTATION_ELASTIC_URL</param>
	/// <param name="apiKey">Elasticsearch API key, alternatively set env DOCUMENTATION_ELASTIC_APIKEY</param>
	/// <param name="username">Elasticsearch username (basic auth), alternatively set env DOCUMENTATION_ELASTIC_USERNAME</param>
	/// <param name="password">Elasticsearch password (basic auth), alternatively set env DOCUMENTATION_ELASTIC_PASSWORD</param>
	/// <param name="enableAiEnrichment">Enable AI enrichment of documents using LLM-generated metadata</param>
	/// <param name="searchNumThreads">The number of search threads the inference endpoint should use. Defaults: 8</param>
	/// <param name="indexNumThreads">The number of index threads the inference endpoint should use. Defaults: 8</param>
	/// <param name="noEis">Do not use the Elastic Inference Service, bootstrap inference endpoint</param>
	/// <param name="forceReindex">Force reindex strategy to semantic index</param>
	/// <param name="bootstrapTimeout">Timeout in minutes for the inference endpoint creation. Defaults: 4</param>
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
	[Command("")]
	public async Task<int> Index(
		[Argument] string config,
		string? endpoint = null,
		string? apiKey = null,
		string? username = null,
		string? password = null,

		// inference options
		bool? enableAiEnrichment = null,
		int? searchNumThreads = null,
		int? indexNumThreads = null,
		bool? noEis = null,
		int? bootstrapTimeout = null,

		// index options
		bool? forceReindex = null,

		// channel buffer options
		int? bufferSize = null,
		int? maxRetries = null,

		// connection options
		bool? debugMode = null,

		// proxy options
		string? proxyAddress = null,
		string? proxyPassword = null,
		string? proxyUsername = null,

		// certificate options
		bool? disableSslVerification = null,
		string? certificateFingerprint = null,
		string? certificatePath = null,
		bool? certificateNotRoot = null,
		Cancel ctx = default
	)
	{
		await using var serviceInvoker = new ServiceInvoker(collector);
		var fs = new FileSystem();

		var configPath = fs.Path.GetFullPath(config);
		var configFile = fs.FileInfo.New(configPath);

		if (!configFile.Exists)
		{
			collector.EmitGlobalError($"Codex configuration file not found: {configPath}");
			return 1;
		}

		var codexConfig = CodexConfiguration.Load(configFile);

		if (string.IsNullOrWhiteSpace(codexConfig.Environment))
		{
			collector.EmitGlobalError("Codex configuration must specify an 'environment' (e.g., 'engineering', 'security').");
			return 1;
		}

		var codexContext = new CodexContext(codexConfig, configFile, collector, fs, fs, null, null);

		using var linkIndexReader = new GitLinkIndexReader(codexConfig.Environment);
		var cloneService = new CodexCloneService(logFactory, linkIndexReader);
		var cloneResult = await cloneService.CloneAll(codexContext, fetchLatest: false, assumeCloned: true, ctx);

		if (cloneResult.Checkouts.Count == 0)
		{
			collector.EmitGlobalError("No documentation sets found. Run 'docs-builder codex clone' first.");
			return 1;
		}

		var esOptions = new ElasticsearchIndexOptions
		{
			Endpoint = endpoint,
			ApiKey = apiKey,
			Username = username,
			Password = password,
			EnableAiEnrichment = enableAiEnrichment,
			SearchNumThreads = searchNumThreads,
			IndexNumThreads = indexNumThreads,
			NoEis = noEis,
			BootstrapTimeout = bootstrapTimeout,
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

		var isolatedBuildService = new IsolatedBuildService(logFactory, configurationContext, githubActionsService);
		var service = new CodexIndexService(logFactory, configurationContext, isolatedBuildService);
		serviceInvoker.AddCommand(service, (codexContext, cloneResult, fs, esOptions),
			static async (s, col, state, c) =>
				await s.Index(state.codexContext, state.cloneResult, state.fs, state.esOptions, c)
		);

		return await serviceInvoker.InvokeAsync(ctx);
	}
}
