// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using AwesomeAssertions;
using Elastic.Documentation.Configuration.Products;
using Elastic.Documentation.Configuration.Versions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elastic.Documentation.Configuration.Tests;

public class ProductFeaturesTests
{
	[Fact]
	public void ProductWithNoFeaturesKey_GetsAllFeaturesEnabled()
	{
		var config = LoadActualProductsConfiguration();
		var elasticsearch = config.Products["elasticsearch"];

		elasticsearch.Features.PublicReference.Should().BeTrue();
		elasticsearch.Features.ReleaseNotes.Should().BeTrue();
	}

	[Fact]
	public void ProductWithPublicReferenceDisabled_HasCorrectFeatures()
	{
		var config = LoadActualProductsConfiguration();
		var docsBuilder = config.Products["docs-builder"];

		docsBuilder.Features.PublicReference.Should().BeFalse();
		docsBuilder.Features.ReleaseNotes.Should().BeTrue();
	}

	[Fact]
	public void ProductWithoutPublicReference_GetsNoneVersioningSystem()
	{
		var config = LoadActualProductsConfiguration();
		var docsBuilder = config.Products["docs-builder"];

		docsBuilder.VersioningSystem.Should().Be(VersioningSystem.None);
		docsBuilder.VersioningSystem.IsVersionless.Should().BeTrue();
		docsBuilder.VersioningSystem.Id.Should().Be(VersioningSystemId.None);
	}

	[Fact]
	public void PublicReferenceProducts_ExcludesProductsWithPublicReferenceDisabled()
	{
		var config = LoadActualProductsConfiguration();

		config.Products.Should().ContainKey("docs-builder");
		config.PublicReferenceProducts.Should().NotContainKey("docs-builder");
	}

	[Fact]
	public void PublicReferenceProducts_IncludesStandardProducts()
	{
		var config = LoadActualProductsConfiguration();

		config.PublicReferenceProducts.Should().ContainKey("elasticsearch");
		config.PublicReferenceProducts.Should().ContainKey("kibana");
	}

	[Fact]
	public void AllProducts_ContainsBothStandardAndOptedOutProducts()
	{
		var config = LoadActualProductsConfiguration();

		config.Products.Should().ContainKey("elasticsearch");
		config.Products.Should().ContainKey("docs-builder");
	}

	[Fact]
	public void ProductFeatures_All_HasBothFeaturesEnabled()
	{
		var all = ProductFeatures.All;

		all.PublicReference.Should().BeTrue();
		all.ReleaseNotes.Should().BeTrue();
	}

	[Fact]
	public void ProductFeatures_KnownKeys_ContainsExpectedEntries()
	{
		ProductFeatures.KnownKeys.Should().Contain("public-reference");
		ProductFeatures.KnownKeys.Should().Contain("release-notes");
		ProductFeatures.KnownKeys.Should().HaveCount(2);
	}

	[Fact]
	public void GetDisplayName_WorksForProductsWithDisabledFeatures()
	{
		var config = LoadActualProductsConfiguration();

		config.GetDisplayName("docs-builder").Should().Be("Elastic Docs Builder");
	}

	[Fact]
	public void GetProductByRepositoryName_WorksForProductsWithDisabledFeatures()
	{
		var config = LoadActualProductsConfiguration();
		var product = config.GetProductByRepositoryName("docs-builder");

		product.Should().NotBeNull();
		product.Id.Should().Be("docs-builder");
	}

	private static ProductsConfiguration LoadActualProductsConfiguration()
	{
		var fileSystem = new FileSystem();
		var provider = new ConfigurationFileProvider(new NullLoggerFactory(), fileSystem);
		var versionsConfig = provider.CreateVersionConfiguration();
		return provider.CreateProducts(versionsConfig);
	}
}
