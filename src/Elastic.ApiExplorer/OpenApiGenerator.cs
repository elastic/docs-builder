// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Abstractions;
using System.Text;
using Elastic.ApiExplorer.Export;
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
using Elastic.Documentation.Search;
using Elastic.Documentation.Site.FileProviders;
using Elastic.Documentation.Site.Navigation;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer;

internal sealed record VersionedOpenApiDocument(ResolvedApiVersion Version, OpenApiDocument Document);

/// <summary>
/// The specs of one API that loaded, and where each declared version is served. <see cref="Routes"/> comes from the
/// declared versions, so a newest major that fails to load leaves the versionless URL empty instead of moving an older one there.
/// </summary>
internal sealed record ResolvedProductDocuments(
	ApiVersionRoutes Routes,
	IReadOnlyList<VersionedOpenApiDocument> Documents,
	string? UnmatchedBaseFilesMoniker
)
{
	public static ResolvedProductDocuments Empty(string apiKey) => new(ApiVersionRoutes.For(apiKey, []), [], null);

	/// <summary>Trees the version index declares whose spec could not be fetched or parsed.</summary>
	public IReadOnlyList<string> UnresolvedMonikers { get; init; } = [];

	/// <summary>Trees the version index declares under a reserved name, such as <c>next</c>. They are not rendered.</summary>
	public IReadOnlyList<string> ReservedMonikers { get; init; } = [];
}

/// <summary>
/// One rendered page tree: a version's spec at one of its URL roots, <c>/api/{Suffix}</c>. <paramref name="IsMirror"/>
/// marks the numbered copy of the default major, which repeats the tree at the versionless root.
/// </summary>
internal sealed record ApiVersionTree(
	VersionedOpenApiDocument Version,
	string Suffix,
	bool IsMirror,
	ApiPageType PageType,
	LandingNavigationItem Navigation
)
{
	public string Moniker => Version.Version.Moniker;
}

/// <summary>
/// The page trees of one API, built once from the resolved specs, one per URL root: the default numeric major has two.
/// The floating section reads them for navigation and <see cref="OpenApiGenerator"/> renders pages from them, so the
/// build never builds a tree twice. <see cref="PageNames"/> is keyed by moniker, because names are relative to a tree's root.
/// </summary>
internal sealed record ApiProductTrees(
	ResolvedProductDocuments Resolved,
	IReadOnlyList<ApiVersionTree> Trees,
	int? HighestMajor,
	IReadOnlyDictionary<string, IReadOnlyDictionary<string, IReadOnlyList<string>>> PageNames
);

internal sealed record ApiProductGeneration(
	OpenApiDocument Document,
	ResolvedApiConfiguration? ApiConfig,
	LandingNavigationItem Navigation,
	IReadOnlyList<ApiVersionSwitcherItem> VersionSwitcherItems,
	string Moniker,
	string? DefaultMoniker,
	bool ValidateSupplemental,
	bool EmitUnmatchedBaseFiles,
	ApiPageType PageType,
	string? VersionlessRootUrl,
	int? SupplementalMajor,
	IReadOnlyList<ApiCatalogEntry> CatalogEntries,
	string? CurrentApiKey
);

