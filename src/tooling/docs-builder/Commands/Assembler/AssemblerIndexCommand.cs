// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Actions.Core.Services;
using ConsoleAppFramework;
using Elastic.Documentation.Assembler.Indexing;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Services;
using Microsoft.Extensions.Logging;

namespace Documentation.Builder.Commands.Assembler;

internal sealed class AssemblerIndexCommand(
	ILoggerFactory logFactory,
	IDiagnosticsCollector collector,
	AssemblyConfiguration configuration,
	IConfigurationContext configurationContext,
	ICoreService githubActionsService
)
{
	/// <summary>
	/// Index documentation to Elasticsearch, calls `docs-builder assembler build --exporters elasticsearch`. Exposes more options
	/// </summary>
	/// <param name="endpoint">-es, Elasticsearch endpoint, alternatively set env DOCUMENTATION_ELASTIC_URL</param>
	/// <param name="environment">The --environment used to clone ends up being part of the index name</param>
	/// <param name="apiKey">Elasticsearch API key, alternatively set env DOCUMENTATION_ELASTIC_APIKEY</param>
	/// <param name="username">Elasticsearch username (basic auth), alternatively set env DOCUMENTATION_ELASTIC_USERNAME</param>
	/// <param name="password">Elasticsearch password (basic auth), alternatively set env DOCUMENTATION_ELASTIC_PASSWORD</param>
	/// <param name="noSemantic">Index without semantic fields</param>
	/// <param name="searchNumThreads">The number of search threads the inference endpoint should use. Defaults: 8</param>
	/// <param name="indexNumThreads">The number of index threads the inference endpoint should use. Defaults: 8</param>
	/// <param name="indexNamePrefix">The prefix for the computed index/alias names. Defaults: semantic-docs</param>
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
		string? environment = null,
		string? apiKey = null,
		string? username = null,
		string? password = null,

		// inference options
		bool? noSemantic = null,
		int? searchNumThreads = null,
		int? indexNumThreads = null,
		int? bootstrapTimeout = null,

		// index options
		string? indexNamePrefix = null,

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
		var service = new AssemblerIndexService(logFactory, configuration, configurationContext, githubActionsService);
		var state = (fs,
				// endpoint options
				endpoint, environment, apiKey, username, password,
				// inference options
				noSemantic, indexNumThreads, searchNumThreads, bootstrapTimeout,
				// channel and connection options
				indexNamePrefix, bufferSize, maxRetries, debugMode,
				// proxy options
				proxyAddress, proxyPassword, proxyUsername,
				// certificate options
				disableSslVerification, certificateFingerprint, certificatePath, certificateNotRoot
			);
		serviceInvoker.AddCommand(service, state,
			static async (s, collector, state, ctx) => await s.Index(collector, state.fs,
				// endpoint options
				state.endpoint, state.environment, state.apiKey, state.username, state.password,
				// inference options
				state.noSemantic, state.searchNumThreads, state.indexNumThreads, state.bootstrapTimeout,
				// channel and connection options
				state.indexNamePrefix, state.bufferSize, state.maxRetries, state.debugMode,
				// proxy options
				state.proxyAddress, state.proxyPassword, state.proxyUsername,
				// certificate options
				state.disableSslVerification, state.certificateFingerprint, state.certificatePath, state.certificateNotRoot
				, ctx)
		);

		return await serviceInvoker.InvokeAsync(ctx);
	}
}
