// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Security.Cryptography.X509Certificates;

namespace Elastic.Documentation.Configuration;

public class DocumentationEndpoints
{
	public required ElasticsearchEndpoint Elasticsearch { get; init; }
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
	public int IndexNumThreads { get; set; } = 8;

	// index options
	public string IndexNamePrefix { get; set; } = "semantic-docs";

	// channel buffer options
	public int BufferSize { get; set; } = 100;
	public int MaxRetries { get; set; } = 3;


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
	public bool NoSemantic { get; set; }
}
