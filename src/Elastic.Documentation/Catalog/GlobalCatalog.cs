// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Catalog;

/// <summary>
/// Global reference data every documentation set, changelog and search document points at: the versioning systems,
/// the products that use them, and the legacy URL mappings that point at those products. Loaded once per process,
/// as one unit, from <c>versions.yml</c>, <c>products.yml</c> and <c>legacy-url-mappings.yml</c>, in that order.
/// </summary>
public sealed record GlobalCatalog(
	VersioningSystemCatalog VersioningSystems,
	ProductCatalog Products,
	LegacyUrlMappingCatalog LegacyUrlMappings
);
