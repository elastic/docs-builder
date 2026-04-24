// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Actions.Core.Services;

using Elastic.Documentation;
using Elastic.Documentation.Assembler.Building;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Services;
using Microsoft.Extensions.Logging;
using Nullean.Argh;

namespace Documentation.Builder.Commands.Assembler;

internal sealed class AssemblerSitemapCommand(
	ILoggerFactory logFactory,
	IDiagnosticsCollector collector,
	AssemblyConfiguration configuration,
	IConfigurationContext configurationContext,
	ICoreService githubActionsService
)
{
	/// <summary>
	/// Generate <c>sitemap.xml</c> from the Elasticsearch index using <c>content_last_updated</c> dates.
	/// </summary>
	/// <remarks>
	/// <code>
	/// docs-builder assembler sitemap --endpoint https://es:9200 --api-key KEY --environment staging
	/// </code>
	/// </remarks>
	/// <param name="environment">The environment name used to resolve the Elasticsearch index</param>
	[CommandName("sitemap")]
	public async Task<int> Sitemap(
		GlobalCliOptions _,
		[AsParameters] ElasticsearchIndexOptions es,
		string? environment = null,
		CancellationToken ct = default
	)
	{
		await using var serviceInvoker = new ServiceInvoker(collector);
		var fs = FileSystemFactory.RealWrite;
		var service = new AssemblerSitemapService(logFactory, configuration, configurationContext, githubActionsService);
		var state = (fs, environment, es);
		serviceInvoker.AddCommand(service, state,
			static async (s, col, state, ctx) => await s.GenerateSitemapAsync(col, state.fs,
				state.es.Endpoint?.ToString(), state.environment, state.es.ApiKey, state.es.Username, state.es.Password,
				state.es.DebugMode, state.es.ProxyAddress?.ToString(), state.es.ProxyPassword, state.es.ProxyUsername,
				state.es.DisableSslVerification, state.es.CertificateFingerprint, state.es.CertificatePath, state.es.CertificateNotRoot,
				ctx)
		);
		return await serviceInvoker.InvokeAsync(ct);
	}
}
