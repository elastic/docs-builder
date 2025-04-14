// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using System.Net.Mime;
using Actions.Core.Services;
using Amazon.S3;
using Amazon.S3.Model;
using ConsoleAppFramework;
using Documentation.Assembler.Building;
using Documentation.Assembler.Configuration;
using Documentation.Assembler.Mapping;
using Documentation.Assembler.Navigation;
using Documentation.Assembler.Sourcing;
using Elastic.Documentation.Tooling.Diagnostics.Console;
using Elastic.Markdown;
using Elastic.Markdown.Exporters;
using Elastic.Markdown.IO;
using Elastic.Markdown.IO.State;
using Microsoft.Extensions.Logging;

namespace Documentation.Assembler.Cli;

internal sealed class RepositoryCommands(ICoreService githubActionsService, ILoggerFactory logger)
{
	[SuppressMessage("Usage", "CA2254:Template should be a static expression")]
	private void AssignOutputLogger()
	{
		var log = logger.CreateLogger<Program>();
		ConsoleApp.Log = msg => log.LogInformation(msg);
		ConsoleApp.LogError = msg => log.LogError(msg);
	}

	// would love to use libgit2 so there is no git dependency but
	// libgit2 is magnitudes slower to clone repositories https://github.com/libgit2/libgit2/issues/4674
	/// <summary> Clones all repositories </summary>
	/// <param name="strict"> Treat warnings as errors and fail the build on warnings</param>
	/// <param name="environment"> The environment to build</param>
	/// <param name="ctx"></param>
	[Command("clone-all")]
	public async Task<int> CloneAll(
		bool? strict = null,
		string? environment = null,
		Cancel ctx = default
	)
	{
		AssignOutputLogger();
		var githubEnvironmentInput = githubActionsService.GetInput("environment");
		environment ??= !string.IsNullOrEmpty(githubEnvironmentInput) ? githubEnvironmentInput : "dev";

		await using var collector = new ConsoleDiagnosticsCollector(logger, githubActionsService);

		var assembleContext = new AssembleContext(environment, collector, new FileSystem(), new FileSystem(), null, null);
		var cloner = new AssemblerRepositorySourcer(logger, assembleContext);
		_ = await cloner.AcquireAllLatest(ctx);

		if (strict ?? false)
			return collector.Errors + collector.Warnings;
		return collector.Errors;
	}

	/// <summary> Builds all repositories </summary>
	/// <param name="force"> Force a full rebuild of the destination folder</param>
	/// <param name="strict"> Treat warnings as errors and fail the build on warnings</param>
	/// <param name="allowIndexing"> Allow indexing and following of html files</param>
	/// <param name="environment"> The environment to build</param>
	/// <param name="ctx"></param>
	[Command("build-all")]
	public async Task<int> BuildAll(
		bool? force = null,
		bool? strict = null,
		bool? allowIndexing = null,
		string? environment = null,
		Cancel ctx = default)
	{
		AssignOutputLogger();
		var githubEnvironmentInput = githubActionsService.GetInput("environment");
		environment ??= !string.IsNullOrEmpty(githubEnvironmentInput) ? githubEnvironmentInput : "dev";

		await using var collector = new ConsoleDiagnosticsCollector(logger, githubActionsService)
		{
			NoHints = true
		};

		_ = collector.StartAsync(ctx);

		var assembleContext = new AssembleContext(environment, collector, new FileSystem(), new FileSystem(), null, null)
		{
			Force = force ?? false,
			AllowIndexing = allowIndexing ?? false,
		};

		// this validates all path prefixes are unique, early exit if duplicates are detected
		if (!GlobalNavigationFile.ValidatePathPrefixes(assembleContext) || assembleContext.Collector.Errors > 0)
		{
			assembleContext.Collector.Channel.TryComplete();
			await assembleContext.Collector.StopAsync(ctx);
			return 1;
		}

		var cloner = new AssemblerRepositorySourcer(logger, assembleContext);
		var checkouts = cloner.GetAll().ToArray();
		if (checkouts.Length == 0)
			throw new Exception("No checkouts found");

		var assembleSources = await AssembleSources.AssembleAsync(assembleContext, checkouts, ctx);
		var navigationFile = new GlobalNavigationFile(assembleContext, assembleSources);

		var navigation = new GlobalNavigation(assembleSources, navigationFile);

		var pathProvider = new GlobalNavigationPathProvider(navigationFile, assembleSources, assembleContext);
		var htmlWriter = new GlobalNavigationHtmlWriter(navigationFile, assembleContext, navigation, assembleSources);

		var historyMapper = new PageHistoryMapper(assembleSources.HistoryMappings);

		var builder = new AssemblerBuilder(logger, assembleContext, navigation, htmlWriter, pathProvider, historyMapper);
		await builder.BuildAllAsync(assembleSources.AssembleSets, ctx);

		var sitemapBuilder = new SitemapBuilder(navigation.NavigationItems, assembleContext.WriteFileSystem, assembleContext.OutputDirectory);
		sitemapBuilder.Generate();

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
		var dict = new ConcurrentDictionary<string, Stopwatch>();
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
					var name = kv.Key.Trim();
					var checkout = cloner.CloneOrUpdateRepository(kv.Value, name, kv.Value.GetBranch(contentSource), dict);
					var outputPath = Directory.CreateTempSubdirectory(checkout.Repository.Name).FullName;
					var context = new BuildContext(
						collector,
						new FileSystem(),
						new FileSystem(),
						checkout.Directory.FullName,
						outputPath
					);
					var set = new DocumentationSet(context, logger);
					var generator = new DocumentationGenerator(set, logger, null, null, new NoopDocumentationFileExporter());
					await generator.GenerateAll(c);

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
		return collector.Errors > 0 ? 1 : 0;
	}
}
