// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Legacy;
using Elastic.Documentation.LegacyPageLookup;

namespace Documentation.Assembler.Legacy;

public record PageLegacyUrlMapper : ILegacyUrlMapper
{
	private IReadOnlyDictionary<string, IReadOnlyCollection<string>> PreviousUrls { get; }
	private LegacyPageLookup LegacyPageLookup { get; }
	public PageLegacyUrlMapper(LegacyPageLookup legacyPageLookup, IReadOnlyDictionary<string, IReadOnlyCollection<string>> previousUrls)
	{
		PreviousUrls = previousUrls;
		LegacyPageLookup = legacyPageLookup;
	}

	public IReadOnlyCollection<LegacyPageMapping> MapLegacyUrl(IReadOnlyCollection<string>? mappedPages)
	{
		if (mappedPages is null)
			return [];

		if (mappedPages.Count == 0)
			return [new LegacyPageMapping(mappedPages.FirstOrDefault() ?? string.Empty, string.Empty, false)];

		var mappedPage = mappedPages.First();

		var versions = PreviousUrls.FirstOrDefault(kv =>
		{
			var (key, _) = kv;
			return mappedPage.Contains(key, StringComparison.OrdinalIgnoreCase);
		});

		if (versions.Value is null)
			return [new LegacyPageMapping(mappedPages.FirstOrDefault() ?? string.Empty, string.Empty, false)];
		return versions.Value
			.Select(v =>
				{
					var legacyPageMapping = new LegacyPageMapping(mappedPage, v, true);
					var path = Uri.TryCreate(legacyPageMapping.ToString(), UriKind.Absolute, out var uri) ? uri : null;
					var exists = LegacyPageLookup.PathExists(path?.AbsolutePath!);
					return legacyPageMapping with { Exists = exists };
				}
			).ToArray();
	}
}
