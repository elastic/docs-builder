// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration;
using Elastic.Transport;
using Elastic.Transport.Products.Elasticsearch;

namespace Elastic.Markdown.Exporters.Elasticsearch;

/// <summary>
/// Factory for creating Elasticsearch transport from endpoint configuration.
/// </summary>
public static class ElasticsearchTransportFactory
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
			ProxyAddress = endpoint.ProxyAddress,
			ProxyPassword = endpoint.ProxyPassword,
			ProxyUsername = endpoint.ProxyUsername,
			ServerCertificateValidationCallback = endpoint.DisableSslVerification
				? CertificateValidations.AllowAll
				: endpoint.Certificate is { } cert
					? endpoint.CertificateIsNotRoot
						? CertificateValidations.AuthorityPartOfChain(cert)
						: CertificateValidations.AuthorityIsRoot(cert)
					: null
		};

		return new DistributedTransport(configuration);
	}
}
