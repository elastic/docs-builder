// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Actions.Core.Services;

using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Isolated;
using Elastic.Documentation.Services;
using Microsoft.Extensions.Logging;
using Nullean.Argh;

namespace Documentation.Builder.Commands;

internal sealed class IndexCommand(
	ILoggerFactory logFactory,
	IDiagnosticsCollector collector,
	IConfigurationContext configurationContext,
	ICoreService githubActionsService,
	IEnvironmentVariables environmentVariables
)
{
	/// <summary>
	/// Index a single documentation set to Elasticsearch.
	/// </summary>
	/// <remarks>
	/// <para>Calls <c>docs-builder --exporters elasticsearch</c> with full control over all Elasticsearch options.</para>
	/// <code>
	/// docs-builder index --endpoint https://localhost:9200 --api-key YOUR_KEY
	/// docs-builder index --endpoint https://es:9200 --username elastic --password secret
	/// </code>
	/// </remarks>
	[CommandName("index")]
	public async Task<int> Index(
		GlobalCliOptions _,
		[AsParameters] ElasticsearchIndexOptions es,
		string? path = null,
		CancellationToken ct = default
	)
	{
		await using var serviceInvoker = new ServiceInvoker(collector);
		var fs = FileSystemFactory.RealGitRootForPath(path);
		var service = new IsolatedIndexService(logFactory, configurationContext, githubActionsService, environmentVariables);
		var state = (fs, path, es);
		serviceInvoker.AddCommand(service, state,
			static async (s, collector, state, ctx) => await s.Index(collector, state.fs, state.path,
				state.es.Endpoint?.ToString(), state.es.ApiKey, state.es.Username, state.es.Password,
				state.es.NoAiEnrichment, state.es.SearchNumThreads, state.es.IndexNumThreads, state.es.NoEis, state.es.BootstrapTimeout,
				state.es.ForceReindex, state.es.BufferSize, state.es.MaxRetries, state.es.DebugMode,
				state.es.ProxyAddress?.ToString(), state.es.ProxyPassword, state.es.ProxyUsername,
				state.es.DisableSslVerification, state.es.CertificateFingerprint, state.es.CertificatePath, state.es.CertificateNotRoot,
				ctx)
		);
		return await serviceInvoker.InvokeAsync(ct);
	}
}
