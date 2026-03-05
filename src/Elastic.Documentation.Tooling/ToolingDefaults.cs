// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;
using System.Net.Sockets;
using Actions.Core.Extensions;
using Actions.Core.Services;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.LegacyUrlMappings;
using Elastic.Documentation.Configuration.Products;
using Elastic.Documentation.Configuration.Search;
using Elastic.Documentation.Configuration.Versions;
using Elastic.Documentation.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.UserSecrets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ServiceDiscovery;

namespace Elastic.Documentation.Tooling;

public static class ToolingDefaults
{
	private const string UserSecretsId = "72f50f33-6fb9-4d08-bff3-39568fe370b3";

	/// <summary>
	/// Adds common tooling defaults for CLI applications.
	/// </summary>
	/// <param name="builder">The host application builder</param>
	/// <param name="diagnosticsCollectorFactory">Factory to create the diagnostics collector</param>
	public static TBuilder AddToolingDefaults<TBuilder>(
		this TBuilder builder,
		Func<IServiceProvider, IDiagnosticsCollector> diagnosticsCollectorFactory
	) where TBuilder : IHostApplicationBuilder
	{
		// Build configuration with user secrets support (Aspire user secrets ID)
		var configBuilder = new ConfigurationBuilder();
		_ = configBuilder.AddUserSecrets("72f50f33-6fb9-4d08-bff3-39568fe370b3");
		_ = configBuilder.AddEnvironmentVariables();
		var secretsConfig = configBuilder.Build();

		_ = builder.Services
			.AddGitHubActionsCore()
			.AddSingleton<DiagnosticsChannel>()
			.AddServiceDiscovery()
			.ConfigureHttpClientDefaults(static client =>
			{
				_ = client.AddServiceDiscovery();
			})
			.AddSingleton(diagnosticsCollectorFactory);

		builder.Services.TryAddSingleton(sp =>
		{
			var resolver = sp.GetRequiredService<ServiceEndpointResolver>();
			return CreateDocumentationEndpoints(resolver, secretsConfig);
		});

		_ = builder.Services
			.AddSingleton<IConfigurationContext>(sp =>
			{
				var endpoints = sp.GetRequiredService<DocumentationEndpoints>();
				var configurationFileProvider = sp.GetRequiredService<ConfigurationFileProvider>();
				var versionsConfiguration = sp.GetRequiredService<VersionsConfiguration>();
				var products = sp.GetRequiredService<ProductsConfiguration>();
				var legacyUrlMappings = sp.GetRequiredService<LegacyUrlMappingConfiguration>();
				var search = sp.GetRequiredService<SearchConfiguration>();
				return new ConfigurationContext
				{
					ConfigurationFileProvider = configurationFileProvider,
					VersionsConfiguration = versionsConfiguration,
					Endpoints = endpoints,
					ProductsConfiguration = products,
					LegacyUrlMappings = legacyUrlMappings,
					SearchConfiguration = search
				};
			});

		return builder;
	}

	private static DocumentationEndpoints CreateDocumentationEndpoints(ServiceEndpointResolver resolver, IConfiguration secretsConfig)
	{
		var elasticsearchUri = ResolveServiceEndpoint(
			resolver,
			() => TryConfigOrEnvVars(
				secretsConfig,
				"http://localhost:9200",
				"Parameters:DocumentationElasticUrl",
				"DOCUMENTATION_ELASTIC_URL",
				"CONNECTIONSTRINGS__ELASTICSEARCH"
			)
		);

		var elasticsearchPassword =
			elasticsearchUri.UserInfo is { } userInfo && userInfo.Contains(':')
				? userInfo.Split(':')[1]
				: TryConfigOrEnvVarsOptional(secretsConfig, "Parameters:DocumentationElasticPassword", "DOCUMENTATION_ELASTIC_PASSWORD");

		var elasticsearchUser =
			elasticsearchUri.UserInfo is { } userInfo2 && userInfo2.Contains(':')
				? userInfo2.Split(':')[0]
				: TryConfigOrEnvVars(secretsConfig, "elastic", "Parameters:DocumentationElasticUsername", "DOCUMENTATION_ELASTIC_USERNAME");

		var elasticsearchApiKey = TryConfigOrEnvVarsOptional(
			secretsConfig,
			"Parameters:DocumentationElasticApiKey",
			"DOCUMENTATION_ELASTIC_APIKEY"
		);

		return new DocumentationEndpoints
		{
			Elasticsearch = new ElasticsearchEndpoint
			{
				Uri = elasticsearchUri,
				Password = elasticsearchPassword,
				ApiKey = elasticsearchApiKey,
				Username = elasticsearchUser
			},
		};
	}

	private static string TryConfigOrEnvVars(IConfiguration config, string fallback, params string[] keys)
	{
		foreach (var key in keys)
		{
			// Try configuration first (user secrets)
			var configValue = config[key];
			if (!string.IsNullOrEmpty(configValue))
				return configValue;

			// Try environment variable (for keys that look like env vars)
			if (key.Contains('_') || string.Equals(key, key.ToUpperInvariant(), StringComparison.Ordinal))
			{
				var envValue = Environment.GetEnvironmentVariable(key);
				if (!string.IsNullOrEmpty(envValue))
					return envValue;
			}
		}
		return fallback;
	}

	private static string? TryConfigOrEnvVarsOptional(IConfiguration config, params string[] keys)
	{
		foreach (var key in keys)
		{
			// Try configuration first (user secrets)
			var configValue = config[key];
			if (!string.IsNullOrEmpty(configValue))
				return configValue;

			// Try environment variable (for keys that look like env vars)
			if (key.Contains('_') || string.Equals(key, key.ToUpperInvariant(), StringComparison.Ordinal))
			{
				var envValue = Environment.GetEnvironmentVariable(key);
				if (!string.IsNullOrEmpty(envValue))
					return envValue;
			}
		}
		return null;
	}

	[SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly")]
	private static Uri ResolveServiceEndpoint(ServiceEndpointResolver resolver, Func<string> fallback)
	{
		var get = resolver.GetEndpointsAsync("https+http://elasticsearch", Cancel.None);
		var endpoint = get.IsCompletedSuccessfully ? get.Result : get.GetAwaiter().GetResult();
		if (endpoint.Endpoints.Count == 0)
			return new Uri(fallback());
		if (endpoint.Endpoints[0].EndPoint.AddressFamily is AddressFamily.Unknown or AddressFamily.Unspecified)
			return new Uri(fallback());
		var uri = new Uri(endpoint.Endpoints[0].ToString() ?? throw new InvalidOperationException("No 'elasticsearch' endpoints found"));
		return uri;
	}
}
