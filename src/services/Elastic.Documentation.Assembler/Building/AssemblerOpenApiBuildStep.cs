// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;
using System.IO.Abstractions;
using Elastic.ApiExplorer;
using Elastic.Documentation;
using Elastic.Documentation.Assembler.Sourcing;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Configuration.Builder;
using Elastic.Documentation.Links.CrossLinks;
using Elastic.Markdown;
using Elastic.Markdown.Exporters;
using Elastic.Markdown.IO;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Assembler.Building;

/// <summary>
/// Renders ApiExplorer HTML into the assembler output tree (e.g. <c>/docs/api/</c>).
/// </summary>
public static class AssemblerOpenApiBuildStep
{
	public static async Task BuildAsync(
		ILoggerFactory logFactory,
		AssembleContext assembleContext,
		IReadOnlyCollection<Checkout> checkouts,
		IConfigurationContext configurationContext,
		Cancel ctx)
	{
		var logger = logFactory.CreateLogger(typeof(AssemblerOpenApiBuildStep));
		var env = assembleContext.Environment;
		var features = new FeatureFlags([]);
		foreach (var (key, value) in env.FeatureFlags)
			features.Set(key, value);

		if (!features.AssemblerApiExplorerEnabled)
		{
			logger.LogInformation("Skipping OpenAPI generation: assembler-api-explorer feature flag is disabled");
			return;
		}

		var repositoryRoot = ResolveDocsBuilderRepositoryRoot(assembleContext.ReadFileSystem, checkouts);
		if (repositoryRoot is null)
		{
			logger.LogInformation("Skipping OpenAPI generation: docs-builder docset not found");
			return;
		}

		var outputDirectory = assembleContext.OutputWithPathPrefixDirectory.FullName;
		var checkout = checkouts.FirstOrDefault(c =>
			string.Equals(c.Repository.Name, "docs-builder", StringComparison.OrdinalIgnoreCase)
			&& string.Equals(c.Directory.FullName, repositoryRoot.FullName, StringComparison.Ordinal));

		var gitConfiguration = checkout is not null
			? new GitCheckoutInformation
			{
				RepositoryName = checkout.Repository.Name,
				Ref = checkout.HeadReference,
				Remote = $"elastic/{checkout.Repository.Name}",
				Branch = checkout.Repository.GetBranch(env.ContentSource)
			}
			: new GitCheckoutInformation
			{
				RepositoryName = "docs-builder",
				Ref = "local",
				Remote = "elastic/docs-builder",
				Branch = "local"
			};

		var buildContext = new BuildContext(
			assembleContext.Collector,
			assembleContext.ReadFileSystem,
			assembleContext.WriteFileSystem,
			configurationContext,
			new HashSet<Exporter> { Exporter.Html },
			repositoryRoot.FullName,
			outputDirectory,
			gitConfiguration)
		{
			UrlPathPrefix = env.PathPrefix,
			Force = true,
			AllowIndexing = env.AllowIndexing,
			GoogleTagManager = new GoogleTagManagerConfiguration
			{
				Enabled = env.GoogleTagManager.Enabled,
				Id = env.GoogleTagManager.Id,
				Auth = env.GoogleTagManager.Auth,
				Preview = env.GoogleTagManager.Preview,
				CookiesWin = env.GoogleTagManager.CookiesWin
			},
			Optimizely = new OptimizelyConfiguration
			{
				Enabled = env.Optimizely.Enabled,
				Id = env.Optimizely.Id
			},
			CanonicalBaseUrl = assembleContext.CanonicalBaseUrl,
			BuildType = BuildType.Assembler
		};

		if (buildContext.Configuration.ApiConfigurations is null)
		{
			logger.LogInformation("Skipping OpenAPI generation: no API specs configured in docs-builder docset");
			return;
		}

		foreach (var (key, value) in env.FeatureFlags)
			buildContext.Configuration.Features.Set(key, value);

		var stopwatch = Stopwatch.StartNew();
		var documentationSet = new DocumentationSet(buildContext, logFactory, NoopCrossLinkResolver.Instance);
		await documentationSet.ResolveDirectoryTree(ctx);
		var generator = new DocumentationGenerator(documentationSet, logFactory);

		var openApiGenerator = new OpenApiGenerator(logFactory, buildContext, generator.MarkdownStringRenderer);
		await openApiGenerator.Generate(ctx);

		stopwatch.Stop();
		logger.LogInformation(
			"Finished generating OpenAPI pages under {OutputDirectory} in {DurationMs} ms",
			outputDirectory,
			stopwatch.ElapsedMilliseconds);
	}

	private static IDirectoryInfo? ResolveDocsBuilderRepositoryRoot(IFileSystem fileSystem, IReadOnlyCollection<Checkout> checkouts)
	{
		var workspaceRoot = fileSystem.DirectoryInfo.New(Directory.GetCurrentDirectory());
		if (HasDocset(fileSystem, workspaceRoot))
			return workspaceRoot;

		var checkout = checkouts.FirstOrDefault(c => c.Repository.Name == "docs-builder");
		if (checkout is not null && HasDocset(fileSystem, checkout.Directory))
			return checkout.Directory;

		return null;
	}

	private static bool HasDocset(IFileSystem fileSystem, IDirectoryInfo repositoryRoot) =>
		fileSystem.File.Exists(fileSystem.Path.Combine(repositoryRoot.FullName, "docs", "_docset.yml"));
}
