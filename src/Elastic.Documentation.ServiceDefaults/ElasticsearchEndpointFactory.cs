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

		var environment = ResolveEnvironment(config, appConfiguration);
		var dataSource = appConfiguration?["DOCS_BUILD_TYPE"] ?? config["DOCS_BUILD_TYPE"] ?? "isolated";

		return new DocumentationEndpoints { Elasticsearch = endpoint, Environment = environment, DataSource = dataSource };
	}

	/// <summary>
	/// Resolves the environment name using this priority:
	/// 1. <c>DOTNET_ENVIRONMENT</c> env var
	/// 2. <c>ENVIRONMENT</c> env var
	/// 3. Fallback: <c>"dev"</c>
	/// </summary>
	private static string ResolveEnvironment(IConfiguration config, IConfiguration? appConfiguration)
	{
		var envVar = appConfiguration?["DOTNET_ENVIRONMENT"]
			?? appConfiguration?["ENVIRONMENT"]
			?? config["DOTNET_ENVIRONMENT"]
			?? config["ENVIRONMENT"];

		return !string.IsNullOrEmpty(envVar) ? envVar.ToLowerInvariant() : "dev";
	}
}
