// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

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

public abstract class ApiViewModel(ApiRenderContext context)
{
	public string NavigationHtml { get; } = context.NavigationHtml;
	public StaticFileContentHashProvider StaticFileContentHashProvider { get; } = context.StaticFileContentHashProvider;
	public INavigationItem CurrentNavigationItem { get; } = context.CurrentNavigation;
	public IMarkdownStringRenderer MarkdownRenderer { get; } = context.MarkdownRenderer;
	public BuildContext BuildContext { get; } = context.BuildContext;
	public OpenApiDocument Document { get; } = context.Model;


	public HtmlString RenderMarkdown(string? markdown) =>
		new(string.IsNullOrEmpty(markdown) ? string.Empty : MarkdownRenderer.Render(markdown, null));

	protected virtual IReadOnlyList<ApiTocItem> GetTocItems() => [];

	public ApiLayoutViewModel CreateGlobalLayoutModel() =>
		new()
		{
			DocsBuilderVersion = ShortId.Create(BuildContext.Version),
			DocSetName = "Api Explorer",
			Description = "",
			CurrentNavigationItem = CurrentNavigationItem,
			Previous = null,
			Next = null,
			NavigationHtml = NavigationHtml,
			NavigationFileName = string.Empty,
			UrlPathPrefix = BuildContext.UrlPathPrefix,
			AllowIndexing = BuildContext.AllowIndexing,
			CanonicalBaseUrl = BuildContext.CanonicalBaseUrl,
			GoogleTagManager = new GoogleTagManagerConfiguration(),
			Features = new FeatureFlags([]),
			StaticFileContentHashProvider = StaticFileContentHashProvider,
			TocItems = GetTocItems()
		};
}
