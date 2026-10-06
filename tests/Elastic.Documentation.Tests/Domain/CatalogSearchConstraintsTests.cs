// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using Elastic.Documentation.Catalog;
using Elastic.Documentation.Search;
using Elastic.Documentation.Versions;
using Nullean.Entities.CsCheck;

namespace Elastic.Documentation.Tests.Domain;

internal static class CatalogFixtures
{
	public static VersioningSystem Versioning(VersioningSystemId id) =>
		new() { Id = id, Base = (SemVersion)"8.0.0", Current = (SemVersion)"9.1.0" };

	public static Product Kibana { get; } = new()
	{
		Id = "kibana",
		DisplayName = "Kibana",
		VersioningSystem = Versioning(VersioningSystemId.Stack)
	};

	public static ProductCatalog Catalog(params KeyValuePair<string, Product>[] products)
	{
		var byId = products.ToFrozenDictionary();
		return new ProductCatalog
		{
			Products = byId,
			PublicReferenceProducts = byId,
			ProductDisplayNames = byId.ToFrozenDictionary(p => p.Key, p => p.Value.DisplayName)
		};
	}

	public static ProductCatalog Catalog(params Product[] products) =>
		Catalog([.. products.Select(p => new KeyValuePair<string, Product>(p.Id, p))]);
}

public class ProductCatalogConstraintsTests
{
	[Test]
	public void Laws() => ProductCatalogConstraints.All.Laws(Valid(), CounterExamples);

	private static ProductCatalog Valid() => CatalogFixtures.Catalog(CatalogFixtures.Kibana);

	private static IReadOnlyDictionary<string, Func<ProductCatalog, ProductCatalog>> CounterExamples =>
		new Dictionary<string, Func<ProductCatalog, ProductCatalog>>
		{
			["product-id-matches-key"] = _ => CatalogFixtures.Catalog(new KeyValuePair<string, Product>("Kibana", CatalogFixtures.Kibana)),
			["product-has-display-name"] = _ => CatalogFixtures.Catalog(CatalogFixtures.Kibana with { DisplayName = "" }),
			["public-reference-product-has-versioning"] =
				_ => CatalogFixtures.Catalog(CatalogFixtures.Kibana with { VersioningSystem = null })
		};
}

public class ProductCatalogInVersioningSystemCatalogConstraintsTests
{
	[Test]
	public void Laws() => ProductCatalogInVersioningSystemCatalogConstraints.All.Laws(Valid(), CounterExamples);

	private static ProductCatalogInVersioningSystemCatalog Valid() =>
		new(
			CatalogFixtures.Catalog(CatalogFixtures.Kibana),
			new VersioningSystemCatalog
			{
				VersioningSystems = new Dictionary<VersioningSystemId, VersioningSystem>
				{
					[VersioningSystemId.Stack] = CatalogFixtures.Versioning(VersioningSystemId.Stack)
				}
			}
		);

	private static IReadOnlyDictionary<string, Func<ProductCatalogInVersioningSystemCatalog, ProductCatalogInVersioningSystemCatalog>> CounterExamples =>
		new Dictionary<string, Func<ProductCatalogInVersioningSystemCatalog, ProductCatalogInVersioningSystemCatalog>>
		{
			["product-versioning-system-defined"] =
				p =>
					p with
					{
						Products = CatalogFixtures.Catalog(CatalogFixtures.Kibana with
						{
							VersioningSystem = CatalogFixtures.Versioning(VersioningSystemId.Ecs)
						})
					}
		};
}

public class LegacyUrlMappingCatalogInProductCatalogConstraintsTests
{
	[Test]
	public void Laws() => LegacyUrlMappingCatalogInProductCatalogConstraints.All.Laws(Valid(), CounterExamples);

	private static LegacyUrlMappingCatalog Mappings(Product product) =>
		new() { Mappings = [new LegacyUrlMapping { BaseUrl = "en/kibana/", Product = product, LegacyVersions = ["8.19"] }] };

	private static LegacyUrlMappingCatalogInProductCatalog Valid() =>
		new(Mappings(CatalogFixtures.Kibana), CatalogFixtures.Catalog(CatalogFixtures.Kibana));

	private static IReadOnlyDictionary<string, Func<LegacyUrlMappingCatalogInProductCatalog, LegacyUrlMappingCatalogInProductCatalog>> CounterExamples =>
		new Dictionary<string, Func<LegacyUrlMappingCatalogInProductCatalog, LegacyUrlMappingCatalogInProductCatalog>>
		{
			["legacy-mapping-product-defined"] = p => p with { LegacyUrlMappings = Mappings(CatalogFixtures.Kibana with { Id = "" }) }
		};
}

public class SearchTuningConstraintsTests
{
	[Test]
	public void Laws() => SearchTuningConstraints.All.Laws(Valid(), CounterExamples);

	private static QueryRule Rule(string id = "rule-1", string[]? ids = null, string metadata = "query", string[]? values = null) =>
		new()
		{
			RuleId = id,
			Type = QueryRuleType.Pinned,
			Criteria = [new QueryRuleCriteria { Type = QueryRuleCriteriaType.Exact, Metadata = metadata, Values = values ?? ["search"] }],
			Actions = new QueryRuleActions { Ids = ids ?? ["doc-1"] }
		};

	private static SearchTuning With(string[][]? synonyms = null, params QueryRule[] rules) =>
		new()
		{
			Synonyms = [.. (synonyms ?? [["car", "auto"]]).Select(t => new SynonymGroup(t))],
			Rules = rules.Length == 0 ? [Rule()] : rules,
			DiminishTerms = []
		};

	private static SearchTuning Valid() => With();

	private static IReadOnlyDictionary<string, Func<SearchTuning, SearchTuning>> CounterExamples =>
		new Dictionary<string, Func<SearchTuning, SearchTuning>>
		{
			["synonym-terms-single-word"] = _ => With([["ice cream", "gelato"]]),
			["query-rule-id-unique"] = _ => With(null, Rule(), Rule()),
			["query-rule-complete"] = _ => With(null, Rule(ids: [])),
			["search-file-not-legacy"] = _ => With() with { SourceFileName = "synonyms.yml" },
			["query-rule-type-known"] = _ => With(null, Rule() with { TypeRaw = "pin" }),
			["query-criteria-type-known"] = _ => With(null, Rule() with { Criteria = [Rule().Criteria.First() with { TypeRaw = "" }] })
		};
}
