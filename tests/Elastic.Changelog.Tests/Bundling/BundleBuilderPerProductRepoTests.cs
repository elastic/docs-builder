// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using AwesomeAssertions;
using Elastic.Changelog.Bundling;
using Elastic.Documentation.Configuration.Products;
using Elastic.Documentation.Configuration.Versions;
using Elastic.Documentation.ReleaseNotes;

namespace Elastic.Changelog.Tests.Bundling;

public class BundleBuilderPerProductRepoTests(ITestOutputHelper output)
{
	private readonly TestDiagnosticsCollector _collector = new(output);
	private readonly BundleBuilder _builder = new();

	private static MatchedChangelogFile MakeEntry(string productId, string version = "9.0.0") =>
		new()
		{
			FilePath = $"{productId}.yaml",
			FileName = $"{productId}.yaml",
			Checksum = "abc",
			Data = new ChangelogEntry
			{
				Title = "Test change",
				Type = ChangelogEntryType.Feature,
				Products = [new ProductReference { ProductId = productId, Versions = [version], Lifecycle = Lifecycle.Ga }]
			}
		};

	private static ProductsConfiguration MakeProducts(params (string id, string? repository)[] products)
	{
		var dict = products.ToDictionary(p => p.id, p => new Product { Id = p.id, DisplayName = p.id, Repository = p.repository ?? p.id });
		return new ProductsConfiguration
		{
			Products = dict.ToFrozenDictionary(),
			PublicReferenceProducts = FrozenDictionary<string, Product>.Empty,
			ProductDisplayNames = dict.ToDictionary(kv => kv.Key, kv => kv.Value.DisplayName).ToFrozenDictionary()
		};
	}

	[Fact]
	public void BuildBundle_WithProductsConfiguration_UsesPerProductRepo()
	{
		var entries = new[] { MakeEntry("cloud-hosted"), MakeEntry("cloud-serverless") };
		var productsConfig = MakeProducts(("cloud-hosted", "cloud"), ("cloud-serverless", "cloud"));

		var result = _builder.BuildBundle(
			_collector,
			entries,
			outputProducts: null,
			repo: "bundle-level-repo",
			owner: "elastic",
			productsConfiguration: productsConfig
		);

		result.IsValid.Should().BeTrue();
		var products = result.Data!.Products;
		products.Should().HaveCount(2);
		products.All(p => p.Repo == "cloud").Should().BeTrue("each product resolves to its repository: cloud");
	}

	[Fact]
	public void BuildBundle_ProductNotInCatalogue_FallsBackToBundleRepo()
	{
		var entries = new[] { MakeEntry("unknown-product") };
		var productsConfig = MakeProducts(("elasticsearch", null));

		var result = _builder.BuildBundle(
			_collector,
			entries,
			outputProducts: null,
			repo: "fallback-repo",
			owner: "elastic",
			productsConfiguration: productsConfig
		);

		result.IsValid.Should().BeTrue();
		result.Data!.Products[0].Repo.Should().Be("fallback-repo");
	}

	[Fact]
	public void BuildBundle_WithoutProductsConfiguration_UsesPassedRepo()
	{
		var entries = new[] { MakeEntry("elasticsearch") };

		var result = _builder.BuildBundle(
			_collector,
			entries,
			outputProducts: null,
			repo: "elasticsearch",
			owner: "elastic",
			productsConfiguration: null
		);

		result.IsValid.Should().BeTrue();
		result.Data!.Products[0].Repo.Should().Be("elasticsearch");
	}

	[Fact]
	public void BuildBundle_ProductWithExplicitRepository_UsesThatRepository()
	{
		var entries = new[] { MakeEntry("cloud-serverless") };
		var productsConfig = MakeProducts(("cloud-serverless", "cloud"));

		var result = _builder.BuildBundle(
			_collector,
			entries,
			outputProducts: null,
			repo: "different-repo",
			owner: "elastic",
			productsConfiguration: productsConfig
		);

		result.IsValid.Should().BeTrue();
		result.Data!.Products[0].Repo.Should().Be("cloud", "the product's repository field takes precedence over the bundle-level repo");
	}

	[Fact]
	public void BuildBundle_ProductWithNoExplicitRepository_UsesProductId()
	{
		var entries = new[] { MakeEntry("elasticsearch") };
		var productsConfig = MakeProducts(("elasticsearch", null));

		var result = _builder.BuildBundle(
			_collector,
			entries,
			outputProducts: null,
			repo: "bundle-repo",
			owner: "elastic",
			productsConfiguration: productsConfig
		);

		result.IsValid.Should().BeTrue();
		result.Data!.Products[0].Repo.Should().Be("elasticsearch", "product.Repository defaults to the product ID when not explicitly set");
	}
}
