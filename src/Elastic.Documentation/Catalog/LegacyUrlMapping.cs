// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Catalog;

/// <summary>Maps legacy <c>/guide</c> URL prefixes to products. Part of the <see cref="GlobalCatalog"/> aggregate.</summary>
public sealed record LegacyUrlMappingCatalog
{
	public required IReadOnlyCollection<LegacyUrlMapping> Mappings { get; init; }
}

public sealed record LegacyUrlMapping
{
	public required string BaseUrl { get; init; }
	public required Product Product { get; init; }
	public required IReadOnlyCollection<string> LegacyVersions { get; init; }
}
