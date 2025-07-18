// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using Actions.Core.Services;
using ConsoleAppFramework;
using Documentation.Builder.Http;
using Elastic.ApiExplorer;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Versions;
using Elastic.Documentation.Refactor;
using Elastic.Documentation.Tooling.Diagnostics.Console;
using Elastic.Markdown;
using Elastic.Markdown.Exporters;
using Elastic.Markdown.IO;
using Elastic.Markdown.Myst.Renderers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Documentation.Builder.Cli;

internal sealed class Commands(ILoggerFactory logFactory, ICoreService githubActionsService, VersionsConfiguration versionsConfig)
{
	private readonly ILogger<Program> _log = logFactory.CreateLogger<Program>();

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
	public async Task Serve(string? path = null, int port = 3000, Cancel ctx = default)
	{
		var host = new DocumentationWebHost(logFactory, path, port, new FileSystem(), new MockFileSystem(), versionsConfig);
		_log.LogInformation("Find your documentation at http://localhost:{Port}/{Path}", port,
			host.GeneratorState.Generator.DocumentationSet.FirstInterestingUrl.TrimStart('/')
		);
		await host.RunAsync(ctx);
		await host.StopAsync(ctx);
	}

	/// <summary>
	/// Serve HTML files directly
	/// </summary>
	/// <param name="port">Port to serve the documentation.</param>
	/// <param name="ctx"></param>
	[Command("serve-static")]
	public async Task ServeStatic(int port = 4000, Cancel ctx = default)
	{
		var host = new StaticWebHost(port);
		await host.RunAsync(ctx);
		await host.StopAsync(ctx);
	}

