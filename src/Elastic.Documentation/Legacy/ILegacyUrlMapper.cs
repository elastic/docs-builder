// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Legacy;

public record LegacyPageMapping(string Url, string Version)
{
	public override string ToString() => Url.Replace("/current/", $"/{Version}/");
};

public interface ILegacyUrlMapper
{
	List<LegacyPageMapping> MapLegacyUrl(IReadOnlyCollection<string>? mappedPages);
}

public record NoopLegacyUrlMapper : ILegacyUrlMapper
{
	public List<LegacyPageMapping> MapLegacyUrl(IReadOnlyCollection<string>? mappedPages) => [];
}
