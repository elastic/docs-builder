// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using AwesomeAssertions;
using Elastic.Documentation.Configuration.Products;
using Elastic.Documentation.Configuration.Versions;
using Elastic.Documentation.FileSystems;
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
		elasticsearch.Features.ReleaseNotes.Should().Be(ReleaseNotesPath.OnRelease);
		elasticsearch.Features.ParticipatesInReleaseNotes.Should().BeTrue();
	}

	[Fact]
	public void ProductWithPublicReferenceDisabled_HasCorrectFeatures()
	{
		var config = LoadActualProductsConfiguration();
		var docsBuilder = config.Products["docs-builder"];

		docsBuilder.Features.PublicReference.Should().BeFalse();
		docsBuilder.Features.ReleaseNotes.Should().Be(ReleaseNotesPath.OnRelease);
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
		all.ReleaseNotes.Should().Be(ReleaseNotesPath.OnRelease);
		all.ParticipatesInReleaseNotes.Should().BeTrue();
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

	[Theory]
	[InlineData("true", ReleaseNotesPath.OnRelease)]
	[InlineData("false", ReleaseNotesPath.None)]
	[InlineData("prestage", ReleaseNotesPath.Prestage)]
	[InlineData("Prestage", ReleaseNotesPath.Prestage)]
	[InlineData("on-release", ReleaseNotesPath.OnRelease)]
	public void ReleaseNotesFeature_AcceptsBooleansAndPathStrings(string value, ReleaseNotesPath expected)
	{
		var config = ParseProducts($"""
			products:
			  widget:
			    display: 'Widget'
			    versioning: 'stack'
			    features:
			      release-notes: {value}
			""");

		config.Products["widget"].Features.ReleaseNotes.Should().Be(expected);
		config.Products["widget"].Features.ParticipatesInReleaseNotes.Should().Be(expected != ReleaseNotesPath.None);
	}

	[Fact]
	public void ReleaseNotesFeature_OmittedInFeaturesMap_DefaultsToOnRelease()
	{
		var config = ParseProducts("""
			products:
			  widget:
			    display: 'Widget'
			    versioning: 'stack'
			    features:
			      public-reference: false
			""");

		config.Products["widget"].Features.ReleaseNotes.Should().Be(ReleaseNotesPath.OnRelease);
		config.Products["widget"].Features.PublicReference.Should().BeFalse();
	}

	[Fact]
	public void ReleaseNotesFeature_InvalidValue_Throws()
	{
		var act = () => ParseProducts("""
			products:
			  widget:
			    display: 'Widget'
			    versioning: 'stack'
			    features:
			      release-notes: sideways
			""");

		act.Should().Throw<InvalidOperationException>()
			.WithMessage("*'release-notes' value 'sideways'*Allowed values: true, false, prestage, on-release*");
	}

	[Fact]
	public void PublicReferenceFeature_InvalidValue_Throws()
	{
		var act = () => ParseProducts("""
			products:
			  widget:
			    display: 'Widget'
			    versioning: 'stack'
			    features:
			      public-reference: prestage
			""");

		act.Should().Throw<InvalidOperationException>()
			.WithMessage("*'public-reference' value 'prestage'*Allowed values: true, false*");
	}

	[Fact]
	public void ReleaseNotesFeature_PresentButEmpty_Throws()
	{
		// A present-but-empty key must be rejected, not silently treated as the omitted-key default.
		var act = () => ParseProducts("""
			products:
			  widget:
			    display: 'Widget'
			    versioning: 'stack'
			    features:
			      release-notes:
			""");

		act.Should().Throw<InvalidOperationException>()
			.WithMessage("*has an empty 'release-notes' value*Allowed values: true, false, prestage, on-release*");
	}

	[Fact]
	public void PublicReferenceFeature_PresentButEmpty_Throws()
	{
		var act = () => ParseProducts("""
			products:
			  widget:
			    display: 'Widget'
			    versioning: 'stack'
			    features:
			      public-reference:
			""");

		act.Should().Throw<InvalidOperationException>()
			.WithMessage("*has an empty 'public-reference' value*Allowed values: true, false*");
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
