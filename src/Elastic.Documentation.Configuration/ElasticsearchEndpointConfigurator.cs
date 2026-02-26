// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.Security.Cryptography.X509Certificates;
using Elastic.Documentation.Diagnostics;

namespace Elastic.Documentation.Configuration;

/// <summary>
/// Options record for configuring an Elasticsearch endpoint from CLI arguments.
/// Shared by all index commands (isolated, assembler, codex).
/// </summary>
public record ElasticsearchIndexOptions
{
	// endpoint options
	public string? Endpoint { get; init; }
	public string? ApiKey { get; init; }
	public string? Username { get; init; }
	public string? Password { get; init; }

	// inference options
	public bool? EnableAiEnrichment { get; init; }
	public int? SearchNumThreads { get; init; }
	public int? IndexNumThreads { get; init; }
	public bool? NoEis { get; init; }
	public int? BootstrapTimeout { get; init; }

	// index options
	public bool? ForceReindex { get; init; }

	// channel buffer options
	public int? BufferSize { get; init; }
	public int? MaxRetries { get; init; }

	// connection options
	public bool? DebugMode { get; init; }
	public string? ProxyAddress { get; init; }
	public string? ProxyPassword { get; init; }
	public string? ProxyUsername { get; init; }

	// certificate options
	public bool? DisableSslVerification { get; init; }
	public string? CertificateFingerprint { get; init; }
	public string? CertificatePath { get; init; }
	public bool? CertificateNotRoot { get; init; }
}

/// <summary>
/// Applies CLI options to an <see cref="ElasticsearchEndpoint"/>. Shared by all index commands.
/// </summary>
public static class ElasticsearchEndpointConfigurator
{
	/// <summary>
	/// Applies the given options to the Elasticsearch endpoint configuration.
	/// </summary>
	public static async Task ApplyAsync(
		ElasticsearchEndpoint cfg,
		ElasticsearchIndexOptions options,
		IDiagnosticsCollector collector,
		IFileSystem fileSystem,
		Cancel ctx)
	{
		if (!string.IsNullOrEmpty(options.Endpoint))
		{
			if (!Uri.TryCreate(options.Endpoint, UriKind.Absolute, out var uri))
				collector.EmitGlobalError($"'{options.Endpoint}' is not a valid URI");
			else
				cfg.Uri = uri;
		}

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
		if (options.NoEis.HasValue)
			cfg.NoElasticInferenceService = options.NoEis.Value;
		if (options.BufferSize.HasValue)
			cfg.BufferSize = options.BufferSize.Value;
		if (options.MaxRetries.HasValue)
			cfg.MaxRetries = options.MaxRetries.Value;
		if (options.DebugMode.HasValue)
			cfg.DebugMode = options.DebugMode.Value;
		if (!string.IsNullOrEmpty(options.CertificateFingerprint))
			cfg.CertificateFingerprint = options.CertificateFingerprint;
		if (!string.IsNullOrEmpty(options.ProxyAddress))
			cfg.ProxyAddress = options.ProxyAddress;
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
			var loader = X509CertificateLoader.LoadCertificate(bytes);
			cfg.Certificate = loader;
		}

		if (options.CertificateNotRoot.HasValue)
			cfg.CertificateIsNotRoot = options.CertificateNotRoot.Value;
		if (options.BootstrapTimeout.HasValue)
			cfg.BootstrapTimeout = options.BootstrapTimeout.Value;

		if (options.EnableAiEnrichment.HasValue)
			cfg.EnableAiEnrichment = options.EnableAiEnrichment.Value;
		if (options.ForceReindex.HasValue)
			cfg.ForceReindex = options.ForceReindex.Value;
	}
}
