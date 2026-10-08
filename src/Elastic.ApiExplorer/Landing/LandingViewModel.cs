// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Model;
using Elastic.ApiExplorer.Operations;
using Elastic.Documentation.Site.Icons;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Landing;

public class LandingViewModel(ApiRenderContext context) : ApiViewModel(context)
{
	public required ApiLanding Landing { get; init; }
	public required OpenApiInfo ApiInfo { get; init; }

	/// <summary>Flattened overview table rows; built before the slice renders.</summary>
	public required IReadOnlyList<ApiOverviewRow> OverviewRows { get; init; }

	public IReadOnlyList<ApiOverviewSection> OverviewSections => field ??= ApiOverviewBuilder.Sections(OverviewRows);

	private ApiOverviewIndex Index => field ??= ApiOverviewBuilder.SplitTopics(OverviewSections);

	/// <summary>Pages such as Authentication and Servers. They sit above the index, not in it.</summary>
	public IReadOnlyList<ApiOverviewRow> TopicPages => Index.Topics;

	/// <summary>The sections the grouped index shows.</summary>
	public IReadOnlyList<ApiOverviewSection> IndexSections => Index.Groups;

	public string JsonUrl { get; } = ApiOutputPaths.JsonUrl(context.CurrentNavigation.Url);
	public string YamlUrl { get; } = ApiOutputPaths.YamlUrl(context.CurrentNavigation.Url);

	public string? IconSvg { get; } = ProductIcons.Get(context.Product?.Id ?? context.CurrentApiKey);

	protected override string? LayoutPageDescription => ApiInfo.Description;
}
