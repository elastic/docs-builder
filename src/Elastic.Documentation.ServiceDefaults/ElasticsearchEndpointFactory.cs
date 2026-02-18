// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration;
using Microsoft.Extensions.Configuration;

namespace Elastic.Documentation.ServiceDefaults;

/// <summary>Centralizes user-secrets + env-var reading for Elasticsearch configuration.</summary>
public static class ElasticsearchEndpointFactory
{
	private const string UserSecretsId = "72f50f33-6fb9-4d08-bff3-39568fe370b3";

	/// <summary>
	/// Creates <see cref="DocumentationEndpoints"/> from user secrets and environment variables.
	/// Returns <c>null</c> when no URL is available.
	/// </summary>
	public static DocumentationEndpoints Create(IConfiguration? appConfiguration = null)
	{
		var configBuilder = new ConfigurationBuilder();
		_ = configBuilder.AddUserSecrets(UserSecretsId);
		_ = configBuilder.AddEnvironmentVariables();
		var config = configBuilder.Build();

		var url =
			config["Parameters:DocumentationElasticUrl"]
			?? config["DOCUMENTATION_ELASTIC_URL"];

		var apiKey =
			config["Parameters:DocumentationElasticApiKey"]
			?? config["DOCUMENTATION_ELASTIC_APIKEY"];

		var password =
			config["Parameters:DocumentationElasticPassword"]
			?? config["DOCUMENTATION_ELASTIC_PASSWORD"];

		var username =
			config["Parameters:DocumentationElasticUsername"]
			?? config["DOCUMENTATION_ELASTIC_USERNAME"]
			?? "elastic";

		if (string.IsNullOrEmpty(url))
		{
			return new DocumentationEndpoints
			{
				Elasticsearch = new ElasticsearchEndpoint { Uri = new Uri("http://localhost:9200") }
			};
		}

		var endpoint = new ElasticsearchEndpoint
		{
			Uri = new Uri(url),
			ApiKey = apiKey,
			Password = password,
			Username = username
		};

		var ns = ResolveNamespace(config, appConfiguration, endpoint.IndexNamePrefix);

		return new DocumentationEndpoints { Elasticsearch = endpoint, Namespace = ns };
	}

	/// <summary>
	/// Resolves the deployment namespace using this priority:
	/// 1. <c>DOCUMENTATION_ELASTIC_INDEX</c> env var — strip prefix and <c>-latest</c> suffix
	/// 2. <c>DOTNET_ENVIRONMENT</c> env var
	/// 3. <c>ENVIRONMENT</c> env var
	/// 4. Fallback: <c>"dev"</c>
	/// </summary>
	private static string ResolveNamespace(IConfiguration config, IConfiguration? appConfiguration, string indexNamePrefix)
	{
		var indexName = appConfiguration?["DOCUMENTATION_ELASTIC_INDEX"]
			?? config["DOCUMENTATION_ELASTIC_INDEX"];

		if (!string.IsNullOrEmpty(indexName))
		{
			var prefix = $"{indexNamePrefix}-";
			const string suffix = "-latest";
			if (indexName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
				indexName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
			{
				var ns = indexName[prefix.Length..^suffix.Length];
				if (!string.IsNullOrEmpty(ns))
					return ns;
			}
		}

		var env = config["DOTNET_ENVIRONMENT"]
			?? config["ENVIRONMENT"];

		return !string.IsNullOrEmpty(env) ? env.ToLowerInvariant() : "dev";
	}
}
