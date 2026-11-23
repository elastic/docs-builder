// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Concurrent;
using System.Collections.Frozen;
using Elastic.ApiExplorer.Landing;
using Elastic.ApiExplorer.Model;
using Elastic.ApiExplorer.Operations;
using Elastic.ApiExplorer.Supplemental;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Products;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Extensions;
using Elastic.Documentation.Navigation;
using Elastic.Documentation.Site.FileProviders;
using Elastic.Documentation.Site.Navigation;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Infrastructure;

/// <param name="Moniker">The version this entry links to: <c>main</c> or a numeric major.</param>
/// <param name="PageNames">
/// Every name of every page this version renders, relative to <paramref name="Url"/> (<c>""</c> is the landing page).
/// Each name maps to all names of that page, canonical first, because a collapsed operation also answers to its former paths.
/// </param>
public sealed record ApiVersionSwitcherItem(string Moniker, string Label, string Url, bool Selected, string? ShortLabel = null)
{
	public IReadOnlyDictionary<string, IReadOnlyList<string>> PageNames
	{
		get;
		init;
	} = FrozenDictionary<string, IReadOnlyList<string>>.Empty;
}

public record ApiRenderContext(
	BuildContext BuildContext,
	OpenApiDocument Model,
	StaticFileContentHashProvider StaticFileContentHashProvider
) : RenderContext<OpenApiDocument>(BuildContext, Model)
{
	public required string NavigationHtml { get; init; }
	public required INavigationItem CurrentNavigation { get; init; }
	public required IMarkdownStringRenderer MarkdownRenderer { get; init; }

	/// <summary>Logger for API Explorer rendering (e.g. OpenAPI extension parsing); optional when the host does not provide one.</summary>
	public ILogger? ApiExplorerLog { get; init; }

	public IReadOnlyList<ApiVersionSwitcherItem> VersionSwitcherItems { get; init; } = [];

	public IReadOnlyList<ApiCatalogEntry> CatalogEntries { get; init; } = [];

	public string? CurrentApiKey { get; init; }

	/// <summary>What the tree being rendered is for search engines. The catalog and other previews count as versionless.</summary>
	public ApiPageType PageType { get; init; } = ApiPageType.Versionless;

	/// <summary>The version of the tree being rendered: <c>main</c> or a numeric major. Null on the catalog and other previews.</summary>
	public string? Moniker { get; init; }

	/// <summary>The minor release the tree renders from, such as <c>9.6</c>. Null for <c>main</c> and when the version index names no minor. It does not depend on the switcher, which is empty when only one version renders.</summary>
	public string? Release { get; init; }

	/// <summary>The newest version the API declares, whether or not its tree rendered. The versionless URL belongs to it.</summary>
	public string? DefaultMoniker { get; init; }

	/// <summary>The root of the versionless tree, set on the numbered copy of the newest major so its pages name the versionless pages as canonical.</summary>
	public string? VersionlessRootUrl { get; init; }

	/// <summary>
	/// The landing description. A versioned spec that carries none borrows the one its API's catalog entry takes from the
	/// canonical spec, so v8 and v9 landings are not left without a meta description.
	/// </summary>
	internal string? LandingDescription =>
		!string.IsNullOrWhiteSpace(Model.Info?.Description)
			? Model.Info.Description
			: CatalogEntries.FirstOrDefault(entry => entry.Key == CurrentApiKey)?.Description;

	/// <summary>The product name the page titles and fallback descriptions use.</summary>
	internal string ProductName => ApiPageTitle.ProductName(Product?.DisplayName, Model.Info?.Title);

	/// <summary>The absolute canonical URL of <paramref name="pageUrl"/>, or its path when the build has no canonical base URL.</summary>
	public string CanonicalUrl(string pageUrl)
	{
		var path = ApiPageTypes.CanonicalPath(PageType, pageUrl, CurrentNavigation.NavigationRoot.Url, VersionlessRootUrl).TrimEnd('/');
		return UrlPath.MakeAbsolute(BuildContext.CanonicalBaseUrl, path)?.TrimEnd('/') ?? path;
	}

	/// <summary>Product bound to this API key, or <see langword="null"/> on the combined catalog.</summary>
	public Product? Product { get; init; }

	/// <summary>The site's top navigation in an assembler build, so API pages carry the same docs bar as the rest of the site.</summary>
	public TopNavRenderModel? SiteTopNav { get; init; }

	internal IReadOnlyDictionary<string, ApiSupplementalDoc> OperationSupplemental
	{
		get;
		init;
	} = FrozenDictionary<string, ApiSupplementalDoc>.Empty;

	internal IReadOnlyDictionary<string, ApiSupplementalDoc> TagSupplemental
	{
		get;
		init;
	} = FrozenDictionary<string, ApiSupplementalDoc>.Empty;

	/// <summary>
	/// Per-generation-unit cache for resolving OpenAPI <c>$ref</c> identifiers to their concrete
	/// component schemas.  Shared by reference across all per-page <c>with</c>-copies of this context
	/// so each component schema is fetched from <see cref="OpenApiDocument.Components"/> only once per
	/// unit rather than on every proxy property access.
	/// </summary>
	/// <remarks>
	/// Records propagate reference-type properties shallowly via <c>with</c>, so this dictionary is
	/// the same object instance in the unit context and in every page-level copy derived from it.
	/// Pages within a unit render in parallel, so the cache must be concurrent.
	/// </remarks>
	internal ConcurrentDictionary<string, IOpenApiSchema?> SchemaResolveCache { get; } = new();

	/// <summary>
	/// Per-generation-unit cache of sanitized description HTML, shared like <see cref="SchemaResolveCache"/>.
	/// The same descriptions repeat across many pages, and HtmlSanitizer serializes on a process-wide
	/// AngleSharp pool lock, so sanitizing each distinct fragment once keeps parallel rendering busy.
	/// </summary>
	internal ConcurrentDictionary<string, string> SanitizedHtmlCache { get; } = new(StringComparer.Ordinal);
}
