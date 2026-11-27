// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.ServiceDefaults;
using Microsoft.Extensions.Configuration;

namespace Elastic.Documentation.Api.Tests;

// The local es:* tasks blank the credentials so a saved cloud key never reaches the local cluster over HTTP.
public class ElasticsearchEndpointFactoryTests
{
	private static IConfiguration SavedCloudCredentialsWithEnvironment(Dictionary<string, string?> environment) =>
		new ConfigurationBuilder()
			.AddInMemoryCollection(new Dictionary<string, string?>
			{
				["DOCUMENTATION_ELASTIC_URL"] = "https://cloud.example:9243",
				["DOCUMENTATION_ELASTIC_APIKEY"] = "saved-cloud-key",
				["DOCUMENTATION_ELASTIC_PASSWORD"] = "saved-cloud-password"
			})
			.AddInMemoryCollection(environment)
			.Build();

	[Test]
	public void FromConfiguration_EmptyCredentialOverrides_DropSavedCloudCredentials()
	{
		var config = SavedCloudCredentialsWithEnvironment(new()
		{
			["DOCUMENTATION_ELASTIC_URL"] = "http://localhost:9200",
			["DOCUMENTATION_ELASTIC_APIKEY"] = "",
			["DOCUMENTATION_ELASTIC_PASSWORD"] = ""
		});

		var endpoint = ElasticsearchEndpointFactory.FromConfiguration(config).Elasticsearch;

		endpoint.Uri.Should().Be(new Uri("http://localhost:9200"));
		endpoint.ApiKey.Should().BeNull();
		endpoint.Password.Should().BeNull();
	}

	[Test]
	public void FromConfiguration_NoOverrides_KeepsSavedCloudCredentials()
	{
		var endpoint = ElasticsearchEndpointFactory.FromConfiguration(SavedCloudCredentialsWithEnvironment([])).Elasticsearch;

		endpoint.ApiKey.Should().Be("saved-cloud-key");
		endpoint.Password.Should().Be("saved-cloud-password");
	}

	[Test]
	public void FromConfiguration_NoUrl_StillAppliesBuildTypeEnvironmentAndOverride()
	{
		var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
		{
			["DOCUMENTATION_ELASTIC_APIKEY"] = "saved-cloud-key",
			["ENVIRONMENT"] = "edge",
			["DOCUMENTATION_ELASTIC_INDEX_OVERRIDE"] = "local-override"
		}).Build();

		var endpoints = ElasticsearchEndpointFactory.FromConfiguration(config, buildType: "assembler");

		endpoints.Elasticsearch.Uri.Should().Be(new Uri("http://localhost:9200"));
		endpoints.Elasticsearch.ApiKey.Should().BeNull();
		endpoints.BuildType.Should().Be("assembler");
		endpoints.Environment.Should().Be("edge");
		endpoints.SearchIndexOverride.Should().Be("local-override");
	}
}
