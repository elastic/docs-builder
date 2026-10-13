// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Landing;

namespace Elastic.ApiExplorer.Tests;

public class ApiHubSwitcherTests
{
	[Test]
	public void Build_NullCurrentKey_ReturnsEmpty()
	{
		var items = ApiHubSwitcher.Build([Entry("elasticsearch", "Elasticsearch")], currentApiKey: null);

		items.Should().BeEmpty();
	}

	[Test]
	public void Build_EmptyEntries_ReturnsEmpty()
	{
		var items = ApiHubSwitcher.Build([], currentApiKey: "elasticsearch");

		items.Should().BeEmpty();
	}

	[Test]
	public void Build_OrdersEntriesAlphabeticallyByTitle()
	{
		var items = ApiHubSwitcher.Build([Entry("kibana", "Kibana"), Entry("elasticsearch", "Elasticsearch")], currentApiKey: "kibana");

		items.Select(i => i.Label).Should().Equal("Elasticsearch", "Kibana");
	}

	[Test]
	public void Build_ListsStackWithVariantsThenCloudThenTheRestByTitle()
	{
		var items = ApiHubSwitcher.Build(
			[
				Entry("zeta", "Zeta API"),
				Entry("kibana-serverless", "Kibana Serverless APIs"),
				Entry("cloud-serverless", "Elastic Cloud Serverless API"),
				Entry("logstash", "Logstash APIs"),
				Entry("alpha", "Alpha API"),
				Entry("cloud", "Elastic Cloud API"),
				Entry("kibana", "Kibana APIs"),
				Entry("cloud-billing", "Cloud Billing API"),
				Entry("elasticsearch", "Elasticsearch API"),
				Entry("elasticsearch-serverless", "Elasticsearch Serverless API")
			],
			currentApiKey: "kibana"
		);

		items
			.Select(i => i.Label)
			.Should()
			.Equal(
				"Elasticsearch API",
				"Elasticsearch Serverless API",
				"Kibana APIs",
				"Kibana Serverless APIs",
				"Logstash APIs",
				"Elastic Cloud API",
				"Cloud Billing API",
				"Elastic Cloud Serverless API",
				"Alpha API",
				"Zeta API"
			);
	}

	[Test]
	public void Build_SelectedMatchesCurrentKey()
	{
		var items = ApiHubSwitcher.Build(
			[Entry("elasticsearch", "Elasticsearch"), Entry("kibana", "Kibana")],
			currentApiKey: "elasticsearch"
		);

		items.Count(i => i.Selected).Should().Be(1);
		items.Single(i => i.Selected).Label.Should().Be("Elasticsearch");
	}

	[Test]
	public void Build_ResolvesTheProductIcon()
	{
		var items = ApiHubSwitcher.Build([Entry("elasticsearch", "Elasticsearch")], currentApiKey: "elasticsearch");

		items.Should().ContainSingle().Which.IconSvg.Should().Contain("<svg");
	}

	private static ApiCatalogEntry Entry(string key, string title) => new(key, title, $"/api/doc/{key}/");
}
