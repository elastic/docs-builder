// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using System.Net.Mime;
using Actions.Core.Services;
using Amazon.S3;
using Amazon.S3.Model;
using ConsoleAppFramework;
using Documentation.Assembler.Building;
using Documentation.Assembler.Legacy;
using Documentation.Assembler.Navigation;
using Documentation.Assembler.Sourcing;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.LegacyDocs;
using Elastic.Documentation.Tooling.Diagnostics.Console;
using Elastic.Markdown;
using Elastic.Markdown.Exporters;
using Elastic.Markdown.IO;
using Microsoft.Extensions.Logging;

namespace Documentation.Assembler.Cli;

internal sealed class RepositoryCommands(ICoreService githubActionsService, ILoggerFactory logger)
{
	private readonly ILogger<Program> _log = logger.CreateLogger<Program>();

	[SuppressMessage("Usage", "CA2254:Template should be a static expression")]
	private void AssignOutputLogger()
	{
		ConsoleApp.Log = msg => _log.LogInformation(msg);
		ConsoleApp.LogError = msg => _log.LogError(msg);
	}

	/// <summary> Clones all repositories </summary>
	/// <param name="strict"> Treat warnings as errors and fail the build on warnings</param>
	/// <param name="environment"> The environment to build</param>
	/// <param name="fetchLatest"> If true, fetch the latest commit of the branch instead of the link registry entry ref</param>
	/// <param name="ctx"></param>
	[Command("clone-all")]
	public async Task<int> CloneAll(
		bool? strict = null,
		string? environment = null,
		bool? fetchLatest = null,
		Cancel ctx = default
	)
	{
		AssignOutputLogger();
		var githubEnvironmentInput = githubActionsService.GetInput("environment");
		environment ??= !string.IsNullOrEmpty(githubEnvironmentInput) ? githubEnvironmentInput : "dev";

		await using var collector = new ConsoleDiagnosticsCollector(logger, githubActionsService).StartAsync(ctx);

		var assembleContext = new AssembleContext(environment, collector, new FileSystem(), new FileSystem(), null, null);
		var cloner = new AssemblerRepositorySourcer(logger, assembleContext);

		_ = await cloner.CloneAll(fetchLatest ?? false, ctx);

		await collector.StopAsync(ctx);

		if (strict ?? false)
			return collector.Errors + collector.Warnings;
		return collector.Errors;
	}

	/// <summary> Builds all repositories </summary>
	/// <param name="force"> Force a full rebuild of the destination folder</param>
	/// <param name="strict"> Treat warnings as errors and fail the build on warnings</param>
	/// <param name="allowIndexing"> Allow indexing and following of html files</param>
	/// <param name="environment"> The environment to build</param>
	/// <param name="exporters"> configure exporters explicitly available (html,llmtext,es), defaults to html</param>
	/// <param name="ctx"></param>
	[Command("build-all")]
	public async Task<int> BuildAll(
		bool? force = null,
		bool? strict = null,
		bool? allowIndexing = null,
		string? environment = null,
		[ExporterParser] IReadOnlySet<ExportOption>? exporters = null,
		Cancel ctx = default)
	{
		exporters ??= new HashSet<ExportOption>([ExportOption.Html]);

		AssignOutputLogger();
		var githubEnvironmentInput = githubActionsService.GetInput("environment");
		environment ??= !string.IsNullOrEmpty(githubEnvironmentInput) ? githubEnvironmentInput : "dev";

		_log.LogInformation("Building all repositories for environment {Environment}", environment);

		await using var collector = new ConsoleDiagnosticsCollector(logger, githubActionsService)
		{
			NoHints = true
		}.StartAsync(ctx);

		_log.LogInformation("Creating assemble context");

		var assembleContext = new AssembleContext(environment, collector, new FileSystem(), new FileSystem(), null, null)
		{
			Force = force ?? false,
			AllowIndexing = allowIndexing ?? false
		};

		_log.LogInformation("Validating navigation.yml does not contain colliding path prefixes");
		// this validates all path prefixes are unique, early exit if duplicates are detected
		if (!GlobalNavigationFile.ValidatePathPrefixes(assembleContext) || assembleContext.Collector.Errors > 0)
		{
			await assembleContext.Collector.StopAsync(ctx);
			return 1;
		}

		_log.LogInformation("Get all clone directory information");
		var cloner = new AssemblerRepositorySourcer(logger, assembleContext);
		var checkoutResult = cloner.GetAll();
		var checkouts = checkoutResult.Checkouts.ToArray();

		if (checkouts.Length == 0)
			throw new Exception("No checkouts found");

		_log.LogInformation("Preparing all assemble sources for build");
		var assembleSources = await AssembleSources.AssembleAsync(logger, assembleContext, checkouts, ctx);
		var navigationFile = new GlobalNavigationFile(assembleContext, assembleSources);

		_log.LogInformation("Create global navigation");
		var navigation = new GlobalNavigation(assembleSources, navigationFile);

		var pathProvider = new GlobalNavigationPathProvider(navigationFile, assembleSources, assembleContext);
		var htmlWriter = new GlobalNavigationHtmlWriter(navigationFile, assembleContext, navigation, assembleSources);
		var legacyPageChecker = new LegacyPageChecker();
		var historyMapper = new PageLegacyUrlMapper(legacyPageChecker, assembleSources.HistoryMappings);

		var builder = new AssemblerBuilder(logger, assembleContext, navigation, htmlWriter, pathProvider, historyMapper);
		await builder.BuildAllAsync(assembleSources.AssembleSets, exporters, ctx);

		await cloner.WriteLinkRegistrySnapshot(checkoutResult.LinkRegistrySnapshot, ctx);

		var redirectsPath = Path.Combine(assembleContext.OutputDirectory.FullName, "redirects.json");
		if (File.Exists(redirectsPath))
			await githubActionsService.SetOutputAsync("redirects-artifact-path", redirectsPath);

		var sitemapBuilder = new SitemapBuilder(navigation.NavigationItems, assembleContext.WriteFileSystem, assembleContext.OutputDirectory);
		sitemapBuilder.Generate();

		await collector.StopAsync(ctx);

		if (strict ?? false)
			return collector.Errors + collector.Warnings;
		return collector.Errors;
	}

