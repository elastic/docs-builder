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
	public void DeriveLabels_SplitsLabelsIntoExcludingAndKeeping()
	{
		var labels = Serverless.DeriveLabels(new Dictionary<string, string>
		{
			["stateful"] = "elasticsearch",
			["serverless"] = "cloud-serverless"
		});

		labels.Should().NotBeNull();
		labels.Value.Exclude.Should().Equal("stateful");
		labels.Value.Keep.Should().Equal("serverless");
	}

	[Test]
	public void DeriveLabels_IgnoresLabelsThatMapToUnrelatedProducts()
	{
		var labels = Serverless.DeriveLabels(new Dictionary<string, string>
		{
			["Team:Security"] = "security",
			["Team:Observability"] = "observability",
			["stateful"] = "elasticsearch"
		});

		labels.Should().NotBeNull();
		labels.Value.Exclude.Should().Equal("stateful");
		labels.Value.Keep.Should().BeEmpty();
	}

	[Test]
	public void DeriveLabels_ReturnsNullWhenNoLabelRoutesAwayFromTheProduct()
	{
		Serverless.DeriveLabels(new Dictionary<string, string> { ["Team:Security"] = "security" }).Should().BeNull();
		Serverless.DeriveLabels(new Dictionary<string, string> { ["serverless"] = "cloud-serverless" }).Should().BeNull();
		Serverless.DeriveLabels(null).Should().BeNull();
		Serverless.DeriveLabels(new Dictionary<string, string>()).Should().BeNull();
	}

	[Test]
	public void DeriveLabels_ReadsTheProductFromASpecWithVersionAndLifecycle()
	{
		var labels = Serverless.DeriveLabels(new Dictionary<string, string>
		{
			["stateful"] = "elasticsearch 9.2.0 ga",
			["serverless"] = "cloud_serverless 2026-10 ga"
		});

		labels.Should().NotBeNull();
		labels.Value.Exclude.Should().Equal("stateful");
		labels.Value.Keep.Should().Equal("serverless");
	}
}
