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

	private ApiOverviewIndex Index => field ??= ApiOverviewBuilder.SplitTopics(OverviewSections);

	/// <summary>The sections the grouped index shows.</summary>
	public IReadOnlyList<ApiOverviewSection> IndexSections => Index.Groups;

	private ApiMarkdown.HtmlSplit DescriptionParts =>
		field ??= ApiMarkdown.SplitAtFirstHeading(RenderMarkdown(ApiInfo.Description).Value ?? "");

	/// <summary>The description up to its first heading; the facts sit right after it on narrow screens.</summary>
	public HtmlString DescriptionLead => new(DescriptionParts.Lead);

	public HtmlString? DescriptionRemainder => DescriptionParts.Remainder is { } rest ? new HtmlString(rest) : null;

	public string? License => ApiInfo.License?.Name;

	public string? Version => string.IsNullOrEmpty(ApiInfo.Version) ? null : ApiInfo.Version;

	/// <summary>The assembler hosts the download in its context bar.</summary>
	public bool ShowSpecDownload => BuildContext.BuildType != BuildType.Assembler;

	public bool HasFacts => Servers.Count > 0 || License is { Length: > 0 } || Version is not null || ShowSpecDownload;

	public IReadOnlyList<ApiServerDisplay> Servers => field ??= StructuralViewModel.ReadServers(Document);

	public string JsonUrl { get; } = ApiOutputPaths.JsonUrl(context.CurrentNavigation.Url);
	public string YamlUrl { get; } = ApiOutputPaths.YamlUrl(context.CurrentNavigation.Url);

	public string? IconSvg { get; } = ProductIcons.Get(context.Product?.Id ?? context.CurrentApiKey);

	protected override string? LayoutPageDescription => ApiInfo.Description;
}
