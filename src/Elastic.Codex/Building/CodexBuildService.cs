// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Codex.Navigation;
using Elastic.Codex.Sourcing;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Isolated;
using Elastic.Documentation.Links.CrossLinks;
using Elastic.Documentation.Navigation;
using Elastic.Documentation.Navigation.Isolated.Node;
using Elastic.Documentation.Services;
using Elastic.Documentation.Site.Navigation;
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
	/// </summary>
	public async Task<CodexBuildResult> BuildAll(
		CodexContext context,
		CodexCloneResult cloneResult,
		IFileSystem fileSystem,
		Cancel ctx)
	{
		var outputDir = context.OutputDirectory;
		if (!outputDir.Exists)
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
			new CodexDocumentationContext(context),
			documentationSets);

		// Phase 3: Build each documentation set
		foreach (var buildContext in buildContexts)
			await BuildDocumentationSet(context, buildContext, ctx);

		// Phase 4: Generate codex landing and category pages
		if (buildContexts.Count > 0)
			await GenerateCodexPages(context, buildContexts[0].BuildContext, codexNavigation, ctx);

		return new CodexBuildResult(codexNavigation, buildContexts.Select(b => b.DocumentationSet).ToList());
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
		Cancel ctx)
	{
		_logger.LogInformation("Building documentation set: {Name}", buildContext.Checkout.Reference.Name);

		try
		{
			_ = await isolatedBuildService.BuildDocumentationSet(
				buildContext.DocumentationSet,
				null, // Use doc set's navigation for traversal
				null, // Use default navigation HTML writer (doc set's navigation)
				ExportOptions.Default,
				ctx);
		}
		catch (Exception ex)
		{
			context.Collector.EmitError(context.ConfigurationPath,
				$"Failed to build documentation set '{buildContext.Checkout.Reference.Name}': {ex.Message}");
			_logger.LogError(ex, "Failed to build documentation set {Name}", buildContext.Checkout.Reference.Name);
		}
	}

	private async Task GenerateCodexPages(
		CodexContext context,
		BuildContext docSetBuildContext,
		CodexNavigation codexNavigation,
		Cancel ctx)
	{
		_logger.LogInformation("Generating codex pages");

		// Create a codex-specific build context using the doc set's context as a base
		// but with codex-specific URL prefix
		var codexBuildContext = docSetBuildContext with
		{
			UrlPathPrefix = context.Configuration.SitePrefix,
			Force = true,
			AllowIndexing = false
		};

		// Use CodexGenerator to render the codex pages to the codex's output directory
		var codexGenerator = new CodexGenerator(logFactory, codexBuildContext, context.OutputDirectory);
		await codexGenerator.Generate(codexNavigation, ctx);
	}
}

/// <summary>
/// Result of building a codex.
/// </summary>
/// <param name="Navigation">The codex navigation structure.</param>
/// <param name="DocumentationSets">The built documentation sets.</param>
public record CodexBuildResult(
	CodexNavigation Navigation,
	IReadOnlyList<DocumentationSet> DocumentationSets);

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
