// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using Elastic.Documentation.Configuration.Versions;
using YamlDotNet.Serialization;

namespace Elastic.Documentation.Configuration.Products;

public record ProductsConfiguration
{
	public required FrozenDictionary<string, Product> Products { get; init; }


	public Product? GetProductByRepositoryName(string repository)
	{
		var tokens = repository.Split('/');
		var repositoryName = tokens.Last();
		if (Products.TryGetValue(repositoryName, out var product))
			return product;
		var match = Products.Values.SingleOrDefault(p => p.Repository is not null && p.Repository.Equals(repositoryName, StringComparison.OrdinalIgnoreCase));
		return match;
	}
}

[YamlSerializable]
public record ProductLink
{
	public string Id { get; set; } = string.Empty;
}

[YamlSerializable]
public record Product
{
	public required string Id { get; init; }
	public required string DisplayName { get; init; }
	public VersioningSystem? VersioningSystem { get; init; }
	public string? Repository { get; init; }
}

