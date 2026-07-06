// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.Text.RegularExpressions;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Landing;
using Elastic.ApiExplorer.Model;
using Elastic.ApiExplorer.Navigation;
using Elastic.ApiExplorer.Operations;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Navigation;
using Elastic.Documentation.Site.FileProviders;
using Elastic.Documentation.Site.Navigation;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer;

/// <summary>
/// Renders API explorer pages for every configured OpenAPI specification: builds the navigation
/// tree via <see cref="ApiNavigationBuilder"/> and writes each page to the output directory.
/// </summary>
public class OpenApiGenerator(ILoggerFactory logFactory, BuildContext context, IMarkdownStringRenderer markdownStringRenderer)
{
	private readonly ILogger _logger = logFactory.CreateLogger<OpenApiGenerator>();
	private readonly IFileSystem _writeFileSystem = context.WriteFileSystem;
	private readonly StaticFileContentHashProvider _contentHashProvider = new(new EmbeddedOrPhysicalFileProvider(context));

	public LandingNavigationItem CreateNavigation(string apiUrlSuffix, OpenApiDocument openApiDocument, ResolvedApiConfiguration? apiConfig = null) =>
		new ApiNavigationBuilder(_logger, context).CreateNavigation(apiUrlSuffix, openApiDocument, apiConfig);

	public async Task Generate(Cancel ctx = default)
	{
		// Use the new API configurations if available, otherwise fall back to legacy OpenApiSpecifications
		if (context.Configuration.ApiConfigurations is not null)
		{
			foreach (var (prefix, apiConfig) in context.Configuration.ApiConfigurations)
			{
				// Validate assumption of single spec per product
				if (apiConfig.SpecFiles.Count > 1)
					throw new InvalidOperationException($"API product '{prefix}' has {apiConfig.SpecFiles.Count} spec files, but only one spec file per product is currently supported.");

				var openApiDocument = await OpenApiReader.Create(apiConfig.PrimarySpecFile);
				if (openApiDocument is null)
					continue;

				await GenerateApiProduct(prefix, openApiDocument, apiConfig, ctx);
			}
		}
		else if (context.Configuration.OpenApiSpecifications is not null)
		{
			// Legacy fallback
			foreach (var (prefix, path) in context.Configuration.OpenApiSpecifications)
			{
				var openApiDocument = await OpenApiReader.Create(path);
				if (openApiDocument is null)
					continue;

				await GenerateApiProduct(prefix, openApiDocument, null, ctx);
			}
		}
	}

	private async Task GenerateApiProduct(string prefix, OpenApiDocument openApiDocument, ResolvedApiConfiguration? apiConfig, Cancel ctx)
	{
		var navigation = CreateNavigation(prefix, openApiDocument, apiConfig);
		_logger.LogInformation("Generating OpenApiDocument {Title}", openApiDocument.Info?.Title ?? "<no title>");

		var navigationRenderer = new IsolatedBuildNavigationHtmlWriter(context, navigation);

		var renderContext = new ApiRenderContext(context, openApiDocument, _contentHashProvider)
		{
			NavigationHtml = string.Empty,
			CurrentNavigation = navigation,
			MarkdownRenderer = markdownStringRenderer,
			ApiExplorerLog = _logger
		};

		await RenderNavigationItems(prefix, renderContext, navigationRenderer, navigation, navigation, ctx);
	}

	private async Task RenderNavigationItems(
		string prefix,
		ApiRenderContext renderContext,
		IsolatedBuildNavigationHtmlWriter navigationRenderer,
		INavigationItem currentNavigation,
		INavigationItem rootNavigation,
		Cancel ctx)
	{
		if (currentNavigation is INodeNavigationItem<IApiModel, INavigationItem> node)
		{
			if (currentNavigation is not ClassificationNavigationItem)
				_ = await Render(prefix, node, node.Index.Model, renderContext, navigationRenderer, ctx);

			foreach (var child in node.NavigationItems)
				await RenderNavigationItems(prefix, renderContext, navigationRenderer, child, rootNavigation, ctx);
		}
		else
		{
			_ = currentNavigation is ILeafNavigationItem<IApiModel> leaf
				? await Render(prefix, leaf, leaf.Model, renderContext, navigationRenderer, ctx)
				: throw new Exception($"Unknown navigation item type {currentNavigation.GetType()}");
		}
	}

#pragma warning disable IDE0060
	private async Task<IFileInfo> Render<T>(string prefix, INavigationItem current, T page, ApiRenderContext renderContext,
#pragma warning restore IDE0060
		IsolatedBuildNavigationHtmlWriter navigationRenderer, Cancel ctx)
		where T : INavigationModel, IPageRenderer<ApiRenderContext>
	{
		var outputFile = OutputFile(current);
		if (!outputFile.Directory!.Exists)
			outputFile.Directory.Create();

		var navigationRenderResult = await navigationRenderer.RenderNavigation(current.NavigationRoot, current, ctx);
		renderContext = renderContext with
		{
			CurrentNavigation = current,
			NavigationHtml = navigationRenderResult.Html
		};
		await using var stream = _writeFileSystem.FileStream.New(outputFile.FullName, FileMode.OpenOrCreate);
		await page.RenderAsync(stream, renderContext, ctx);
		return outputFile;

		IFileInfo OutputFile(INavigationItem currentNavigation)
		{
			const string indexHtml = "index.html";
			var fileName = Regex.Replace(currentNavigation.Url + "/" + indexHtml, $"^{context.UrlPathPrefix}", string.Empty);
			var fileInfo = _writeFileSystem.FileInfo.New(Path.Join(context.OutputDirectory.FullName, fileName.Trim('/')));
			return fileInfo;
		}
	}
}
