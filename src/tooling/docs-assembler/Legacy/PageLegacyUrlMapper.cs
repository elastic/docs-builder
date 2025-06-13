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

	public PageLegacyUrlMapper(IReadOnlyDictionary<string, IReadOnlyCollection<string>> previousUrls) => PreviousUrls = previousUrls;

	public IReadOnlyCollection<LegacyPageMapping> MapLegacyUrl(IReadOnlyCollection<string>? mappedPages)
	{
		if (mappedPages is null)
			return [];

		if (mappedPages.Count == 0)
			return [new LegacyPageMapping(mappedPages.FirstOrDefault() ?? string.Empty, string.Empty)];

		var mappedPage = mappedPages.First();

		var versions = PreviousUrls.FirstOrDefault(kv =>
		{
			var (key, _) = kv;
			return mappedPage.Contains(key, StringComparison.OrdinalIgnoreCase);
		});

		if (versions.Value is null)
			return [new LegacyPageMapping(mappedPages.FirstOrDefault() ?? string.Empty, string.Empty)];
		return versions.Value
			.Select(
				v => new LegacyPageMapping(mappedPage, v)
			).ToArray();
	}
}
