// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using Elastic.Documentation.Configuration.Versions;
using Elastic.Documentation.Legacy;
using Elastic.Documentation.LegacyDocs;

namespace Documentation.Assembler.Legacy;

public record PageLegacyUrlMapper : ILegacyUrlMapper
{
	private LegacyPageChecker LegacyPageChecker { get; }
	private string DefaultVersion { get; }
	private FrozenDictionary<string, IReadOnlyCollection<string>> HistoryMappings { get; }
	public PageLegacyUrlMapper(LegacyPageChecker legacyPageChecker, VersionsConfiguration versions, FrozenDictionary<string, IReadOnlyCollection<string>> historyMappings)
	{
		LegacyPageChecker = legacyPageChecker;
		DefaultVersion = $"{versions.VersioningSystems[VersioningSystemId.Stack].Base.Major}.{versions.VersioningSystems[VersioningSystemId.Stack].Base.Minor}";
		HistoryMappings = historyMappings;
	}


	public IReadOnlyCollection<LegacyPageMapping>? MapLegacyUrl(string productId, IReadOnlyCollection<string>? mappedPages)
	{
		if (mappedPages is null)
			return null;

		if (mappedPages.Count == 0)
			return [new LegacyPageMapping(mappedPages.FirstOrDefault() ?? string.Empty, DefaultVersion, false)];

		var mappedPage = mappedPages.First();

		if (!HistoryMappings.TryGetValue(productId, out var productInfo))
		{
			return [new LegacyPageMapping(mappedPages.FirstOrDefault() ?? string.Empty, DefaultVersion, false)];
		}

		var allVersions = new List<LegacyPageMapping>();

		allVersions.AddRange(productInfo.Select(v =>
		{
			var mapping = new LegacyPageMapping(mappedPage, v, true);
			var path = Uri.TryCreate(mapping.ToString(), UriKind.Absolute, out var uri) ? uri : null;
			var exists = path is not null && LegacyPageChecker.PathExists(path.AbsolutePath);
			return mapping with { Exists = exists };
		}));

		return allVersions;
	}
}
