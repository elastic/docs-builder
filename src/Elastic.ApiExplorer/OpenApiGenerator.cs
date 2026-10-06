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

internal sealed record ResolvedProductDocuments(IReadOnlyList<VersionedOpenApiDocument> Documents, string? UnmatchedBaseFilesMoniker);

internal sealed record ApiProductGeneration(
	string Prefix,
	OpenApiDocument Document,
	ResolvedApiConfiguration? ApiConfig,
	IReadOnlyList<ApiVersionSwitcherItem> VersionSwitcherItems,
	string Moniker,
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
/// For versioned products, renders the canonical <c>main</c> tree at the unversioned path plus one
/// full tree per released numeric major at <c>/vN/</c>. Versionless products render only
/// <c>main</c>. When more than one version is rendered, assembler pages host the Docs
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
	private readonly ConcurrentDictionary<string, Task<ResolvedProductDocuments>> _documents = new(StringComparer.OrdinalIgnoreCase);

	public LandingNavigationItem CreateNavigation(
		string apiUrlSuffix,
		OpenApiDocument openApiDocument,
		ResolvedApiConfiguration? apiConfig = null,
		int? versionMajor = null
	) => new ApiNavigationBuilder(_logger, context).CreateNavigation(apiUrlSuffix, openApiDocument, apiConfig, versionMajor);

	public async Task Generate(Cancel ctx = default)
	{
		var catalogEntries = await GenerateProducts(ctx: ctx).ConfigureAwait(false);
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

		// The switcher label is the landing-page h1 (OpenAPI info.title). Resolve specs first so
		// every page can list sibling APIs by that name. Callers that pass hubEntries, such as the
		// assembler, have already resolved those titles.
		var catalogForSwitcher = hubEntries ?? await ResolveCatalogEntries(ctx).ConfigureAwait(false);

		// Fan out every API product in parallel.  Results collected into a ConcurrentBag then sorted
		// back to the original config declaration order so the catalog page is stable build-to-build.
		var catalogEntriesBag = new ConcurrentBag<(int Order, ApiCatalogEntry Entry)>();
		var apiConfigs = ApiConfigsToRender();

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
	/// Loads each configured spec and returns catalog entries labeled with the landing-page heading.
	/// Does not write pages. Later <see cref="GenerateProducts"/> calls reuse the loaded documents.
	/// </summary>
	public async Task<IReadOnlyList<ApiCatalogEntry>> ResolveCatalogEntries(Cancel ctx = default)
	{
		if (context.Configuration.ApiConfigurations is null)
			return [];

		var entries = new ConcurrentBag<(int Order, ApiCatalogEntry Entry)>();
		var apiConfigs = ApiConfigsToRender();
		await Parallel.ForEachAsync(apiConfigs, new ParallelOptions
		{
			CancellationToken = ctx,
			MaxDegreeOfParallelism = Environment.ProcessorCount
		}, async (item, token) =>
		{
			var (order, prefix, apiConfig) = item;
			var resolved = await DocumentsFor(prefix, apiConfig, token).ConfigureAwait(false);
			var entry = CatalogEntry(prefix, apiConfig, resolved);
			if (entry is not null)
				entries.Add((order, entry));
		}).ConfigureAwait(false);

		return [.. entries.OrderBy(x => x.Order).Select(x => x.Entry)];
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
		var resolved = await DocumentsFor(prefix, apiConfig, ctx).ConfigureAwait(false);
		if (resolved.Documents.Count == 0)
			return null;

		var versionedDocuments = resolved.Documents;
		var monikers = versionedDocuments.Select(v => v.Version.Moniker).ToArray();
		var highestMajor = monikers.Max(TryParseMajor);

		// Each moniker gets an independent ApiRenderContext, navigation tree and navigation HTML
		// writer, so there is no shared mutable state between concurrent versions. The outer loop in
		// GenerateProducts fans out products only; rendering versions sequentially here made a
		// product's total time the sum of all its versions. Pages within one version stay sequential,
		// so concurrency is bounded by the number of product×version units, not ProcessorCount².
		await Parallel.ForEachAsync(versionedDocuments, ctx, async (versioned, token) =>
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
					SupplementalMajor: SupplementalMajor(versioned.Version.Moniker, highestMajor),
					CatalogEntries: hubEntries,
					CurrentApiKey: prefix
				),
				token
			).ConfigureAwait(false);
		}).ConfigureAwait(false);

		return CatalogEntry(prefix, apiConfig, resolved);
	}

	private List<(int Index, string Key, ResolvedApiConfiguration Value)> ApiConfigsToRender()
	{
		if (context.Configuration.ApiConfigurations is null)
			return [];

		// docs/_docset.yml declares docs-builder-* copies of product specs for isolated serve.
		// Assembler preview also loads that checkout, so those keys would list the same API twice.
		// Isolated builds have no such collision and serve the fixtures under the short product key.
		if (context.BuildType == BuildType.Assembler)
		{
			return context
				.Configuration
				.ApiConfigurations
				.Where(kv => !IsolatedApiAliases.IsFixtureKey(kv.Key))
				.Select((kv, i) => (i, kv.Key, kv.Value))
				.ToList();
		}

		var keys = context.Configuration.ApiConfigurations.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
		return context.Configuration.ApiConfigurations.Select((kv, i) => (i, UrlKey(kv.Key, keys), kv.Value)).ToList();

		static string UrlKey(string apiKey, HashSet<string> keys) =>
			IsolatedApiAliases.UrlKey(apiKey) is var shortKey && !keys.Contains(shortKey) ? shortKey : apiKey;
	}

	private Task<ResolvedProductDocuments> DocumentsFor(string apiKey, ResolvedApiConfiguration apiConfig, Cancel ctx) =>
		_documents.GetOrAdd(apiKey, _ => LoadDocuments(apiKey, apiConfig, ctx));

	private async Task<ResolvedProductDocuments> LoadDocuments(string apiKey, ResolvedApiConfiguration apiConfig, Cancel ctx)
	{
		try
		{
			var resolved = await ResolveDocumentsForProduct(apiKey, apiConfig, ctx).ConfigureAwait(false);
			if (resolved.Documents.Count == 0)
			{
				context.Collector.EmitGlobalWarning(
					$"API '{apiKey}': no documents could be loaded from the spec — check earlier errors for details."
				);
			}

			return resolved;
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			context.Collector.EmitGlobalError($"API '{apiKey}' could not be generated: {ex.Message}");
			return new([], null);
		}
	}

	private ApiCatalogEntry? CatalogEntry(string prefix, ResolvedApiConfiguration apiConfig, ResolvedProductDocuments resolved)
	{
		if (resolved.Documents.Count == 0)
			return null;

		var canonical = resolved.Documents.FirstOrDefault(v => v.Version.Moniker == "main") ?? resolved.Documents[0];
		var title = canonical.Document.Info?.Title ?? apiConfig.Product.DisplayName ?? prefix;
		var url = $"{ApiUrlBuilder.ProductRoot(context.UrlPathPrefix, prefix)}/";
		return new ApiCatalogEntry(prefix, title, url, apiConfig.Product.Id, canonical.Document.Info?.Description)
		{
			CatalogCategories = apiConfig.CatalogCategories
		};
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

		// Download and parse every version at once; results keep the declared version order.
		var documents = await Task.WhenAll(
			versionsToRender.Select(v => ResolveDocumentForVersion(apiKey, apiConfig, v, ctx))
		).ConfigureAwait(false);
		var results = versionsToRender
			.Zip(documents, static (version, document) => document is null ? null : new VersionedOpenApiDocument(version, document))
			.OfType<VersionedOpenApiDocument>()
			.ToList();

		return ToResolvedProductDocuments(results, latestDeclared);
	}

	private async Task<ResolvedProductDocuments> ResolveLocalMainOnly(IFileInfo localFile)
	{
		var document = await _openApiReader.ReadAsync(localFile, context.Collector).ConfigureAwait(false);
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
			return await _openApiReader.ReadAsync(version.LocalFile!, context.Collector).ConfigureAwait(false);

		var stream = await _versionIndexClient.FetchSpecStreamAsync(apiKey, version, context.Collector, ctx).ConfigureAwait(false);
		if (stream is null)
			return null;

		return await _openApiReader.ReadAsync(stream, apiConfig.SpecFileName, context.Collector).ConfigureAwait(false);
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
			new(generation.Document, context.Collector, generation.Moniker, EmitUnmatchedBaseFiles: generation.EmitUnmatchedBaseFiles)
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

		ApplySupplementalIntroHeadings(navigation, tags);

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

	private static void ApplySupplementalIntroHeadings(INavigationItem item, IReadOnlyDictionary<string, ApiSupplementalDoc> tags)
	{
		if (item is not INodeNavigationItem<INavigationModel, INavigationItem> node)
			return;

		var children = node.NavigationItems.ToArray();
		if (
			item is TagNavigationItem tag
			&& tags.TryGetValue(tag.Index.Model.Name, out var doc)
			&& !string.IsNullOrWhiteSpace(doc.Description)
		)
			tag.ApplyIntroHeadings(doc.Description);

		foreach (var child in children)
			ApplySupplementalIntroHeadings(child, tags);
	}

	private async Task RenderNavigationItems(
		ApiRenderContext renderContext,
		IsolatedBuildNavigationHtmlWriter navigationRenderer,
		INavigationItem root,
		Cancel ctx
	)
	{
		var pages = new List<(INavigationItem Item, IApiModel Model)>();
		CollectPages(root, pages);

		// Pages of one version render in parallel. When two items share a URL the last one wins,
		// as it did when pages were written sequentially in depth-first order.
		var unique = pages.AsEnumerable().Reverse().DistinctBy(p => p.Item.Url).ToArray();
		await Parallel.ForEachAsync(
			unique,
			new ParallelOptions { CancellationToken = ctx, MaxDegreeOfParallelism = Environment.ProcessorCount },
			async (page, token) => _ = await Render(page.Item, page.Model, renderContext, navigationRenderer, token).ConfigureAwait(false)
		).ConfigureAwait(false);
	}

	private static void CollectPages(INavigationItem item, List<(INavigationItem Item, IApiModel Model)> pages)
	{
		if (item is ISidebarSeparatorNavigationItem or IntroHeadingNavigationItem)
			return;

		if (item is INodeNavigationItem<IApiModel, INavigationItem> node)
		{
			if (item is not ClassificationNavigationItem)
				pages.Add((node, node.Index.Model));
			foreach (var child in node.NavigationItems)
				CollectPages(child, pages);
			return;
		}

		if (item is not ILeafNavigationItem<IApiModel> leaf)
			throw new Exception($"Unknown navigation item type {item.GetType()}");
		pages.Add((leaf, leaf.Model));
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
		await using var stream = _writeFileSystem.FileStream.New(outputFile.FullName, FileMode.Create);

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
