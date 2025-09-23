// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.Security.Cryptography.X509Certificates;
using Actions.Core.Services;
using Elastic.Documentation.Assembler.Building;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Diagnostics;
using Microsoft.Extensions.Logging;
using static Elastic.Documentation.Exporter;

namespace Elastic.Documentation.Assembler.Indexing;

public class AssemblerIndexService(
	ILoggerFactory logFactory,
	AssemblyConfiguration assemblyConfiguration,
	IConfigurationContext configurationContext,
	ICoreService githubActionsService
) : AssemblerBuildService(logFactory, assemblyConfiguration, configurationContext, githubActionsService)
{
	private readonly IConfigurationContext _configurationContext = configurationContext;

	/// <summary>
	/// Index documentation to Elasticsearch, calls `docs-builder assembler build --exporters elasticsearch`. Exposes more options
	/// </summary>
	/// <param name="collector"></param>
	/// <param name="fileSystem"></param>
	/// <param name="endpoint">Elasticsearch endpoint, alternatively set env DOCUMENTATION_ELASTIC_URL</param>
	/// <param name="environment">The --environment used to clone ends up being part of the index name</param>
	/// <param name="apiKey">Elasticsearch API key, alternatively set env DOCUMENTATION_ELASTIC_APIKEY</param>
	/// <param name="username">Elasticsearch username (basic auth), alternatively set env DOCUMENTATION_ELASTIC_USERNAME</param>
	/// <param name="password">Elasticsearch password (basic auth), alternatively set env DOCUMENTATION_ELASTIC_PASSWORD</param>
	/// <param name="noSemantic">Index without semantic fields</param>
	/// <param name="searchNumThreads">The number of search threads the inference endpoint should use. Defaults: 8</param>
	/// <param name="indexNumThreads">The number of index threads the inference endpoint should use. Defaults: 8</param>
	/// <param name="bootstrapTimeout">Timeout in minutes for the inference endpoint creation. Defaults: 4</param>
	/// <param name="indexNamePrefix">The prefix for the computed index/alias names. Defaults: semantic-docs</param>
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
		if (!string.IsNullOrEmpty(endpoint))
		{
			if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri))
				collector.EmitGlobalError($"'{endpoint}' is not a valid URI");
			else
				cfg.Uri = uri;
		}

		if (!string.IsNullOrEmpty(apiKey))
			cfg.ApiKey = apiKey;
		if (!string.IsNullOrEmpty(username))
			cfg.Username = username;
		if (!string.IsNullOrEmpty(password))
			cfg.Password = password;

		if (searchNumThreads.HasValue)
			cfg.SearchNumThreads = searchNumThreads.Value;
		if (indexNumThreads.HasValue)
			cfg.IndexNumThreads = indexNumThreads.Value;
		if (!string.IsNullOrEmpty(indexNamePrefix))
			cfg.IndexNamePrefix = indexNamePrefix;
		if (bufferSize.HasValue)
			cfg.BufferSize = bufferSize.Value;
		if (maxRetries.HasValue)
			cfg.MaxRetries = maxRetries.Value;
		if (debugMode.HasValue)
			cfg.DebugMode = debugMode.Value;
		if (!string.IsNullOrEmpty(certificateFingerprint))
			cfg.CertificateFingerprint = certificateFingerprint;
		if (!string.IsNullOrEmpty(proxyAddress))
			cfg.ProxyAddress = proxyAddress;
		if (!string.IsNullOrEmpty(proxyPassword))
			cfg.ProxyPassword = proxyPassword;
		if (!string.IsNullOrEmpty(proxyUsername))
			cfg.ProxyUsername = proxyUsername;
		if (disableSslVerification.HasValue)
			cfg.DisableSslVerification = disableSslVerification.Value;
		if (!string.IsNullOrEmpty(certificatePath))
		{
			if (!fileSystem.File.Exists(certificatePath))
				collector.EmitGlobalError($"'{certificatePath}' does not exist");
			var bytes = await fileSystem.File.ReadAllBytesAsync(certificatePath, ctx);
			var loader = X509CertificateLoader.LoadCertificate(bytes);
			cfg.Certificate = loader;
		}

		if (certificateNotRoot.HasValue)
			cfg.CertificateIsNotRoot = certificateNotRoot.Value;
		if (bootstrapTimeout.HasValue)
			cfg.BootstrapTimeout = bootstrapTimeout.Value;

		var exporters = new HashSet<Exporter> { noSemantic.GetValueOrDefault(false) ? ElasticsearchNoSemantic : Elasticsearch };

		return await BuildAll(collector, strict: false, environment, metadataOnly: true, exporters, fileSystem, ctx);
	}
}
