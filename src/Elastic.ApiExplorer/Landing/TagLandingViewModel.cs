// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.ApiExplorer.Components.PropertyTree;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Model;
using Elastic.ApiExplorer.Operations;
using Elastic.ApiExplorer.Supplemental;
using Microsoft.AspNetCore.Html;

namespace Elastic.ApiExplorer.Landing;

/// <summary>Display form of a tag's external documentation link.</summary>
public record TagExternalDocsDisplay(string Url, bool IsElasticDocs, string LinkText);

public class TagLandingViewModel(ApiRenderContext context) : ApiViewModel(context)
{
	public required ApiTag Tag { get; init; }

	/// <summary>Built before the slice renders so the view only iterates.</summary>
	public required IReadOnlyList<ApiOverviewRow> OverviewRows { get; init; }

	public string? DescriptionMarkdown
	{
		get
		{
			if (RenderContext.TagSupplemental.TryGetValue(Tag.Name, out var doc))
				return doc.DescriptionOr(Tag.Description);
			return Tag.Description;
		}
	}

	/// <summary>The intro keeps the heading anchor ids the sidebar links to, from <see cref="IntroHeadings.ForPage"/>.</summary>
	public HtmlString RenderDescription()
	{
		var postSections = RenderContext.TagSupplemental.TryGetValue(Tag.Name, out var doc) ? doc.PostSections : [];
		var root = RenderContext.CurrentNavigation.NavigationRoot.Url;
		var headingAnchors = ApiMarkdown
			.ReadIntroHeadings(RenderContext.MarkdownRenderer, DescriptionMarkdown, root, postSections)
			.Select(heading => heading.Slug)
			.ToHashSet(StringComparer.Ordinal);
		return ApiMarkdown.RenderWithHeadingAnchors(RenderContext, DescriptionMarkdown, headingAnchors);
	}

	public IReadOnlyList<ApiPostSection> PostSections =>
		RenderContext.TagSupplemental.TryGetValue(Tag.Name, out var doc) ? ApiPostSection.From(RenderContext, doc.PostSections) : [];

	public TagExternalDocsDisplay? ExternalDocsDisplay =>
		Tag.ExternalDocs is null
			? null
			: new TagExternalDocsDisplay(
				Tag.ExternalDocs.Url,
				ApiPropertyTreeBuilder.IsElasticDocsUrl(Tag.ExternalDocs.Url),
				string.IsNullOrWhiteSpace(Tag.ExternalDocs.Description) ? "Documentation" : Tag.ExternalDocs.Description
			);

	/// <inheritdoc />
	protected override string? LayoutPageTitle => Tag.DisplayName;

	protected override string? LayoutPageDescription => DescriptionMarkdown;
}
