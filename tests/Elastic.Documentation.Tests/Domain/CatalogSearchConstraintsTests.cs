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
	public void Laws() => ProductCatalogConstraints.All.Laws(DomainGen.Of<ProductCatalog>(), CounterExamples);

	// each case adds one product that is wrong in one way, so it stands on its own whatever products the generated catalog holds
	private static ProductCatalog Added(ProductCatalog c, KeyValuePair<string, Product> product) =>
		ProductCatalog.FromPublicProducts([.. c.Products, product]);

	private static IReadOnlyDictionary<string, Func<ProductCatalog, ProductCatalog>> CounterExamples =>
		new Dictionary<string, Func<ProductCatalog, ProductCatalog>>
		{
			["product-id-matches-key"] = c => Added(c, new KeyValuePair<string, Product>("Kibana", CatalogFixtures.Kibana)),
			["product-has-display-name"] =
				c => Added(c, new KeyValuePair<string, Product>("kibana", CatalogFixtures.Kibana with { DisplayName = "" })),
			["public-reference-product-has-versioning"] =
				c => Added(c, new KeyValuePair<string, Product>("kibana", CatalogFixtures.Kibana with { VersioningSystem = null }))
		};
}

public class ProductCatalogInVersioningSystemCatalogConstraintsTests
{
	[Test]
	public void Laws() =>
		ProductCatalogInVersioningSystemCatalogConstraints.All.Laws(
			DomainGen.Of<ProductCatalogInVersioningSystemCatalog>(),
			CounterExamples
		);

	private static IReadOnlyDictionary<string, Func<ProductCatalogInVersioningSystemCatalog, ProductCatalogInVersioningSystemCatalog>> CounterExamples =>
		new Dictionary<string, Func<ProductCatalogInVersioningSystemCatalog, ProductCatalogInVersioningSystemCatalog>>
		{
			// a product on the ecs system, and versions.yml without it
			["product-versioning-system-defined"] =
				p =>
					p with
					{
						Products = ProductCatalog.FromPublicProducts([
							.. p.Products.Products,
							new KeyValuePair<string, Product>(
								"kibana",
								CatalogFixtures.Kibana with { VersioningSystem = CatalogFixtures.Versioning(VersioningSystemId.Ecs) }
							)
						]),
						VersioningSystems = new VersioningSystemCatalog
						{
							VersioningSystems = p
								.VersioningSystems
								.VersioningSystems
								.Where(v => v.Key != VersioningSystemId.Ecs)
								.ToDictionary()
						}
					}
		};
}

public class LegacyUrlMappingCatalogInProductCatalogConstraintsTests
{
	[Test]
	public void Laws() =>
		LegacyUrlMappingCatalogInProductCatalogConstraints.All.Laws(
			DomainGen.Of<LegacyUrlMappingCatalogInProductCatalog>(o => o.Normalize<LegacyUrlMappingCatalogInProductCatalog>(p =>
			{
				// every mapping is for a product of the catalog (the catalog gains one when it has none)
				var catalog = p.Products.Products.Count > 0 ? p.Products : CatalogFixtures.Catalog(CatalogFixtures.Kibana);
				var products = catalog.Products.Values.ToList();
				return p with
				{
					Products = catalog,
					LegacyUrlMappings = new LegacyUrlMappingCatalog
					{
						Mappings = [.. p.LegacyUrlMappings.Mappings.Select((m, i) => m with { Product = products[i % products.Count] })]
					}
				};
			})),
			CounterExamples
		);

	private static LegacyUrlMapping Mapping(Product product) =>
		new() { BaseUrl = "en/kibana/", Product = product, LegacyVersions = ["8.19"] };

	private static IReadOnlyDictionary<string, Func<LegacyUrlMappingCatalogInProductCatalog, LegacyUrlMappingCatalogInProductCatalog>> CounterExamples =>
		new Dictionary<string, Func<LegacyUrlMappingCatalogInProductCatalog, LegacyUrlMappingCatalogInProductCatalog>>
		{
			// a mapping for a product the catalog does not have (an empty id, the way a missing 'product' reads)
			["legacy-mapping-product-defined"] =
				p =>
					p with
					{
						LegacyUrlMappings = new LegacyUrlMappingCatalog
						{
							Mappings = [.. p.LegacyUrlMappings.Mappings, Mapping(CatalogFixtures.Kibana with { Id = "" })]
						}
					}
		};
}

public class SearchTuningConstraintsTests
{
	[Test]
	public void Laws() =>
		SearchTuningConstraints.All.Laws(
			DomainGen.Of<SearchTuning>(
				o => o
					.Identity<QueryRule, string>(r => r.RuleId)
					.Normalize<SynonymGroup>(g => new SynonymGroup([.. g.Terms.Select(t => t.Replace(' ', '-'))]))
					.Normalize<QueryRuleCriteria>(
						c =>
							c with
							{
								TypeRaw = c.Type.ToString(),
								Metadata = string.IsNullOrWhiteSpace(c.Metadata) ? "query" : c.Metadata,
								Values = c.Values.Count > 0 ? c.Values : ["search"]
							}
					)
					.Normalize<QueryRule>(
						r =>
							r with
							{
								TypeRaw = r.Type.ToString(),
								Actions = r.Actions.Ids.Count > 0 ? r.Actions : new QueryRuleActions { Ids = ["doc-1"] }
							}
					)
					.Normalize<SearchTuning>(s => s with { SourceFileName = "search.yml" })
			),
			CounterExamples
		);

	private static QueryRule Rule(string id = "rule-1", string[]? ids = null) =>
		new()
		{
			RuleId = id,
			Type = QueryRuleType.Pinned,
			Criteria = [new QueryRuleCriteria { Type = QueryRuleCriteriaType.Exact, Metadata = "query", Values = ["search"] }],
			Actions = new QueryRuleActions { Ids = ids ?? ["doc-1"] }
		};

	private static SearchTuning WithRules(SearchTuning s, params QueryRule[] rules) => s with { Rules = [.. s.Rules, .. rules] };

	private static IReadOnlyDictionary<string, Func<SearchTuning, SearchTuning>> CounterExamples =>
		new Dictionary<string, Func<SearchTuning, SearchTuning>>
		{
			["synonym-terms-single-word"] = s => s with { Synonyms = [.. s.Synonyms, new SynonymGroup(["ice cream", "gelato"])] },
			["query-rule-id-unique"] = s => WithRules(s, Rule("dup"), Rule("dup")),
			["query-rule-complete"] = s => WithRules(s, Rule("incomplete", ids: [])),
			["search-file-not-legacy"] = s => s with { SourceFileName = "synonyms.yml" },
			["query-rule-type-known"] = s => WithRules(s, Rule("typed") with { TypeRaw = "pin" }),
			["query-criteria-type-known"] =
				s => WithRules(s, Rule("criteria") with { Criteria = [Rule().Criteria.First() with { TypeRaw = "" }] })
		};
}
