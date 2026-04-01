// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Actions.Core.Services;
using ConsoleAppFramework;
using Elastic.Documentation.Assembler.Building;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Services;
using Microsoft.Extensions.Logging;

namespace Documentation.Builder.Commands.Assembler;

internal sealed class AssemblerSitemapCommand(
	ILoggerFactory logFactory,
	IDiagnosticsCollector collector,
	AssemblyConfiguration configuration,
	IConfigurationContext configurationContext,
	ICoreService githubActionsService
)
{
	/// <summary>
	/// Generate sitemap.xml from the Elasticsearch index with correct last_updated dates
	/// </summary>
	/// <param name="endpoint">-es, Elasticsearch endpoint, alternatively set env DOCUMENTATION_ELASTIC_URL</param>
	/// <param name="environment">The --environment used to resolve the ES index name</param>
	/// <param name="apiKey">Elasticsearch API key, alternatively set env DOCUMENTATION_ELASTIC_APIKEY</param>
	/// <param name="username">Elasticsearch username (basic auth), alternatively set env DOCUMENTATION_ELASTIC_USERNAME</param>
	/// <param name="password">Elasticsearch password (basic auth), alternatively set env DOCUMENTATION_ELASTIC_PASSWORD</param>
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
	public async Task<int> Sitemap(
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
		await using var serviceInvoker = new ServiceInvoker(collector);
		var fs = FileSystemFactory.RealRead;
		var service = new AssemblerSitemapService(logFactory, configuration, configurationContext, githubActionsService);
		var state = (fs,
			endpoint, environment, apiKey, username, password,
			debugMode, proxyAddress, proxyPassword, proxyUsername,
			disableSslVerification, certificateFingerprint, certificatePath, certificateNotRoot
		);
		serviceInvoker.AddCommand(service, state,
			static async (s, col, state, ct) => await s.GenerateSitemapAsync(col, state.fs,
				state.endpoint, state.environment, state.apiKey, state.username, state.password,
				state.debugMode, state.proxyAddress, state.proxyPassword, state.proxyUsername,
				state.disableSslVerification, state.certificateFingerprint, state.certificatePath, state.certificateNotRoot,
				ct)
		);

		return await serviceInvoker.InvokeAsync(ctx);
	}
}
