// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.ComponentModel.DataAnnotations;
using System.IO.Abstractions;
using System.Security.Cryptography.X509Certificates;
using Elastic.Documentation.Diagnostics;
using Nullean.Argh;

namespace Elastic.Documentation.Configuration;

/// <summary>
/// Elasticsearch connection and indexing options shared by all index commands.
/// Bind from CLI flags via argh <c>[AsParameters]</c>.
/// </summary>
public record ElasticsearchIndexOptions
{
	// --- endpoint ---

	/// <summary>-es,--endpoint, Elasticsearch endpoint URL. Falls back to env <c>DOCUMENTATION_ELASTIC_URL</c>.</summary>
	[Url]
	public Uri? Endpoint { get; init; }

	/// <summary>API key for authentication. Falls back to env <c>DOCUMENTATION_ELASTIC_APIKEY</c>.</summary>
	public string? ApiKey { get; init; }

	/// <summary>Username for basic authentication. Falls back to env <c>DOCUMENTATION_ELASTIC_USERNAME</c>.</summary>
	public string? Username { get; init; }

	/// <summary>Password for basic authentication. Falls back to env <c>DOCUMENTATION_ELASTIC_PASSWORD</c>.</summary>
	public string? Password { get; init; }

	// --- inference ---

	/// <summary>Enable AI enrichment of documents using LLM-generated metadata (enabled by default).</summary>
	public bool? AiEnrichment { get; init; }

	/// <summary>Number of search threads for the inference endpoint.</summary>
	[Range(1, 128)]
	public int? SearchNumThreads { get; init; }

	/// <summary>Number of index threads for the inference endpoint.</summary>
	[Range(1, 128)]
	public int? IndexNumThreads { get; init; }

	/// <summary>Use the Elastic Inference Service to bootstrap the inference endpoint (enabled by default).</summary>
	public bool? Eis { get; init; }

	/// <summary>How long to wait for the inference endpoint to become ready (e.g. <c>4m</c>, <c>90s</c>).</summary>
	[TimeSpanRange("1s", "60m")]
	public TimeSpan? BootstrapTimeout { get; init; }

	// --- index behavior ---

	/// <summary>Force a full reindex, discarding any incremental state.</summary>
	public bool? ForceReindex { get; init; }

	/// <summary>Number of documents per bulk request.</summary>
	[Range(1, 10_000)]
	public int? BufferSize { get; init; }

	/// <summary>Number of retry attempts for failed bulk items.</summary>
	[Range(0, 20)]
	public int? MaxRetries { get; init; }

	/// <summary>Log every Elasticsearch request and response body; append <c>?pretty</c> to all requests.</summary>
	public bool? DebugMode { get; init; }

	// --- proxy ---

	/// <summary>Route requests through this proxy URL.</summary>
	[Url]
	public Uri? ProxyAddress { get; init; }

	/// <summary>Proxy server username.</summary>
	public string? ProxyUsername { get; init; }

	/// <summary>Proxy server password.</summary>
	public string? ProxyPassword { get; init; }

	// --- certificate ---

	/// <summary>Disable SSL certificate validation. Use only in controlled environments.</summary>
	public bool? DisableSslVerification { get; init; }

	/// <summary>SHA-256 fingerprint of a self-signed server certificate.</summary>
	public string? CertificateFingerprint { get; init; }

	/// <summary>Path to a PEM or DER certificate file for SSL validation.</summary>
	public string? CertificatePath { get; init; }

	/// <summary>Set when the certificate is an intermediate CA rather than the root.</summary>
	public bool? CertificateNotRoot { get; init; }
}

/// <summary>
/// Applies <see cref="ElasticsearchIndexOptions"/> to an <see cref="ElasticsearchEndpoint"/>. Shared by all index commands.
/// </summary>
public static class ElasticsearchEndpointConfigurator
{
	public static async Task ApplyAsync(
		ElasticsearchEndpoint cfg,
		ElasticsearchIndexOptions options,
		IDiagnosticsCollector collector,
		IFileSystem fileSystem,
		Cancel ctx)
	{
		if (options.Endpoint is not null)
			cfg.Uri = options.Endpoint;

		if (!string.IsNullOrEmpty(options.ApiKey))
			cfg.ApiKey = options.ApiKey;
		if (!string.IsNullOrEmpty(options.Username))
			cfg.Username = options.Username;
		if (!string.IsNullOrEmpty(options.Password))
			cfg.Password = options.Password;

		if (options.SearchNumThreads.HasValue)
			cfg.SearchNumThreads = options.SearchNumThreads.Value;
		if (options.IndexNumThreads.HasValue)
			cfg.IndexNumThreads = options.IndexNumThreads.Value;
		if (options.Eis == false)
			cfg.NoElasticInferenceService = true;
		if (options.BufferSize.HasValue)
			cfg.BufferSize = options.BufferSize.Value;
		if (options.MaxRetries.HasValue)
			cfg.MaxRetries = options.MaxRetries.Value;
		if (options.DebugMode.HasValue)
			cfg.DebugMode = options.DebugMode.Value;
		if (!string.IsNullOrEmpty(options.CertificateFingerprint))
			cfg.CertificateFingerprint = options.CertificateFingerprint;
		if (options.ProxyAddress is not null)
			cfg.ProxyAddress = options.ProxyAddress.ToString();
		if (!string.IsNullOrEmpty(options.ProxyPassword))
			cfg.ProxyPassword = options.ProxyPassword;
		if (!string.IsNullOrEmpty(options.ProxyUsername))
			cfg.ProxyUsername = options.ProxyUsername;
		if (options.DisableSslVerification.HasValue)
			cfg.DisableSslVerification = options.DisableSslVerification.Value;
		if (!string.IsNullOrEmpty(options.CertificatePath))
		{
			if (!fileSystem.File.Exists(options.CertificatePath))
				collector.EmitGlobalError($"'{options.CertificatePath}' does not exist");
			var bytes = await fileSystem.File.ReadAllBytesAsync(options.CertificatePath, ctx);
			cfg.Certificate = X509CertificateLoader.LoadCertificate(bytes);
		}
		if (options.CertificateNotRoot.HasValue)
			cfg.CertificateIsNotRoot = options.CertificateNotRoot.Value;
		if (options.BootstrapTimeout.HasValue)
			cfg.BootstrapTimeout = (int)options.BootstrapTimeout.Value.TotalMinutes;
		if (options.AiEnrichment == false)
			cfg.EnableAiEnrichment = false;
		if (options.ForceReindex.HasValue)
			cfg.ForceReindex = options.ForceReindex.Value;
	}
}
