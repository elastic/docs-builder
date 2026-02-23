// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Codex.Navigation;
using Elastic.Codex.Page;
using Elastic.Codex.Sourcing;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Codex;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Isolated;
using Elastic.Documentation.Links.CrossLinks;
using Elastic.Documentation.Navigation;
using Elastic.Documentation.Navigation.Isolated.Node;
using Elastic.Documentation.Services;
using Elastic.Documentation.Site;
using Elastic.Documentation.Site.Navigation;
using Elastic.Markdown.Exporters;
using Elastic.Markdown.IO;
using Microsoft.Extensions.Logging;

namespace Elastic.Codex.Building;

/// <summary>
/// Service for building all documentation sets in a codex.
/// </summary>
public class CodexBuildService(
	ILoggerFactory logFactory,
	IConfigurationContext configurationContext,
	IsolatedBuildService isolatedBuildService) : IService
{
	private readonly ILogger _logger = logFactory.CreateLogger<CodexBuildService>();

	/// <summary>
	/// Builds all documentation sets from the cloned checkouts.
	/// When <paramref name="exporters"/> includes the Elasticsearch exporter, a single shared exporter
	/// is created and its lifecycle is managed across all documentation sets.
	/// </summary>
	public async Task<CodexBuildResult> BuildAll(
		CodexContext context,
		CodexCloneResult cloneResult,
		IFileSystem fileSystem,
		Cancel ctx,
		IReadOnlySet<Exporter>? exporters = null)
	{
		var outputDir = context.OutputDirectory;
		if (outputDir.Exists)
		{
			_logger.LogInformation("Cleaning target output directory: {Directory}", outputDir.FullName);
			outputDir.Delete(true);
		}

		outputDir.Create();

		_logger.LogInformation("Building {Count} documentation sets to {Directory}",
			cloneResult.Checkouts.Count, outputDir.FullName);

		var documentationSets = new Dictionary<string, IDocumentationSetNavigation>();
		var buildContexts = new List<CodexDocumentationSetBuildContext>();

		// Phase 1: Load and parse all documentation sets
		foreach (var checkout in cloneResult.Checkouts)
		{
			var buildContext = await LoadDocumentationSet(context, checkout, fileSystem, ctx);
			if (buildContext != null)
			{
				buildContexts.Add(buildContext);
				documentationSets[checkout.Reference.ResolvedRepoName] = buildContext.DocumentationSet.Navigation;
			}
		}

		// Phase 2: Create codex navigation
		var codexNavigation = new CodexNavigation(
			context.Configuration,
			cloneResult.DocumentationSetReferences,
			new CodexDocumentationContext(context),
			documentationSets);

		// Phase 3: Build each documentation set
		// When exporters are specified (e.g., Elasticsearch), create a single shared exporter
		// with one _batchIndexDate across all doc sets, mirroring AssemblerBuilder.BuildAllAsync
		IMarkdownExporter[]? sharedExporters = null;
		if (exporters is not null && buildContexts.Count > 0)
		{
			var firstContext = buildContexts[0].BuildContext;
			sharedExporters = exporters.CreateMarkdownExporters(logFactory, firstContext, context.IndexNamespace).ToArray();
			var startTasks = sharedExporters.Select(async e => await e.StartAsync(ctx));
			await Task.WhenAll(startTasks);
		}

		foreach (var buildContext in buildContexts)
			await BuildDocumentationSet(context, buildContext, sharedExporters, ctx);

		if (sharedExporters is not null)
		{
			foreach (var exporter in sharedExporters)
			{
				_logger.LogInformation("Calling FinishExportAsync on {ExporterName}", exporter.GetType().Name);
				_ = await exporter.FinishExportAsync(context.OutputDirectory, ctx);
			}

			var stopTasks = sharedExporters.Select(async e => await e.StopAsync(ctx));
			await Task.WhenAll(stopTasks);
		}

		// Phase 4: Generate codex landing and group pages
		CodexGenerator? codexGenerator = null;
		if (buildContexts.Count > 0)
		{
			codexGenerator = await GenerateCodexPages(context, buildContexts[0].BuildContext, codexNavigation, ctx);
		}

		return new CodexBuildResult(codexNavigation, buildContexts.Select(b => b.DocumentationSet).ToList(), codexGenerator);
	}

	private async Task<CodexDocumentationSetBuildContext?> LoadDocumentationSet(
		CodexContext context,
		CodexCheckout checkout,
		IFileSystem fileSystem,
		Cancel ctx)
	{
		_logger.LogInformation("Loading documentation set: {Name}", checkout.Reference.Name);

		try
		{
			// All repos use stable /r/repoName paths (group-independent)
			var repoName = checkout.Reference.ResolvedRepoName;
			var sitePrefix = context.Configuration.SitePrefix?.Trim('/') ?? "";

			// Build output path: {outputDir}/{sitePrefix}/r/{repoName} or {outputDir}/r/{repoName} if no prefix
			var outputPath = string.IsNullOrEmpty(sitePrefix)
				? fileSystem.Path.Combine(context.OutputDirectory.FullName, "r", repoName)
				: fileSystem.Path.Combine(context.OutputDirectory.FullName, sitePrefix, "r", repoName);

			// Build URL path prefix: /r/{repoName} or /{sitePrefix}/r/{repoName}
			var pathPrefix = string.IsNullOrEmpty(sitePrefix)
				? $"/r/{repoName}"
				: $"/{sitePrefix}/r/{repoName}";

			// Create git checkout information
			var git = new GitCheckoutInformation
			{
				Branch = checkout.Reference.Branch,
				Remote = checkout.Reference.ResolvedOrigin,
				Ref = checkout.CommitHash,
				RepositoryName = checkout.Reference.Name,
				GitHubRef = Environment.GetEnvironmentVariable("GITHUB_REF")
			};

			// Pre-compute codex site root for HTMX (no URL parsing in providers)
			var siteRootPath = string.IsNullOrEmpty(sitePrefix) ? "/" : $"/{sitePrefix.Trim('/')}/";

			// Create build context for this documentation set
			var buildContext = new BuildContext(
				context.Collector,
				fileSystem,
				fileSystem,
				configurationContext,
				ExportOptions.Default,
				checkout.DocsDirectory.FullName,
				outputPath,
				git)
			{
				UrlPathPrefix = pathPrefix,
				SiteRootPath = siteRootPath,
				Force = true,
				AllowIndexing = false,
				BuildType = BuildType.Codex
			};

			// Create cross-link resolver (simplified for codex - no external links)
			var crossLinkResolver = NoopCrossLinkResolver.Instance;

			// Create documentation set
			var documentationSet = new DocumentationSet(buildContext, logFactory, crossLinkResolver);
			await documentationSet.ResolveDirectoryTree(ctx);

			return new CodexDocumentationSetBuildContext(checkout, buildContext, documentationSet);
		}
		catch (Exception ex)
		{
			context.Collector.EmitError(context.ConfigurationPath,
				$"Failed to load documentation set '{checkout.Reference.Name}': {ex.Message}");
			_logger.LogError(ex, "Failed to load documentation set {Name}", checkout.Reference.Name);
			return null;
		}
	}

	private async Task BuildDocumentationSet(
		CodexContext context,
		CodexDocumentationSetBuildContext buildContext,
		IMarkdownExporter[]? sharedExporters,
		Cancel ctx)
	{
		_logger.LogInformation("Building documentation set: {Name}", buildContext.Checkout.Reference.Name);

		try
		{
			var codexBreadcrumbs = ResolveCodexBreadcrumbs(context, buildContext);

			_ = await isolatedBuildService.BuildDocumentationSet(
				buildContext.DocumentationSet,
				null, // Use doc set's navigation for traversal
				null, // Use default navigation HTML writer (doc set's navigation)
				ExportOptions.Default,
				sharedExporters,
				pageViewFactory: new CodexPageViewFactory(context.Configuration.Title, codexBreadcrumbs),
				ctx);
		}
		catch (Exception ex)
		{
			context.Collector.EmitError(context.ConfigurationPath,
				$"Failed to build documentation set '{buildContext.Checkout.Reference.Name}': {ex.Message}");
			_logger.LogError(ex, "Failed to build documentation set {Name}", buildContext.Checkout.Reference.Name);
		}
	}

	private static IReadOnlyList<CodexBreadcrumb> ResolveCodexBreadcrumbs(
		CodexContext context,
		CodexDocumentationSetBuildContext buildContext)
	{
		var reference = buildContext.Checkout.Reference;
		var sitePrefix = context.Configuration.SitePrefix?.Trim('/') ?? "";
		var repoName = reference.ResolvedRepoName;
		var groupId = reference.Group;
		var homeUrl = string.IsNullOrEmpty(sitePrefix) ? "/" : $"/{sitePrefix}/";
		var docSetUrl = string.IsNullOrEmpty(sitePrefix) ? $"/r/{repoName}" : $"/{sitePrefix}/r/{repoName}";
		var indexTitle = buildContext.DocumentationSet.Navigation.Index.Model.Title;
		var docSetTitle = !string.IsNullOrEmpty(indexTitle) ? indexTitle : repoName;

		if (string.IsNullOrEmpty(groupId))
			return [new CodexBreadcrumb("Home", homeUrl), new CodexBreadcrumb(docSetTitle, docSetUrl)];

		var groupUrl = string.IsNullOrEmpty(sitePrefix) ? $"/g/{groupId}" : $"/{sitePrefix}/g/{groupId}";
		var groupDef = context.Configuration.Groups.FirstOrDefault(g => g.Id == groupId);
		var groupTitle = groupDef?.Name ?? groupId;
		return [new CodexBreadcrumb("Home", homeUrl), new CodexBreadcrumb(groupTitle, groupUrl), new CodexBreadcrumb(docSetTitle, docSetUrl)];
	}

	private async Task<CodexGenerator> GenerateCodexPages(
		CodexContext context,
		BuildContext docSetBuildContext,
		CodexNavigation codexNavigation,
		Cancel ctx)
	{
		_logger.LogInformation("Generating codex pages");

		// Pre-compute codex site root for HTMX
		var siteRootPath = string.IsNullOrEmpty(context.Configuration.SitePrefix)
			? "/"
			: $"/{context.Configuration.SitePrefix.Trim('/')}/";

		// Create a codex-specific build context using the doc set's context as a base
		// but with codex-specific URL prefix
		var codexBuildContext = docSetBuildContext with
		{
			UrlPathPrefix = context.Configuration.SitePrefix,
			SiteRootPath = siteRootPath,
			Force = true,
			AllowIndexing = false
		};

		// Use CodexGenerator to render the codex pages to the codex's output directory
		var codexGenerator = new CodexGenerator(logFactory, codexBuildContext, context.OutputDirectory);
		await codexGenerator.Generate(codexNavigation, ctx);
		return codexGenerator;
	}
}

