// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Immutable;
using Elastic.Documentation.Configuration.Products;

namespace Elastic.Documentation.Configuration.LegacyUrlMappings;

public static class LegacyUrlMappingExtensions
{
	public static LegacyUrlMappingConfiguration CreateLegacyUrlMappings(this ConfigurationFileProvider provider, ProductsConfiguration products)
	{
		var legacyUrlMappingsFilePath = provider.LegacyUrlMappingsFile;

		var legacyUrlMappingsDto = ConfigurationFileProvider.Deserializer.Deserialize<LegacyUrlMappingConfigDto>(legacyUrlMappingsFilePath.OpenText());

		var legacyUrlMappings = legacyUrlMappingsDto.Mappings.Select(kvp =>
			new LegacyUrlMapping
			{
				BaseUrl = kvp.Key,
				Product = products.Products[kvp.Value.Product],
				LegacyVersions = kvp.Value.LegacyVersions.ToImmutableList()
			});

		return new LegacyUrlMappingConfiguration { Mappings = legacyUrlMappings.ToImmutableList() };
	}
}

// Private DTOs for deserialization. These match the YAML structure directly.

internal sealed record LegacyUrlMappingConfigDto
{
	public IEnumerable<string> Stack { get; set; } = [];
	public Dictionary<string, LegacyUrlMappingDto> Mappings { get; set; } = [];
}

internal sealed record LegacyUrlMappingDto
{
	public string Product { get; set; } = string.Empty;
	public IEnumerable<string> LegacyVersions { get; set; } = [];
}
