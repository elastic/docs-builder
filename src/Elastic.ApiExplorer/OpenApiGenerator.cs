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
/// <remarks>
/// Only renders the current tree for each API: a local override file when the docset carries one,
/// otherwise the <c>main</c> moniker resolved remotely through <see cref="VersionIndexClient"/>. There
/// is no version-prefixed output or switcher yet — see issue #721 for multi-version generation.
/// </remarks>
public class OpenApiGenerator(
	ILoggerFactory logFactory,
	BuildContext context,
	IMarkdownStringRenderer markdownStringRenderer,
	VersionIndexClient? versionIndexClient = null,
	IOpenApiSpecificationReader? openApiReader = null)
{
	private readonly ILogger _logger = logFactory.CreateLogger<OpenApiGenerator>();
	private readonly IFileSystem _writeFileSystem = context.WriteFileSystem;
	private readonly StaticFileContentHashProvider _contentHashProvider = new(new EmbeddedOrPhysicalFileProvider(context));
	private readonly VersionIndexClient _versionIndexClient = versionIndexClient ?? new VersionIndexClient();
	private readonly IOpenApiSpecificationReader _openApiReader = openApiReader ?? OpenApiReader.Instance;

	public LandingNavigationItem CreateNavigation(string apiUrlSuffix, OpenApiDocument openApiDocument, ResolvedApiConfiguration? apiConfig = null) =>
		new ApiNavigationBuilder(_logger, context).CreateNavigation(apiUrlSuffix, openApiDocument, apiConfig);

	public async Task Generate(Cancel ctx = default)
	{
		if (context.Configuration.ApiConfigurations is null)
			return;

		var catalogEntries = new List<ApiCatalogEntry>();

		foreach (var (prefix, apiConfig) in context.Configuration.ApiConfigurations)
		{
			try
			{
				var openApiDocument = await ResolveCurrentDocument(prefix, apiConfig, ctx).ConfigureAwait(false);
				if (openApiDocument is null)
					continue;

				await GenerateApiProduct(prefix, openApiDocument, apiConfig, ctx);

				var title = openApiDocument.Info?.Title
					?? apiConfig.Product.DisplayName
					?? prefix;
				var url = $"{context.UrlPathPrefix}/api/{prefix}/";
				catalogEntries.Add(new ApiCatalogEntry(prefix, title, url));
			}
			catch (Exception ex) when (ex is not OperationCanceledException)
			{
				context.Collector.EmitGlobalError(
					$"API '{prefix}' could not be generated: {ex.Message}");
			}
		}

		if (catalogEntries.Count > 0)
			await GenerateApiCatalog(catalogEntries, ctx).ConfigureAwait(false);
	}

	/// <summary>
	/// Resolves the document to render for the current tree: the local override file when present,
	/// otherwise the <c>main</c> moniker fetched remotely through the version index. Returns null when
	/// nothing could be resolved; <see cref="VersionIndexClient"/> has already emitted the diagnostic.
	/// </summary>
	internal async Task<OpenApiDocument?> ResolveCurrentDocument(string apiKey, ResolvedApiConfiguration apiConfig, Cancel ctx)
	{
		if (apiConfig.LocalSpecFile is { } localFile)
			return await _openApiReader.ReadAsync(localFile);

		var versions = await _versionIndexClient.ResolveVersionsAsync(context.Git, apiKey, apiConfig, context.Collector, ctx).ConfigureAwait(false);
		var current = versions.FirstOrDefault(v => v.Moniker == "main");
		if (current is null)
		{
			if (versions.Count > 0)
			{
				context.Collector.EmitGlobalWarning(
					$"Version index for API '{apiKey}' has no 'main' entry; this API will not be rendered.");
			}
			return null;
		}

		if (current.IsLocal)
			return await _openApiReader.ReadAsync(current.LocalFile!);

		var stream = await _versionIndexClient.FetchSpecStreamAsync(apiKey, current, context.Collector, ctx).ConfigureAwait(false);
		if (stream is null)
			return null;

		return await _openApiReader.ReadAsync(stream, apiConfig.SpecFileName).ConfigureAwait(false);
	}

	private static readonly OpenApiDocument CatalogDocument = new()
	{
		Info = new OpenApiInfo { Title = "API Explorer", Version = "1.0" }
	};

	private async Task GenerateApiCatalog(IReadOnlyList<ApiCatalogEntry> entries, Cancel ctx)
	{
		var catalogUrl = $"{context.UrlPathPrefix}/api/";
		var navigation = new ApiCatalogNavigationItem(catalogUrl, entries);
		var navigationRenderer = new IsolatedBuildNavigationHtmlWriter(context, navigation);

		var renderContext = new ApiRenderContext(context, CatalogDocument, _contentHashProvider)
		{
			NavigationHtml = string.Empty,
			CurrentNavigation = navigation.Index,
			MarkdownRenderer = markdownStringRenderer,
			ApiExplorerLog = _logger
		};

		_ = await Render(navigation.Index, navigation.Index.Model, renderContext, navigationRenderer, ctx).ConfigureAwait(false);
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

		await RenderNavigationItems(renderContext, navigationRenderer, navigation, ctx);
	}

	private async Task RenderNavigationItems(
		ApiRenderContext renderContext,
		IsolatedBuildNavigationHtmlWriter navigationRenderer,
		INavigationItem currentNavigation,
		Cancel ctx)
	{
		if (currentNavigation is INodeNavigationItem<IApiModel, INavigationItem> node)
		{
			if (currentNavigation is not ClassificationNavigationItem)
				_ = await Render(node, node.Index.Model, renderContext, navigationRenderer, ctx);

			foreach (var child in node.NavigationItems)
				await RenderNavigationItems(renderContext, navigationRenderer, child, ctx);
		}
		else
		{
			_ = currentNavigation is ILeafNavigationItem<IApiModel> leaf
				? await Render(leaf, leaf.Model, renderContext, navigationRenderer, ctx)
				: throw new Exception($"Unknown navigation item type {currentNavigation.GetType()}");
		}
	}

	private async Task<IFileInfo> Render<T>(INavigationItem current, T page, ApiRenderContext renderContext,
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
