// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using Elastic.Documentation.Configuration.Versions;
using YamlDotNet.Serialization;

namespace Elastic.Documentation.Configuration.Products;

public static class ProductExtensions
{
	public static ProductsConfiguration CreateProducts(this ConfigurationFileProvider provider, VersionsConfiguration versionsConfiguration)
	{
		var productsFilePath = provider.ProductsFile;

		var productsDto = ConfigurationFileProvider.Deserializer.Deserialize<ProductConfigDto>(productsFilePath.OpenText());

		var products = productsDto.Products.ToDictionary(
			kvp => kvp.Key,
			kvp => new Product
			{
				Id = kvp.Key,
				DisplayName = kvp.Value.Display,
				VersioningSystem = ResolveVersioningSystem(versionsConfiguration, kvp.Value.Versioning ?? kvp.Key),
				Repository = kvp.Value.Repository ?? kvp.Key,
				Features = ResolveFeatures(kvp.Key, kvp.Value.Features)
			});

		var publicReferenceProducts = products
			.Where(kvp => kvp.Value.Features.PublicReference)
			.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

		var productDisplayNames = productsDto.Products.ToDictionary(
			kvp => kvp.Key,
			kvp => kvp.Value.Display);

		return new ProductsConfiguration
		{
			Products = products.ToFrozenDictionary(),
			PublicReferenceProducts = publicReferenceProducts.ToFrozenDictionary(),
			ProductDisplayNames = productDisplayNames.ToFrozenDictionary()
		};
	}

	private static VersioningSystem? ResolveVersioningSystem(VersionsConfiguration versionsConfiguration, string id) =>
		VersioningSystemIdExtensions.TryParse(id, out var versioningSystemId, ignoreCase: true, allowMatchingMetadataAttribute: true)
			? versionsConfiguration.GetVersioningSystem(versioningSystemId)
			: null;

	private static ProductFeatures ResolveFeatures(string productId, Dictionary<string, bool>? featuresDto)
	{
		if (featuresDto is null)
			return ProductFeatures.All;

		var unknownKeys = featuresDto.Keys
			.Where(k => !ProductFeatures.KnownKeys.Contains(k))
			.ToList();

		if (unknownKeys is { Count: > 0 })
		{
			var known = string.Join(", ", ProductFeatures.KnownKeys.Order());
			throw new InvalidOperationException(
				$"Product '{productId}' has unknown feature key(s): {string.Join(", ", unknownKeys)}. Known features: {known}."
			);
		}

		return new ProductFeatures
		{
			PublicReference = featuresDto.GetValueOrDefault("public-reference"),
			ReleaseNotes = featuresDto.GetValueOrDefault("release-notes")
		};
	}
}

// Private DTOs for deserialization. These match the YAML structure directly.

internal sealed record ProductConfigDto
{
	[YamlMember(Alias = "products")]
	public Dictionary<string, ProductDto> Products { get; set; } = [];
}
internal sealed record ProductDto
{
	[YamlMember(Alias = "display")]
	public string Display { get; set; } = string.Empty;

	[YamlMember(Alias = "versioning")]
	public string? Versioning { get; set; }

	public string? Repository { get; set; }

	[YamlMember(Alias = "features")]
	public Dictionary<string, bool>? Features { get; set; }
}
