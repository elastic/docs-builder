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

	/// <summary>The sections the grouped index shows.</summary>
	public IReadOnlyList<ApiOverviewSection> IndexSections => field ??= ApiOverviewBuilder.WithoutGuidePages(OverviewSections);

	/// <summary>The anchors the page itself owns, so a description heading never takes them.</summary>
	public const string EndpointsAnchor = "endpoints";

	private RenderedIntro Intro => field ??= RenderIntro();

	private ApiMarkdown.HtmlSplit DescriptionParts => field ??= ApiMarkdown.SplitAtFirstHeading(Intro.Html.Value ?? "");

	/// <summary>The description up to its first heading; the facts sit right after it on narrow screens.</summary>
	public HtmlString DescriptionLead => new(DescriptionParts.Lead);

	public HtmlString? DescriptionRemainder => DescriptionParts.Remainder is { } rest ? new HtmlString(rest) : null;

	public string Heading => ApiPageTitle.Heading(ApiInfo.Title, ApiPageTitle.ReleasedVersionLabel(RenderContext.VersionSwitcherItems));

	public string? Version => string.IsNullOrEmpty(ApiInfo.Version) ? null : ApiInfo.Version;

	public bool HasFacts => Servers.Count > 0 || Version is not null;

	public IReadOnlyList<ApiServerDisplay> Servers => field ??= StructuralViewModel.ReadServers(Document);

	public string? IconSvg { get; } = ProductIcons.Get(context.Product?.Id ?? context.CurrentApiKey);

	protected override string? LayoutPageDescription => RenderContext.LandingDescription;

	/// <summary>On this page, in page order: the description's headings, then the endpoint groups.</summary>
	protected override IReadOnlyList<TocItem> GetTocItems()
	{
		TocItem[] endpoints = IndexSections.Count > 0 ? [new TocItem("Endpoints", EndpointsAnchor, 2)] : [];
		return [.. Intro.Headings.Where(static heading => heading.Level <= 3), .. endpoints];
	}

	private RenderedIntro RenderIntro()
	{
		var (html, headings) = ApiMarkdown.RenderWithHeadings(
			RenderContext,
			ApiInfo.Description,
			new HashSet<string>([EndpointsAnchor], StringComparer.Ordinal)
		);
		return new RenderedIntro(html, headings);
	}

	private sealed record RenderedIntro(HtmlString Html, IReadOnlyList<TocItem> Headings);
}
