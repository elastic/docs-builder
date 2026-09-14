// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Changelog;
using Elastic.Changelog.Bundling;
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

	[Fact]
	public void BuildBundle_StampsAuthoringRepoOnEveryProduct()
	{
		var entries = new[] { MakeEntry("cloud-hosted"), MakeEntry("cloud-serverless") };

		var result = _builder.BuildBundle(_collector, entries, outputProducts: null, repo: "elasticsearch", owner: "elastic");

		result.IsValid.Should().BeTrue();
		var products = result.Data!.Products;
		products.Should().HaveCount(2);
		products.All(p => p.Repo == "elasticsearch").Should().BeTrue("products[].repo is the authoring repo, not products.yml");
	}

	[Fact]
	public void BuildBundle_CloudServerless_UsesAuthoringRepoNotCatalogCloud()
	{
		var entries = new[] { MakeEntry("cloud-serverless") };

		var result = _builder.BuildBundle(_collector, entries, outputProducts: null, repo: "elasticsearch", owner: "elastic");

		result.IsValid.Should().BeTrue();
		result.Data!.Products[0].Repo.Should().Be("elasticsearch");
	}

	[Fact]
	public void BuildBundle_CloudServerless_KibanaAuthoringRepo()
	{
		var entries = new[] { MakeEntry("cloud-serverless") };

		var result = _builder.BuildBundle(_collector, entries, outputProducts: null, repo: "kibana", owner: "elastic");

		result.IsValid.Should().BeTrue();
		result.Data!.Products[0].Repo.Should().Be("kibana");
	}

	[Fact]
	public void BuildBundle_WithoutAuthoringRepo_OmitsRepo()
	{
		var entries = new[] { MakeEntry("cloud-serverless") };

		var result = _builder.BuildBundle(_collector, entries, outputProducts: null, repo: null, owner: "elastic");

		result.IsValid.Should().BeTrue();
		result.Data!.Products[0].Repo.Should().BeNull();
	}

	[Fact]
	public void BuildBundle_OutputProducts_StampsAuthoringRepo()
	{
		var entries = new[] { MakeEntry("elasticsearch") };
		var outputProducts = new[] { new ProductArgument { Product = "cloud-serverless", Target = "2026-09-08" } };

		var result = _builder.BuildBundle(_collector, entries, outputProducts, repo: "elasticsearch", owner: "elastic");

		result.IsValid.Should().BeTrue();
		result.Data!.Products.Should().ContainSingle();
		result.Data.Products[0].ProductId.Should().Be("cloud-serverless");
		result.Data.Products[0].Repo.Should().Be("elasticsearch");
	}
}
