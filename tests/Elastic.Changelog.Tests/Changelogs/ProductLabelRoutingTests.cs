// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Changelog.Bundling;

namespace Elastic.Changelog.Tests.Changelogs;

public class ProductLabelRoutingTests
{
	private static readonly ProductLabelRouting Serverless = new("cloud-serverless", ["cloud-serverless", "elasticsearch"]);

	[Test]
	public void CreateLabelRouter_RoutesOutAPullRequestWithAnotherProductLabel()
	{
		var route = Serverless.CreateLabelRouter(new Dictionary<string, string>
		{
			["stateful"] = "elasticsearch",
			["serverless"] = "cloud-serverless"
		});

		route.Should().NotBeNull();
		route!(["stateful"]).Should().Be("stateful");
		route(["Team:Search", "stateful"]).Should().Be("stateful");
		route(["STATEFUL"]).Should().Be("stateful");
	}

	[Test]
	public void CreateLabelRouter_KeepsAPullRequestWithTheProductLabelOrNoRoutingLabel()
	{
		var route = Serverless.CreateLabelRouter(new Dictionary<string, string>
		{
			["stateful"] = "elasticsearch",
			["serverless"] = "cloud-serverless"
		});

		route!(["serverless"]).Should().BeNull();
		route(["stateful", "serverless"]).Should().BeNull();
		route(["Serverless", "stateful"]).Should().BeNull();
		route(["Team:Search"]).Should().BeNull();
		route([]).Should().BeNull();
	}

	[Test]
	public void CreateLabelRouter_IgnoresLabelsThatMapToUnrelatedProducts()
	{
		var route = Serverless.CreateLabelRouter(new Dictionary<string, string>
		{
			["Team:Security"] = "security",
			["Team:Observability"] = "observability",
			["stateful"] = "elasticsearch"
		});

		route.Should().NotBeNull();
		route!(["Team:Security"]).Should().BeNull();
		route(["Team:Security", "stateful"]).Should().Be("stateful");
	}

	[Test]
	public void CreateLabelRouter_ReturnsNullWhenNoLabelRoutesAwayFromTheProduct()
	{
		Serverless.CreateLabelRouter(new Dictionary<string, string> { ["Team:Security"] = "security" }).Should().BeNull();
		Serverless.CreateLabelRouter(new Dictionary<string, string> { ["serverless"] = "cloud-serverless" }).Should().BeNull();
		Serverless.CreateLabelRouter(null).Should().BeNull();
		Serverless.CreateLabelRouter(new Dictionary<string, string>()).Should().BeNull();
	}

	[Test]
	public void CreateLabelRouter_ReadsTheProductFromASpecWithVersionAndLifecycle()
	{
		var route = Serverless.CreateLabelRouter(new Dictionary<string, string>
		{
			["stateful"] = "elasticsearch 9.2.0 ga",
			["serverless"] = "cloud_serverless 2026-10 ga"
		});

		route.Should().NotBeNull();
		route!(["stateful"]).Should().Be("stateful");
		route(["stateful", "serverless"]).Should().BeNull();
	}
}
