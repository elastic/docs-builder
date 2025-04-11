// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Markdown.IO.HistoryMapping;

public record LegacyPageMapping(string Url, string Version);

public interface IHistoryMapper
{
	LegacyPageMapping? MapLegacyUrl(IReadOnlyCollection<string>? mappedPages);
}

public record BypassHistoryMapper : IHistoryMapper
{
	public LegacyPageMapping? MapLegacyUrl(IReadOnlyCollection<string>? mappedPages) => null;
}
