// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Actions.Core.Services;
using ConsoleAppFramework;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Isolated;
using Elastic.Documentation.Services;
using Microsoft.Extensions.Logging;

namespace Documentation.Builder.Commands;

internal sealed class IndexCommand(
	ILoggerFactory logFactory,
	IDiagnosticsCollector collector,
	IConfigurationContext configurationContext,
	ICoreService githubActionsService
)
{
	/// <summary>
	/// Index a single documentation set to Elasticsearch, calls `docs-builder --exporters elasticsearch`. Exposes more options
	/// </summary>
	/// <param name="endpoint">-es, Elasticsearch endpoint, alternatively set env DOCUMENTATION_ELASTIC_URL</param>
	/// <param name="path">path to the documentation folder, defaults to pwd.</param>
	/// <param name="apiKey">Elasticsearch API key, alternatively set env DOCUMENTATION_ELASTIC_APIKEY</param>
	/// <param name="username">Elasticsearch username (basic auth), alternatively set env DOCUMENTATION_ELASTIC_USERNAME</param>
	/// <param name="password">Elasticsearch password (basic auth), alternatively set env DOCUMENTATION_ELASTIC_PASSWORD</param>
	/// <param name="enableAiEnrichment">Enable AI enrichment of documents using LLM-generated metadata</param>
	/// <param name="searchNumThreads">The number of search threads the inference endpoint should use. Defaults: 8</param>
	/// <param name="indexNumThreads">The number of index threads the inference endpoint should use. Defaults: 8</param>
	/// <param name="indexNamePrefix">The prefix for the computed index/alias names. Defaults: semantic-docs</param>
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
		string? endpoint = null,
		string? path = null,
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
		string? indexNamePrefix = null,
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
		var service = new IsolatedIndexService(logFactory, configurationContext, githubActionsService);
		var state = (fs, path,
				// endpoint options
				endpoint, apiKey, username, password,
				// inference options
				enableAiEnrichment, indexNumThreads, noEis, searchNumThreads, bootstrapTimeout,
				// channel and connection options
				indexNamePrefix, forceReindex, bufferSize, maxRetries, debugMode,
				// proxy options
				proxyAddress, proxyPassword, proxyUsername,
				// certificate options
				disableSslVerification, certificateFingerprint, certificatePath, certificateNotRoot
			);
		serviceInvoker.AddCommand(service, state,
			static async (s, collector, state, ctx) => await s.Index(collector, state.fs, state.path,
				// endpoint options
				state.endpoint, state.apiKey, state.username, state.password,
				// inference options
				state.enableAiEnrichment, state.searchNumThreads, state.indexNumThreads, state.noEis, state.bootstrapTimeout,
				// channel and connection options
				state.indexNamePrefix, state.forceReindex, state.bufferSize, state.maxRetries, state.debugMode,
				// proxy options
				state.proxyAddress, state.proxyPassword, state.proxyUsername,
				// certificate options
				state.disableSslVerification, state.certificateFingerprint, state.certificatePath, state.certificateNotRoot
				, ctx)
		);

		return await serviceInvoker.InvokeAsync(ctx);
	}
}
