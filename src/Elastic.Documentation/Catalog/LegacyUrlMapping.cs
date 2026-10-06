// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Generator.Equals;

namespace Elastic.Documentation.Catalog;

/// <summary>Maps legacy <c>/guide</c> URL prefixes to products. Part of the <see cref="GlobalCatalog"/> aggregate.</summary>
[Equatable]
public sealed partial record LegacyUrlMappingCatalog
{
	[OrderedEquality]
	public IReadOnlyCollection<LegacyUrlMapping> Mappings { get; internal init; } = default!;

	public static LegacyUrlMappingCatalog None { get; } = new() { Mappings = [] };
}

[Equatable]
public sealed partial record LegacyUrlMapping
{
	public string BaseUrl { get; internal init; } = default!;
	[DefaultEquality]
	public Product Product { get; internal init; } = default!;
	[OrderedEquality]
	public IReadOnlyCollection<string> LegacyVersions { get; internal init; } = default!;
}
