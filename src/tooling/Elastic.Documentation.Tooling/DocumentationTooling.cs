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
using Elastic.Documentation.Configuration.Versions;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.ServiceDefaults;
using Elastic.Documentation.Tooling.Diagnostics.Console;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ServiceDiscovery;

namespace Elastic.Documentation.Tooling;

public static class DocumentationTooling
{
	public static TBuilder AddDocumentationToolingDefaults<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
	{
		_ = builder.Services
			.AddGitHubActionsCore()
			.AddSingleton<DiagnosticsChannel>()
			.AddServiceDiscovery()
			.ConfigureHttpClientDefaults(static client =>
			{
				_ = client.AddServiceDiscovery();
			})
			.AddSingleton<IDiagnosticsCollector>(sp =>
			{
				var logFactory = sp.GetRequiredService<ILoggerFactory>();
				var githubActionsService = sp.GetRequiredService<ICoreService>();
				var globalArgs = sp.GetRequiredService<GlobalCliArgs>();
				if (globalArgs.IsHelpOrVersion)
					return new DiagnosticsCollector([]);
				return new ConsoleDiagnosticsCollector(logFactory, githubActionsService);
			})
			.AddSingleton(sp =>
			{
				var resolver = sp.GetRequiredService<ServiceEndpointResolver>();
				var elasticsearchUri = ResolveServiceEndpoint(resolver,
					() => TryEnvVars("http://localhost:9200", "DOCUMENTATION_ELASTIC_URL", "CONNECTIONSTRINGS__ELASTICSEARCH")
				);
				var elasticsearchPassword =
					elasticsearchUri.UserInfo is { } userInfo && userInfo.Contains(':')
						? userInfo.Split(':')[1]
						: TryEnvVarsOptional("DOCUMENTATION_ELASTIC_PASSWORD");

				var elasticsearchUser =
					elasticsearchUri.UserInfo is { } userInfo2 && userInfo2.Contains(':')
						? userInfo2.Split(':')[0]
						: TryEnvVars("elastic", "DOCUMENTATION_ELASTIC_USERNAME");

				var elasticsearchApiKey = TryEnvVarsOptional("DOCUMENTATION_ELASTIC_APIKEY");
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
			})
			.AddSingleton<IConfigurationContext>(sp =>
			{
				var endpoints = sp.GetRequiredService<DocumentationEndpoints>();
				var configurationFileProvider = sp.GetRequiredService<ConfigurationFileProvider>();
				var versionsConfiguration = sp.GetRequiredService<VersionsConfiguration>();
				var products = sp.GetRequiredService<ProductsConfiguration>();
				var legacyUrlMappings = sp.GetRequiredService<LegacyUrlMappingConfiguration>();
				return new ConfigurationContext
				{
					ConfigurationFileProvider = configurationFileProvider,
					VersionsConfiguration = versionsConfiguration,
					Endpoints = endpoints,
					ProductsConfiguration = products,
					LegacyUrlMappings = legacyUrlMappings
				};
			});

		return builder;
	}

	private static string TryEnvVars(string fallback, params string[] keys)
	{
		foreach (var key in keys)
		{
			if (Environment.GetEnvironmentVariable(key) is { } value)
				return value;
		}
		return fallback;
	}
	private static string? TryEnvVarsOptional(params string[] keys)
	{
		foreach (var key in keys)
		{
			if (Environment.GetEnvironmentVariable(key) is { } value)
				return value;
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
