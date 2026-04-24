// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Actions.Core.Services;
using Elastic.Documentation;
using Elastic.Documentation.Assembler.Indexing;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Services;
using Microsoft.Extensions.Logging;
using Nullean.Argh;

namespace Documentation.Builder.Commands.Assembler;

internal sealed class AssemblerIndexCommand(
	ILoggerFactory logFactory,
	IDiagnosticsCollector collector,
	AssemblyConfiguration configuration,
	IConfigurationContext configurationContext,
	ICoreService githubActionsService,
	IEnvironmentVariables environmentVariables
)
{
	/// <summary>
	/// Index assembled documentation to Elasticsearch.
	/// </summary>
	/// <remarks>
	/// <para>Calls <c>docs-builder assembler build --exporters elasticsearch</c> with full Elasticsearch option control.</para>
	/// <code>
	/// docs-builder assembler index --endpoint https://es:9200 --api-key KEY --environment staging
	/// </code>
	/// </remarks>
	/// <param name="environment">The environment name; becomes part of the index name</param>
	[CommandName("index")]
	public async Task<int> Index(
		GlobalCliOptions _,
		[AsParameters] ElasticsearchIndexOptions es,
		string? environment = null,
		CancellationToken ct = default
	)
	{
		await using var serviceInvoker = new ServiceInvoker(collector);
		var readFs = FileSystemFactory.RealRead;
		var writeFs = FileSystemFactory.RealWrite;
		var service = new AssemblerIndexService(logFactory, configuration, configurationContext, githubActionsService, environmentVariables);
		serviceInvoker.AddCommand(service,
			async (s, col, ctx) => await s.Index(col, readFs, writeFs, es, environment, ctx)
		);
		return await serviceInvoker.InvokeAsync(ct);
	}
}
