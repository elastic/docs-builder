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
	/// <summary>
	/// Move a file or folder and update all links in the documentation.
	/// </summary>
	/// <remarks>
	/// <code>
	/// docs-builder mv ./docs/old-page.md ./docs/new-page.md
	/// docs-builder mv ./docs/old-section ./docs/new-section --dry-run
	/// </code>
	/// </remarks>
	/// <param name="source">The source file or folder path to move from</param>
	/// <param name="target">The target file or folder path to move to</param>
	/// <param name="path">-p, Defaults to the <c>cwd</c> folder</param>
	/// <param name="dryRun">Preview the move operation without applying changes</param>
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

	/// <summary>
	/// Format documentation files by fixing common issues such as irregular spacing.
	/// </summary>
	/// <remarks>
	/// <para>Exactly one of <c>--check</c> or <c>--write</c> must be specified.</para>
	/// <code>
	/// docs-builder format --check
	/// docs-builder format --write -p ./my-docs
	/// </code>
	/// </remarks>
	/// <param name="path">-p, Path to the documentation folder. Defaults to <c>cwd</c></param>
	/// <param name="check">Check if files need formatting without modifying them (exits with code 1 if formatting is needed)</param>
	/// <param name="write">Write formatting changes to files</param>
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
