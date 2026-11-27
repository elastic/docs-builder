// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Catalog;
using Elastic.Documentation.Configuration.LegacyUrlMappings;
using Elastic.Documentation.Configuration.Loading;
using Elastic.Documentation.Configuration.Products;
using Elastic.Documentation.Configuration.RelatedLearning;
using Elastic.Documentation.Configuration.Tests.Golden;
using Elastic.Documentation.Configuration.Versions;
using Elastic.Documentation.Diagnostics;
using Severity = Elastic.Documentation.Diagnostics.Severity;

namespace Elastic.Documentation.Configuration.Tests.Loading;

/// <summary>
/// The catalog loaders against the parsers they wrap: a file that loads gives the same records and no findings, and a file that does not
/// fails with the text the parser throws, so a caller that requires the catalog sees the message it always saw.
/// </summary>
public class CatalogLoaderTests
{
	[Test]
	public void Load_BaselineFiles_GivesTheRecordsTheParsersBuildAndNoFindings()
	{
		using var provider = GoldenConfig.CreateProvider();

		var result = CatalogLoader.Load(provider);

		result.Findings().Should().BeEmpty();
		var catalog = result.OrThrow();
		var versions = provider.CreateVersionConfiguration();
		catalog.VersioningSystems.Should().Be(versions);
		catalog.Products.Should().Be(provider.CreateProducts(versions));
		catalog.LegacyUrlMappings.Should().Be(provider.CreateLegacyUrlMappings(catalog.Products));
	}

	[Test]
	[Arguments(GoldenConfig.Versions, "")]
	[Arguments(GoldenConfig.Versions, "versioning_systems:\n  stack:\n    base: not-a-version\n    current: 9.0.0\n")]
	[Arguments(GoldenConfig.Versions, "bogus: 1\n")]
	[Arguments(GoldenConfig.Products, "")]
	[Arguments(GoldenConfig.Products, "bogus: 1\n")]
	[Arguments(GoldenConfig.Products, "products:\n  apm:\n    display: APM\n    versioning: not-a-system\n")]
	[Arguments(GoldenConfig.Products, "products:\n  apm:\n    display: APM\n    versioning: stack\n    features:\n      public-reference: maybe\n")]
	[Arguments(GoldenConfig.LegacyUrlMappings, "")]
	[Arguments(GoldenConfig.LegacyUrlMappings, "mappings:\n  en/x/:\n    product: not-a-product\n")]
	public void Load_FileTheParserRejects_FailsWithTheParsersMessage(string file, string yaml)
	{
		using var provider = GoldenConfig.CreateProvider(file, yaml);
		var thrown = Catch(() => ParseAll(provider));
		thrown.Should().NotBeNull("the parsers reject this file today");

		var result = CatalogLoader.Load(provider);

		(result.Value is Unreadable<GlobalCatalog>).Should().BeTrue();
		var error = result.Findings().Should().ContainSingle().Subject;
		error.Severity.Should().Be(Severity.Error);
		error.Message.Should().Be(thrown!.Message);
		Catch(() => result.OrThrow()).Should().BeOfType<ConfigurationException>().Which.Message.Should().Be(thrown.Message);
	}

	[Test]
	public void Load_FileThatFails_NamesThatFile()
	{
		using var provider = GoldenConfig.CreateProvider(GoldenConfig.Products, "bogus: 1\n");

		var error = CatalogLoader.Load(provider).Findings().Should().ContainSingle().Subject;

		error.File.Should().Be(provider.ProductsFile.FullName);
	}

	[Test]
	public void LoadRelatedLearning_BaselineFile_GivesTheRecordTheParserBuilds()
	{
		using var provider = GoldenConfig.CreateProvider();

		var result = RelatedLearningLoader.Load(provider);

		result.Findings().Should().BeEmpty();
		result.OrThrow().Should().Be(provider.CreateRelatedLearningConfiguration());
	}

	[Test]
	[Arguments("")]
	[Arguments("links:\n  demo:\n    url: https://www.elastic.co/demo\n")]
	[Arguments("links:\n  demo:\n    title: Demo\n    url: /relative\n")]
	public void LoadRelatedLearning_FileTheParserRejects_FailsWithTheParsersMessage(string yaml)
	{
		using var provider = GoldenConfig.CreateProvider(GoldenConfig.RelatedLearning, yaml);
		var thrown = Catch(() => provider.CreateRelatedLearningConfiguration());
		thrown.Should().NotBeNull("the parser rejects this file today");

		var result = RelatedLearningLoader.Load(provider);

		result.Findings().Should().ContainSingle().Which.Message.Should().Be(thrown!.Message);
	}

	private static Exception? Catch(Action action)
	{
		try
		{
			action();
			return null;
		}
		catch (Exception e)
		{
			return e;
		}
	}

	private static void ParseAll(ConfigurationFileProvider provider)
	{
		var versions = provider.CreateVersionConfiguration();
		var products = provider.CreateProducts(versions);
		_ = provider.CreateLegacyUrlMappings(products);
	}
}
