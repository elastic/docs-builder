// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using ConsoleAppFramework;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Refactor.Tracking;
using Elastic.Documentation.Services;
using Microsoft.Extensions.Logging;

namespace Documentation.Builder.Commands;

internal sealed class ListDependentsCommand(
	ILoggerFactory logFactory,
	IDiagnosticsCollector collector,
	IConfigurationContext configurationContext
)
{
	/// <summary>
	/// Lists the markdown pages that transitively include the given files (snippets, CSVs, or
	/// other inputs to {{include}} / {{csv-include}} directives). Used by the docs preview
	/// workflow so a PR that only edits non-page files still gets a preview URL pointing at
	/// the pages that would re-render.
	/// </summary>
	/// <param name="files">Comma-separated list of file paths (git-relative or absolute) to resolve dependents for.</param>
	/// <param name="path"> -p, Defaults to the `{pwd}/docs` folder</param>
	/// <param name="format">Output format: 'json' (default) or 'text'.</param>
	/// <param name="ctx"></param>
	[Command("")]
	public async Task<int> ListDependents(
		string files,
		string? path = null,
		string format = "json",
		Cancel ctx = default
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
		return await serviceInvoker.InvokeAsync(ctx);
	}
}
