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

		var ns = ResolveEnvironment(config, appConfiguration);

		return new DocumentationEndpoints { Elasticsearch = endpoint, Namespace = ns };
	}

	/// <summary>
	/// Resolves the environment name using this priority:
	/// 1. <c>DOCUMENTATION_ELASTIC_INDEX</c> env var — parse old format <c>{variant}-docs-{env}-{timestamp}</c>
	/// 2. <c>DOTNET_ENVIRONMENT</c> env var
	/// 3. <c>ENVIRONMENT</c> env var
	/// 4. Fallback: <c>"dev"</c>
	/// </summary>
	private static string ResolveEnvironment(IConfiguration config, IConfiguration? appConfiguration)
	{
		var indexName = appConfiguration?["DOCUMENTATION_ELASTIC_INDEX"]
			?? config["DOCUMENTATION_ELASTIC_INDEX"];

		if (!string.IsNullOrEmpty(indexName))
		{
			// Old production format: {variant}-docs-{env}-{timestamp}
			// e.g. "lexical-docs-edge-2025.10.23.120521"
			// Extract the environment segment after "docs-" and before the next "-" followed by digits.
			const string marker = "-docs-";
			var markerIndex = indexName.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
			if (markerIndex >= 0)
			{
				var afterMarker = indexName[(markerIndex + marker.Length)..];
				var dashIndex = afterMarker.IndexOf('-');
				var env = dashIndex > 0 ? afterMarker[..dashIndex] : afterMarker;
				if (!string.IsNullOrEmpty(env) && (dashIndex < 0 || char.IsDigit(afterMarker[dashIndex + 1])))
					return env.ToLowerInvariant();
			}
		}

		var envVar = config["DOTNET_ENVIRONMENT"]
			?? config["ENVIRONMENT"];

		return !string.IsNullOrEmpty(envVar) ? envVar.ToLowerInvariant() : "dev";
	}
}
