// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Transport;
using Elastic.Transport.Products.Elasticsearch;

namespace Elastic.SiteSearch.Cli.Elasticsearch;

internal static class ElasticsearchTransportFactory
{
	public static DistributedTransport Create(ElasticsearchEndpoint endpoint)
	{
		var configuration = new ElasticsearchConfiguration(endpoint.Uri)
		{
			Authentication = endpoint.ApiKey is { } apiKey
				? new ApiKey(apiKey)
				: endpoint is { Username: { } username, Password: { } password }
					? new BasicAuthentication(username, password)
					: null,
			EnableHttpCompression = true,
			DebugMode = endpoint.DebugMode,
			CertificateFingerprint = endpoint.CertificateFingerprint,
			ServerCertificateValidationCallback = endpoint.DisableSslVerification
				? CertificateValidations.AllowAll
				: null
		};

		return new DistributedTransport(configuration);
	}
}
