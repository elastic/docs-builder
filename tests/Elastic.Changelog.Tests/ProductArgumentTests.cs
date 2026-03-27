// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;

namespace Elastic.Changelog.Tests;

public class ProductArgumentTests
{
	[Theory]
	[InlineData("cloud-hosted", null, null, "cloud-hosted")]
	[InlineData("elasticsearch", "9.2.0", null, "elasticsearch 9.2.0")]
	[InlineData("elasticsearch", "9.2.0", "ga", "elasticsearch 9.2.0 ga")]
	[InlineData("cloud-serverless", "2025-06", null, "cloud-serverless 2025-06")]
	public void ToSpecString_FormatsCorrectly(string product, string? target, string? lifecycle, string expected)
	{
		var arg = new ProductArgument { Product = product, Target = target, Lifecycle = lifecycle };

		arg.ToSpecString().Should().Be(expected);
	}

	[Fact]
	public void FormatProductSpecs_MultipleProducts_JoinsWithComma()
	{
		var products = new List<ProductArgument>
		{
			new() { Product = "cloud-hosted" },
			new() { Product = "cloud-serverless" }
		};

		ProductArgument.FormatProductSpecs(products).Should().Be("cloud-hosted, cloud-serverless");
	}

	[Fact]
	public void FormatProductSpecs_WithTargetAndLifecycle_FormatsCorrectly()
	{
		var products = new List<ProductArgument>
		{
			new() { Product = "elasticsearch", Target = "9.2.0", Lifecycle = "ga" },
			new() { Product = "cloud-serverless", Target = "2025-06" }
		};

		ProductArgument.FormatProductSpecs(products).Should().Be("elasticsearch 9.2.0 ga, cloud-serverless 2025-06");
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("  ")]
	public void ParseProductSpecs_NullOrEmpty_ReturnsEmpty(string? input) =>
		ProductArgument.ParseProductSpecs(input).Should().BeEmpty();

	[Fact]
	public void ParseProductSpecs_SingleProduct_ParsesCorrectly()
	{
		var result = ProductArgument.ParseProductSpecs("cloud-hosted");

		result.Should().HaveCount(1);
		result[0].Product.Should().Be("cloud-hosted");
		result[0].Target.Should().BeNull();
		result[0].Lifecycle.Should().BeNull();
	}

	[Fact]
	public void ParseProductSpecs_MultipleProducts_ParsesCorrectly()
	{
		var result = ProductArgument.ParseProductSpecs("cloud-hosted, cloud-serverless");

		result.Should().HaveCount(2);
		result[0].Product.Should().Be("cloud-hosted");
		result[1].Product.Should().Be("cloud-serverless");
	}

	[Fact]
	public void ParseProductSpecs_WithTargetAndLifecycle_ParsesAllParts()
	{
		var result = ProductArgument.ParseProductSpecs("elasticsearch 9.2.0 ga, cloud-serverless 2025-06");

		result.Should().HaveCount(2);
		result[0].Product.Should().Be("elasticsearch");
		result[0].Target.Should().Be("9.2.0");
		result[0].Lifecycle.Should().Be("ga");
		result[1].Product.Should().Be("cloud-serverless");
		result[1].Target.Should().Be("2025-06");
		result[1].Lifecycle.Should().BeNull();
	}

	[Fact]
	public void ParseProductSpecs_Roundtrip_PreservesData()
	{
		var original = new List<ProductArgument>
		{
			new() { Product = "elasticsearch", Target = "9.2.0", Lifecycle = "ga" },
			new() { Product = "cloud-hosted" }
		};

		var formatted = ProductArgument.FormatProductSpecs(original);
		var parsed = ProductArgument.ParseProductSpecs(formatted);

		parsed.Should().HaveCount(2);
		parsed[0].Product.Should().Be("elasticsearch");
		parsed[0].Target.Should().Be("9.2.0");
		parsed[0].Lifecycle.Should().Be("ga");
		parsed[1].Product.Should().Be("cloud-hosted");
		parsed[1].Target.Should().BeNull();
		parsed[1].Lifecycle.Should().BeNull();
	}
}