	/// <param name="contentSource"> The content source. "current" or "next"</param>
	/// <param name="ctx"></param>
	[Command("update-all-link-reference")]
	public async Task<int> UpdateLinkIndexAll(ContentSource contentSource, Cancel ctx = default)
	{
		var collector = new ConsoleDiagnosticsCollector(logger, githubActionsService);
		// The environment ist not relevant here.
		// It's only used to get the list of repositories.
		var assembleContext = new AssembleContext("prod", collector, new FileSystem(), new FileSystem(), null, null);
		var cloner = new RepositorySourcer(logger, assembleContext.CheckoutDirectory, new FileSystem(), collector);
		var repositories = new Dictionary<string, Repository>(assembleContext.Configuration.ReferenceRepositories)
		{
			{ NarrativeRepository.RepositoryName, assembleContext.Configuration.Narrative }
		};
		await Parallel.ForEachAsync(repositories,
			new ParallelOptions
			{
				CancellationToken = ctx,
				MaxDegreeOfParallelism = Environment.ProcessorCount
			}, async (kv, c) =>
			{
				try
				{
					var checkout = cloner.CloneRef(kv.Value, kv.Value.GetBranch(contentSource), true);
					var outputPath = Directory.CreateTempSubdirectory(checkout.Repository.Name).FullName;
					var context = new BuildContext(
						collector,
						new FileSystem(),
						new FileSystem(),
						checkout.Directory.FullName,
						outputPath
					);
					var set = new DocumentationSet(context, logger);
					var generator = new DocumentationGenerator(set, logger, null, null, null, new NoopDocumentationFileExporter());
					_ = await generator.GenerateAll(c);

					IAmazonS3 s3Client = new AmazonS3Client();
					const string bucketName = "elastic-docs-link-index";
					var linksJsonPath = Path.Combine(outputPath, "links.json");
					var content = await File.ReadAllTextAsync(linksJsonPath, c);
					var putObjectRequest = new PutObjectRequest
					{
						BucketName = bucketName,
						Key = $"elastic/{checkout.Repository.Name}/{checkout.Repository.GetBranch(contentSource)}/links.json",
						ContentBody = content,
						ContentType = MediaTypeNames.Application.Json,
						ChecksumAlgorithm = ChecksumAlgorithm.SHA256
					};
					var response = await s3Client.PutObjectAsync(putObjectRequest, c);
					if (response.HttpStatusCode != System.Net.HttpStatusCode.OK)
						collector.EmitError(linksJsonPath, $"Failed to upload {putObjectRequest.Key} to S3");
				}
				catch (Exception e)
				{
					collector.EmitError(kv.Key, $"Failed to update link index for {kv.Key}: {e.Message}", e);
				}
			}).ConfigureAwait(false);

		await collector.StopAsync(ctx);

		return collector.Errors > 0 ? 1 : 0;
	}
}

[AttributeUsage(AttributeTargets.Parameter)]
public class ExporterParserAttribute : Attribute, IArgumentParser<IReadOnlySet<ExportOption>>
{
	public static bool TryParse(ReadOnlySpan<char> s, out IReadOnlySet<ExportOption> result)
	{
		result = new HashSet<ExportOption>([ExportOption.Html]);
		var set = new HashSet<ExportOption>();
		var ranges = s.Split(',');
		foreach (var range in ranges)
		{
			ExportOption? export = s[range].Trim().ToString().ToLowerInvariant() switch
			{
				"llm" => ExportOption.LLMText,
				"llmtext" => ExportOption.LLMText,
				"es" => ExportOption.Elasticsearch,
				"elasticsearch" => ExportOption.Elasticsearch,
				"html" => ExportOption.Html,
				_ => null
			};
			if (export.HasValue)
				_ = set.Add(export.Value);
		}
		result = set;
		return true;
	}
}
