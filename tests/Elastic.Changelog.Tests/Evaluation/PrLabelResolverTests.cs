// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Changelog.Evaluation;
using Elastic.Documentation.Configuration.Changelog;

namespace Elastic.Changelog.Tests.Evaluation;

public class PrLabelResolverTests
{
	private static readonly Dictionary<string, string> TypeLabels = new() { ["type:feature"] = "feature", ["type:bug"] = "bug-fix" };

	private static ChangelogConfiguration Config(Dictionary<string, string>? products = null, ProductsConfig? productsConfig = null) =>
		ChangelogConfiguration.Default with { LabelToType = TypeLabels, LabelToProducts = products, ProductsConfiguration = productsConfig };

	private static readonly Dictionary<string, string> TwoProducts = new() { ["product:es"] = "elasticsearch", ["product:kb"] = "kibana" };

	[Test]
	public void Resolve_SingleTypeLabel_ReturnsType()
	{
		var result = PrLabelResolver.Resolve(["type:feature"], Config());

		result.Type.Should().Be("feature");
		result.MissingType.Should().BeFalse();
		result.IsAmbiguousType.Should().BeFalse();
	}

	[Test]
	public void Resolve_NoTypeLabel_IsMissingType()
	{
		var result = PrLabelResolver.Resolve(["unrelated"], Config());

		result.Type.Should().BeNull();
		result.MissingType.Should().BeTrue();
	}

	[Test]
	public void Resolve_MultipleTypeLabels_IsAmbiguousAndNotMissing()
	{
		var result = PrLabelResolver.Resolve(["type:feature", "type:bug"], Config());

		result.Type.Should().BeNull();
		result.IsAmbiguousType.Should().BeTrue();
		result.MissingType.Should().BeFalse();
		result.AmbiguousTypeLabels.Should().Contain("type:feature").And.Contain("type:bug");
	}

	[Test]
	public void Resolve_NoProductsConfigured_AssignsNothing()
	{
		var result = PrLabelResolver.Resolve(["type:feature"], Config());

		result.Products.Should().BeNull();
		result.ProductAssignment.Should().Be(ProductAssignment.None);
		result.ProductLabelTable.Should().BeNull();
	}

	[Test]
	public void Resolve_ProductLabelPresent_ResolvesFromLabels()
	{
		var result = PrLabelResolver.Resolve(["type:feature", "product:kb"], Config(TwoProducts));

		result.Products.Should().Contain("kibana");
		result.ProductAssignment.Should().Be(ProductAssignment.FromLabels);
		result.ProductLabelTable.Should().BeNull();
	}

	[Test]
	public void Resolve_SingleDistinctProductConfigured_AssignsWithoutLabel()
	{
		var single = new Dictionary<string, string> { ["product:es"] = "elasticsearch", ["team:es"] = "elasticsearch" };

		var result = PrLabelResolver.Resolve(["type:feature"], Config(single));

		result.Products.Should().Contain("elasticsearch");
		result.ProductAssignment.Should().Be(ProductAssignment.SingleConfigured);
		result.ProductLabelTable.Should().BeNull();
	}

	[Test]
	public void Resolve_MultipleProductsAndNoProductLabel_BuildsProductLabelTable()
	{
		var result = PrLabelResolver.Resolve(["type:feature"], Config(TwoProducts));

		result.Products.Should().BeNull();
		result.ProductAssignment.Should().Be(ProductAssignment.None);
		result.ProductLabelTable.Should().Contain("product:es").And.Contain("product:kb");
	}
}
