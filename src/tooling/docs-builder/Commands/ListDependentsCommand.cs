// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Refactor.Tracking;
using Elastic.Documentation.Services;
using Microsoft.Extensions.Logging;
using Nullean.Argh;

namespace Documentation.Builder.Commands;

internal sealed class ListDependentsCommand(
	ILoggerFactory logFactory,
	IDiagnosticsCollector collector,
	IConfigurationContext configurationContext
)
{
	/// <summary>Lists the markdown pages that transitively include the given files.</summary>
	/// <remarks>
	/// <para>
	/// Resolves snippets, CSVs, or other inputs to <c>{{include}}</c> / <c>{{csv-include}}</c>
	/// directives to the non-snippet pages that pull them in, following transitive chains.
	/// Intended for the docs preview workflow: when a PR only edits non-page files, feed the
	/// changed files through this command to get the page URLs to link in the preview comment.
	/// </para>
	/// </remarks>
	/// <param name="files">Comma-separated list of file paths (git-relative or absolute) to resolve dependents for.</param>
	/// <param name="path">-p, Documentation source directory. Defaults to the <c>cwd/docs</c> folder.</param>
	/// <param name="format">Output format: <c>json</c> (default) or <c>text</c>.</param>
	[CommandName("list-dependents")]
	public async Task<int> ListDependents(
		GlobalCliOptions _,
		string files,
		string? path = null,
		string format = "json",
		CancellationToken ct = default
	)
	{
		await using var serviceInvoker = new ServiceInvoker(collector);

		var fileList = files.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		var service = new ListDependentsService(logFactory, configurationContext);
		var fs = FileSystemFactory.RealGitRootForPath(path);

		serviceInvoker.AddCommand(service, (path, fs, fileList, format),
			async static (s, collector, state, ctx) => await s.ListDependents(
				collector, state.fs, state.path, state.fileList, state.format, ctx)
		);
		return await serviceInvoker.InvokeAsync(ct);
	}
}
