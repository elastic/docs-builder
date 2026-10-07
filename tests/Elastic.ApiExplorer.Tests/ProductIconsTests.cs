// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Site.Icons;

namespace Elastic.ApiExplorer.Tests;

public class ProductIconsTests
{
	[Test]
	public void Get_KnownKey_ReturnsSvg() => ProductIcons.Get("elasticsearch").Should().Contain("<svg");

	[Test]
	public void Get_ServerlessAlias_ReusesStackMark()
	{
		ProductIcons.Get("serverless-elasticsearch").Should().Be(ProductIcons.Get("elasticsearch"));
		ProductIcons.Get("serverless-kibana").Should().Be(ProductIcons.Get("kibana"));
	}

	[Test]
	public void Get_CloudProducts_ReuseCloudMark()
	{
		ProductIcons.Get("ess").Should().Contain("<svg");
		ProductIcons.Get("cloud-serverless").Should().Be(ProductIcons.Get("ess"));
	}

	[Test]
	[Arguments("elasticsearch")]
	[Arguments("kibana")]
	[Arguments("observability")]
	[Arguments("security")]
	[Arguments("logstash")]
	[Arguments("ess")]
	[Arguments("cloud-enterprise")]
	public void Get_LogoWithNegativeSpace_UsesEuiNegativeFillNotFixedWhite(string key)
	{
		var svg = ProductIcons.Get(key);

		svg.Should().Contain("euiIcon__fillNegative");
		svg.Should().NotContain("rgba(255,255,255", "a fixed white fill is invisible on a light background");
	}

	[Test]
	[Arguments("elasticsearch")]
	[Arguments("kibana")]
	[Arguments("observability")]
	[Arguments("security")]
	[Arguments("logstash")]
	[Arguments("elastic-stack")]
	[Arguments("ess")]
	[Arguments("cloud-enterprise")]
	[Arguments("vectordb")]
	public void Get_AnyIcon_HasNoFixedWhiteFill(string key) => ProductIcons.Get(key).Should().NotContain("rgba(255,255,255");

	[Test]
	public void Get_CloudHosted_ReusesTheHostedCloudMark() =>
		ProductIcons.Get("cloud-hosted").Should().Be(ProductIcons.Get("ess")).And.Contain("<svg");

	[Test]
	public void Get_CloudEnterprise_HasItsOwnMark()
	{
		var svg = ProductIcons.Get("cloud-enterprise");

		svg.Should().Contain("<svg");
		svg.Should().NotBe(ProductIcons.Get("ess"));
	}

	[Test]
	public void Get_VectorDatabase_ReturnsOfficialMark()
	{
		ProductIcons.Get("vectordb").Should().Contain("<svg");
		ProductIcons.Get("serverless-vector-database").Should().Be(ProductIcons.Get("vectordb"));
	}
}
