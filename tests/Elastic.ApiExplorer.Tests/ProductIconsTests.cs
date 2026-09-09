// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Site.Icons;

namespace Elastic.ApiExplorer.Tests;

public class ProductIconsTests
{
	[Fact]
	public void Get_KnownKey_ReturnsSvg() => ProductIcons.Get("elasticsearch").Should().Contain("<svg");

	[Fact]
	public void Get_ServerlessAlias_ReusesStackMark()
	{
		ProductIcons.Get("serverless-elasticsearch").Should().Be(ProductIcons.Get("elasticsearch"));
		ProductIcons.Get("serverless-kibana").Should().Be(ProductIcons.Get("kibana"));
	}

	[Fact]
	public void Get_CloudProducts_ReuseCloudMark()
	{
		ProductIcons.Get("ess").Should().Contain("<svg");
		ProductIcons.Get("cloud-serverless").Should().Be(ProductIcons.Get("ess"));
	}
}
