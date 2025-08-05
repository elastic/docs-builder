// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;
using System.Net.Sockets;
using Actions.Core.Extensions;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.ServiceDiscovery;

namespace Elastic.Documentation.Tooling;

public static class DocumentationTooling
{
	public static TBuilder AddDocumentationToolingDefaults<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
	{
		_ = builder.Services
			.AddGitHubActionsCore()
			.AddSingleton<DiagnosticsChannel>()
			.AddSingleton<DiagnosticsCollector>()
			.AddServiceDiscovery()
			.ConfigureHttpClientDefaults(static client =>
			{
				_ = client.AddServiceDiscovery();
			})
			.AddSingleton(sp =>
			{
				var resolver = sp.GetRequiredService<ServiceEndpointResolver>();
				var uri = ResolveServiceEndpoint(resolver, "http://localhost:9200");
				return new DocumentationEndpoints
				{
					Elasticsearch = uri
				};
			});


		return builder;
	}

	[SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly")]
	private static Uri ResolveServiceEndpoint(ServiceEndpointResolver resolver, string fallback)
	{
		var get = resolver.GetEndpointsAsync("https+http://elasticsearch", Cancel.None);
		var endpoint = get.IsCompletedSuccessfully ? get.Result : get.GetAwaiter().GetResult();
		if (endpoint.Endpoints.Count == 0)
			return new Uri(fallback);
		if (endpoint.Endpoints[0].EndPoint.AddressFamily is AddressFamily.Unknown or AddressFamily.Unspecified)
			return new Uri(fallback);
		var uri = new Uri(endpoint.Endpoints[0].ToString() ?? throw new InvalidOperationException("No 'elasticsearch' endpoints found"));
		return uri;
	}
}
