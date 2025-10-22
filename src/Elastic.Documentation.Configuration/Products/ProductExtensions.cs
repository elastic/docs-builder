// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using Elastic.Documentation.Configuration.Versions;

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
				VersioningSystem = versionsConfiguration.GetVersioningSystem(VersionsConfigurationExtensions.ToVersioningSystemId(kvp.Value.Versioning ?? kvp.Key)),
				Repository = kvp.Value.Repository ?? kvp.Key
			});

		return new ProductsConfiguration
		{
			Products = products.ToFrozenDictionary()
		};
	}
}

// Private DTOs for deserialization. These match the YAML structure directly.

internal sealed record ProductConfigDto
{
	public Dictionary<string, ProductDto> Products { get; set; } = [];
}
internal sealed record ProductDto
{
	public string Display { get; set; } = string.Empty;
	public string? Versioning { get; set; }
	public string? Repository { get; set; }
}