/// <summary>
/// Renders API explorer pages for every configured OpenAPI specification: builds the navigation
/// tree via <see cref="ApiNavigationBuilder"/> and writes each page to the output directory.
/// </summary>
/// <remarks>
/// For versioned products, renders the newest released major at the unversioned path and at its own <c>/vN/</c>,
/// every older major at <c>/vN/</c>, and <c>main</c> at <c>/next/</c> (<see cref="ApiVersionRoutes"/>).
/// Versionless products render only <c>main</c>, at the unversioned path. When more than one version is rendered, assembler pages host the Docs
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
	/// <summary>The site's top navigation in an assembler build, shown in the docs bar of every API page.</summary>
	public TopNavRenderModel? SiteTopNav { get; init; }

	private readonly ILogger _logger = logFactory.CreateLogger<OpenApiGenerator>();
	private readonly IFileSystem _writeFileSystem = context.WriteFileSystem;
	private readonly StaticFileContentHashProvider _contentHashProvider = new(new EmbeddedOrPhysicalFileProvider(context));
	private readonly VersionIndexClient _versionIndexClient = versionIndexClient ?? new VersionIndexClient();
	private readonly IOpenApiSpecificationReader _openApiReader = openApiReader ?? OpenApiReader.Instance;
	private readonly ConcurrentDictionary<string, Task<ResolvedProductDocuments>> _documents = new(StringComparer.OrdinalIgnoreCase);
	private readonly ConcurrentDictionary<string, Task<ApiProductTrees>> _trees = new(StringComparer.OrdinalIgnoreCase);

	public LandingNavigationItem CreateNavigation(
		string apiUrlSuffix,
		OpenApiDocument openApiDocument,
		ResolvedApiConfiguration? apiConfig = null,
		int? versionMajor = null
	) =>
		new ApiNavigationBuilder(_logger, context, markdownStringRenderer).CreateNavigation(
			apiUrlSuffix,
			openApiDocument,
			apiConfig,
			versionMajor
		);

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
				// EmitGlobalError only keeps the message; the stack trace is what says where it failed.
				_logger.LogError(ex, "API '{Prefix}' could not be generated", prefix);
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
	/// Every version tree the explorer renders for every configured API, for the search index, except <c>main</c> at <c>/next</c>. A tree the version index
	/// declares that did not load, or a version index that could not be fetched, fails the export: indexing without
	/// those trees would delete their documents. Page rendering keeps its warning and its local-spec fallback.
	/// </summary>
	public async Task<IReadOnlyList<IndexedTree>> ResolveIndexedTrees(Cancel ctx = default)
	{
		var configs = ApiConfigsToRender();
		var resolved = await Task.WhenAll(configs.Select(c => DocumentsFor(c.Key, c.Value, ctx))).ConfigureAwait(false);
		var apis = configs.Zip(resolved, static (c, r) => (c.Key, c.Value.Product.Id, Resolved: r)).ToList();

		List<string> missing =
		[
			.. apis.SelectMany(
				static a => a
					.Resolved
					.UnresolvedMonikers
					.Select(m => $"API '{a.Key}': version '{m}' is declared in the version index but could not be loaded")
			),
			// A reserved version is not rendered, but it is still declared: an export without it would delete its documents.
			.. apis.SelectMany(
				static a => a
					.Resolved
					.ReservedMonikers
					.Select(m => $"API '{a.Key}': version '{m}' uses a reserved name, so it is not rendered")
			)
		];
		if (_versionIndexClient.RootIndexFetchError is { } fetchError)
			missing.Add($"The API version index could not be fetched ({fetchError}), so the declared version trees are unknown");

		foreach (var problem in missing)
			context.Collector.EmitGlobalError($"{problem}, so the search export stops.");
		if (missing.Count > 0)
			throw new InvalidOperationException($"The API search export stops: {string.Join("; ", missing)}.");

		return [
			.. apis.SelectMany(
				static a => a
					.Resolved
					.Documents
					.Where(v => !a.Resolved.Routes.IsNext(v.Version.Moniker))
					.Select(v => new IndexedTree(a.Resolved.Routes, a.Id, v.Version.Moniker, v.Document))
			)
		];
	}

	/// <summary>Stands in for the tree root in hashed Markdown, so a link's tree does not change the hash.</summary>
	private const string TreeRootToken = "{api-root}";

	/// <summary>
	/// Replaces <paramref name="root"/> with <see cref="TreeRootToken"/> where it starts a link into that tree. A link into
	/// another version tree below the root, such as <c>/v8</c> or <c>/next</c>, and a sibling API that shares the prefix, such as
	/// <c>elasticsearch-serverless</c>, keep their URL. The rule reads only the link, so the set of trees cannot change the hash.
	/// </summary>
	internal static string RelativeToTree(string markdown, string root)
	{
		var result = new StringBuilder(markdown.Length);
		var start = 0;
		for (var i = markdown.IndexOf(root, StringComparison.Ordinal); i >= 0; i = markdown.IndexOf(
			root,
			i + root.Length,
			StringComparison.Ordinal
		))
		{
			var end = i + root.Length;
			if (!EndsAt(markdown, end) || StartsVersionSegment(markdown, end))
				continue;
			_ = result.Append(markdown, start, i - start).Append(TreeRootToken);
			start = end;
		}
		return result.Append(markdown, start, markdown.Length - start).ToString();

		// Sentence punctuation ends a bare autolink only before a space or the end, as the Markdown renderer reads it,
		// so a slug such as `v8.19` stays one segment.
		static bool EndsAt(string text, int index) =>
			index == text.Length
				|| text[index] is '/' or '#' or '?' or ')' or '"' or '\'' or '>' or ']'
				|| char.IsWhiteSpace(text[index])
				|| (text[index] is '.' or ',' or ';' or ':' or '!' && (index + 1 == text.Length || char.IsWhiteSpace(text[index + 1])));

		// `/v8` or `/next` right after the root names another version tree of the API.
		static bool StartsVersionSegment(string text, int index)
		{
			if (index >= text.Length || text[index] != '/')
				return false;
			var segmentStart = index + 1;
			var segmentEnd = segmentStart;
			while (segmentEnd < text.Length && char.IsAsciiLetterOrDigit(text[segmentEnd]))
				segmentEnd++;
			var segment = text.AsSpan(segmentStart, segmentEnd - segmentStart);
			var isVersion = segment.SequenceEqual(ApiUrlBuilder.NextSegment)
				|| (segment.Length > 1 && segment[0] == 'v' && segment[1..].IndexOfAnyExceptInRange('0', '9') < 0);
			return isVersion && EndsAt(text, segmentEnd);
		}
	}

	/// <summary>
	/// The content of every page in <paramref name="tree"/>, hashed from the page's Markdown export, so the search export can
	/// date each page by what it says. Links into the tree are hashed relative to its root: the same page in two trees hashes the same.
	/// </summary>
	public async Task<IReadOnlyList<ApiPageContent>> PageContentsAsync(IndexedTree tree, Cancel ctx = default)
	{
		var apiConfig = ApiConfigsToRender().First(c => string.Equals(c.Key, tree.ApiKey, StringComparison.OrdinalIgnoreCase)).Value;
		var trees = await TreesFor(tree.ApiKey, apiConfig, ctx).ConfigureAwait(false);
		var versionTree = trees.Trees.First(t => t.Moniker == tree.Moniker && !t.IsMirror);
		var renderContext = CreateRenderContext(
			new(
				versionTree.Version.Document,
				apiConfig,
				versionTree.Navigation,
				VersionSwitcherItems: [],
				tree.Moniker,
				trees.Resolved.Routes.DefaultMoniker,
				ValidateSupplemental: false,
				EmitUnmatchedBaseFiles: false,
				versionTree.PageType,
				VersionlessRootUrl: null,
				SupplementalMajor(tree.Moniker, trees.HighestMajor),
				CatalogEntries: [],
				CurrentApiKey: tree.ApiKey
			)
		);

		var pages = UniquePages(versionTree.Navigation);
		var root = versionTree.Navigation.Url.TrimEnd('/');
		var contents = new ApiPageContent?[pages.Length];
		await Parallel.ForAsync(0, pages.Length, new ParallelOptions
		{
			CancellationToken = ctx,
			MaxDegreeOfParallelism = Environment.ProcessorCount
		}, async (i, token) =>
		{
			var (item, page) = pages[i];
			var pageContext = renderContext with { CurrentNavigation = item };
			var markdown = await page.RenderCommonMarkAsync(pageContext, page.CreatePageModel(pageContext), token).ConfigureAwait(false);
			if (!string.IsNullOrEmpty(markdown))
				contents[i] = new ApiPageContent(
					item.Url,
					item.NavigationTitle,
					ContentHash.CreateNormalized(RelativeToTree(markdown, root))
				);
		}).ConfigureAwait(false);

		return [.. contents.OfType<ApiPageContent>()];
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
		var trees = await TreesFor(prefix, apiConfig, ctx).ConfigureAwait(false);
		var resolved = trees.Resolved;
		if (resolved.Documents.Count == 0)
			return null;

		var monikers = resolved.Documents.Select(v => v.Version.Moniker).ToArray();

		// Each tree gets an independent ApiRenderContext, navigation tree and navigation HTML
		// writer, so there is no shared mutable state between concurrent trees. The outer loop in
		// GenerateProducts fans out products only; rendering trees sequentially here made a
		// product's total time the sum of all its trees. Pages within one tree stay sequential,
		// so concurrency is bounded by the number of product×tree units, not ProcessorCount².
		await Parallel.ForEachAsync(trees.Trees, ctx, async (tree, token) =>
		{
			var moniker = tree.Moniker;
			var switcherItems = ApiVersionSwitcher.Build(context.UrlPathPrefix, resolved.Routes, monikers, tree.Suffix, trees.PageNames);
			await GenerateApiProduct(
				new(
					tree.Version.Document,
					apiConfig,
					tree.Navigation,
					switcherItems,
					moniker,
					resolved.Routes.DefaultMoniker,
					// Both copies of the default major read the same files, so only the first copy reports on them.
					ValidateSupplemental: !tree.IsMirror,
					EmitUnmatchedBaseFiles: moniker == resolved.UnmatchedBaseFilesMoniker,
					PageType: tree.PageType,
					VersionlessRootUrl: tree.IsMirror
						? ApiUrlBuilder.ProductRoot(context.UrlPathPrefix, resolved.Routes.Suffix(moniker))
						: null,
					SupplementalMajor: SupplementalMajor(moniker, trees.HighestMajor),
					CatalogEntries: hubEntries,
					CurrentApiKey: prefix
				),
				token
			).ConfigureAwait(false);
		}).ConfigureAwait(false);

		return CatalogEntry(prefix, apiConfig, resolved);
	}

	/// <summary>
	/// Builds, once per API key, the navigation tree of every URL root of every resolved version. Each tree is built
	/// before any page renders, so every picker can link to the same page in the other versions.
	/// </summary>
	internal Task<ApiProductTrees> TreesFor(string apiKey, ResolvedApiConfiguration apiConfig, Cancel ctx) =>
		_trees.GetOrAdd(apiKey, _ => BuildTrees(apiKey, apiConfig, ctx));

	private async Task<ApiProductTrees> BuildTrees(string apiKey, ResolvedApiConfiguration apiConfig, Cancel ctx)
	{
		var resolved = await DocumentsFor(apiKey, apiConfig, ctx).ConfigureAwait(false);
		if (resolved.Documents.Count == 0)
			return new(resolved, [], null, new Dictionary<string, IReadOnlyDictionary<string, IReadOnlyList<string>>>());

		var highestMajor = resolved.Documents.Select(v => v.Version.Moniker).Max(ApiVersions.TryParseMajor);
		var roots = resolved.Documents.SelectMany(
			v => resolved.Routes.Suffixes(v.Version.Moniker).Select((suffix, i) => (Version: v, Suffix: suffix, IsMirror: i > 0))
		);
		var trees = await Task.WhenAll(
			roots.Select(
				r => Task.Run(
					() =>
					{
						var pageType = ApiPageTypes.Of(resolved.Routes, r.Version.Version.Moniker, r.IsMirror);
						var navigation = CreateNavigation(
							r.Suffix,
							r.Version.Document,
							apiConfig,
							SupplementalMajor(r.Version.Version.Moniker, highestMajor)
						);
						navigation.InSitemap = ApiPageTypes.Indexing(pageType).InSitemap;
						return new ApiVersionTree(r.Version, r.Suffix, r.IsMirror, pageType, navigation);
					},
					ctx
				)
			)
		).ConfigureAwait(false);
		var pageNames = trees.Where(static t => !t.IsMirror).ToDictionary(static t => t.Moniker, static t => PageNames(t.Navigation));
		return new(resolved, trees, highestMajor, pageNames);
	}

	private List<(int Index, string Key, ResolvedApiConfiguration Value)> ApiConfigsToRender() => ApiConfigsToRender(context);

	/// <summary>The APIs a build renders, keyed by the key their URL uses.</summary>
	internal static List<(int Index, string Key, ResolvedApiConfiguration Value)> ApiConfigsToRender(BuildContext context)
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
			return ResolvedProductDocuments.Empty(apiKey);
		}
	}

	/// <summary>
	/// The document that titles the API: the default version's, or the newest released one that loaded when the default
	/// did not. Never the unreleased tree, which no listing links to.
	/// </summary>
	private static VersionedOpenApiDocument? CanonicalDocument(ResolvedProductDocuments resolved) =>
		resolved.Documents.FirstOrDefault(v => v.Version.Moniker == resolved.Routes.DefaultMoniker)
			?? resolved.Documents.Where(v => !resolved.Routes.IsNext(v.Version.Moniker)).MaxBy(v => ApiVersions.Recency(v.Version.Moniker));

	internal ApiCatalogEntry? CatalogEntry(string prefix, ResolvedApiConfiguration apiConfig, ResolvedProductDocuments resolved)
	{
		if (CanonicalDocument(resolved) is not { } canonical)
			return null;

		var title = canonical.Document.Info?.Title ?? apiConfig.Product.DisplayName ?? prefix;
		// The versionless URL when the default version rendered, else the released tree that did, so the card never links to a page nothing wrote.
		var url = ApiUrlBuilder.ProductRoot(context.UrlPathPrefix, resolved.Routes.Suffix(canonical.Version.Moniker));
		return new ApiCatalogEntry(prefix, title, url, apiConfig.Product.Id, canonical.Document.Info?.Description);
	}

	/// <summary>
	/// Resolves every OpenAPI document to render for one API key: <c>main</c> and the released numeric majors.
	/// Returns empty documents when nothing could be resolved. <see cref="ResolvedProductDocuments.UnmatchedBaseFilesMoniker"/>
	/// is <c>main</c> when declared, else the first declared version, and only when that document actually resolved.
	/// </summary>
	internal async Task<ResolvedProductDocuments> ResolveDocumentsForProduct(string apiKey, ResolvedApiConfiguration apiConfig, Cancel ctx)
	{
		var versionless = IsVersionlessProduct(apiConfig.Product);
		if (apiConfig.LocalSpecFile is { } localFile && versionless)
			return await ResolveLocalMainOnly(apiKey, localFile).ConfigureAwait(false);

		var versions = await _versionIndexClient.ResolveVersionsAsync(
			context.Git,
			apiKey,
			apiConfig,
			context.Collector,
			ctx
		).ConfigureAwait(false);

		var (declaredVersions, reserved) = WithoutReservedMonikers(apiKey, versions);
		var versionsToRender = versionless ? declaredVersions.Where(v => v.Moniker == "main").ToArray() : declaredVersions;

		if (versionsToRender.Length == 0)
			return ResolvedProductDocuments.Empty(apiKey) with { ReservedMonikers = reserved };

		var declared = versionsToRender.Select(static v => v.Moniker).ToArray();
		var routes = ApiVersionRoutes.For(apiKey, declared);
		var unmatchedBaseFiles = declared.Contains("main") ? "main" : declared[0];

		// Download and parse every version at once; results keep the declared version order.
		var documents = await Task.WhenAll(
			versionsToRender.Select(v => ResolveDocumentForVersion(apiKey, apiConfig, v, ctx))
		).ConfigureAwait(false);
		var results = versionsToRender
			.Zip(documents, static (version, document) => document is null ? null : new VersionedOpenApiDocument(version, document))
			.OfType<VersionedOpenApiDocument>()
			.ToList();
		var unresolved = versionsToRender.Zip(documents).Where(static x => x.Second is null).Select(static x => x.First.Moniker).ToArray();

		return ToResolvedProductDocuments(routes, results, unmatchedBaseFiles) with
		{
			UnresolvedMonikers = unresolved,
			ReservedMonikers = reserved
		};
	}

	/// <summary>
	/// <c>next</c> is the URL of <c>main</c>, so a version with that name would read as the unreleased spec. It is reported,
	/// left out of the render, and returned by name so the search export can stop on it.
	/// </summary>
	private (ResolvedApiVersion[] Kept, string[] Reserved) WithoutReservedMonikers(
		string apiKey,
		IReadOnlyList<ResolvedApiVersion> versions
	)
	{
		var reserved = versions.Where(
			static v => v.Moniker.Equals(ApiUrlBuilder.NextSegment, StringComparison.OrdinalIgnoreCase)
		).ToArray();
		foreach (var version in reserved)
		{
			context.Collector.EmitGlobalError(
				$"API '{apiKey}': the version index declares a version named '{version.Moniker}', which is the URL segment of the unreleased 'main' spec. Rename the version; it is not rendered."
			);
		}

		return ([.. versions.Except(reserved)], [.. reserved.Select(static v => v.Moniker)]);
	}

	private async Task<ResolvedProductDocuments> ResolveLocalMainOnly(string apiKey, IFileInfo localFile)
	{
		var document = await _openApiReader.ReadAsync(localFile, context.Collector).ConfigureAwait(false);
		if (document is null)
			return ResolvedProductDocuments.Empty(apiKey);

		VersionedOpenApiDocument[] documents =
		[
			new(new ResolvedApiVersion { Moniker = "main", Version = "main", IsLocal = true, LocalFile = localFile }, document)
		];
		return ToResolvedProductDocuments(ApiVersionRoutes.For(apiKey, ["main"]), documents, "main");
	}

	private static ResolvedProductDocuments ToResolvedProductDocuments(
		ApiVersionRoutes routes,
		IReadOnlyList<VersionedOpenApiDocument> documents,
		string unmatchedBaseFiles
	) => new(routes, documents, documents.Any(d => d.Version.Moniker == unmatchedBaseFiles) ? unmatchedBaseFiles : null);

	private static bool IsVersionlessProduct(Product product) => product.VersioningSystem?.IsVersionless == true;

	/// <summary>
	/// Numeric monikers map 1:1. <c>main</c> uses the highest rendered numeric major, so the unreleased tree
	/// carries the current-major overlay too.
	/// </summary>
	internal static int? SupplementalMajor(string moniker, int? highestNumericMoniker) =>
		ApiVersions.TryParseMajor(moniker) ?? (moniker == "main" ? highestNumericMoniker : null);

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
		var catalogUrl = ApiUrlBuilder.ApiRoot(context.UrlPathPrefix);
		var navigation = new ApiCatalogNavigationItem(catalogUrl, entries);
		var navigationRenderer = new IsolatedBuildNavigationHtmlWriter(context, navigation, suppressNavigationDropdown: true);

		var renderContext = new ApiRenderContext(context, CatalogDocument, _contentHashProvider)
		{
			SiteTopNav = SiteTopNav,
			NavigationHtml = string.Empty,
			CurrentNavigation = navigation.Index,
			MarkdownRenderer = markdownStringRenderer,
			ApiExplorerLog = _logger
		};

		_ = await Render(navigation.Index, navigation.Index.Model, renderContext, navigationRenderer, ctx).ConfigureAwait(false);
	}

	/// <summary>The render context of one tree, with its supplemental files applied. Page HTML and the search export share it.</summary>
	private ApiRenderContext CreateRenderContext(ApiProductGeneration generation)
	{
		var navigation = generation.Navigation;
		var discovery = DiscoverSupplemental(generation.Document, generation.ApiConfig);
		if (generation.ValidateSupplemental)
		{
			ApiSupplementalValidator.Validate(
				discovery,
				new(generation.Document, context.Collector, generation.Moniker, EmitUnmatchedBaseFiles: generation.EmitUnmatchedBaseFiles)
			);
		}
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

		ApplySupplementalIntroHeadings(navigation, navigation.Url, tags);

		return new ApiRenderContext(context, generation.Document, _contentHashProvider)
		{
			SiteTopNav = SiteTopNav,
			NavigationHtml = string.Empty,
			CurrentNavigation = navigation,
			MarkdownRenderer = markdownStringRenderer,
			ApiExplorerLog = _logger,
			VersionSwitcherItems = generation.VersionSwitcherItems,
			CatalogEntries = generation.CatalogEntries,
			CurrentApiKey = generation.CurrentApiKey,
			PageType = generation.PageType,
			Moniker = generation.Moniker,
			DefaultMoniker = generation.DefaultMoniker,
			VersionlessRootUrl = generation.VersionlessRootUrl,
			OperationSupplemental = operations,
			TagSupplemental = tags,
			Product = generation.ApiConfig?.Product
		};
	}

	private async Task GenerateApiProduct(ApiProductGeneration generation, Cancel ctx)
	{
		var title = generation.Document.Info?.Title ?? "<no title>";
		var started = Stopwatch.StartNew();
		var navigation = generation.Navigation;
		_logger.LogInformation(
			"Generating OpenApiDocument {Title} ({Operations} operations)",
			title,
			generation.Document.Paths.Sum(static p => p.Value.Operations?.Count ?? 0)
		);

		var navigationRenderer = new IsolatedBuildNavigationHtmlWriter(
			context,
			navigation,
			MapVersionSwitcher(generation.VersionSwitcherItems),
			suppressNavigationDropdown: true
		);
		var renderContext = CreateRenderContext(generation);

		await RenderNavigationItems(renderContext, navigationRenderer, navigation, ctx).ConfigureAwait(false);
		await WriteSpecDownloads(navigation, generation.Document, ctx).ConfigureAwait(false);
		_logger.LogInformation("Generated OpenApiDocument {Title} in {Elapsed:0.0}s", title, started.Elapsed.TotalSeconds);
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

	private void ApplySupplementalIntroHeadings(
		INavigationItem item,
		string apiBaseUrl,
		IReadOnlyDictionary<string, ApiSupplementalDoc> tags
	)
	{
		if (item is not INodeNavigationItem<INavigationModel, INavigationItem> node)
			return;

		var children = node.NavigationItems.ToArray();
		// Same description and post sections as TagLandingViewModel, so the sidebar links only to anchors the page keeps.
		if (item is TagNavigationItem tag && tags.TryGetValue(tag.Index.Model.Name, out var doc))
		{
			var description = doc.DescriptionOr(tag.Index.Model.Description);
			tag.ApplyIntroHeadings(
				ApiMarkdown.ReadIntroHeadings(
					markdownStringRenderer,
					description,
					apiBaseUrl,
					doc.PostSections,
					EndpointsSection.ReservedAnchors
				)
			);
		}

		foreach (var child in children)
			ApplySupplementalIntroHeadings(child, apiBaseUrl, tags);
	}

	private Task RenderNavigationItems(
		ApiRenderContext renderContext,
		IsolatedBuildNavigationHtmlWriter navigationRenderer,
		INavigationItem root,
		Cancel ctx
	) =>
		Parallel.ForEachAsync(UniquePages(root), new ParallelOptions
		{
			CancellationToken = ctx,
			MaxDegreeOfParallelism = Environment.ProcessorCount
		}, async (page, token) =>
		{
			_ = await Render(page.Item, page.Model, renderContext, navigationRenderer, token).ConfigureAwait(false);
			if (page.Item is OperationNavigationItem { AliasUrls.Count: > 0 } collapsed)
				await WriteRedirects(collapsed, token).ConfigureAwait(false);
		});

	/// <summary>Former per-operation URLs of a collapsed page forward to it, keeping any <c>#fragment</c>.</summary>
	private async Task WriteRedirects(OperationNavigationItem page, Cancel ctx)
	{
		var html = ApiRedirectPage.Html(page.Url);
		foreach (var alias in page.AliasUrls)
		{
			await WriteSpecSibling(
				ApiOutputPaths.RelativeHtmlFile(alias, context.UrlPathPrefix),
				async (stream, c) =>
				{
					await using var writer = new StreamWriter(stream);
					await writer.WriteAsync(html.AsMemory(), c).ConfigureAwait(false);
				},
				ctx
			).ConfigureAwait(false);
		}
	}

	/// <summary>
	/// Every name of every page in one version's tree, relative to its product root. A collapsed operation is reachable by its
	/// canonical path and its former per-operation paths, and a <c>children:</c> page also answers to the topic URLs it replaces. Each name maps to all names of that page, canonical first.
	/// </summary>
	internal static IReadOnlyDictionary<string, IReadOnlyList<string>> PageNames(LandingNavigationItem navigation)
	{
		var pages = new List<(INavigationItem Item, IApiModel Model)>();
		CollectPages(navigation, pages);
		var root = navigation.Url.TrimEnd('/');
		var names = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
		foreach (var (item, _) in pages)
		{
			var all = RelativeNames(item, root);
			foreach (var name in all)
				names[name] = all;
		}

		return names;
	}

	private static string[] RelativeNames(INavigationItem item, string root)
	{
		var urls = item switch
		{
			OperationNavigationItem collapsed => collapsed.AliasUrls.Prepend(item.Url),
			SimpleMarkdownNavigationItem child => child.ReplacedTopicUrls.Prepend(item.Url),
			_ => [item.Url]
		};
		return [
			.. urls
				.Select(static url => url.TrimEnd('/'))
				.Where(url => url.StartsWith(root, StringComparison.Ordinal))
				.Select(url => url[root.Length..])
				.Distinct(StringComparer.Ordinal)
		];
	}

	/// <summary>
	/// The pages of a tree, one per URL. When two items share a URL the last one wins, as it did when pages were written
	/// sequentially in depth-first order. Rendering and the search export's content hashes both read this set.
	/// </summary>
	private static (INavigationItem Item, IApiModel Model)[] UniquePages(INavigationItem root)
	{
		var pages = new List<(INavigationItem Item, IApiModel Model)>();
		CollectPages(root, pages);
		return [.. pages.AsEnumerable().Reverse().DistinctBy(p => p.Item.Url)];
	}

	private static void CollectPages(INavigationItem item, List<(INavigationItem Item, IApiModel Model)> pages)
	{
		if (item is IntroHeadingNavigationItem)
			return;

		if (item is INodeNavigationItem<IApiModel, INavigationItem> node)
		{
			// Sidebar headings group pages but have no page of their own.
			if (item is not ISidebarHeadingNavigationItem)
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
		// An empty authored page still gets its export, which its Copy page links to.
		if (markdown is null)
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
