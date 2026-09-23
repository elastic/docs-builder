// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Concurrent;
using System.IO.Abstractions;
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

internal sealed record ResolvedProductDocuments(
	IReadOnlyList<VersionedOpenApiDocument> Documents,
	ApiSpecVersion? UnmatchedBaseFilesVersion
);

internal sealed record ApiProductGeneration(
	string Prefix,
	OpenApiDocument Document,
	ResolvedApiConfiguration? ApiConfig,
	IReadOnlyList<ApiVersionSwitcherItem> VersionSwitcherItems,
	ApiSpecVersion SpecVersion,
	bool EmitUnmatchedBaseFiles,
	int? SupplementalMajor,
	IReadOnlyList<ApiCatalogEntry> CatalogEntries,
	string? CurrentApiKey
);

/// <summary>
/// Renders API explorer pages for every configured OpenAPI specification: builds the navigation
/// tree via <see cref="ApiNavigationBuilder"/> and writes each page to the output directory.
/// </summary>
/// <remarks>
/// For versioned products, renders the latest tree at the unversioned path plus one
/// full tree per released numeric major at <c>/vN/</c>. Versionless products render only
/// latest. When more than one version is rendered, assembler pages host the Docs
/// <c>version-dropdown</c> on the secondary top bar. Isolated builds keep a left-nav switcher.
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
		var declaredEntries = ApiHubSwitcher.CollectDeclaredEntries(context.UrlPathPrefix, context.Configuration.ApiConfigurations);
		var catalogEntries = await GenerateProducts(hubEntries: declaredEntries, ctx).ConfigureAwait(false);
		if (catalogEntries.Count > 0)
			await GenerateCatalog(catalogEntries, ctx).ConfigureAwait(false);
	}

	/// <summary>
	/// Renders every configured API product for this build context and returns catalog entries.
	/// Does not write the combined API catalog page.
	/// </summary>
	public async Task<IReadOnlyList<ApiCatalogEntry>> GenerateProducts(
		IReadOnlyList<ApiCatalogEntry>? hubEntries = null,
		Cancel ctx = default
	)
	{
		if (context.Configuration.ApiConfigurations is null)
			return [];

		var catalogForSwitcher = hubEntries
			?? ApiHubSwitcher.CollectDeclaredEntries(context.UrlPathPrefix, context.Configuration.ApiConfigurations);

		// Fan out every API product in parallel.  Results collected into a ConcurrentBag then sorted
		// back to the original config declaration order so the catalog page is stable build-to-build.
		var catalogEntriesBag = new ConcurrentBag<(int Order, ApiCatalogEntry Entry)>();
		var apiConfigs = context.Configuration.ApiConfigurations.Select((kv, i) => (Index: i, kv.Key, kv.Value)).ToList();

		await Parallel.ForEachAsync(apiConfigs, new ParallelOptions
		{
			CancellationToken = ctx,
			MaxDegreeOfParallelism = Environment.ProcessorCount
		}, async (item, token) =>
		{
			var (order, prefix, apiConfig) = item;
			try
			{
				var entry = await GenerateProduct(prefix, apiConfig, catalogForSwitcher, token).ConfigureAwait(false);
				if (entry is not null)
					catalogEntriesBag.Add((order, entry));
			}
			catch (Exception ex) when (ex is not OperationCanceledException)
			{
				context.Collector.EmitGlobalError($"API '{prefix}' could not be generated: {ex.Message}");
			}
		}).ConfigureAwait(false);

		return [.. catalogEntriesBag.OrderBy(x => x.Order).Select(x => x.Entry)];
	}

	/// <summary>
	/// Writes the combined API catalog page once from entries collected across one or more owners.
	/// </summary>
	public Task GenerateCatalog(IReadOnlyList<ApiCatalogEntry> entries, Cancel ctx = default) =>
		entries.Count == 0 ? Task.CompletedTask : GenerateApiCatalog(entries, ctx);

	private async Task<ApiCatalogEntry?> GenerateProduct(
		string prefix,
		ResolvedApiConfiguration apiConfig,
		IReadOnlyList<ApiCatalogEntry> hubEntries,
		Cancel ctx
	)
	{
		var resolved = await ResolveDocumentsForProduct(prefix, apiConfig, ctx).ConfigureAwait(false);
		if (resolved.Documents.Count == 0)
			return null;

		var versionedDocuments = resolved.Documents;
		var specVersions = versionedDocuments.Select(v => v.Version.SpecVersion).ToArray();
		var highestMajor = specVersions.Max(v => v.TryGetMajor(out var major) ? major : default(int?));

		foreach (var versioned in versionedDocuments)
		{
			ctx.ThrowIfCancellationRequested();
			var specVersion = versioned.Version.SpecVersion;
			var switcherItems = ApiVersionSwitcher.Build(context.UrlPathPrefix, prefix, specVersions, specVersion);
			var apiUrlSuffix = ApiUrlBuilder.ProductSuffix(prefix, specVersion);
			await GenerateApiProduct(
				new(
					apiUrlSuffix,
					versioned.Document,
					apiConfig,
					switcherItems,
					specVersion,
					EmitUnmatchedBaseFiles: specVersion == resolved.UnmatchedBaseFilesVersion,
					SupplementalMajor: SupplementalMajor(specVersion, highestMajor),
					CatalogEntries: hubEntries,
					CurrentApiKey: prefix
				),
				ctx
			).ConfigureAwait(false);
		}

		var canonical = versionedDocuments.FirstOrDefault(v => v.Version.SpecVersion.IsLatest) ?? versionedDocuments[0];
		var title = canonical.Document.Info?.Title ?? apiConfig.Product.DisplayName ?? prefix;
		var url = $"{ApiUrlBuilder.ProductRoot(context.UrlPathPrefix, prefix)}/";
		return new ApiCatalogEntry(prefix, title, url, apiConfig.Product.Id, canonical.Document.Info?.Description)
		{
			CatalogCategories = apiConfig.CatalogCategories
		};
	}

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

		var versionsToRender = versionless ? versions.Where(v => v.SpecVersion.IsLatest).ToArray() : [.. versions];

		if (versionsToRender.Length == 0)
			return new([], null);

		if (!versionless && versionsToRender.All(v => !v.SpecVersion.IsLatest) && versions.Count > 0)
		{
			context.Collector.EmitGlobalWarning(
				$"Version index for API '{apiKey}' has no 'main' entry; the unversioned path will not be rendered."
			);
		}

		var latestDeclared = versionsToRender.FirstOrDefault(v => v.SpecVersion.IsLatest)?.SpecVersion ?? versionsToRender[0].SpecVersion;

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
			new(
				new ResolvedApiVersion { SpecVersion = ApiSpecVersion.Latest, Branch = "main", IsLocal = true, LocalFile = localFile },
				document
			)
		];
		return ToResolvedProductDocuments(documents, ApiSpecVersion.Latest);
	}

	private static ResolvedProductDocuments ToResolvedProductDocuments(
		IReadOnlyList<VersionedOpenApiDocument> documents,
		ApiSpecVersion latestDeclared
	) => new(documents, documents.Any(d => d.Version.SpecVersion == latestDeclared) ? latestDeclared : null);

	private static bool IsVersionlessProduct(Product product) => product.VersioningSystem?.IsVersionless == true;

	internal static int? SupplementalMajor(ApiSpecVersion version, int? highestNumericMajor) =>
		version.TryGetMajor(out var major) ? major : highestNumericMajor;

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

	private static readonly OpenApiDocument CatalogDocument = new()
	{
		Info = new OpenApiInfo { Title = ApiCatalog.PageTitle, Version = "1.0" }
	};

	private async Task GenerateApiCatalog(IReadOnlyList<ApiCatalogEntry> entries, Cancel ctx)
	{
		var catalogUrl = $"{ApiUrlBuilder.ApiRoot(context.UrlPathPrefix)}/";
		var navigation = new ApiCatalogNavigationItem(catalogUrl, entries);
		var navigationRenderer = new IsolatedBuildNavigationHtmlWriter(context, navigation, suppressNavigationDropdown: true);

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
			new(generation.Document, context.Collector, generation.SpecVersion, EmitUnmatchedBaseFiles: generation.EmitUnmatchedBaseFiles)
		);
		var navigation = CreateNavigation(
			generation.Prefix,
			generation.Document,
			generation.ApiConfig,
			versionMajor: generation.SupplementalMajor
		);
		_logger.LogInformation("Generating OpenApiDocument {Title}", generation.Document.Info?.Title ?? "<no title>");

		var navigationRenderer = new IsolatedBuildNavigationHtmlWriter(
			context,
			navigation,
			MapVersionSwitcher(generation.VersionSwitcherItems),
			suppressNavigationDropdown: true
		);

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
			CatalogEntries = generation.CatalogEntries,
			CurrentApiKey = generation.CurrentApiKey,
			OperationSupplemental = operations,
			TagSupplemental = tags,
			Product = generation.ApiConfig?.Product
		};

		await RenderNavigationItems(renderContext, navigationRenderer, navigation, ctx).ConfigureAwait(false);
		await WriteSpecDownloads(navigation, generation.Document, ctx).ConfigureAwait(false);
	}

	private async Task WriteSpecDownloads(INavigationItem landing, OpenApiDocument document, Cancel ctx)
	{
		await WriteSpecSibling(
			ApiOutputPaths.RelativeJsonFile(landing.Url, context.UrlPathPrefix),
			(stream, token) => document.SerializeAsJsonAsync(stream, OpenApiSpecVersion.OpenApi3_1, token),
			ctx
		).ConfigureAwait(false);
		await WriteSpecSibling(
			ApiOutputPaths.RelativeYamlFile(landing.Url, context.UrlPathPrefix),
			(stream, token) => document.SerializeAsYamlAsync(stream, OpenApiSpecVersion.OpenApi3_1, token),
			ctx
		).ConfigureAwait(false);
	}

	private async Task WriteSpecSibling(string relativeFile, Func<Stream, Cancel, Task> write, Cancel ctx)
	{
		var file = _writeFileSystem.FileInfo.New(Path.Join(context.OutputDirectory.FullName, relativeFile));
		try
		{
			file.Directory!.Create();
		}
		catch (IOException) { }

		await using var stream = _writeFileSystem.FileStream.New(file.FullName, FileMode.Create);
		await write(stream, ctx).ConfigureAwait(false);
	}

	private static IReadOnlyList<NavigationSelectOption> MapVersionSwitcher(IReadOnlyList<ApiVersionSwitcherItem> items) =>
		items.Count <= 1 ? [] : [.. items.Select(static i => new NavigationSelectOption(i.Label, i.Url, i.Selected))];

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
		if (currentNavigation is ISidebarSeparatorNavigationItem)
			return;

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
		// Use CreateIfNotExists-style pattern: another concurrent render may already have created it.
		try
		{
			outputFile.Directory!.Create();
		}
		catch (IOException) { }

		var navigationRenderResult = await navigationRenderer.RenderNavigation(current.NavigationRoot, current, ctx);
		renderContext = renderContext with { CurrentNavigation = current, NavigationHtml = navigationRenderResult.Html };
		await using var stream = _writeFileSystem.FileStream.New(outputFile.FullName, FileMode.OpenOrCreate);

		// Build the expensive page model once and pass it to both render paths so that
		// OperationPageModel.Create / SchemaPageModel.Create / StructuralViewModel.Create
		// are not called twice per page (once for HTML, once for CommonMark).
		if (page is IApiModel apiPage)
		{
			var pageModel = apiPage.CreatePageModel(renderContext);
			await apiPage.RenderAsync(stream, renderContext, pageModel, ctx);
			await WriteCommonMark(current, apiPage, renderContext, pageModel, ctx).ConfigureAwait(false);
		}
		else
		{
			await page.RenderAsync(stream, renderContext, ctx);
		}

		return outputFile;

		IFileInfo OutputFile(INavigationItem currentNavigation)
		{
			var fileName = ApiOutputPaths.RelativeHtmlFile(currentNavigation.Url, context.UrlPathPrefix);
			return _writeFileSystem.FileInfo.New(Path.Join(context.OutputDirectory.FullName, fileName));
		}
	}

	private async Task WriteCommonMark(
		INavigationItem current,
		IApiModel page,
		ApiRenderContext renderContext,
		object? pageModel,
		Cancel ctx
	)
	{
		var markdown = await page.RenderCommonMarkAsync(renderContext, pageModel, ctx).ConfigureAwait(false);
		if (string.IsNullOrEmpty(markdown))
			return;

		markdown = ApiMarkdownFrontMatter.Wrap(markdown, current, renderContext, page);

		var markdownFile = _writeFileSystem.FileInfo.New(
			Path.Join(context.OutputDirectory.FullName, ApiOutputPaths.RelativeMarkdownFile(current.Url, context.UrlPathPrefix))
		);
		// Another concurrent render may already have created the directory.
		try
		{
			markdownFile.Directory!.Create();
		}
		catch (IOException) { }

		await _writeFileSystem.File.WriteAllTextAsync(markdownFile.FullName, markdown, ctx).ConfigureAwait(false);
	}
}
