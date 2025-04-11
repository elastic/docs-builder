// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.IO.HistoryMapping;

namespace Documentation.Assembler.Mapping;

public record PageHistoryMapper : IHistoryMapper
{
	private IReadOnlyDictionary<string, string> PreviousUrls { get; }

	public PageHistoryMapper(IReadOnlyDictionary<string, string> previousUrls) => PreviousUrls = previousUrls;

	public LegacyPageMapping? MapLegacyUrl(IReadOnlyCollection<string>? mappedPages)
	{
		if (mappedPages is null)
			return null;

		foreach (var mappedPage in mappedPages)
		{
			var versionMarker = PreviousUrls.FirstOrDefault(x => mappedPage.Contains(x.Key));
			if (versionMarker.Key != string.Empty && versionMarker.Value != "undefined")
			{
				return mappedPage.Contains("current")
					? new LegacyPageMapping(mappedPage.Replace($"{versionMarker.Key}current/", $"{versionMarker.Key}{versionMarker.Value}/"), versionMarker.Value)
					: null;
			}
		}

		return new LegacyPageMapping(mappedPages.FirstOrDefault() ?? string.Empty, string.Empty);
	}
}
