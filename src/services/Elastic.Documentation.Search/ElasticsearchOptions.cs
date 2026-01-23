// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Microsoft.Extensions.Configuration;

namespace Elastic.Documentation.Search;

public class ElasticsearchOptions
{
	public ElasticsearchOptions(IConfiguration configuration)
	{
		// Build a new ConfigurationBuilder to read user secrets
		var configBuilder = new ConfigurationBuilder();
		_ = configBuilder.AddUserSecrets("72f50f33-6fb9-4d08-bff3-39568fe370b3");
		var userSecretsConfig = configBuilder.Build();
		var elasticUrlFromSecret = userSecretsConfig["Parameters:DocumentationElasticUrl"];
		var elasticApiKeyFromSecret = userSecretsConfig["Parameters:DocumentationElasticApiKey"];

		Url = GetEnv("DOCUMENTATION_ELASTIC_URL", elasticUrlFromSecret);
		ApiKey = GetEnv("DOCUMENTATION_ELASTIC_APIKEY", elasticApiKeyFromSecret);
		IndexName = configuration["DOCUMENTATION_ELASTIC_INDEX"] ?? "semantic-docs-dev-latest";
	}

	private static string GetEnv(string name, string? defaultValue = null)
	{
		var value = Environment.GetEnvironmentVariable(name);
		if (!string.IsNullOrEmpty(value))
			return value;
		if (defaultValue != null)
			return defaultValue;
		throw new ArgumentException($"Environment variable '{name}' not found.");
	}

	// Read from environment variables (set by Terraform from SSM at deploy time)
	public string Url { get; }
	public string ApiKey { get; }
	public string IndexName { get; }
}
