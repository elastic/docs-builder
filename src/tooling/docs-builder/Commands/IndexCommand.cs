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
	/// <summary>Index a single documentation set into Elasticsearch.</summary>
	/// <remarks>
	/// <para>
	/// Builds the documentation set in metadata-only mode and streams the output to Elasticsearch.
	/// Does not write HTML to disk. Requires a running cluster and valid credentials.
	/// </para>
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
		serviceInvoker.AddCommand(service,
			async (s, col, ctx) => await s.Index(col, fs, es, path, ctx)
		);
		return await serviceInvoker.InvokeAsync(ct);
	}
}
