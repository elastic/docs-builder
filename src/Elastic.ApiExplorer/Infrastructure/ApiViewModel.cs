// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.ApiExplorer.Model;
using Elastic.ApiExplorer.Operations;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Extensions;
using Elastic.Documentation.Navigation;
using Elastic.Documentation.Site;
using Elastic.Documentation.Site.FileProviders;
using Elastic.Documentation.Site.Navigation;
using Microsoft.AspNetCore.Html;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Infrastructure;

public record ApiTocItem(string Heading, string Slug, int Level = 2);

public record ApiLayoutViewModel : GlobalLayoutViewModel
{
	public required IReadOnlyList<ApiTocItem> TocItems { get; init; }

	/// <summary>When set, operation pages render examples in the right rail instead of the in-page TOC.</summary>
	public OperationExamplesPanelModel? ExamplesPanel { get; init; }

	public required ApiBreadcrumbTrail Breadcrumbs { get; init; }
	public IReadOnlyList<ApiVersionSwitcherItem> VersionSwitcherItems { get; init; } = [];
	public IReadOnlyList<ApiVersionSwitcherItem> HubSwitcherItems { get; init; } = [];

	/// <summary>Assembler builds host Jump to API unless the site is air-gapped.</summary>
	public bool ShowJumpToPage => BuildType == BuildType.Assembler && !Features.AirGappedEnabled;

	public required string MarkdownUrl { get; init; }

	public string? ProductName { get; init; }

	/// <summary>
	/// Preload hint for API links. Body already hx-boosts into <c>#main-container</c>,
	/// so the examples rail swaps with the article without a dedicated OOB provider.
	/// </summary>
	public string HxAttributes => $" preload=\"{Htmx.Preload}\"";
}

public abstract class ApiViewModel(ApiRenderContext context)
{
	public string NavigationHtml { get; } = context?.NavigationHtml ?? string.Empty;
	public StaticFileContentHashProvider StaticFileContentHashProvider { get; } = context?.StaticFileContentHashProvider
		?? throw new ArgumentNullException(nameof(context), "StaticFileContentHashProvider cannot be null");
	public INavigationItem CurrentNavigationItem { get; } = context?.CurrentNavigation
		?? throw new ArgumentNullException(nameof(context), "CurrentNavigation cannot be null");
	public IMarkdownStringRenderer MarkdownRenderer { get; } = context?.MarkdownRenderer
		?? throw new ArgumentNullException(nameof(context), "MarkdownRenderer cannot be null");
	public BuildContext BuildContext { get; } = context?.BuildContext
		?? throw new ArgumentNullException(nameof(context), "BuildContext cannot be null");
	public OpenApiDocument Document { get; } = context?.Model
		?? throw new ArgumentNullException(nameof(context), "OpenApiDocument cannot be null");

	/// <summary>Current API render context (OpenAPI model, nav, optional logging).</summary>
	protected ApiRenderContext RenderContext { get; } = context ?? throw new ArgumentNullException(nameof(context));

	public HtmlString RenderMarkdown(string? markdown) => ApiMarkdown.Render(RenderContext, markdown);

	protected virtual IReadOnlyList<ApiTocItem> GetTocItems() => [];

	/// <summary>When set, drives <see cref="GlobalLayoutViewModel.Title"/> for this page (e.g. intro/outro markdown). Does not affect <see cref="GlobalLayoutViewModel.HeaderTitle"/> which stays as the API product name.</summary>
	protected virtual string? LayoutPageTitle => null;

	/// <summary>Last breadcrumb label. Defaults to <see cref="LayoutPageTitle"/> or the nav title.</summary>
	protected virtual string BreadcrumbCurrentTitle => LayoutPageTitle ?? CurrentNavigationItem.NavigationTitle;

	/// <summary>Raw markdown used for the meta description. Excerpted before it reaches the layout.</summary>
	protected virtual string? LayoutPageDescription => null;

	private string? GetGitHubDocsUrl()
	{
		var repo = BuildContext.Git.RepositoryName;
		var branch = BuildContext.Git.Branch;
		if (string.IsNullOrEmpty(repo) || repo == "unavailable" || string.IsNullOrEmpty(branch) || branch == "unavailable")
			return null;
		return $"https://github.com/elastic/{repo}/tree/{branch}/docs";
	}

