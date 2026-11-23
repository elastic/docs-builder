// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.ApiExplorer.Components.PropertyTree;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Model;
using Elastic.ApiExplorer.Operations;
using Elastic.ApiExplorer.Supplemental;
using Elastic.Documentation;
using Microsoft.AspNetCore.Html;

namespace Elastic.ApiExplorer.Landing;

/// <summary>Display form of a tag's external documentation link.</summary>
public record TagExternalDocsDisplay(string Url, bool IsElasticDocs, string LinkText);

public class TagLandingViewModel(ApiRenderContext context) : ApiViewModel(context)
{
	public required ApiTag Tag { get; init; }

	/// <summary>Built before the slice renders so the view only iterates.</summary>
	public required IReadOnlyList<ApiOverviewRow> OverviewRows { get; init; }

	private ApiSupplementalDoc? TagDoc => RenderContext.TagSupplemental.GetValueOrDefault(Tag.Name);

	public string? DescriptionMarkdown => ApiMarkdownFrontMatter.TagDescription(Tag, RenderContext);

	/// <summary>The intro headings that get an anchor: the sidebar links to them, and so does On this page.</summary>
	private IReadOnlyList<TocItem> IntroHeadings =>
		field ??= ApiMarkdown.ReadIntroHeadings(
			RenderContext.MarkdownRenderer,
			DescriptionMarkdown,
			RenderContext.CurrentNavigation.NavigationRoot.Url,
			TagDoc?.PostSections ?? [],
			EndpointsSection.ReservedAnchors
		);

	/// <summary>The intro keeps the anchor ids of its headings, which the sidebar and On this page link to.</summary>
	public HtmlString RenderDescription() =>
		ApiMarkdown.RenderWithHeadingAnchors(
			RenderContext,
			DescriptionMarkdown,
			IntroHeadings.Select(static heading => heading.Slug).ToHashSet(StringComparer.Ordinal)
		);

	/// <summary>On this page, in page order: the intro's headings, the supplemental sections, then the endpoint list.</summary>
	protected override IReadOnlyList<TocItem> GetTocItems() =>
		EndpointsSection.Toc(
			[.. IntroHeadings, .. PostSections.Select(static section => new TocItem(section.Heading, section.Anchor, 3))],
			OverviewRows.Count > 0
		);

	public IReadOnlyList<ApiPostSection> PostSections =>
		field ??= ApiPostSection.From(RenderContext, TagDoc?.PostSections ?? [], EndpointsSection.ReservedAnchors);

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

	protected override string? FallbackPageDescription =>
		ApiFallbackDescription.For(Tag, RenderContext, CurrentNavigationItem.NavigationTitle, OverviewRows);
}
