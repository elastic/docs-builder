// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Legacy;

public record LegacyPageMapping(string RawUrl, string Version, bool Exists)
{
	public override string ToString() => RawUrl.Replace("/current/", $"/{Version}/");
};

public interface ILegacyUrlMapper
{
	IReadOnlyCollection<LegacyPageMapping>? MapLegacyUrl(string productId, IReadOnlyCollection<string>? mappedPages);
}

public record NoopLegacyUrlMapper : ILegacyUrlMapper
{
	public IReadOnlyCollection<LegacyPageMapping> MapLegacyUrl(string productId, IReadOnlyCollection<string>? mappedPages) => [];
}
