// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer.Landing;

namespace Elastic.ApiExplorer.Tests;

public class ApiCatalogViewModelTests
{
	// Mirrors the api: block in elastic/docs-content docset.yml: key, title, categories.
	private static readonly ApiCatalogEntry[] Production =
	[
		Entry("cloud-serverless", "Elastic Cloud Serverless API", "serverless"),
		Entry("logstash", "Logstash APIs", "self"),
		Entry("cloud-billing", "Cloud Billing API", "ess"),
		Entry("kibana-serverless", "Kibana Serverless APIs", "serverless"),
		Entry("cloud-connect", "Elastic Cloud Connected API"),
		Entry("elasticsearch", "Elasticsearch API", "self", "ece", "ess"),
		Entry("cloud-enterprise", "Elastic Cloud Enterprise API"),
		Entry("kibana", "Kibana APIs", "self", "ece", "ess"),
		Entry("cloud", "Elastic Cloud API"),
		Entry("elasticsearch-serverless", "Elasticsearch Serverless API", "serverless")
	];

	[Test]
	public void Order_ProductionShape_ListsPriorityApisFirstThenTheRestByTitle() =>
		ApiCatalogViewModel
			.Order(Production)
			.Select(e => e.Key)
			.Should()
			.Equal(
				"elasticsearch",
				"kibana",
				"elasticsearch-serverless",
				"kibana-serverless",
				"logstash",
				"cloud",
				"cloud-billing",
				"cloud-connect",
				"cloud-enterprise",
				"cloud-serverless"
			);

	[Test]
	public void Order_ShowsEveryApiExactlyOnce()
	{
		var ordered = ApiCatalogViewModel.Order(Production).Select(e => e.Key).ToArray();

		ordered.Should().BeEquivalentTo(Production.Select(e => e.Key));
		ordered.Should().OnlyHaveUniqueItems();
	}

	[Test]
	public void Order_UnknownApis_SortByTitleAfterThePriorityOnes() =>
		ApiCatalogViewModel
			.Order([Entry("zeta", "Zeta API"), Entry("alpha", "alpha API"), Entry("kibana", "Kibana APIs")])
			.Select(e => e.Key)
			.Should()
			.Equal("kibana", "alpha", "zeta");

	[Test]
	public void Order_SameTitle_FallsBackToKey() =>
		ApiCatalogViewModel.Order([Entry("b", "Same API"), Entry("a", "Same API")]).Select(e => e.Key).Should().Equal("a", "b");

	[Test]
	public void Order_Empty_ReturnsEmpty() => ApiCatalogViewModel.Order([]).Should().BeEmpty();

	[Test]
	public void Split_BothFeaturedPresent_PullsThemOutInOrder()
	{
		var (featured, others) = ApiCatalogViewModel.Split(Production);

		featured.Select(e => e.Key).Should().Equal("elasticsearch", "kibana");
		others
			.Select(e => e.Key)
			.Should()
			.Equal(
				"elasticsearch-serverless",
				"kibana-serverless",
				"logstash",
				"cloud",
				"cloud-billing",
				"cloud-connect",
				"cloud-enterprise",
				"cloud-serverless"
			);
	}

	[Test]
	public void Split_OneFeaturedMissing_KeepsEverythingInOneGrid()
	{
		var (featured, others) = ApiCatalogViewModel.Split([Entry("kibana", "Kibana APIs"), Entry("logstash", "Logstash APIs")]);

		featured.Should().BeEmpty();
		others.Select(e => e.Key).Should().Equal("kibana", "logstash");
	}

	[Test]
	public void Split_Empty_ReturnsNothing()
	{
		var (featured, others) = ApiCatalogViewModel.Split([]);

		featured.Should().BeEmpty();
		others.Should().BeEmpty();
	}

	[Test]
	public void DeploymentsOf_AllCategories_ListsThemInReadingOrderWithFullNames()
	{
		var deployments = ApiCatalogViewModel.DeploymentsOf(
			Entry("elasticsearch", "Elasticsearch API", "serverless", "ess", "self", "ece")
		);

		deployments.Select(d => d.Label).Should().Equal("Self-managed", "ECE", "ECH", "Serverless");
		deployments.Select(d => d.Name).Should().Equal("Self-managed", "Elastic Cloud Enterprise", "Elastic Cloud Hosted", "Serverless");
	}

	[Test]
	public void DeploymentsOf_NoCategories_IsEmpty() =>
		ApiCatalogViewModel.DeploymentsOf(Entry("cloud", "Elastic Cloud API")).Should().BeEmpty();

	private static ApiCatalogEntry Entry(string key, string title, params string[] categories) =>
		new(key, title, $"/api/doc/{key}/") { CatalogCategories = categories };
}
