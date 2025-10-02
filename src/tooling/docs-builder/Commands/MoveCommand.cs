// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using ConsoleAppFramework;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Refactor;
using Elastic.Documentation.Services;
using Microsoft.Extensions.Logging;

namespace Documentation.Builder.Commands;

internal sealed class MoveCommand(
	ILoggerFactory logFactory,
	IDiagnosticsCollector collector,
	IConfigurationContext configurationContext
)
{
	/// <summary>
	/// Move a file from one location to another and update all links in the documentation
	/// </summary>
	/// <param name="source">The source file or folder path to move from</param>
	/// <param name="target">The target file or folder path to move to</param>
	/// <param name="path"> -p, Defaults to the`{pwd}` folder</param>
	/// <param name="dryRun">Dry run the move operation</param>
	/// <param name="ctx"></param>
	[Command("")]
	public async Task<int> Move(
		[Argument] string source,
		[Argument] string target,
		bool? dryRun = null,
		string? path = null,
		Cancel ctx = default
	)
	{
		await using var serviceInvoker = new ServiceInvoker(collector);

		var service = new MoveFileService(logFactory, configurationContext);
		var fs = new FileSystem();

		serviceInvoker.AddCommand(service, (source, target, dryRun, path, fs),
			async static (s, collector, state, ctx) => await s.Move(collector, state.source, state.target, state.dryRun, state.path, state.fs, ctx)
		);
		return await serviceInvoker.InvokeAsync(ctx);
	}
}