	/// <summary>
	/// Converts a source Markdown folder or file to an output folder
	/// <para>global options:</para>
	/// --log-level level
	/// </summary>
	/// <param name="path"> -p, Defaults to the`{pwd}/docs` folder</param>
	/// <param name="output"> -o, Defaults to `.artifacts/html` </param>
	/// <param name="pathPrefix"> Specifies the path prefix for urls </param>
	/// <param name="force"> Force a full rebuild of the destination folder</param>
	/// <param name="strict"> Treat warnings as errors and fail the build on warnings</param>
	/// <param name="allowIndexing"> Allow indexing and following of HTML files</param>
	/// <param name="metadataOnly"> Only emit documentation metadata to output</param>
	/// <param name="canonicalBaseUrl"> The base URL for the canonical url tag</param>
	/// <param name="ctx"></param>
	[Command("generate")]
	public async Task<int> Generate(
		string? path = null,
		string? output = null,
		string? pathPrefix = null,
		bool? force = null,
		bool? strict = null,
		bool? allowIndexing = null,
		bool? metadataOnly = null,
		string? canonicalBaseUrl = null,
		Cancel ctx = default
	)
	{
		pathPrefix ??= githubActionsService.GetInput("prefix");
		var fileSystem = new FileSystem();
		await using var collector = new ConsoleDiagnosticsCollector(logFactory, githubActionsService).StartAsync(ctx);

		var runningOnCi = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));
		BuildContext context;

		Uri? canonicalBaseUri;

		if (runningOnCi)
		{
			ConsoleApp.Log($"Build running on CI, forcing a full rebuild of the destination folder");
			force = true;
		}

		if (canonicalBaseUrl is null)
			canonicalBaseUri = new Uri("https://docs-v3-preview.elastic.dev");
		else if (!Uri.TryCreate(canonicalBaseUrl, UriKind.Absolute, out canonicalBaseUri))
			throw new ArgumentException($"The canonical base url '{canonicalBaseUrl}' is not a valid absolute uri");

		try
		{
			context = new BuildContext(collector, fileSystem, fileSystem, versionsConfig, path, output)
			{
				UrlPathPrefix = pathPrefix,
				Force = force ?? false,
				AllowIndexing = allowIndexing ?? false,
				CanonicalBaseUrl = canonicalBaseUri
			};
		}
		// On CI, we are running on a merge commit which may have changes against an older
		// docs folder (this can happen on out-of-date PR's).
		// At some point in the future we can remove this try catch
		catch (Exception e) when (runningOnCi && e.Message.StartsWith("Can not locate docset.yml file in"))
		{
			var outputDirectory = !string.IsNullOrWhiteSpace(output)
				? fileSystem.DirectoryInfo.New(output)
				: fileSystem.DirectoryInfo.New(Path.Combine(Paths.WorkingDirectoryRoot.FullName, ".artifacts/docs/html"));
			// we temporarily do not error when pointed to a non-documentation folder.
			_ = fileSystem.Directory.CreateDirectory(outputDirectory.FullName);

			ConsoleApp.Log($"Skipping build as we are running on a merge commit and the docs folder is out of date and has no docset.yml. {e.Message}");

			await githubActionsService.SetOutputAsync("skip", "true");
			return 0;
		}

		if (runningOnCi)
			await githubActionsService.SetOutputAsync("skip", "false");

		// always delete output folder on CI
		var set = new DocumentationSet(context, logFactory);
		if (runningOnCi)
			set.ClearOutputDirectory();


		if (bool.TryParse(githubActionsService.GetInput("metadata-only"), out var metaValue) && metaValue)
			metadataOnly ??= metaValue;
		var exporter = metadataOnly.HasValue && metadataOnly.Value ? new NoopDocumentationFileExporter() : null;

		// Add LLM markdown export alongside HTML generation
		var markdownExporters = new List<IMarkdownExporter>();
		markdownExporters.AddLlmMarkdownExport(); // Consistent LLM-optimized output

		var generator = new DocumentationGenerator(set, logFactory, null, null, markdownExporters.ToArray(), exporter);
		_ = await generator.GenerateAll(ctx);


		var openApiGenerator = new OpenApiGenerator(logFactory, context, generator.MarkdownStringRenderer);
		await openApiGenerator.Generate(ctx);

		if (runningOnCi)
			await githubActionsService.SetOutputAsync("landing-page-path", set.FirstInterestingUrl);

		await collector.StopAsync(ctx);
		if (bool.TryParse(githubActionsService.GetInput("strict"), out var strictValue) && strictValue)
			strict ??= strictValue;
		if (strict ?? false)
			return context.Collector.Errors + context.Collector.Warnings;
		return context.Collector.Errors;
	}

	/// <summary>
	/// Converts a source Markdown folder or file to an output folder
	/// </summary>
	/// <param name="path"> -p, Defaults to the`{pwd}/docs` folder</param>
	/// <param name="output"> -o, Defaults to `.artifacts/html` </param>
	/// <param name="pathPrefix"> Specifies the path prefix for urls </param>
	/// <param name="force"> Force a full rebuild of the destination folder</param>
	/// <param name="strict"> Treat warnings as errors and fail the build on warnings</param>
	/// <param name="allowIndexing"> Allow indexing and following of HTML files</param>
	/// <param name="metadataOnly"> Only emit documentation metadata to output</param>
	/// <param name="canonicalBaseUrl"> The base URL for the canonical url tag</param>
	/// <param name="ctx"></param>
	[Command("")]
	public async Task<int> GenerateDefault(
		string? path = null,
		string? output = null,
		string? pathPrefix = null,
		bool? force = null,
		bool? strict = null,
		bool? allowIndexing = null,
		bool? metadataOnly = null,
		string? canonicalBaseUrl = null,
		Cancel ctx = default
	) =>
		await Generate(path, output, pathPrefix, force, strict, allowIndexing, metadataOnly, canonicalBaseUrl, ctx);


	/// <summary>
	/// Move a file from one location to another and update all links in the documentation
	/// </summary>
	/// <param name="source">The source file or folder path to move from</param>
	/// <param name="target">The target file or folder path to move to</param>
	/// <param name="path"> -p, Defaults to the`{pwd}` folder</param>
	/// <param name="dryRun">Dry run the move operation</param>
	/// <param name="ctx"></param>
	[Command("mv")]
	public async Task<int> Move(
		[Argument] string source,
		[Argument] string target,
		bool? dryRun = null,
		string? path = null,
		Cancel ctx = default
	)
	{
		var fileSystem = new FileSystem();
		await using var collector = new ConsoleDiagnosticsCollector(logFactory, null).StartAsync(ctx);
		var context = new BuildContext(collector, fileSystem, fileSystem, versionsConfig, path, null);
		var set = new DocumentationSet(context, logFactory);

		var moveCommand = new Move(logFactory, fileSystem, fileSystem, set);
		var result = await moveCommand.Execute(source, target, dryRun ?? false, ctx);
		await collector.StopAsync(ctx);
		return result;
	}
}