	public ApiLayoutViewModel CreateGlobalLayoutModel()
	{
		var docTitle = Document.Info?.Title ?? "API Documentation";
		var pageTitle = LayoutPageTitle;
		var documentTitle = pageTitle is not null ? $"{pageTitle} | {docTitle}" : docTitle;
		var catalogUrl = $"{ApiUrlBuilder.ApiRoot(BuildContext.UrlPathPrefix)}/";

		var hubItems = ApiHubSwitcher.Build(RenderContext.CatalogEntries, RenderContext.CurrentApiKey, catalogUrl);
		var assembler = BuildContext.BuildType == BuildType.Assembler;
		var specRootUrl = CurrentNavigationItem.NavigationRoot.Url;
		var onCatalog = SameUrl(specRootUrl, catalogUrl) || SameUrl(CurrentNavigationItem.Url, catalogUrl);
		var crumbs = ApiBreadcrumbBuilder.Collect(CurrentNavigationItem, BreadcrumbCurrentTitle, Document.Info?.Title, catalogUrl);

		return new()
		{
			DocsBuilderVersion = ShortId.Create(BuildContext.Version),
			DocSetName = "Api Explorer",
			Description = ApiSeoDescription.Excerpt(LayoutPageDescription) ?? "",
			Title = documentTitle,
			CurrentNavigationItem = CurrentNavigationItem,
			Previous = null,
			Next = null,
			NavigationHtml = NavigationHtml,
			NavigationActiveUrl = CurrentNavigationItem.Url,
			UrlPathPrefix = BuildContext.UrlPathPrefix,
			AllowIndexing = BuildContext.AllowIndexing,
			CanonicalBaseUrl = BuildContext.CanonicalBaseUrl,
			GoogleTagManager = BuildContext.GoogleTagManager,
			Optimizely = BuildContext.Optimizely,
			Features = BuildContext.Configuration.Features,
			StaticFileContentHashProvider = StaticFileContentHashProvider,
			BuildType = BuildContext.BuildType,
			PageFeedbackSurface = "api",
			TocItems = GetTocItems(),
			Breadcrumbs = ApiBreadcrumbBuilder.TrailFrom(crumbs),
			StructuredBreadcrumbsJson = ApiBreadcrumbBuilder.ToJsonLd(crumbs, BuildContext.CanonicalBaseUrl),
			ProductName = RenderContext.Product?.DisplayName,
			VersionSwitcherItems = RenderContext.VersionSwitcherItems,
			HubSwitcherItems = hubItems,
			LegacyBarProductSwitcher = assembler
				? [.. hubItems.Select(static i => new NavigationSelectOption(i.Label, i.Url, i.Selected))]
				: [],
			ApiCatalogUrl = catalogUrl,
			SpecJsonUrl = onCatalog ? null : ApiOutputPaths.JsonUrl(specRootUrl),
			SpecYamlUrl = onCatalog ? null : ApiOutputPaths.YamlUrl(specRootUrl),
			MarkdownUrl = ApiOutputPaths.MarkdownUrl(CurrentNavigationItem.Url),
			ShowVersionDropdown = false,
			ShowLegacyBarVersionDropdown = false,
			CurrentVersion = ApiVersionSwitcher.CurrentVersionLabel(
				RenderContext.Product?.VersioningSystem,
				RenderContext.VersionSwitcherItems
			),
			VersionDropdownSerializedModel = ApiVersionSwitcher.SerializeDropdownItems(RenderContext.VersionSwitcherItems),
			// Header properties for isolated mode
			HeaderTitle = docTitle,
			HeaderVersion = Document.Info?.Version ?? "1.0",
			GitBranch = BuildContext.Git.Branch != "unavailable" ? BuildContext.Git.Branch : null,
			GitCommitShort = BuildContext.Git.Ref is { Length: >= 7 } r && r != "unavailable" ? r[..7] : null,
			GitRepository = BuildContext.Git.RepositoryName != "unavailable" ? BuildContext.Git.RepositoryName : null,
			GitHubDocsUrl = GetGitHubDocsUrl(),
			GitHubRef = BuildContext.Git.GitHubRef
		};
	}

	private static bool SameUrl(string? left, string? right)
	{
		if (left is null || right is null)
			return false;
		return string.Equals(left.TrimEnd('/'), right.TrimEnd('/'), StringComparison.Ordinal);
	}
}
