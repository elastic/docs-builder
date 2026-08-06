// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration;
using Elastic.SiteSearch.Cli.ContentStack;
using Microsoft.Extensions.Configuration;

namespace Elastic.SiteSearch.Cli.Elasticsearch;

/// <summary>
/// Combines Contentstack + Elasticsearch configuration for the sourcing pipeline.
/// Reads from user secrets, environment variables, and CLI overrides.
/// </summary>
internal sealed class SourcingConfiguration
{
	/// <summary>Non-null when Contentstack env / user-secrets are configured; required for <c>contentstack</c> commands only.</summary>
	public ContentStackConfiguration? ContentStack { get; init; }

	/// <summary>
	/// Resolved when a command needs Contentstack credentials. Re-reads configuration so missing key vs token errors match <see cref="ContentStackConfiguration.CreateFromConfiguration" />.
	/// </summary>
	public ContentStackConfiguration RequireContentStack()
	{
		if (ContentStack is not null)
			return ContentStack;

		var config = new ConfigurationBuilder()
			.AddUserSecrets("docs-builder")
			.AddEnvironmentVariables()
			.Build();
		return ContentStackConfiguration.CreateFromConfiguration(config);
	}
	public required ElasticsearchEndpoint Elasticsearch { get; init; }

	/// <summary>
	/// Destination cluster for cross-cluster operations (e.g. <c>indices copy</c>, <c>indices sync-remote</c>).
	/// Seeds from the same <c>Parameters:ElasticsearchUrl</c> / <c>Parameters:ElasticsearchApiKey</c> values as
	/// <see cref="Elasticsearch"/> — one configured secret covers both ends of a copy. Set
	/// <c>DESTINATION_ELASTIC_URL</c> / <c>DESTINATION_ELASTIC_APIKEY</c> (or pass <c>--to-url</c> / <c>--to-api-key</c>
	/// on the command) to point only the destination elsewhere while the source still comes from the seed.
	/// </summary>
	public required ElasticsearchEndpoint Destination { get; init; }

	public string ElasticsearchEnvironment { get; init; } = "dev";
	public string BuildType { get; init; } = "public";

	public static SourcingConfiguration CreateFromEnvironment(
		string? esUrl = null,
		string? esApiKey = null
	)
	{
		var config = new ConfigurationBuilder()
			.AddUserSecrets("docs-builder")
			.AddEnvironmentVariables()
			.Build();

		var csConfig = ContentStackConfiguration.TryCreateFromConfiguration(config);

		var elasticUrl = esUrl
			?? config["Parameters:ElasticsearchUrl"]
			?? config["DOCUMENTATION_ELASTIC_URL"]
			?? config["ELASTICSEARCH_URL"]
			?? "http://localhost:9200";

		var apiKey = esApiKey
			?? config["Parameters:ElasticsearchApiKey"]
			?? config["DOCUMENTATION_ELASTIC_APIKEY"]
			?? config["ELASTICSEARCH_API_KEY"];

		var environment = config["ENVIRONMENT"] ?? "dev";

		var username = config["DOCUMENTATION_ELASTIC_USERNAME"] ?? "elastic";
		var password = config["DOCUMENTATION_ELASTIC_PASSWORD"];
		var buildType = config["DOCS_BUILD_TYPE"] ?? "public";

		var endpoint = new ElasticsearchEndpoint
		{
			Uri = new Uri(elasticUrl),
			ApiKey = apiKey,
			Username = string.IsNullOrEmpty(apiKey) ? username : null,
			Password = string.IsNullOrEmpty(apiKey) ? password : null,
		};

		// Destination cluster for cross-cluster commands (e.g. indices copy). Seeds from the same
		// Parameters:ElasticsearchUrl / Parameters:ElasticsearchApiKey values as the source so a single
		// configured secret covers both ends — DESTINATION_ELASTIC_* only needs to be set to point the
		// destination somewhere else.
		var destUrl = config["DESTINATION_ELASTIC_URL"] ?? elasticUrl;
		var destApiKey = config["DESTINATION_ELASTIC_APIKEY"] ?? apiKey;
		var destUsername = config["DESTINATION_ELASTIC_USERNAME"] ?? username;
		var destPassword = config["DESTINATION_ELASTIC_PASSWORD"] ?? password;
		var destEndpoint = new ElasticsearchEndpoint
		{
			Uri = new Uri(destUrl),
			ApiKey = destApiKey,
			Username = string.IsNullOrEmpty(destApiKey) ? destUsername : null,
			Password = string.IsNullOrEmpty(destApiKey) ? destPassword : null,
		};

		return new SourcingConfiguration
		{
			ContentStack = csConfig,
			Elasticsearch = endpoint,
			Destination = destEndpoint,
			ElasticsearchEnvironment = environment,
			BuildType = buildType
		};
	}
}
