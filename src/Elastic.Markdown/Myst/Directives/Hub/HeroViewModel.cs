// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Markdown.Myst.Directives.Hub;

public class HeroViewModel : DirectiveViewModel
{
	public required string? IconKey { get; init; }
	public required string? IconSvg { get; init; }
	public required string? Version { get; init; }
	public required bool ShowSearch { get; init; }
	public required IReadOnlyList<HeroQuickLink> QuickLinks { get; init; }
	public required string? ReleasesHtml { get; init; }
}
