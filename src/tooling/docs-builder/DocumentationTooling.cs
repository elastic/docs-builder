// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;
using System.Net.Sockets;
using Actions.Core.Extensions;
using Actions.Core.Services;
using Documentation.Builder.Diagnostics.Console;
using Elastic.Documentation;
using Elastic.Documentation.Assembler;
using Elastic.Documentation.Catalog;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Loading;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Search;
using Elastic.Documentation.ServiceDefaults;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ServiceDiscovery;

namespace Documentation.Builder;

public static class DocumentationTooling
{
	public static TBuilder AddDocumentationToolingDefaults<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
	{
		_ = builder
			.Services
			.AddGitHubActionsCore()
			.AddSingleton<IEnvironmentVariables>(SystemEnvironmentVariables.Instance)
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
				var collector = new ConsoleDiagnosticsCollector(logFactory, githubActionsService);
				// Global configuration findings predate this collector; the first (only) one of the process reports them, for every command.
				_ = sp.GetRequiredService<StartupDiagnostics>().DrainInto(collector);
				return collector;
			})
			.AddSingleton(_ =>
			{
				var endpoints = ElasticsearchEndpointFactory.Create(builder.Configuration);
				return endpoints;
			})
			.AddSingleton<IConfigurationContext>(sp =>
			{
				var endpoints = sp.GetRequiredService<DocumentationEndpoints>();
				var configurationFileProvider = sp.GetRequiredService<ConfigurationFileProvider>();
				var catalog = sp.GetRequiredService<GlobalCatalog>();
				var search = sp.GetRequiredService<SearchTuning>();
				return new ConfigurationContext
				{
					ConfigurationFileProvider = configurationFileProvider,
					Catalog = catalog,
					Endpoints = endpoints,
					SearchTuning = search
				};
			})
			// One AssembleContext per run: every assembler service takes it from here, so the navigation is parsed (and diagnosed) once.
			.AddSingleton<AssembleContextProvider>();

		return builder;
	}
}
