// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration.LegacyUrlMappings;
using Elastic.Documentation.Configuration.Versions;

namespace Elastic.Documentation.LegacyDocs;

public record PageLegacyUrlMapper : ILegacyUrlMapper
{
	private LegacyPageService LegacyPageService { get; }
	private string DefaultVersion { get; }
	private LegacyUrlMappingConfiguration LegacyUrlMappings { get; }

	public PageLegacyUrlMapper(LegacyPageService legacyPageService, VersionsConfiguration versions, LegacyUrlMappingConfiguration legacyUrlMappings)
	{
		LegacyPageService = legacyPageService;
		DefaultVersion = $"{versions.VersioningSystems[VersioningSystemId.Stack].Base.Major}.{versions.VersioningSystems[VersioningSystemId.Stack].Base.Minor}";
		LegacyUrlMappings = legacyUrlMappings;
	}


	public IReadOnlyCollection<LegacyPageMapping>? MapLegacyUrl(IReadOnlyCollection<string>? mappedPages)
	{
		if (mappedPages is null || mappedPages.Count == 0)
			return null;

		var mappedPage = mappedPages.First();

		if (LegacyUrlMappings.Mappings.FirstOrDefault(x => mappedPage.Contains(x.BaseUrl, StringComparison.OrdinalIgnoreCase)) is not { } legacyMappingMatch)
			return [new LegacyPageMapping(LegacyUrlMappings.Mappings.First(x => x.Product.Id.Equals("elastic-stack", StringComparison.OrdinalIgnoreCase)).Product, mappedPages.FirstOrDefault() ?? string.Empty, DefaultVersion, false)];

		var allVersions = new List<LegacyPageMapping>();

		allVersions.AddRange(legacyMappingMatch.LegacyVersions.Select(x =>
		{
			var mapping = new LegacyPageMapping(legacyMappingMatch.Product, mappedPage, x, false);
			var path = Uri.TryCreate(mapping.ToString(), UriKind.Absolute, out var uri) ? uri : null;
			var exists = path is not null && LegacyPageService.PathExists(path.AbsolutePath);
			return mapping with { Exists = exists };
		}));

		return allVersions;
	}
}
