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
}

[YamlSerializable]
public record Product
{
	public required string Id { get; init; }
	public required string DisplayName { get; init; }
	public VersioningSystem? VersioningSystem { get; init; }
	public string? Repository { get; init; }
}

