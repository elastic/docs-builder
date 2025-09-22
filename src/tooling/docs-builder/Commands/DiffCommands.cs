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

internal sealed class DiffCommands(
	ILoggerFactory logFactory,
	IDiagnosticsCollector collector,
	IConfigurationContext configurationContext
)
{
	/// <summary>
	/// Validates redirect updates in the current branch using the redirect file against changes reported by git.
	/// </summary>
	/// <param name="path"> -p, Defaults to the`{pwd}/docs` folder</param>
	/// <param name="ctx"></param>
	[Command("validate")]
	public async Task<int> ValidateRedirects(string? path = null, Cancel ctx = default)
	{
		await using var serviceInvoker = new ServiceInvoker(collector);

		var service = new LocalChangeTrackingService(logFactory, configurationContext);
		var fs = new FileSystem();

		serviceInvoker.AddCommand(service, (path, fs),
				async static (s, collector, state, ctx) => await s.ValidateRedirects(collector, state.path, state.fs, ctx)
		);
		return await serviceInvoker.InvokeAsync(ctx);
	}

}
