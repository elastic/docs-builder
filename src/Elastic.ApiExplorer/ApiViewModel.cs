// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.RegularExpressions;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Configuration.Builder;
using Elastic.Documentation.Extensions;
using Elastic.Documentation.Navigation;
using Elastic.Documentation.Site;
using Elastic.Documentation.Site.FileProviders;
using Microsoft.AspNetCore.Html;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer;

public record ApiTocItem(string Heading, string Slug, int Level = 2);

public record ApiLayoutViewModel : GlobalLayoutViewModel
{
	public required IReadOnlyList<ApiTocItem> TocItems { get; init; }
}

public abstract partial class ApiViewModel(ApiRenderContext context)
{
	public string NavigationHtml { get; } = context?.NavigationHtml ?? string.Empty;
	public StaticFileContentHashProvider StaticFileContentHashProvider { get; } = context?.StaticFileContentHashProvider ?? throw new ArgumentNullException(nameof(context), "StaticFileContentHashProvider cannot be null");
	public INavigationItem CurrentNavigationItem { get; } = context?.CurrentNavigation ?? throw new ArgumentNullException(nameof(context), "CurrentNavigation cannot be null");
	public IMarkdownStringRenderer MarkdownRenderer { get; } = context?.MarkdownRenderer ?? throw new ArgumentNullException(nameof(context), "MarkdownRenderer cannot be null");
	public BuildContext BuildContext { get; } = context?.BuildContext ?? throw new ArgumentNullException(nameof(context), "BuildContext cannot be null");
	public OpenApiDocument Document { get; } = context?.Model ?? throw new ArgumentNullException(nameof(context), "OpenApiDocument cannot be null");


	public HtmlString RenderMarkdown(string? markdown)
	{
		if (string.IsNullOrEmpty(markdown))
			return new HtmlString(string.Empty);

		// Escape mustache-style patterns by wrapping in backticks (inline code won't process substitutions)
		var escaped = MustachePattern().Replace(markdown, match => $"`{match.Value}`");
		return new HtmlString(MarkdownRenderer.Render(escaped, null));
	}

	// Regex to match mustache-style patterns like {{var}} or {{{var}}} that conflict with docs-builder substitutions
	[GeneratedRegex(@"\{\{\{?[^}]+\}?\}\}")]
	private static partial Regex MustachePattern();

	protected virtual IReadOnlyList<ApiTocItem> GetTocItems() => [];

	/// <summary>When set, drives <see cref="GlobalLayoutViewModel.Title"/> for this page (e.g. intro/outro markdown). Does not affect <see cref="GlobalLayoutViewModel.HeaderTitle"/> which stays as the API product name.</summary>
	protected virtual string? LayoutPageTitle => null;

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
		var rootPath = BuildContext.SiteRootPath ?? GetDefaultRootPath(BuildContext.UrlPathPrefix);
		var docTitle = Document.Info?.Title ?? "API Documentation";
		var pageTitle = LayoutPageTitle;
		var documentTitle = pageTitle is not null
			? $"{pageTitle} | {docTitle}"
			: docTitle;

		return new()
		{
			DocsBuilderVersion = ShortId.Create(BuildContext.Version),
			DocSetName = "Api Explorer",
			Description = "",
			Title = documentTitle,
			CurrentNavigationItem = CurrentNavigationItem,
			Previous = null,
			Next = null,
			NavigationHtml = NavigationHtml,
			UrlPathPrefix = BuildContext.UrlPathPrefix,
			Htmx = new DefaultHtmxAttributeProvider(rootPath),
			AllowIndexing = BuildContext.AllowIndexing,
			CanonicalBaseUrl = BuildContext.CanonicalBaseUrl,
			GoogleTagManager = new GoogleTagManagerConfiguration(),
			Optimizely = new OptimizelyConfiguration(),
			Features = new FeatureFlags([]),
			StaticFileContentHashProvider = StaticFileContentHashProvider,
			BuildType = BuildContext.BuildType,
			TocItems = GetTocItems(),
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

	private static string GetDefaultRootPath(string? urlPathPrefix)
	{
		var prefix = urlPathPrefix?.Trim('/') ?? "";
		return string.IsNullOrEmpty(prefix) ? "/" : $"/{prefix}/";
	}
}