/// <summary>
/// Result of building a codex.
/// </summary>
/// <param name="Navigation">The codex navigation structure.</param>
/// <param name="DocumentationSets">The built documentation sets.</param>
/// <param name="CodexGenerator">Generator for re-rendering codex pages (e.g. for dev server).</param>
public record CodexBuildResult(
	CodexNavigation Navigation,
	IReadOnlyList<DocumentationSet> DocumentationSets,
	CodexGenerator? CodexGenerator = null);

/// <summary>
/// Build context for a single documentation set within the codex.
/// </summary>
public record CodexDocumentationSetBuildContext(
	CodexCheckout Checkout,
	BuildContext BuildContext,
	DocumentationSet DocumentationSet);

/// <summary>
/// Documentation context adapter for codex navigation creation.
/// </summary>
internal sealed class CodexDocumentationContext(CodexContext codexContext) : ICodexDocumentationContext
{
	/// <inheritdoc />
	public IFileInfo ConfigurationPath => codexContext.ConfigurationPath;

	/// <inheritdoc />
	public IDiagnosticsCollector Collector => codexContext.Collector;

	/// <inheritdoc />
	public IFileSystem ReadFileSystem => codexContext.ReadFileSystem;

	/// <inheritdoc />
	public IFileSystem WriteFileSystem => codexContext.WriteFileSystem;

	/// <inheritdoc />
	public IDirectoryInfo OutputDirectory => codexContext.OutputDirectory;

	/// <inheritdoc />
	public BuildType BuildType => BuildType.Codex;

	/// <inheritdoc />
	public void EmitError(string message) =>
		codexContext.Collector.EmitError(codexContext.ConfigurationPath, message);
}
