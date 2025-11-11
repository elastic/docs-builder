// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration.Products;

namespace Elastic.Documentation.Configuration.LegacyUrlMappings;

public record LegacyUrlMappingConfiguration
{
	public required IReadOnlyCollection<LegacyUrlMapping> Mappings { get; init; }
}
public record LegacyUrlMapping
{
	public required string BaseUrl { get; init; }
	public required Product Product { get; init; }
	public required IReadOnlyCollection<string> LegacyVersions { get; init; }
}

public record LegacyPageMapping(Product Product, string RawUrl, string Version, bool Exists)
{
	public override string ToString() => RawUrl.Replace("/current/", $"/{Version}/");
}

public interface ILegacyUrlMapper
{
	IReadOnlyCollection<LegacyPageMapping>? MapLegacyUrl(IReadOnlyCollection<string>? mappedPages);
}

public record NoopLegacyUrlMapper : ILegacyUrlMapper
{
	public IReadOnlyCollection<LegacyPageMapping> MapLegacyUrl(IReadOnlyCollection<string>? mappedPages) => [];
}
