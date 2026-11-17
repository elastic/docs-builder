// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Nullean.Entities;

namespace Elastic.Documentation.Catalog;

/// <summary>
/// Every rule a <see cref="LegacyUrlMappingCatalog"/> must satisfy on its own, in the order they are reported. None: what a mapping points at is a rule
/// of <see cref="LegacyUrlMappingCatalogInProductCatalogConstraints"/>.
/// </summary>
[Constraints<LegacyUrlMappingCatalog>]
public static partial class LegacyUrlMappingConstraints;
