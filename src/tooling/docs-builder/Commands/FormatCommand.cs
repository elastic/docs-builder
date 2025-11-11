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
	/// Format documentation files by fixing common issues like irregular space
	/// </summary>
	/// <param name="path"> -p, Path to the documentation folder, defaults to pwd</param>
	/// <param name="check">Check if files need formatting without modifying them (exits with code 1 if formatting needed)</param>
	/// <param name="write">Write formatting changes to files</param>
	/// <param name="ctx"></param>
	[Command("")]
	public async Task<int> Format(
		string? path = null,
		bool check = false,
		bool write = false,
		Cancel ctx = default
	)
	{
		// Validate that exactly one of --check or --write is specified
		if (check == write)
		{
			collector.EmitError(string.Empty, "Must specify exactly one of --check or --write");
			return 1;
		}

		await using var serviceInvoker = new ServiceInvoker(collector);

		var service = new FormatService(logFactory, configurationContext);
		var fs = new FileSystem();

		serviceInvoker.AddCommand(service, (path, check, fs),
			async static (s, collector, state, ctx) => await s.Format(collector, state.path, state.check, state.fs, ctx)
		);
		return await serviceInvoker.InvokeAsync(ctx);
	}
}
