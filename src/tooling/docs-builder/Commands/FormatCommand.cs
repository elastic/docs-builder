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

internal sealed class FormatCommand(
	ILoggerFactory logFactory,
	IDiagnosticsCollector collector,
	IConfigurationContext configurationContext
)
{
	/// <summary>
	/// Format documentation files by fixing common issues like irregular whitespace
	/// </summary>
	/// <param name="path"> -p, Path to the documentation folder, defaults to pwd</param>
	/// <param name="dryRun">Preview changes without modifying files</param>
	/// <param name="ctx"></param>
	[Command("")]
	public async Task<int> Format(
		string? path = null,
		bool? dryRun = null,
		Cancel ctx = default
	)
	{
		await using var serviceInvoker = new ServiceInvoker(collector);

		var service = new FormatService(logFactory, configurationContext);
		var fs = new FileSystem();

		serviceInvoker.AddCommand(service, (path, dryRun, fs),
			async static (s, collector, state, ctx) => await s.Format(collector, state.path, state.dryRun, state.fs, ctx)
		);
		return await serviceInvoker.InvokeAsync(ctx);
	}
}
