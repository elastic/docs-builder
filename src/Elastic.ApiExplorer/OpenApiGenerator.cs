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
using Elastic.Documentation.Configuration.Products;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Navigation;
using Elastic.Documentation.Site.FileProviders;
using Elastic.Documentation.Site.Navigation;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer;

/// <summary>
/// One resolved OpenAPI document for a version-index moniker, with the URL suffix used for navigation and output.
/// </summary>
internal sealed record VersionedOpenApiDocument(
	ResolvedApiVersion Version,
	OpenApiDocument Document,
	string ApiUrlSuffix);

/// <summary>
/// Renders API explorer pages for every configured OpenAPI specification: builds the navigation
/// tree via <see cref="ApiNavigationBuilder"/> and writes each page to the output directory.
/// </summary>
/// <remarks>
/// For versioned products, renders the canonical <c>main</c> tree at the unversioned path plus one
/// full tree per released numeric major at <c>/vN/</c>. Versionless products render only
/// <c>main</c>. The version switcher UI is tracked separately in issue #723.
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
				var versionedDocuments = await ResolveDocumentsForProduct(prefix, apiConfig, ctx).ConfigureAwait(false);
				if (versionedDocuments.Count == 0)
					continue;

				foreach (var versioned in versionedDocuments)
					await GenerateApiProduct(versioned.ApiUrlSuffix, versioned.Document, apiConfig, ctx).ConfigureAwait(false);

				var canonical = versionedDocuments.FirstOrDefault(v => v.Version.Moniker == "main")
					?? versionedDocuments[0];
				var title = canonical.Document.Info?.Title
					?? apiConfig.Product.DisplayName
					?? prefix;
				var url = $"{ApiUrlBuilder.ProductRoot(context.UrlPathPrefix, prefix)}/";
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
	/// Resolves every OpenAPI document to render for one API key, including canonical <c>main</c>
	/// and released numeric majors. Returns an empty list when nothing could be resolved.
	/// </summary>
	internal async Task<IReadOnlyList<VersionedOpenApiDocument>> ResolveDocumentsForProduct(
		string apiKey,
		ResolvedApiConfiguration apiConfig,
		Cancel ctx)
	{
		if (apiConfig.LocalSpecFile is { } localFile && IsVersionlessProduct(apiConfig.Product))
			return await ResolveLocalMainOnly(apiKey, localFile).ConfigureAwait(false);

		var versions = await _versionIndexClient.ResolveVersionsAsync(
			context.Git, apiKey, apiConfig, context.Collector, ctx).ConfigureAwait(false);

		var versionsToRender = IsVersionlessProduct(apiConfig.Product)
			? versions.Where(v => v.Moniker == "main").ToArray()
			: [.. versions];

		if (versionsToRender.Length == 0)
			return [];

		if (!IsVersionlessProduct(apiConfig.Product)
			&& versionsToRender.All(v => v.Moniker != "main")
			&& versions.Count > 0)
		{
			context.Collector.EmitGlobalWarning(
				$"Version index for API '{apiKey}' has no 'main' entry; the unversioned path will not be rendered.");
		}

		var results = new List<VersionedOpenApiDocument>(versionsToRender.Length);
		foreach (var version in versionsToRender)
		{
			var document = await ResolveDocumentForVersion(apiKey, apiConfig, version, ctx).ConfigureAwait(false);
			if (document is null)
				continue;

			results.Add(new VersionedOpenApiDocument(
				version,
				document,
				ApiUrlBuilder.ProductSuffix(apiKey, version.Moniker)));
		}

		return results;
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

		return await ResolveDocumentForVersion(apiKey, apiConfig, current, ctx).ConfigureAwait(false);
	}

	private async Task<IReadOnlyList<VersionedOpenApiDocument>> ResolveLocalMainOnly(string apiKey, IFileInfo localFile)
	{
		var document = await _openApiReader.ReadAsync(localFile).ConfigureAwait(false);
		if (document is null)
			return [];

		return
		[
			new VersionedOpenApiDocument(
				new ResolvedApiVersion
				{
					Moniker = "main",
					Version = "main",
					IsLocal = true,
					LocalFile = localFile
				},
				document,
				ApiUrlBuilder.ProductSuffix(apiKey, "main"))
		];
	}

	private static bool IsVersionlessProduct(Product product) =>
		product.VersioningSystem?.IsVersionless == true;

	private async Task<OpenApiDocument?> ResolveDocumentForVersion(
		string apiKey,
		ResolvedApiConfiguration apiConfig,
		ResolvedApiVersion version,
		Cancel ctx)
	{
		if (version.IsLocal)
			return await _openApiReader.ReadAsync(version.LocalFile!).ConfigureAwait(false);

		var stream = await _versionIndexClient.FetchSpecStreamAsync(apiKey, version, context.Collector, ctx).ConfigureAwait(false);
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
		var catalogUrl = $"{ApiUrlBuilder.ApiRoot(context.UrlPathPrefix)}/";
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

		await RenderNavigationItems(renderContext, navigationRenderer, navigation, ctx).ConfigureAwait(false);
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
