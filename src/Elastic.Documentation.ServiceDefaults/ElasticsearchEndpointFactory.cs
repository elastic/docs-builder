// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration;
using Microsoft.Extensions.Configuration;

namespace Elastic.Documentation.ServiceDefaults;

/// <summary>Centralizes env-var + user-secrets reading for Elasticsearch configuration.</summary>
public static class ElasticsearchEndpointFactory
{
	private const string UserSecretsId = "72f50f33-6fb9-4d08-bff3-39568fe370b3";

	/// <summary>
	/// Creates <see cref="DocumentationEndpoints"/> from user secrets and environment variables.
	/// Environment variables take priority over user secrets.
	/// </summary>
	public static DocumentationEndpoints Create(IConfiguration? appConfiguration = null, string? buildType = null, string? environment = null)
	{
		var configBuilder = new ConfigurationBuilder();
		_ = configBuilder.AddUserSecrets(UserSecretsId);
		_ = configBuilder.AddEnvironmentVariables();
		var config = configBuilder.Build();

		var url =
			config["DOCUMENTATION_ELASTIC_URL"]
			?? config["Parameters:DocumentationElasticUrl"];

		var apiKey =
			config["DOCUMENTATION_ELASTIC_APIKEY"]
			?? config["Parameters:DocumentationElasticApiKey"];

		var password =
			config["DOCUMENTATION_ELASTIC_PASSWORD"]
			?? config["Parameters:DocumentationElasticPassword"];

		var username =
			config["DOCUMENTATION_ELASTIC_USERNAME"]
			?? config["Parameters:DocumentationElasticUsername"]
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

		environment ??= ResolveEnvironment(config, appConfiguration);
		buildType ??= appConfiguration?["DOCS_BUILD_TYPE"] ?? config["DOCS_BUILD_TYPE"] ?? "isolated";

		var searchIndexOverride =
			config["DOCUMENTATION_ELASTIC_INDEX_OVERRIDE"]
			?? config["Parameters:DocumentationElasticIndexOverride"];

		return new DocumentationEndpoints
		{
			Elasticsearch = endpoint,
			Environment = environment,
			BuildType = buildType,
			SearchIndexOverride = !string.IsNullOrEmpty(searchIndexOverride) ? searchIndexOverride : null
		};
	}

	/// <summary>
	/// Resolves the domain environment name using this priority:
	/// 1. <c>ENVIRONMENT</c> env var (our deployment env: dev, edge, staging, prod)
	/// 2. Fallback: <c>"dev"</c>
	/// </summary>
	/// <remarks>
	/// Deliberately reads only <c>ENVIRONMENT</c>, not <c>DOTNET_ENVIRONMENT</c>.
	/// <c>DOTNET_ENVIRONMENT</c> is set by <c>AddDocumentationServiceDefaults</c> to the
	/// mapped .NET hosting name (Development/Staging/Production) and must not be used as the
	/// domain environment for index naming or telemetry.
	/// </remarks>
	private static string ResolveEnvironment(IConfiguration config, IConfiguration? appConfiguration)
	{
		var envVar = appConfiguration?["ENVIRONMENT"] ?? config["ENVIRONMENT"];

		string[] allowedEnvironments = ["dev", "edge", "staging", "prod"];
		if (!allowedEnvironments.Contains(envVar?.ToLowerInvariant()))
			envVar = "dev";

		return envVar!.ToLowerInvariant();
	}
}
