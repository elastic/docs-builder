// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Model;
using Elastic.ApiExplorer.Operations;
using Elastic.ApiExplorer.Structural;
using Elastic.Documentation;
using Elastic.Documentation.Site.Icons;
using Microsoft.AspNetCore.Html;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Landing;

/// <summary>The landing facts render twice: in the right rail on wide screens, under the intro on narrow ones.</summary>
public sealed record LandingFactsModel(LandingViewModel Page, string Placement);

public class LandingViewModel(ApiRenderContext context) : ApiViewModel(context)
{
	public required ApiLanding Landing { get; init; }
	public required OpenApiInfo ApiInfo { get; init; }

	/// <summary>Flattened overview table rows; built before the slice renders.</summary>
	public required IReadOnlyList<ApiOverviewRow> OverviewRows { get; init; }

	public IReadOnlyList<ApiOverviewSection> OverviewSections => field ??= ApiOverviewBuilder.Sections(OverviewRows);

	/// <summary>The endpoint groups in lists of their own, one per classification heading, so each list can carry the group page's frame.</summary>
	public IReadOnlyList<ApiIndexBlock> IndexBlocks => field ??= ApiIndexBlock.Of(ApiOverviewBuilder.WithoutGuidePages(OverviewSections));

	/// <summary>"45 groups": the caption above the endpoint groups.</summary>
	public string GroupsLabel => CountLabel.Of(IndexBlocks.Sum(static block => block.Groups.Count), "group");

	private RenderedIntro Intro => field ??= RenderIntro();

	private ApiMarkdown.HtmlSplit DescriptionParts => field ??= ApiMarkdown.SplitAtFirstHeading(Intro.Html.Value ?? "");

	/// <summary>The description up to its first heading; the facts sit right after it on narrow screens.</summary>
	public HtmlString DescriptionLead => new(DescriptionParts.Lead);

	public HtmlString? DescriptionRemainder => DescriptionParts.Remainder is { } rest ? new HtmlString(rest) : null;

	public string Heading => ApiPageTitle.Heading(ApiInfo.Title, ApiPageTitle.ReleasedVersionLabel(RenderContext.VersionSwitcherItems));

	/// <summary>The minor release this tree renders from, such as <c>9.6</c>. A spec's own <c>info.version</c> is only the fallback, because ECE's reads <c>1</c> for every release.</summary>
	public string? Version => RenderContext.Release ?? (string.IsNullOrEmpty(ApiInfo.Version) ? null : ApiInfo.Version);

	public bool HasFacts => Servers.Count > 0 || Version is not null;

	public IReadOnlyList<ApiServerDisplay> Servers => field ??= StructuralViewModel.ReadServers(Document);

	public string? IconSvg { get; } = ProductIcons.Get(context.Product?.Id ?? context.CurrentApiKey);

	protected override string? LayoutPageDescription => RenderContext.LandingDescription;

	/// <summary>On this page, in page order: the description's headings, then the endpoint groups.</summary>
	protected override IReadOnlyList<TocItem> GetTocItems() => EndpointsSection.Toc(Intro.Headings, IndexBlocks.Count > 0);

	private RenderedIntro RenderIntro()
	{
		var (html, headings) = ApiMarkdown.RenderWithHeadings(RenderContext, ApiInfo.Description, EndpointsSection.ReservedAnchors);
		return new RenderedIntro(html, headings);
	}

	private sealed record RenderedIntro(HtmlString Html, IReadOnlyList<TocItem> Headings);
}

/// <summary>A classification heading (null before the first) and the endpoint groups listed under it.</summary>
public sealed record ApiIndexBlock(string? Heading, IReadOnlyList<ApiOverviewSection> Groups)
{
	public static IReadOnlyList<ApiIndexBlock> Of(IReadOnlyList<ApiOverviewSection> sections)
	{
		var blocks = new List<ApiIndexBlock>();
		string? heading = null;
		var groups = new List<ApiOverviewSection>();
		foreach (var section in sections)
		{
			if (section.Heading is { Kind: OverviewRowKind.ClassificationHeading } classification)
			{
				Flush();
				heading = classification.Title;
				continue;
			}
			if (section.Heading is { Url: not null })
				groups.Add(section);
		}
		Flush();
		return blocks;

		void Flush()
		{
			if (groups.Count > 0)
				blocks.Add(new ApiIndexBlock(heading, groups));
			groups = [];
		}
	}
}
