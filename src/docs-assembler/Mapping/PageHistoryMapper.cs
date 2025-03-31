// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.IO.HistoryMapping;

namespace Documentation.Assembler.Mapping;

public record PageHistoryMapper : IHistoryMapper
{
	private IReadOnlyDictionary<string, string> PreviousUrls { get; }

	public PageHistoryMapper(IReadOnlyDictionary<string, string> previousUrls) => PreviousUrls = previousUrls;

	public string? MapLegacyUrl(string? currentUrl)
	{
		if (currentUrl is null)
			return null;

		var versionMarker = PreviousUrls.FirstOrDefault(x => currentUrl.Contains(x.Key));
		if (versionMarker.Key == string.Empty)
			return null;

		return !currentUrl.Contains("current") ? null : currentUrl.Replace($"{versionMarker.Key}/current/", $"{versionMarker.Key}/{versionMarker.Value}/");
	}
}
