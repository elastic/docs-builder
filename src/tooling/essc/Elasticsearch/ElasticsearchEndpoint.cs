// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.SiteSearch.Cli.Elasticsearch;

internal sealed class ElasticsearchEndpoint
{
	public static ElasticsearchEndpoint Default { get; } = new() { Uri = new Uri("http://localhost:9200") };

	public required Uri Uri { get; set; }
	public string? Username { get; set; }
	public string? Password { get; set; }
	public string? ApiKey { get; set; }

	public int IndexNumThreads { get; set; } = 4;
	public int BufferSize { get; set; } = 100;
	public int MaxRetries { get; set; } = 5;

	public bool DebugMode { get; set; }
	public string? CertificateFingerprint { get; set; }
	public bool DisableSslVerification { get; set; }

	public bool EnableAiEnrichment { get; set; } = true;
}
