// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Links.CrossLinks;
using Elastic.Documentation.Navigation;
using Elastic.Documentation.Navigation.Isolated.Node;
using Elastic.Documentation.Portal.Sourcing;
using Elastic.Documentation.Services;
using Elastic.Markdown;
using Elastic.Markdown.Exporters;
using Elastic.Markdown.IO;
using Elastic.Portal;
using Elastic.Portal.Navigation;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Portal.Building;

/// <summary>
/// Service for building all documentation sets in a portal.
/// </summary>
public class PortalBuildService(ILoggerFactory logFactory, IConfigurationContext configurationContext) : IService
{
	private readonly ILogger _logger = logFactory.CreateLogger<PortalBuildService>();

	/// <summary>
	/// Builds all documentation sets from the cloned checkouts.
	/// </summary>
	public async Task<PortalBuildResult> BuildAll(
		PortalContext context,
		PortalCloneResult cloneResult,
		IFileSystem fileSystem,
		Cancel ctx)
	{
		var outputDir = context.OutputDirectory;
		if (!outputDir.Exists)
			outputDir.Create();

		_logger.LogInformation("Building {Count} documentation sets to {Directory}",
			cloneResult.Checkouts.Count, outputDir.FullName);

		var documentationSets = new Dictionary<string, IDocumentationSetNavigation>();
		var buildContexts = new List<PortalDocumentationSetBuildContext>();

		// Phase 1: Load and parse all documentation sets
		foreach (var checkout in cloneResult.Checkouts)
		{
			var buildContext = await LoadDocumentationSet(context, checkout, fileSystem, ctx);
			if (buildContext != null)
			{
				buildContexts.Add(buildContext);
				documentationSets[checkout.Reference.Name] = buildContext.DocumentationSet.Navigation;
			}
		}

		// Phase 2: Create portal navigation
		var portalNavigation = new PortalNavigation(
			context.Configuration,
			new PortalDocumentationContext(context),
			documentationSets);

		// Phase 3: Build each documentation set
		foreach (var buildContext in buildContexts)
			await BuildDocumentationSet(context, buildContext, portalNavigation, ctx);

		// Phase 4: Generate portal index and category pages using PortalGenerator
		await GeneratePortalPages(context, portalNavigation, ctx);

		return new PortalBuildResult(portalNavigation, buildContexts.Select(b => b.DocumentationSet).ToList());
	}

	private async Task<PortalDocumentationSetBuildContext?> LoadDocumentationSet(
		PortalContext context,
		PortalCheckout checkout,
		IFileSystem fileSystem,
		Cancel ctx)
	{
		_logger.LogInformation("Loading documentation set: {Name}", checkout.Reference.Name);

		try
		{
			// Calculate output path based on category
			var outputPath = string.IsNullOrEmpty(checkout.Reference.Category)
				? fileSystem.Path.Combine(context.OutputDirectory.FullName, checkout.Reference.Name)
				: fileSystem.Path.Combine(context.OutputDirectory.FullName, checkout.Reference.Category, checkout.Reference.Name);

			// Calculate URL path prefix
			var pathPrefix = string.IsNullOrEmpty(checkout.Reference.Category)
				? $"{context.Configuration.SitePrefix}/{checkout.Reference.Name}"
				: $"{context.Configuration.SitePrefix}/{checkout.Reference.Category}/{checkout.Reference.Name}";

			// Create git checkout information
			var git = new GitCheckoutInformation
			{
				Branch = checkout.Reference.Branch,
				Remote = checkout.Reference.ResolvedOrigin,
				Ref = checkout.CommitHash,
				RepositoryName = checkout.Reference.Name
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
				AllowIndexing = false
			};

			// Create cross-link resolver (simplified for portal - no external links)
			var crossLinkResolver = NoopCrossLinkResolver.Instance;

			// Create documentation set
			var documentationSet = new DocumentationSet(buildContext, logFactory, crossLinkResolver);
			await documentationSet.ResolveDirectoryTree(ctx);

			return new PortalDocumentationSetBuildContext(checkout, buildContext, documentationSet);
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
		PortalContext context,
		PortalDocumentationSetBuildContext buildContext,
		PortalNavigation portalNavigation,
		Cancel ctx)
	{
		_logger.LogInformation("Building documentation set: {Name}", buildContext.Checkout.Reference.Name);

		try
		{
			var exporters = ExportOptions.Default.CreateMarkdownExporters(logFactory, buildContext.BuildContext, "portal");
			var tasks = exporters.Select(async e => await e.StartAsync(ctx));
			await Task.WhenAll(tasks);

			var generator = new DocumentationGenerator(
				buildContext.DocumentationSet,
				logFactory,
				portalNavigation,
				null,
				null,
				exporters.ToArray());
			_ = await generator.GenerateAll(ctx);

			tasks = exporters.Select(async e => await e.StopAsync(ctx));
			await Task.WhenAll(tasks);
		}
		catch (Exception ex)
		{
			context.Collector.EmitError(context.ConfigurationPath,
				$"Failed to build documentation set '{buildContext.Checkout.Reference.Name}': {ex.Message}");
			_logger.LogError(ex, "Failed to build documentation set {Name}", buildContext.Checkout.Reference.Name);
		}
	}

	private async Task GeneratePortalPages(
		PortalContext context,
		PortalNavigation portalNavigation,
		Cancel ctx)
	{
		_logger.LogInformation("Generating portal pages");

		// Create a build context for the portal pages
		var portalBuildContext = new BuildContext(
			context.Collector,
			context.ReadFileSystem,
			context.WriteFileSystem,
			configurationContext,
			ExportOptions.Default,
			context.OutputDirectory.FullName,
			context.OutputDirectory.FullName,
			null)
		{
			UrlPathPrefix = context.Configuration.SitePrefix,
			Force = true,
			AllowIndexing = false
		};

		// Use PortalGenerator to render the portal pages
		var portalGenerator = new PortalGenerator(logFactory, portalBuildContext);
		await portalGenerator.Generate(portalNavigation, ctx);
	}
}

/// <summary>
/// Result of building a portal.
/// </summary>
/// <param name="Navigation">The portal navigation structure.</param>
/// <param name="DocumentationSets">The built documentation sets.</param>
public record PortalBuildResult(
	PortalNavigation Navigation,
	IReadOnlyList<DocumentationSet> DocumentationSets);

/// <summary>
/// Build context for a single documentation set within the portal.
/// </summary>
public record PortalDocumentationSetBuildContext(
	PortalCheckout Checkout,
	BuildContext BuildContext,
	DocumentationSet DocumentationSet);

/// <summary>
/// Documentation context adapter for portal navigation creation.
/// </summary>
internal sealed class PortalDocumentationContext(PortalContext portalContext) : IPortalDocumentationContext
{
	/// <inheritdoc />
	public IFileInfo ConfigurationPath => portalContext.ConfigurationPath;

	/// <inheritdoc />
	public IDiagnosticsCollector Collector => portalContext.Collector;

	/// <inheritdoc />
	public IFileSystem ReadFileSystem => portalContext.ReadFileSystem;

	/// <inheritdoc />
	public IFileSystem WriteFileSystem => portalContext.WriteFileSystem;

	/// <inheritdoc />
	public IDirectoryInfo OutputDirectory => portalContext.OutputDirectory;

	/// <inheritdoc />
	public bool AssemblerBuild => false;

	/// <inheritdoc />
	public void EmitError(string message) =>
		portalContext.Collector.EmitError(portalContext.ConfigurationPath, message);
}
