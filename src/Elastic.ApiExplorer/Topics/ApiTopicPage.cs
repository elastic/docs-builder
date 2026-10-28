// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.Text;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Landing;
using Elastic.Documentation;
using Elastic.Documentation.Extensions;
using Elastic.Documentation.Navigation;
using Microsoft.AspNetCore.Html;
using RazorSlices;

namespace Elastic.ApiExplorer.Topics;

/// <summary>One entry of the spec's <c>x-topics</c> extension, and the page of prose it renders as.</summary>
public record ApiTopicPage(string Title, string Content) : IApiModel<TopicViewModel>
{
	/// <summary>A topic titled Authentication becomes the <c>/authentication</c> page, as on Bump.sh.</summary>
	public bool IsAuthentication => string.Equals(Title, ApiUrlBuilder.AuthenticationSegment, StringComparison.OrdinalIgnoreCase);

	/// <summary>
	/// True when a <c>children:</c> page covers this topic: its first heading matches the title, or its text matches the
	/// content, ignoring whitespace differences.
	/// </summary>
	public bool IsCoveredBy(string? heading, string? body) =>
		string.Equals(heading, Title, StringComparison.OrdinalIgnoreCase)
			|| (!string.IsNullOrWhiteSpace(Content) && CollapseWhitespace(body) == CollapseWhitespace(Content));

	private static string CollapseWhitespace(string? text) =>
		string.Join(' ', (text ?? string.Empty).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

	public TopicViewModel? CreatePageModel(ApiRenderContext context) => TopicViewModel.Create(this, context);

	public async Task RenderAsync(FileSystemStream stream, ApiRenderContext context, TopicViewModel? pageModel, Cancel ctx = default)
	{
		var viewModel = pageModel ?? TopicViewModel.Create(this, context);
		var slice = TopicView.Create(viewModel);
		await slice.RenderAsync(stream, cancellationToken: ctx);
	}

	public Task<string?> RenderCommonMarkAsync(ApiRenderContext context, TopicViewModel? pageModel, Cancel ctx = default)
	{
		var markdown = new StringBuilder();
		ApiCommonMark.Heading(markdown, 1, Title);
		ApiCommonMark.Prepared(markdown, Content, context.CurrentNavigation.NavigationRoot.Url);
		return Task.FromResult<string?>(markdown.ToString());
	}
}

public class TopicNavigationItem(
	string url,
	ApiTopicPage page,
	IRootNavigationItem<INavigationModel, INavigationItem> root,
	INodeNavigationItem<INavigationModel, INavigationItem> parent
) : ApiPageNavigationItem<ApiTopicPage>(url, page.Title, page, root, parent);

/// <summary>The Topics sidebar heading. It groups the topic pages above the endpoint groups and starts expanded on every page.</summary>
public class TopicsNavigationItem(LandingNavigationItem rootNavigation) : ApiSidebarHeadingNavigationItem(
	Title,
	rootNavigation
), ISidebarExpandedNavigationItem
{
	public const string Title = "Topics";

	/// <summary>Topics listed first, in this order. Every other topic follows alphabetically. Add a title here to pin it.</summary>
	public static readonly string[] PinnedTitles = ["Authentication", "Rate limiting"];

	/// <summary>Pinned topics first, in <see cref="PinnedTitles"/> order, then the rest by title.</summary>
	public static IReadOnlyList<INavigationItem> Order(IEnumerable<INavigationItem> items) =>
		[.. items.OrderBy(PinnedRank).ThenBy(static item => item.NavigationTitle, StringComparer.OrdinalIgnoreCase)];

	private static int PinnedRank(INavigationItem item)
	{
		var rank = Array.FindIndex(PinnedTitles, title => string.Equals(title, item.NavigationTitle, StringComparison.OrdinalIgnoreCase));
		return rank < 0 ? PinnedTitles.Length : rank;
	}
}

public class TopicViewModel(ApiRenderContext context) : ApiViewModel(context)
{
	public required ApiTopicPage Page { get; init; }
	public required HtmlString ContentHtml { get; init; }
	public required IReadOnlyList<TocItem> Headings { get; init; }

	protected override string? LayoutPageTitle => Page.Title;

	protected override string? LayoutPageDescription => Page.Content;

	protected override IReadOnlyList<TocItem> GetTocItems() => Headings;

	public static TopicViewModel Create(ApiTopicPage page, ApiRenderContext context)
	{
		var (contentHtml, headings) = ApiMarkdown.RenderWithHeadings(context, page.Content);
		return new TopicViewModel(context) { Page = page, Headings = headings, ContentHtml = contentHtml };
	}
}
