// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Serialization;
using Amazon.S3.Util;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Configuration.Builder;
using Elastic.Documentation.Legacy;
using Elastic.Documentation.Site;
using Elastic.Documentation.Site.FileProviders;
using Elastic.Documentation.Site.Navigation;
using Elastic.Markdown.IO;
using Elastic.Markdown.IO.Navigation;
using Elastic.Markdown.Myst.FrontMatter;

namespace Elastic.Markdown.Slices;

public class IndexViewModel
{
	public required string SiteName { get; init; }
	public required string DocSetName { get; init; }
	public required string Title { get; init; }
	public required string Description { get; init; }
	public required string TitleRaw { get; init; }
	public required string MarkdownHtml { get; init; }
	public required DocumentationGroup Tree { get; init; }
	public required IReadOnlyCollection<PageTocItem> PageTocItems { get; init; }
	public required MarkdownFile CurrentDocument { get; init; }

	public required INavigationItem? CurrentNavigationItem { get; init; }
	public required INavigationItem? PreviousDocument { get; init; }
	public required INavigationItem? NextDocument { get; init; }
	public required INavigationItem[] Parents { get; init; }

	public required string NavigationHtml { get; init; }
	public required string? CurrentVersion { get; init; }
	public required LegacyPageMapping[] LegacyPages { get; init; }
	public required VersionDrownDownItemViewModel[] VersionDropdownItems { get; init; }
	public required string? UrlPathPrefix { get; init; }
	public required string? GithubEditUrl { get; init; }
	public required string? ReportIssueUrl { get; init; }
	public required ApplicableTo? AppliesTo { get; init; }
	public required bool AllowIndexing { get; init; }
	public required Uri? CanonicalBaseUrl { get; init; }

	public required GoogleTagManagerConfiguration GoogleTagManager { get; init; }

	public required FeatureFlags Features { get; init; }
	public required StaticFileContentHashProvider StaticFileContentHashProvider { get; init; }

	public required HashSet<Product> Products { get; init; }
}

public class VersionDrownDownItemViewModel
{
	[JsonPropertyName("name")]
	public required string Name { get; init; }

	[JsonPropertyName("href")]
	public required string? Href { get; init; }

	[JsonPropertyName("children")]
	public required VersionDrownDownItemViewModel[]? Children { get; init; }

	public static VersionDrownDownItemViewModel[] FromLegacyPageMappings(LegacyPageMapping[] legacyPageMappings)
	{
		var potentialGroups = GetGroupedVersions(legacyPageMappings);
		return potentialGroups.Select(m =>
		{
			if (m.Value.Count != 1)
			{
				return new VersionDrownDownItemViewModel
				{
					Name = m.Key,
					Href = null,
					Children = m.Value.Select(v => new VersionDrownDownItemViewModel
					{
						Name = v,
						Href = legacyPageMappings.First(x => x.Version == v).ToString(),
						Children = null
					}).ToArray()
				};
			}

			var legacyPageMapping = legacyPageMappings.First(x => x.Version == m.Value.First());
			return new VersionDrownDownItemViewModel
			{
				Name = legacyPageMapping.Version,
				Href = legacyPageMapping.ToString(),
				Children = null
			};
		}).ToArray();
	}

	private static Dictionary<string, List<string>> GetGroupedVersions(LegacyPageMapping[] legacyPageMappings) =>
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

[JsonSerializable(typeof(VersionDrownDownItemViewModel[]))]
public partial class ViewModelSerializerContext : JsonSerializerContext;
