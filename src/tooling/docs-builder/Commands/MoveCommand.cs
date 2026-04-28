// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Refactor;
using Elastic.Documentation.Services;
using Microsoft.Extensions.Logging;
using Nullean.Argh;

namespace Documentation.Builder.Commands;

internal sealed class RefactorCommands(
	ILoggerFactory logFactory,
	IDiagnosticsCollector collector,
	IConfigurationContext configurationContext
)
{
	/// <summary>Move a file or folder and rewrite all inbound links across the documentation set.</summary>
	/// <param name="source">Source file or folder path.</param>
	/// <param name="target">Destination file or folder path.</param>
	/// <param name="path">-p, Documentation root. Defaults to <c>cwd</c>.</param>
	/// <param name="dryRun">Print the changes that would be made without applying them.</param>
	[CommandName("mv")]
	public async Task<int> Move(
		GlobalCliOptions _,
		[Argument] string source,
		[Argument] string target,
		bool? dryRun = null,
		string? path = null,
		Cancel ct = default
	)
	{
		await using var serviceInvoker = new ServiceInvoker(collector);

		var service = new MoveFileService(logFactory, configurationContext);
		var fs = FileSystemFactory.RealGitRootForPath(path);

		serviceInvoker.AddCommand(service, (source, target, dryRun, path, fs),
			async static (s, collector, state, ctx) => await s.Move(collector, state.source, state.target, state.dryRun, state.path, state.fs, ctx)
		);
		return await serviceInvoker.InvokeAsync(ct);
	}

	/// <summary>Fix common formatting issues (irregular spacing, trailing whitespace) across documentation files.</summary>
	/// <remarks>Exactly one of <c>--check</c> or <c>--write</c> must be specified.</remarks>
	/// <param name="path">-p, Documentation root. Defaults to <c>cwd</c>.</param>
	/// <param name="check">Report files that need formatting without modifying them. Exits 1 when any file is out of format.</param>
	/// <param name="write">Apply formatting changes in place.</param>
	[CommandName("format")]
	public async Task<int> Format(
		GlobalCliOptions _,
		string? path = null,
		bool check = false,
		bool write = false,
		Cancel ct = default
	)
	{
		if (check == write)
		{
			collector.EmitError(string.Empty, "Must specify exactly one of --check or --write");
			return 1;
		}

		await using var serviceInvoker = new ServiceInvoker(collector);

		var service = new FormatService(logFactory, configurationContext);
		var fs = FileSystemFactory.RealGitRootForPath(path);

		serviceInvoker.AddCommand(service, (path, check, fs),
			async static (s, collector, state, ctx) => await s.Format(collector, state.path, state.check, state.fs, ctx)
		);
		return await serviceInvoker.InvokeAsync(ct);
	}
}
