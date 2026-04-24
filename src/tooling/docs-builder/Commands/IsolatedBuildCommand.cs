// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Actions.Core.Services;
using Documentation.Builder.Arguments;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Isolated;
using Elastic.Documentation.Services;
using Microsoft.Extensions.Logging;
using Nullean.Argh;

namespace Documentation.Builder.Commands;

internal sealed class IsolatedBuildCommand(
	ILoggerFactory logFactory,
	IDiagnosticsCollector collector,
	ICoreService githubActionsService,
	IConfigurationContext configurationContext,
	IEnvironmentVariables environmentVariables
)
{
	/// <summary>
	/// Builds a source documentation set folder.
	/// </summary>
	/// <remarks>
	/// <code>
	/// docs-builder
	/// docs-builder -p ./my-docs -o .artifacts/html --strict
	/// docs-builder --exporters html,es --canonical-base-url https://elastic.co/docs
	/// </code>
	/// </remarks>
	/// <param name="path">-p, Defaults to the <c>cwd/docs</c> folder</param>
	/// <param name="output">-o, Defaults to <c>.artifacts/html</c></param>
	/// <param name="pathPrefix">Specifies the path prefix for URLs</param>
	/// <param name="force">Force a full rebuild of the destination folder</param>
	/// <param name="strict">Treat warnings as errors and fail the build on warnings</param>
	/// <param name="allowIndexing">Allow indexing and following of HTML files</param>
	/// <param name="metadataOnly">Only emit documentation metadata to output (ignored when <c>--exporters</c> is also set)</param>
	/// <param name="exporters">
	/// Comma-separated exporter list. Values: html, es, config, links, state, llm, redirect, metadata, none, default.
	/// Default: (html, config, links, state, redirect).
	/// </param>
	/// <param name="canonicalBaseUrl">The base URL for the canonical URL tag</param>
	/// <param name="inMemory">Run the build in memory without writing to disk</param>
	/// <param name="skipApi">Skip OpenAPI documentation generation for faster builds</param>
	[DefaultCommand]
	public async Task<int> Build(
		GlobalCliOptions _,
		string? path = null,
		string? output = null,
		string? pathPrefix = null,
		bool? force = null,
		bool? strict = null,
		bool? allowIndexing = null,
		bool? metadataOnly = null,
		[ArgumentParser(typeof(ExporterParser))] IReadOnlySet<Exporter>? exporters = null,
		string? canonicalBaseUrl = null,
		bool inMemory = false,
		bool skipApi = false,
		CancellationToken ct = default
	)
	{
		await using var serviceInvoker = new ServiceInvoker(collector);

		var service = new IsolatedBuildService(logFactory, configurationContext, githubActionsService, environmentVariables);
		var readFs = inMemory ? FileSystemFactory.InMemory() : FileSystemFactory.RealGitRootForPath(path);
		var writeFs = inMemory ? null : FileSystemFactory.RealGitRootForPathWrite(path, output);
		var options = new IsolatedBuildOptions
		{
			Path = path, Output = output, PathPrefix = pathPrefix,
			Force = force, Strict = strict, AllowIndexing = allowIndexing,
			MetadataOnly = metadataOnly, Exporters = exporters,
			CanonicalBaseUrl = canonicalBaseUrl, SkipOpenApi = skipApi
		};
		var strictCommand = service.IsStrict(strict);

		serviceInvoker.AddCommand(service, (options, readFs, writeFs), strictCommand,
			static async (s, col, state, ctx) => await s.Build(col, state.readFs, state.options, state.writeFs, ctx)
		);
		return await serviceInvoker.InvokeAsync(ct);
	}
}
