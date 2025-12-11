// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.Text.RegularExpressions;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Navigation;
using Elastic.Documentation.Site.FileProviders;
using Elastic.Documentation.Site.Navigation;
using Elastic.Portal.Category;
using Elastic.Portal.Landing;
using Elastic.Portal.Navigation;
using Microsoft.Extensions.Logging;
using RazorSlices;

namespace Elastic.Portal;

/// <summary>
/// Interface for portal models that can render themselves.
/// </summary>
public interface IPortalModel : INavigationModel, IPortalPageRenderer;

/// <summary>
/// Interface for rendering portal pages.
/// </summary>
public interface IPortalPageRenderer
{
	/// <summary>
	/// Renders the page to the given stream.
	/// </summary>
	Task RenderAsync(FileSystemStream stream, PortalRenderContext context, CancellationToken ctx = default);
}

/// <summary>
/// Generator for portal HTML pages.
/// </summary>
public class PortalGenerator(ILoggerFactory logFactory, BuildContext context, IDirectoryInfo outputDirectory)
{
	private readonly ILogger _logger = logFactory.CreateLogger<PortalGenerator>();
	private readonly IFileSystem _writeFileSystem = context.WriteFileSystem;
	private readonly StaticFileContentHashProvider _contentHashProvider = new(new EmbeddedOrPhysicalFileProvider(context));
	private readonly IDirectoryInfo _outputDirectory = outputDirectory;

	/// <summary>
	/// Generates all portal pages (landing and category pages).
	/// </summary>
	public async Task Generate(PortalNavigation portalNavigation, Cancel ctx = default)
	{
		_logger.LogInformation("Generating portal pages for {Title}", portalNavigation.NavigationTitle);

		// Extract static files to the portal's _static directory
		await ExtractEmbeddedStaticResources(ctx);

		var navigationRenderer = new PortalNavigationHtmlWriter(context, portalNavigation);

		var renderContext = new PortalRenderContext(context, portalNavigation, _contentHashProvider)
		{
			NavigationHtml = string.Empty,
			CurrentNavigation = portalNavigation.Index
		};

		// Render the portal landing page
		await RenderLandingPage(portalNavigation, renderContext, navigationRenderer, ctx);

		// Render category pages
		foreach (var item in portalNavigation.NavigationItems)
		{
			if (item is CategoryNavigation category)
			{
				await RenderCategoryPage(category, renderContext, navigationRenderer, ctx);
			}
		}
	}

	private async Task ExtractEmbeddedStaticResources(Cancel ctx)
	{
		_logger.LogInformation("Copying static files to portal output directory");
		var assembly = typeof(EmbeddedOrPhysicalFileProvider).Assembly;
		var embeddedStaticFiles = assembly
			.GetManifestResourceNames()
			.ToList();

		foreach (var resourceName in embeddedStaticFiles)
		{
			await using var resourceStream = assembly.GetManifestResourceStream(resourceName);
			if (resourceStream == null)
				continue;

			// Convert resource name to file path: Elastic.Documentation.Site._static.file.ext -> _static/file.ext
			var path = resourceName
				.Replace("Elastic.Documentation.Site.", "")
				.Replace("_static.", $"_static{Path.DirectorySeparatorChar}");

			// Output to portal's URL prefix directory (e.g., internal-docs/_static/)
			var outputPath = Path.Combine(
				_outputDirectory.FullName,
				context.UrlPathPrefix?.Trim('/') ?? string.Empty,
				path);

			var outputFile = _writeFileSystem.FileInfo.New(outputPath);
			if (outputFile.Directory is { Exists: false })
				outputFile.Directory.Create();

			await using var stream = outputFile.OpenWrite();
			await resourceStream.CopyToAsync(stream, ctx);
			_logger.LogDebug("Copied static embedded resource {Path}", path);
		}
	}

	private async Task RenderLandingPage(
		PortalNavigation portalNavigation,
		PortalRenderContext renderContext,
		PortalNavigationHtmlWriter navigationRenderer,
		CancellationToken ctx)
	{
		var navigationRenderResult = await navigationRenderer.RenderNavigation(
			portalNavigation,
			portalNavigation.Index,
			INavigationHtmlWriter.AllLevels,
			ctx);

		renderContext = renderContext with
		{
			CurrentNavigation = portalNavigation.Index,
			NavigationHtml = navigationRenderResult.Html
		};

		var viewModel = new LandingViewModel(renderContext)
		{
			IndexPage = (PortalIndexPage)portalNavigation.Index.Model
		};

		var outputFile = GetOutputFile(portalNavigation.Url);
		if (!outputFile.Directory!.Exists)
			outputFile.Directory.Create();

		var slice = LandingView.Create(viewModel);

		await using var stream = _writeFileSystem.FileStream.New(outputFile.FullName, FileMode.Create);
		await slice.RenderAsync(stream, cancellationToken: ctx);


		_logger.LogDebug("Generated portal landing page: {Path}", outputFile.FullName);
	}

	private async Task RenderCategoryPage(
		CategoryNavigation category,
		PortalRenderContext renderContext,
		PortalNavigationHtmlWriter navigationRenderer,
		CancellationToken ctx)
	{
		var navigationRenderResult = await navigationRenderer.RenderNavigation(
			category.NavigationRoot,
			category.Index,
			INavigationHtmlWriter.AllLevels,
			ctx);

		renderContext = renderContext with
		{
			CurrentNavigation = category.Index,
			NavigationHtml = navigationRenderResult.Html
		};

		var viewModel = new CategoryViewModel(renderContext)
		{
			Category = category
		};

		var outputFile = GetOutputFile(category.Url);
		if (!outputFile.Directory!.Exists)
			outputFile.Directory.Create();

		await using var stream = _writeFileSystem.FileStream.New(outputFile.FullName, FileMode.Create);
		var slice = CategoryView.Create(viewModel);
		await slice.RenderAsync(stream, cancellationToken: ctx);

		_logger.LogDebug("Generated category page: {Path}", outputFile.FullName);
	}

	private IFileInfo GetOutputFile(string url)
	{
		const string indexHtml = "index.html";
		// Keep the full URL path so file structure matches URLs
		var fileName = url.TrimStart('/') + "/" + indexHtml;
		return _writeFileSystem.FileInfo.New(Path.Combine(_outputDirectory.FullName, fileName.Trim('/')));
	}
}

/// <summary>
/// Navigation HTML writer for portal builds.
/// </summary>
internal sealed class PortalNavigationHtmlWriter(BuildContext context, PortalNavigation portalNavigation)
	: IsolatedBuildNavigationHtmlWriter(context, portalNavigation);
