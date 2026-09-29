// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using AwesomeAssertions;
using Elastic.Documentation.Configuration.Loading;
using static Elastic.Documentation.Configuration.Tests.Golden.Golden;

namespace Elastic.Documentation.Configuration.Tests.Golden;

/// <summary>
/// Loading the same input twice yields equal aggregates, and a change anywhere in an aggregate makes it unequal.
/// This is what value equality on the domain records buys: caches, change detection and property tests can compare
/// aggregates directly instead of projecting them.
/// </summary>
public class AggregateEqualityTests
{
	[Test]
	public void GlobalCatalog_LoadedTwice_IsEqual() =>
		CatalogLoader.Load(BaselineProvider()).OrThrow().Should().Be(CatalogLoader.Load(BaselineProvider()).OrThrow());

	[Test]
	public void GlobalCatalog_OneProductRenamed_IsNotEqual()
	{
		var catalog = CatalogLoader.Load(BaselineProvider()).OrThrow();
		var (id, product) = catalog.Products.Products.First();
		var renamed = catalog with
		{
			Products = catalog.Products with
			{
				Products = catalog
					.Products
					.Products
					.Select(kv => kv.Key == id ? new(kv.Key, product with { DisplayName = product.DisplayName + " (renamed)" }) : kv)
					.ToFrozenDictionary()
			}
		};

		renamed.Should().NotBe(catalog);
	}

	[Test]
	public void SearchTuning_LoadedTwice_IsEqual() =>
		SearchTuningLoader.Load(BaselineProvider()).OrThrow().Should().Be(SearchTuningLoader.Load(BaselineProvider()).OrThrow());

	[Test]
	public void RelatedLearning_LoadedTwice_IsEqual() =>
		RelatedLearningLoader.Load(BaselineProvider()).OrThrow().Should().Be(RelatedLearningLoader.Load(BaselineProvider()).OrThrow());
}
