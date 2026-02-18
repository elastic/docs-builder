// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Serialization;
using Elastic.Documentation;
using Elastic.Documentation.AppliesTo;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Configuration.Builder;
using Elastic.Documentation.Configuration.LegacyUrlMappings;
using Elastic.Documentation.Configuration.Products;
using Elastic.Documentation.Configuration.Versions;
using Elastic.Documentation.Navigation;
using Elastic.Documentation.Site.FileProviders;
using Elastic.Markdown.IO;
using Elastic.Markdown.Myst.Components;

namespace Elastic.Markdown.Page;

public class IndexViewModel
{
	public required BuildType BuildType { get; init; }
	public required string SiteName { get; init; }
	public required string DocSetName { get; init; }
	public required string Title { get; init; }
	public required string Description { get; init; }
	public required string TitleRaw { get; init; }
	public required string MarkdownHtml { get; init; }
	public required IReadOnlyCollection<PageTocItem> PageTocItems { get; init; }
	public required MarkdownFile CurrentDocument { get; init; }
	public required INavigationItem CurrentNavigationItem { get; init; }
	public required INavigationItem? PreviousDocument { get; init; }
	public required INavigationItem? NextDocument { get; init; }
	public required INavigationItem[] Breadcrumbs { get; init; }

	public required string NavigationHtml { get; init; }

	public required string CurrentVersion { get; init; }

	public required string? AllVersionsUrl { get; init; }
	public required LegacyPageMapping[]? LegacyPages { get; init; }
	public required VersionDropDownItemViewModel[]? VersionDropdownItems { get; init; }
	public required string? UrlPathPrefix { get; init; }
	public required string? GithubEditUrl { get; init; }
	public required string MarkdownUrl { get; init; }
	public required string? ReportIssueUrl { get; init; }
	public required ApplicableTo? AppliesTo { get; init; }
	public required bool AllowIndexing { get; init; }
	public required Uri? CanonicalBaseUrl { get; init; }

	public required GoogleTagManagerConfiguration GoogleTagManager { get; init; }
	public required OptimizelyConfiguration Optimizely { get; init; }

	public required FeatureFlags Features { get; init; }
	public required StaticFileContentHashProvider StaticFileContentHashProvider { get; init; }

	public required HashSet<Product> Products { get; init; }

	public required VersioningSystem VersioningSystem { get; init; }
	public required VersionsConfiguration VersionsConfig { get; init; }

	// https://developers.google.com/search/docs/appearance/structured-data/breadcrumb#json-ld
	public required string StructuredBreadcrumbsJson { get; init; }

	// Git info for isolated header
	public string? GitBranch { get; init; }
	public string? GitCommitShort { get; init; }
	public string? GitRepository { get; init; }
	public string? GitHubDocsUrl { get; init; }
	public string? GitHubRef { get; init; }

	/// <summary>Codex site header title. When set (codex builds), overrides DocSetName in the header.</summary>
	public string? SiteHeaderTitle { get; set; }

	/// <summary>Pre-computed site root path for HTMX. When set (codex builds), used as data-root-path.</summary>
	public string? SiteRootPath { get; set; }
}

public class VersionDropDownItemViewModel
{
	[JsonPropertyName("name")]
	public required string Name { get; init; }

	[JsonPropertyName("href")]
	public required string? Href { get; init; }

	[JsonPropertyName("disabled")]
	public required bool IsDisabled { get; init; }

	[JsonPropertyName("children")]
	public required VersionDropDownItemViewModel[]? Children { get; init; }

	// This logic currently only handles one level of children. Although the model supports multiple levels, it is not currently used.
	public static VersionDropDownItemViewModel[]? FromLegacyPageMappings(LegacyPageMapping[]? legacyPageMappings)
	{
		if (legacyPageMappings is null || legacyPageMappings.Length == 0)
			return null;
		var groupedVersions = GroupByMajorVersion(legacyPageMappings);

		List<VersionDropDownItemViewModel> versions = [];
		foreach (var versionGroup in groupedVersions)
		{
			if (versionGroup.Value.Count != 1)
			{
				versions.Add(new VersionDropDownItemViewModel
				{
					Name = versionGroup.Key,
					Href = null,
					IsDisabled = false,
					Children = versionGroup.Value.Select(v => new VersionDropDownItemViewModel
					{
						Name = v,
						Href = legacyPageMappings.First(x => x.Version == v).ToString(),
						IsDisabled = !legacyPageMappings.First(x => x.Version == v).Exists,
						Children = null
					}).ToArray()
				});
			}
			else
			{
				var legacyPageMapping = legacyPageMappings.First(x => x.Version == versionGroup.Value.First());

				versions.Add(new VersionDropDownItemViewModel
				{
					Name = legacyPageMapping.Version,
					Href = legacyPageMapping.ToString(),
					IsDisabled = !legacyPageMapping.Exists,
					Children = null
				});
			}
		}

		return versions.ToArray();
	}

	// The legacy page mappings provide a list of versions.
	// But in the actual dropdown, we want to group them by major version
	// E.g., 8.0 – 8.18 should be grouped under 8.x
	private static Dictionary<string, List<string>> GroupByMajorVersion(LegacyPageMapping[] legacyPageMappings) =>
		legacyPageMappings.Aggregate<LegacyPageMapping, Dictionary<string, List<string>>>([], (acc, curr) =>
		{
			var major = curr.Version.Split('.')[0];
			if (!int.TryParse(major, out _))
				return acc;
			var key = $"{major}.x";
			if (!acc.TryGetValue(key, out var value))
				acc[key] = [curr.Version];
			else
				value.Add(curr.Version);
			return acc;
		});
}

[JsonSerializable(typeof(VersionDropDownItemViewModel[]))]
[JsonSerializable(typeof(ApplicabilityRenderer.PopoverData))]
public partial class ViewModelSerializerContext : JsonSerializerContext;
