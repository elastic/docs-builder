// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration;
using Microsoft.Extensions.Configuration;

namespace Elastic.Documentation.ServiceDefaults;

/// <summary>Centralizes env-var + user-secrets reading for Elasticsearch configuration.</summary>
public static class ElasticsearchEndpointFactory
{
	private const string UserSecretsId = "docs-builder";

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
			?? config["Parameters:ElasticsearchUrl"];

		var apiKey =
			config["DOCUMENTATION_ELASTIC_APIKEY"]
			?? config["Parameters:ElasticsearchApiKey"];

		var password =
			config["DOCUMENTATION_ELASTIC_PASSWORD"]
			?? config["Parameters:ElasticsearchPassword"];

		var username =
			config["DOCUMENTATION_ELASTIC_USERNAME"]
			?? config["Parameters:ElasticsearchUsername"]
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

		buildType ??= appConfiguration?["DOCS_BUILD_TYPE"] ?? config["DOCS_BUILD_TYPE"] ?? "isolated";
		IEnvironmentValidator environmentValidator = buildType == "codex"
			? new CodexEnvironmentValidator()
			: new SiteEnvironmentValidator();
		environment ??= environmentValidator.Resolve(appConfiguration?["ENVIRONMENT"] ?? config["ENVIRONMENT"]);

		var searchIndexOverride =
			config["DOCUMENTATION_ELASTIC_INDEX_OVERRIDE"]
			?? config["Parameters:ElasticsearchIndexOverride"];

		return new DocumentationEndpoints
		{
			Elasticsearch = endpoint,
			Environment = environment,
			BuildType = buildType,
			SearchIndexOverride = !string.IsNullOrEmpty(searchIndexOverride) ? searchIndexOverride : null
		};
	}
}
