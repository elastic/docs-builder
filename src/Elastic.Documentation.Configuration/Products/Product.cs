// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration.Versions;
using YamlDotNet.Serialization;

namespace Elastic.Documentation.Configuration.Products;

[YamlSerializable]
public record Product
{
	public required string Id { get; init; }
	public required string DisplayName { get; init; }
	public VersioningSystemId? VersionSystem { get; init; }

	public static IReadOnlyCollection<Product> All(VersionsConfiguration versions) => [.. versions.Products.Values];
	public static IReadOnlyDictionary<string, Product> AllById(VersionsConfiguration versions) => versions.Products;
}

public sealed class ProductEqualityComparer : IEqualityComparer<Product>, IComparer<Product>
{
	public bool Equals(Product? x, Product? y) => x?.Id == y?.Id;
	public int GetHashCode(Product obj) => obj.Id.GetHashCode();

	public int Compare(Product? x, Product? y)
	{
		if (ReferenceEquals(x, y))
			return 0;
		if (y is null)
			return 1;
		if (x is null)
			return -1;
		var idComparison = string.Compare(x.Id, y.Id, StringComparison.OrdinalIgnoreCase);
		if (idComparison != 0)
			return idComparison;
		var displayNameComparison = string.Compare(x.DisplayName, y.DisplayName, StringComparison.OrdinalIgnoreCase);
		return displayNameComparison != 0 ? displayNameComparison : Nullable.Compare(x.VersionSystem, y.VersionSystem);
	}
}
