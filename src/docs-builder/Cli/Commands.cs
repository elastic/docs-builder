// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
using System.IO.Abstractions;
using Actions.Core.Services;
using ConsoleAppFramework;
using Documentation.Builder.Http;
using Elastic.Documentation.Tooling.Diagnostics.Console;
using Elastic.Documentation.Tooling.Filters;
using Elastic.Markdown;
using Elastic.Markdown.IO;
using Elastic.Markdown.IO.Navigation;
using Elastic.Markdown.Refactor;
using Microsoft.Extensions.Logging;

namespace Documentation.Builder.Cli;

internal sealed class Commands(ILoggerFactory logger, ICoreService githubActionsService)
{
	private void AssignOutputLogger()
	{
		var log = logger.CreateLogger<Program>();
#pragma warning disable CA2254
		ConsoleApp.Log = msg => log.LogInformation(msg);
		ConsoleApp.LogError = msg => log.LogError(msg);
#pragma warning restore CA2254
	}

	/// <summary>
	///	Continuously serve a documentation folder at http://localhost:3000.
	/// File systems changes will be reflected without having to restart the server.
	/// </summary>
	/// <param name="path">-p, Path to serve the documentation.
	/// Defaults to the`{pwd}/docs` folder
	/// </param>
	/// <param name="port">Port to serve the documentation.</param>
	/// <param name="ctx"></param>
	[Command("serve")]
	[ConsoleAppFilter<CheckForUpdatesFilter>]
	public async Task Serve(string? path = null, int port = 3000, Cancel ctx = default)
	{
		AssignOutputLogger();
		var host = new DocumentationWebHost(path, port, logger, new FileSystem());
		await host.RunAsync(ctx);
		await host.StopAsync(ctx);
	}

	/// <summary>
	/// Converts a source markdown folder or file to an output folder
	/// <para>global options:</para>
	/// --log-level level
	/// </summary>
	/// <param name="path"> -p, Defaults to the`{pwd}/docs` folder</param>
	/// <param name="output"> -o, Defaults to `.artifacts/html` </param>
	/// <param name="pathPrefix"> Specifies the path prefix for urls </param>
	/// <param name="force"> Force a full rebuild of the destination folder</param>
	/// <param name="strict"> Treat warnings as errors and fail the build on warnings</param>
	/// <param name="allowIndexing"> Allow indexing and following of html files</param>
	/// <param name="ctx"></param>
	[Command("generate")]
	[ConsoleAppFilter<StopwatchFilter>]
	[ConsoleAppFilter<CatchExceptionFilter>]
	[ConsoleAppFilter<CheckForUpdatesFilter>]
	public async Task<int> Generate(
		string? path = null,
		string? output = null,
		string? pathPrefix = null,
		bool? force = null,
		bool? strict = null,
		bool? allowIndexing = null,
		Cancel ctx = default
	)
	{
		AssignOutputLogger();
		pathPrefix ??= githubActionsService.GetInput("prefix");
		var fileSystem = new FileSystem();
		var collector = new ConsoleDiagnosticsCollector(logger, githubActionsService);

		var runningOnCi = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));
		BuildContext context;
		try
		{
			context = new BuildContext(collector, fileSystem, fileSystem, path, output)
			{
				UrlPathPrefix = pathPrefix,
				Force = force ?? false,
				AllowIndexing = allowIndexing != null
			};
		}
		// On CI, we are running on merge commit which may have changes against an older
		// docs folder (this can happen on out of date PR's).
		// At some point in the future we can remove this try catch
		catch (Exception e) when (runningOnCi && e.Message.StartsWith("Can not locate docset.yml file in"))
		{
			var outputDirectory = !string.IsNullOrWhiteSpace(output)
				? fileSystem.DirectoryInfo.New(output)
				: fileSystem.DirectoryInfo.New(Path.Combine(Paths.Root.FullName, ".artifacts/docs/html"));
			// we temporarily do not error when pointed to a non documentation folder.
			_ = fileSystem.Directory.CreateDirectory(outputDirectory.FullName);

			ConsoleApp.Log($"Skipping build as we are running on a merge commit and the docs folder is out of date and has no docset.yml. {e.Message}");

			await githubActionsService.SetOutputAsync("skip", "true");
			return 0;
		}
		if (runningOnCi)
			await githubActionsService.SetOutputAsync("skip", "false");
		var set = new DocumentationSet(context, logger);
		var generator = new DocumentationGenerator(set, logger);
		await generator.GenerateAll(ctx);
		if (runningOnCi)
			await githubActionsService.SetOutputAsync("landing-page-path", set.MarkdownFiles.First().Value.Url);
		if (bool.TryParse(githubActionsService.GetInput("strict"), out var strictValue) && strictValue)
			strict ??= strictValue;

		if (strict ?? false)
			return context.Collector.Errors + context.Collector.Warnings;
		return context.Collector.Errors;
	}

	/// <summary>
	/// Converts a source markdown folder or file to an output folder
	/// </summary>
	/// <param name="path"> -p, Defaults to the`{pwd}/docs` folder</param>
	/// <param name="output"> -o, Defaults to `.artifacts/html` </param>
	/// <param name="pathPrefix"> Specifies the path prefix for urls </param>
	/// <param name="force"> Force a full rebuild of the destination folder</param>
	/// <param name="strict"> Treat warnings as errors and fail the build on warnings</param>
	/// <param name="allowIndexing"> Allow indexing and following of html files</param>
	/// <param name="ctx"></param>
	[Command("")]
	[ConsoleAppFilter<StopwatchFilter>]
	[ConsoleAppFilter<CatchExceptionFilter>]
	[ConsoleAppFilter<CheckForUpdatesFilter>]
	public async Task<int> GenerateDefault(
		string? path = null,
		string? output = null,
		string? pathPrefix = null,
		bool? force = null,
		bool? strict = null,
		bool? allowIndexing = null,
		Cancel ctx = default
	) =>
		await Generate(path, output, pathPrefix, force, strict, allowIndexing, ctx);


	/// <summary>
	/// Move a file from one location to another and update all links in the documentation
	/// </summary>
	/// <param name="source">The source file or folder path to move from</param>
	/// <param name="target">The target file or folder path to move to</param>
	/// <param name="path"> -p, Defaults to the`{pwd}` folder</param>
	/// <param name="dryRun">Dry run the move operation</param>
	/// <param name="ctx"></param>
	[Command("mv")]
	[ConsoleAppFilter<StopwatchFilter>]
	[ConsoleAppFilter<CatchExceptionFilter>]
	[ConsoleAppFilter<CheckForUpdatesFilter>]
	public async Task<int> Move(
		[Argument] string source,
		[Argument] string target,
		bool? dryRun = null,
		string? path = null,
		Cancel ctx = default
	)
	{
		AssignOutputLogger();
		var fileSystem = new FileSystem();
		var collector = new ConsoleDiagnosticsCollector(logger, null);
		var context = new BuildContext(collector, fileSystem, fileSystem, path, null);
		var set = new DocumentationSet(context, logger);

		var moveCommand = new Move(fileSystem, fileSystem, set, logger);
		return await moveCommand.Execute(source, target, dryRun ?? false, ctx);
	}
}
