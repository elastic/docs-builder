// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;

namespace Elastic.Authoring.Tests.FrontMatter.ProductsFrontMatter;

static file class FrontMatterHelper
{
	public static Scenario WithYaml(string yaml) =>
		Setup.Document($"---\n{yaml}\n---\n# Test Page\n\nThis is a test page with products frontmatter.\n");

	public static Scenario WithDocsetProducts(string markdown) =>
		Setup.Generate(new SetupOptions { DocsetProducts = ["elasticsearch"] }, [Index(markdown)]);
}

public class ProductsFrontMatterInHtml : AuthoringTest
{
	protected override Scenario Scenario =>
		FrontMatterHelper.WithYaml("""
			products:
			  - id: elasticsearch
			  - id: ecctl
			""");

	[Test, DisplayName("includes products meta tags when products are specified")]
	public async Task IncludesProductsMetaTags() =>
		await Docs.Converts("index.md").ContainsHtml(
			"""
			<meta class="elastic" name="product_name" content="Elasticsearch,Elastic Cloud Control ECCTL"/>
			<meta name="DC.subject" content="Elasticsearch,Elastic Cloud Control ECCTL"/>
			"""
		);

	[Test, DisplayName("does not include products meta tags when no products are specified")]
	public async Task DoesNotIncludeProductsMetaTags()
	{
		var noProducts = Setup.Document(
			"""
			# Test Page

			This is a test page without products frontmatter.
			"""
		);
		var result = await noProducts.Converts("index.md").Inner;
		result.Html.Should().NotContain("product_name");
		result.Html.Should().NotContain("DC.subject");
	}
}

public class ProductsFrontMatterInLlmMarkdown : AuthoringTest
{
	protected override Scenario Scenario =>
		FrontMatterHelper.WithYaml("""
			products:
			  - id: elasticsearch
			  - id: ecctl
			""");

	[Test, DisplayName("includes products in frontmatter when products are specified")]
	public async Task IncludesProductsInFrontMatter()
	{
		var file = await Docs.Converts("index.md").MarkdownFile();
		var frontMatter = file.YamlFrontMatter;
		frontMatter.Should().NotBeNull();
		var products = frontMatter!.Products;
		products.Should().NotBeNull();
		products.Count.Should().Be(2);
		var ids = products.Select(p => p.Id).ToHashSet();
		ids.Should().Contain("elasticsearch");
		ids.Should().Contain("ecctl");
	}

	[Test, DisplayName("does not include products in frontmatter when no products are specified")]
	public async Task DoesNotIncludeProducts()
	{
		var noProducts = Setup.Document(
			"""
			# Test Page

			This is a test page without products frontmatter.
			"""
		);
		var file = await noProducts.Converts("index.md").MarkdownFile();
		var products = file.YamlFrontMatter?.Products;
		if (products is not null)
			products.Count.Should().Be(0);
	}
}

public class DocsetProductsMerging : AuthoringTest
{
	private static readonly Scenario DocsetOnly = FrontMatterHelper.WithDocsetProducts(
		"""
			# Test Page

			This is a test page without frontmatter products.
			"""
	);

	private static readonly Scenario DocsetAndFrontmatter = FrontMatterHelper.WithDocsetProducts(
		"""
			---
			products:
			  - id: ecctl
			---
			# Test Page

			This page has both docset and frontmatter products.
			"""
	);

	protected override Scenario Scenario => DocsetOnly; // satisfies abstract; tests run on static scenarios

	[Test, DisplayName("docset products appear in HTML when no frontmatter products")]
	public async Task DocsetProductsAppearInHtml() =>
		await DocsetOnly.Converts("index.md").ContainsRawHtml("""<meta class="elastic" name="product_name" content="Elasticsearch"/>""");

	[Test, DisplayName("docset products merge with frontmatter products")]
	public async Task DocsetProductsMergeWithFrontmatter() =>
		await DocsetAndFrontmatter.Converts("index.md").ContainsHtml(
			"""
			<meta class="elastic" name="product_name" content="Elasticsearch,Elastic Cloud Control ECCTL"/>
			<meta name="DC.subject" content="Elasticsearch,Elastic Cloud Control ECCTL"/>
			"""
		);
}
