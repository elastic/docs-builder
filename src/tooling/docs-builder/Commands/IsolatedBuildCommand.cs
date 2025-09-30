// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Actions.Core.Services;
using ConsoleAppFramework;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Isolated;
using Elastic.Documentation.Services;
using Elastic.Documentation.Tooling.Arguments;
using Microsoft.Extensions.Logging;

namespace Documentation.Builder.Commands;

internal sealed class IsolatedBuildCommand(
	ILoggerFactory logFactory,
	IDiagnosticsCollector collector,
	ICoreService githubActionsService,
	IConfigurationContext configurationContext
)
{
	/// <summary>
	/// Builds a source documentation set folder.
	/// <para>global options:</para>
	/// --log-level level
	/// </summary>
	/// <param name="path"> -p, Defaults to the`{pwd}/docs` folder</param>
	/// <param name="output"> -o, Defaults to `.artifacts/html` </param>
	/// <param name="pathPrefix"> Specifies the path prefix for urls </param>
	/// <param name="force"> Force a full rebuild of the destination folder</param>
	/// <param name="strict"> Treat warnings as errors and fail the build on warnings</param>
	/// <param name="allowIndexing"> Allow indexing and following of HTML files</param>
	/// <param name="metadataOnly"> Only emit documentation metadata to output, ignored if 'exporters' is also set </param>
	/// <param name="exporters"> Set available exporters:
	///					html, es, config, links, state, llm, redirect, metadata, none.
	///					Defaults to (html, config, links, state, redirect) or 'default'.
	/// </param>
	/// <param name="canonicalBaseUrl"> The base URL for the canonical url tag</param>
	/// <param name="ctx"></param>
	[Command("")]
	public async Task<int> Build(
		string? path = null,
		string? output = null,
		string? pathPrefix = null,
		bool? force = null,
		bool? strict = null,
		bool? allowIndexing = null,
		bool? metadataOnly = null,
		[ExporterParser] IReadOnlySet<Exporter>? exporters = null,
		string? canonicalBaseUrl = null,
		Cancel ctx = default
	)
	{
		await using var serviceInvoker = new ServiceInvoker(collector);

		var service = new IsolatedBuildService(logFactory, configurationContext, githubActionsService);
		var fs = new FileSystem();
		var strictCommand = service.IsStrict(strict);

		serviceInvoker.AddCommand(service,
			(path, output, pathPrefix, force, strict, allowIndexing, metadataOnly, exporters, canonicalBaseUrl, fs), strictCommand,
			async static (s, collector, state, ctx) => await s.Build(
				collector, state.fs, state.path, state.output, state.pathPrefix,
				state.force, state.strict, state.allowIndexing, state.metadataOnly,
				state.exporters, state.canonicalBaseUrl, ctx
			)
		);
		return await serviceInvoker.InvokeAsync(ctx);
	}

}
