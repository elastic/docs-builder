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
public class PortalGenerator(ILoggerFactory logFactory, BuildContext context)
{
	private readonly ILogger _logger = logFactory.CreateLogger<PortalGenerator>();
	private readonly IFileSystem _writeFileSystem = context.WriteFileSystem;
	private readonly StaticFileContentHashProvider _contentHashProvider = new(new EmbeddedOrPhysicalFileProvider(context));

	/// <summary>
	/// Generates all portal pages (landing and category pages).
	/// </summary>
	public async Task Generate(PortalNavigation portalNavigation, Cancel ctx = default)
	{
		_logger.LogInformation("Generating portal pages for {Title}", portalNavigation.NavigationTitle);

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

		await using var stream = _writeFileSystem.FileStream.New(outputFile.FullName, FileMode.Create);
		var slice = LandingView.Create(viewModel);
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
		var fileName = Regex.Replace(url + "/" + indexHtml, $"^{context.UrlPathPrefix}", string.Empty);
		return _writeFileSystem.FileInfo.New(Path.Combine(context.OutputDirectory.FullName, fileName.Trim('/')));
	}
}

/// <summary>
/// Navigation HTML writer for portal builds.
/// </summary>
internal sealed class PortalNavigationHtmlWriter(BuildContext context, PortalNavigation portalNavigation)
	: IsolatedBuildNavigationHtmlWriter(context, portalNavigation);
