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
using Elastic.ApiExplorer.Supplemental;
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

internal sealed record VersionedOpenApiDocument(ResolvedApiVersion Version, OpenApiDocument Document);

internal sealed record ResolvedProductDocuments(IReadOnlyList<VersionedOpenApiDocument> Documents, string? UnmatchedBaseFilesMoniker);

internal sealed record ApiProductGeneration(
	string Prefix,
	OpenApiDocument Document,
	ResolvedApiConfiguration? ApiConfig,
	IReadOnlyList<ApiVersionSwitcherItem> VersionSwitcherItems,
	string Moniker,
	bool EmitUnmatchedBaseFiles,
	int? SupplementalMajor
);

/// <summary>
/// Renders API explorer pages for every configured OpenAPI specification: builds the navigation
/// tree via <see cref="ApiNavigationBuilder"/> and writes each page to the output directory.
/// </summary>
/// <remarks>
/// For versioned products, renders the canonical <c>main</c> tree at the unversioned path plus one
/// full tree per released numeric major at <c>/vN/</c>. Versionless products render only
/// <c>main</c>. When more than one version is rendered, pages include a left-nav version switcher.
/// </remarks>
public class OpenApiGenerator(
	ILoggerFactory logFactory,
	BuildContext context,
	IMarkdownStringRenderer markdownStringRenderer,
	VersionIndexClient? versionIndexClient = null,
	IOpenApiSpecificationReader? openApiReader = null
)
{
	private readonly ILogger _logger = logFactory.CreateLogger<OpenApiGenerator>();
	private readonly IFileSystem _writeFileSystem = context.WriteFileSystem;
	private readonly StaticFileContentHashProvider _contentHashProvider = new(new EmbeddedOrPhysicalFileProvider(context));
	private readonly VersionIndexClient _versionIndexClient = versionIndexClient ?? new VersionIndexClient();
	private readonly IOpenApiSpecificationReader _openApiReader = openApiReader ?? OpenApiReader.Instance;

	public LandingNavigationItem CreateNavigation(
		string apiUrlSuffix,
		OpenApiDocument openApiDocument,
		ResolvedApiConfiguration? apiConfig = null,
		int? versionMajor = null
	) => new ApiNavigationBuilder(_logger, context).CreateNavigation(apiUrlSuffix, openApiDocument, apiConfig, versionMajor);

	public async Task Generate(Cancel ctx = default)
	{
		var catalogEntries = await GenerateProducts(ctx).ConfigureAwait(false);
		if (catalogEntries.Count > 0)
			await GenerateCatalog(catalogEntries, ctx).ConfigureAwait(false);
	}

	/// <summary>
	/// Renders every configured API product for this build context and returns catalog entries.
	/// Does not write the combined API catalog page.
	/// </summary>
	public async Task<IReadOnlyList<ApiCatalogEntry>> GenerateProducts(Cancel ctx = default)
	{
		if (context.Configuration.ApiConfigurations is null)
			return [];

		var catalogEntries = new List<ApiCatalogEntry>();

		foreach (var (prefix, apiConfig) in context.Configuration.ApiConfigurations)
		{
			try
			{
				var entry = await GenerateProduct(prefix, apiConfig, ctx).ConfigureAwait(false);
				if (entry is not null)
					catalogEntries.Add(entry);
			}
			catch (Exception ex) when (ex is not OperationCanceledException)
			{
				context.Collector.EmitGlobalError($"API '{prefix}' could not be generated: {ex.Message}");
			}
		}

		return catalogEntries;
	}

	/// <summary>
	/// Writes the combined API catalog page once from entries collected across one or more owners.
	/// </summary>
	public Task GenerateCatalog(IReadOnlyList<ApiCatalogEntry> entries, Cancel ctx = default) =>
		entries.Count == 0 ? Task.CompletedTask : GenerateApiCatalog(entries, ctx);

	private async Task<ApiCatalogEntry?> GenerateProduct(string prefix, ResolvedApiConfiguration apiConfig, Cancel ctx)
	{
		var resolved = await ResolveDocumentsForProduct(prefix, apiConfig, ctx).ConfigureAwait(false);
		if (resolved.Documents.Count == 0)
			return null;

		var versionedDocuments = resolved.Documents;
		var monikers = versionedDocuments.Select(v => v.Version.Moniker).ToArray();
		var highestMajor = monikers.Max(TryParseMajor);
		foreach (var versioned in versionedDocuments)
		{
			var switcherItems = ApiVersionSwitcher.Build(context.UrlPathPrefix, prefix, monikers, versioned.Version.Moniker);
			var apiUrlSuffix = ApiUrlBuilder.ProductSuffix(prefix, versioned.Version.Moniker);
			await GenerateApiProduct(
				new(
					apiUrlSuffix,
					versioned.Document,
					apiConfig,
					switcherItems,
					versioned.Version.Moniker,
					EmitUnmatchedBaseFiles: versioned.Version.Moniker == resolved.UnmatchedBaseFilesMoniker,
					SupplementalMajor: SupplementalMajor(versioned.Version.Moniker, highestMajor)
				),
				ctx
			).ConfigureAwait(false);
		}

		var canonical = versionedDocuments.FirstOrDefault(v => v.Version.Moniker == "main") ?? versionedDocuments[0];
		var title = canonical.Document.Info?.Title ?? apiConfig.Product.DisplayName ?? prefix;
		var url = $"{ApiUrlBuilder.ProductRoot(context.UrlPathPrefix, prefix)}/";
		return new ApiCatalogEntry(prefix, title, url);
	}

	/// <summary>
	/// Resolves every OpenAPI document to render for one API key, including canonical <c>main</c>
	/// and released numeric majors. Returns empty documents when nothing could be resolved.
	/// <see cref="ResolvedProductDocuments.UnmatchedBaseFilesMoniker"/> is the declared latest
	/// version only when that document actually resolved.
	/// </summary>
	internal async Task<ResolvedProductDocuments> ResolveDocumentsForProduct(string apiKey, ResolvedApiConfiguration apiConfig, Cancel ctx)
	{
		var versionless = IsVersionlessProduct(apiConfig.Product);
		if (apiConfig.LocalSpecFile is { } localFile && versionless)
			return await ResolveLocalMainOnly(localFile).ConfigureAwait(false);

		var versions = await _versionIndexClient.ResolveVersionsAsync(
			context.Git,
			apiKey,
			apiConfig,
			context.Collector,
			ctx
		).ConfigureAwait(false);

		var versionsToRender = versionless ? versions.Where(v => v.Moniker == "main").ToArray() : [.. versions];

		if (versionsToRender.Length == 0)
			return new([], null);

		if (!versionless && versionsToRender.All(v => v.Moniker != "main") && versions.Count > 0)
		{
			context.Collector.EmitGlobalWarning(
				$"Version index for API '{apiKey}' has no 'main' entry; the unversioned path will not be rendered."
			);
		}

		var latestDeclared = versionsToRender.Any(v => v.Moniker == "main") ? "main" : versionsToRender[0].Moniker;

		var results = new List<VersionedOpenApiDocument>(versionsToRender.Length);
		foreach (var version in versionsToRender)
		{
			var document = await ResolveDocumentForVersion(apiKey, apiConfig, version, ctx).ConfigureAwait(false);
			if (document is null)
				continue;

			results.Add(new VersionedOpenApiDocument(version, document));
		}

		return ToResolvedProductDocuments(results, latestDeclared);
	}

	private async Task<ResolvedProductDocuments> ResolveLocalMainOnly(IFileInfo localFile)
	{
		var document = await _openApiReader.ReadAsync(localFile).ConfigureAwait(false);
		if (document is null)
			return new([], null);

		VersionedOpenApiDocument[] documents =
		[
			new(new ResolvedApiVersion { Moniker = "main", Version = "main", IsLocal = true, LocalFile = localFile }, document)
		];
		return ToResolvedProductDocuments(documents, "main");
	}

	private static ResolvedProductDocuments ToResolvedProductDocuments(
		IReadOnlyList<VersionedOpenApiDocument> documents,
		string latestDeclared
	) => new(documents, documents.Any(d => d.Version.Moniker == latestDeclared) ? latestDeclared : null);

	private static bool IsVersionlessProduct(Product product) => product.VersioningSystem?.IsVersionless == true;

	/// <summary>
	/// Numeric monikers map 1:1. <c>main</c> uses the highest rendered numeric major so the
	/// unversioned URL matches the current-major overlay (the one page CLI authors expect).
	/// </summary>
	internal static int? SupplementalMajor(string moniker, int? highestNumericMoniker) =>
		TryParseMajor(moniker) ?? (moniker == "main" ? highestNumericMoniker : null);

	private static int? TryParseMajor(string moniker) => int.TryParse(moniker, out var major) ? major : null;

	private async Task<OpenApiDocument?> ResolveDocumentForVersion(
		string apiKey,
		ResolvedApiConfiguration apiConfig,
		ResolvedApiVersion version,
		Cancel ctx
	)
	{
		if (version.IsLocal)
			return await _openApiReader.ReadAsync(version.LocalFile!).ConfigureAwait(false);

		var stream = await _versionIndexClient.FetchSpecStreamAsync(apiKey, version, context.Collector, ctx).ConfigureAwait(false);
		if (stream is null)
			return null;

		return await _openApiReader.ReadAsync(stream, apiConfig.SpecFileName).ConfigureAwait(false);
	}

	private static readonly OpenApiDocument CatalogDocument = new() { Info = new OpenApiInfo { Title = "API Explorer", Version = "1.0" } };

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

	private async Task GenerateApiProduct(ApiProductGeneration generation, Cancel ctx)
	{
		var discovery = DiscoverSupplemental(generation.Document, generation.ApiConfig);
		ApiSupplementalValidator.Validate(
			discovery,
			new(generation.Document, context.Collector, generation.Moniker, EmitUnmatchedBaseFiles: generation.EmitUnmatchedBaseFiles)
		);
		var navigation = CreateNavigation(
			generation.Prefix,
			generation.Document,
			generation.ApiConfig,
			versionMajor: generation.SupplementalMajor
		);
		_logger.LogInformation("Generating OpenApiDocument {Title}", generation.Document.Info?.Title ?? "<no title>");

		var navigationRenderer = new IsolatedBuildNavigationHtmlWriter(context, navigation);

		var operations = ApiSupplementalDoc.Load(discovery.Operations);
		var tags = ApiSupplementalDoc.Load(discovery.Tags);
		if (generation.SupplementalMajor is { } major && discovery.VersionSuffixed.Count > 0)
		{
			var (_, tagNames) = ApiSupplementalDiscovery.CollectEntities(generation.Document);
			var (tagBySlug, _) = ApiSupplementalDiscovery.IndexTags(tagNames);
			operations = ApiSupplementalDoc.OverlayVersionFiles(
				operations,
				discovery.VersionSuffixed,
				major,
				(stem, kind) => kind == ApiSupplementalKind.Operation ? stem : null
			);
			tags = ApiSupplementalDoc.OverlayVersionFiles(
				tags,
				discovery.VersionSuffixed,
				major,
				(stem, kind) => kind == ApiSupplementalKind.Tag && tagBySlug.TryGetValue(stem, out var name) ? name : null
			);
		}

		var renderContext = new ApiRenderContext(context, generation.Document, _contentHashProvider)
		{
			NavigationHtml = string.Empty,
			CurrentNavigation = navigation,
			MarkdownRenderer = markdownStringRenderer,
			ApiExplorerLog = _logger,
			VersionSwitcherItems = generation.VersionSwitcherItems,
			OperationSupplemental = operations,
			TagSupplemental = tags
		};

		await RenderNavigationItems(renderContext, navigationRenderer, navigation, ctx).ConfigureAwait(false);
	}

	/// <summary>
	/// Associates <c>op-*.md</c> / <c>tag-*.md</c> files under <c>api/&lt;key&gt;/</c> with this
	/// document. Parsed matches are attached to the render context by the caller.
	/// </summary>
	internal ApiSupplementalDiscoveryResult DiscoverSupplemental(OpenApiDocument openApiDocument, ResolvedApiConfiguration? apiConfig)
	{
		var result = ApiSupplementalDiscovery.Discover(apiConfig?.ApiContentDirectory, openApiDocument);
		if (result.Operations.Count == 0 && result.Tags.Count == 0 && result.Unmatched.Count == 0)
			return result;

		_logger.LogInformation(
			"API '{ApiKey}' supplemental files: {Operations} operations, {Tags} tags, {Unmatched} unmatched",
			apiConfig?.ProductKey ?? "unknown",
			result.Operations.Count,
			result.Tags.Count,
			result.Unmatched.Count
		);
		return result;
	}

	private async Task RenderNavigationItems(
		ApiRenderContext renderContext,
		IsolatedBuildNavigationHtmlWriter navigationRenderer,
		INavigationItem currentNavigation,
		Cancel ctx
	)
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

	private async Task<IFileInfo> Render<T>(
		INavigationItem current,
		T page,
		ApiRenderContext renderContext,
		IsolatedBuildNavigationHtmlWriter navigationRenderer,
		Cancel ctx
	) where T : INavigationModel, IPageRenderer<ApiRenderContext>
	{
		var outputFile = OutputFile(current);
		if (!outputFile.Directory!.Exists)
			outputFile.Directory.Create();

		var navigationRenderResult = await navigationRenderer.RenderNavigation(current.NavigationRoot, current, ctx);
		renderContext = renderContext with { CurrentNavigation = current, NavigationHtml = navigationRenderResult.Html };
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
