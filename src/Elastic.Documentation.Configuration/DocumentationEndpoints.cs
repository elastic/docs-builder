// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Security.Cryptography.X509Certificates;

namespace Elastic.Documentation.Configuration;

public class DocumentationEndpoints
{
	public required ElasticsearchEndpoint Elasticsearch { get; init; }

	/// <summary>
	/// Index namespace for environment isolation. Maps to the <c>{env}</c> placeholder
	/// in index name templates. Overridden by the assembler <c>--environment</c> flag.
	/// </summary>
	public string Environment { get; set; } = "dev";

	/// <summary>
	/// Build type identifier (assembler, isolated, codex). Controlled by DOCS_BUILD_TYPE env var.
	/// </summary>
	public string BuildType { get; set; } = "isolated";

	/// <summary>
	/// Optional comma-separated list of index names that override the computed search index.
	/// When set, search queries target these indices instead of the one derived from
	/// <see cref="BuildType"/> and <see cref="Environment"/>.
	/// Controlled by <c>DOCUMENTATION_ELASTIC_INDEX_OVERRIDE</c> env var.
	/// </summary>
	public string? SearchIndexOverride { get; set; }
}

public class ElasticsearchEndpoint
{
	public static ElasticsearchEndpoint Default { get; } = new() { Uri = new Uri("https://localhost:9200") };

	public required Uri Uri { get; set; }
	public string? Username { get; set; }
	public string? Password { get; set; }
	public string? ApiKey { get; set; }

	// inference options
	public int SearchNumThreads { get; set; } = 8;
	public int IndexNumThreads { get; set; } = 4; // Reduced for Serverless rate limits
	public bool NoElasticInferenceService { get; set; }

	// channel buffer options
	public int BufferSize { get; set; } = 100; // Reduced for Serverless rate limits
	public int MaxRetries { get; set; } = 5; // Increased for 429 retries

	// connection options
	public bool DebugMode { get; set; }
	public string? CertificateFingerprint { get; set; }
	public string? ProxyAddress { get; set; }
	public string? ProxyPassword { get; set; }
	public string? ProxyUsername { get; set; }

	public bool DisableSslVerification { get; set; }
	public X509Certificate? Certificate { get; set; }
	public bool CertificateIsNotRoot { get; set; }
	public int? BootstrapTimeout { get; set; }
	public bool ForceReindex { get; set; }

	/// <summary>
	/// Enable AI enrichment of documents using LLM-generated metadata.
	/// When enabled, documents are enriched with summaries, search queries, and questions.
	/// </summary>
	public bool EnableAiEnrichment { get; set; } = true;
}
