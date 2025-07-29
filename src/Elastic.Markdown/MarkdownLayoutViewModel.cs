// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Legacy;
using Elastic.Documentation.Site;
using Elastic.Documentation.Site.Navigation;

namespace Elastic.Markdown;

public record MarkdownLayoutViewModel : GlobalLayoutViewModel
{
	public required bool IsAssemblerBuild { get; init; }
	public required string? GithubEditUrl { get; init; }

	public required string? ReportIssueUrl { get; init; }

	public required INavigationItem[] Parents { get; init; }

	public required LegacyPageMapping[]? LegacyPages { get; init; }

	public required IReadOnlyCollection<PageTocItem> PageTocItems { get; init; }

	public required MarkdownPageLayout? Layout { get; init; }

	public required string? VersionDropdownSerializedModel { get; init; }

	public required string? CurrentVersion { get; init; }

	public required string? AllVersionsUrl { get; init; }
}

public record PageTocItem
{
	public required string Heading { get; init; }
	public required string Slug { get; init; }
	public required int Level { get; init; }
}
