// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Configuration.Products;
using Elastic.Documentation.Configuration.Versions;
using Elastic.Documentation.FileSystems;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elastic.Documentation.Configuration.Tests;

public class ProductsConfigurationTests
{
	[Fact]
	public void GetProductsByRepositoryName_ProductIdMatch_ReturnsSingleElement()
	{
		var config = ParseProducts(
			"""
			products:
			  elasticsearch:
			    display: Elasticsearch
			    versioning: stack
			"""
		);

		var results = config.GetProductsByRepositoryName("elasticsearch");

		results.Should().HaveCount(1);
		results[0].Id.Should().Be("elasticsearch");
	}

	[Fact]
	public void GetProductsByRepositoryName_OwnerRepoForm_UsesLastSegment()
	{
		var config = ParseProducts(
			"""
			products:
			  elasticsearch:
			    display: Elasticsearch
			    versioning: stack
			"""
		);

		var results = config.GetProductsByRepositoryName("elastic/elasticsearch");

		results.Should().HaveCount(1);
		results[0].Id.Should().Be("elasticsearch");
	}

	[Fact]
	public void GetProductsByRepositoryName_ThreeProductsSharingRepository_ReturnsAllThree()
	{
		var config = ParseProducts(
			"""
			products:
			  cloud-hosted:
			    display: Cloud Hosted
			    repository: 'cloud'
			    features:
			      public-reference: false
			  cloud-serverless:
			    display: Cloud Serverless
			    repository: 'cloud'
			    features:
			      public-reference: false
			  cloud-enterprise:
			    display: Cloud Enterprise
			    repository: 'cloud'
			    features:
			      public-reference: false
			"""
		);

		var results = config.GetProductsByRepositoryName("cloud");

		results.Should().HaveCount(3);
		results.Select(p => p.Id).Should().Contain(["cloud-hosted", "cloud-serverless", "cloud-enterprise"]);
	}

	[Fact]
	public void GetProductsByRepositoryName_BlankRepository_ReturnsEmpty()
	{
		var config = ParseProducts(
			"""
			products:
			  elasticsearch:
			    display: Elasticsearch
			    versioning: stack
			"""
		);

		var results = config.GetProductsByRepositoryName(string.Empty);

		results.Should().BeEmpty();
	}

	[Fact]
	public void GetProductsByRepositoryName_UnknownRepository_ReturnsEmpty()
	{
		var config = ParseProducts(
			"""
			products:
			  elasticsearch:
			    display: Elasticsearch
			    versioning: stack
			"""
		);

		var results = config.GetProductsByRepositoryName("nonexistent");

		results.Should().BeEmpty();
	}

	[Fact]
	public void GetProductByRepositoryName_SingleMatch_ReturnsProduct()
	{
		var config = ParseProducts(
			"""
			products:
			  elasticsearch:
			    display: Elasticsearch
			    versioning: stack
			"""
		);

		var product = config.GetProductByRepositoryName("elasticsearch");

		product.Should().NotBeNull();
		product!.Id.Should().Be("elasticsearch");
	}

	[Fact]
	public void GetProductByRepositoryName_MultipleMatches_ReturnsNull()
	{
		var config = ParseProducts(
			"""
			products:
			  cloud-hosted:
			    display: Cloud Hosted
			    repository: 'cloud'
			    features:
			      public-reference: false
			  cloud-serverless:
			    display: Cloud Serverless
			    repository: 'cloud'
			    features:
			      public-reference: false
			"""
		);

		var product = config.GetProductByRepositoryName("cloud");

		product.Should().BeNull();
	}

	[Fact]
	public void GetProductByRepositoryName_NoMatch_ReturnsNull()
	{
		var config = ParseProducts(
			"""
			products:
			  elasticsearch:
			    display: Elasticsearch
			    versioning: stack
			"""
		);

		var product = config.GetProductByRepositoryName("nonexistent");

		product.Should().BeNull();
	}

	[Fact]
	public void GetProductsByRepositoryName_ActualCloudProducts_ReturnsThreeProducts()
	{
		var config = LoadActualProductsConfiguration();

		var results = config.GetProductsByRepositoryName("cloud");

		results.Should().HaveCount(3, "elastic/cloud hosts cloud-hosted, cloud-serverless, and cloud-enterprise");
		results.Select(p => p.Id).Should().Contain(["cloud-hosted", "cloud-serverless", "cloud-enterprise"]);
	}

	private static ProductsConfiguration ParseProducts(string yaml)
	{
		var provider = new ConfigurationFileProvider(new NullLoggerFactory(), new ConfigurationFileSystem());
		var versionsConfig = provider.CreateVersionConfiguration();
		using var reader = new StringReader(yaml);
		return ProductExtensions.CreateProducts(reader, versionsConfig);
	}

	private static ProductsConfiguration LoadActualProductsConfiguration()
	{
		var provider = new ConfigurationFileProvider(new NullLoggerFactory(), new ConfigurationFileSystem());
		var versionsConfig = provider.CreateVersionConfiguration();
		return provider.CreateProducts(versionsConfig);
	}
}
