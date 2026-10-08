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
	public void Group_ProductionShape_SplitsTheStackFromOrchestration()
	{
		var groups = ApiCatalogViewModel.Group(Production);

		groups.Select(g => g.Title).Should().Equal("Elastic Stack", "Orchestration");
		groups[0]
			.Entries
			.Select(e => e.Key)
			.Should()
			.Equal("elasticsearch", "kibana", "logstash", "elasticsearch-serverless", "kibana-serverless");
		groups[1]
			.Entries
			.Select(e => e.Key)
			.Should()
			.Equal("cloud", "cloud-billing", "cloud-connect", "cloud-enterprise", "cloud-serverless");
	}

	[Test]
	public void Group_ShowsEveryApiExactlyOnce()
	{
		var ordered = ApiCatalogViewModel.Group(Production).SelectMany(g => g.Entries).Select(e => e.Key).ToArray();

		ordered.Should().BeEquivalentTo(Production.Select(e => e.Key));
		ordered.Should().OnlyHaveUniqueItems();
	}

	[Test]
	public void Group_UnknownApis_FollowTheGroupsSortedByTitle()
	{
		var groups = ApiCatalogViewModel.Group([
			Entry("zeta", "Zeta API"),
			Entry("alpha", "alpha API"),
			Entry("kibana", "Kibana APIs"),
			Entry("cloud", "Elastic Cloud API")
		]);

		groups.Select(g => g.Title).Should().Equal("Elastic Stack", "Orchestration", null);
		groups[2].Entries.Select(e => e.Key).Should().Equal("alpha", "zeta");
	}

	[Test]
	public void Group_OnlyOneGroupHasApis_DropsItsTitle()
	{
		var groups = ApiCatalogViewModel.Group([Entry("logstash", "Logstash APIs"), Entry("kibana", "Kibana APIs")]);

		groups.Should().ContainSingle().Which.Title.Should().BeNull();
		groups[0].Entries.Select(e => e.Key).Should().Equal("kibana", "logstash");
	}

	[Test]
	public void Group_OnlyOneGroupHasApis_StaysGrouped()
	{
		var groups = ApiCatalogViewModel.Group([Entry("cloud", "Elastic Cloud API"), Entry("cloud-billing", "Cloud Billing API")]);

		groups.Should().ContainSingle().Which.Grouped.Should().BeTrue("a lone group keeps its compact cards, only its label goes");
	}

	[Test]
	public void Group_UnknownApis_AreNotGrouped() =>
		ApiCatalogViewModel.Group([Entry("zeta", "Zeta API"), Entry("cloud", "Elastic Cloud API")])[^1].Grouped.Should().BeFalse();

	[Test]
	public void Group_Empty_ReturnsNothing() => ApiCatalogViewModel.Group([]).Should().BeEmpty();

	[Test]
	public void Group_AllFeaturedPresent_PullsThemOutInOrder()
	{
		var stack = ApiCatalogViewModel.Group(Production)[0];

		stack.Featured.Select(e => e.Key).Should().Equal("elasticsearch", "kibana", "logstash");
		stack.Others.Select(e => e.Key).Should().Equal("elasticsearch-serverless", "kibana-serverless");
	}

	[Test]
	public void Group_OnlyOneFeaturedPresent_KeepsEverythingInOneGrid()
	{
		var stack = ApiCatalogViewModel.Group([
			Entry("kibana", "Kibana APIs"),
			Entry("kibana-serverless", "Kibana Serverless APIs")
		]).Single();

		stack.Featured.Should().BeEmpty();
		stack.Others.Select(e => e.Key).Should().Equal("kibana", "kibana-serverless");
	}

	[Test]
	public void ColumnOf_Variant_IsTheColumnOfItsParentWhateverTheOrder()
	{
		var elasticsearch = Item("elasticsearch");
		var kibana = Item("kibana");
		var logstash = Item("logstash");
		var kibanaServerless = Item("kibana-serverless", parentKey: "kibana");
		var group = new ApiCatalogGroup("Elastic Stack", Grouped: true, [elasticsearch, kibana, logstash], [kibanaServerless]);

		group.ColumnOf(kibanaServerless).Should().Be(2, "the tag-on sits under Kibana even though Elasticsearch has no variant");
		group.HasVariant(kibana).Should().BeTrue();
		group.HasVariant(elasticsearch).Should().BeFalse();
	}

	[Test]
	public void ColumnOf_VariantWithoutAFeaturedParent_IsNull()
	{
		var orphan = Item("kibana-serverless", parentKey: "kibana");
		var group = new ApiCatalogGroup("Elastic Stack", Grouped: true, [Item("elasticsearch"), Item("logstash")], [orphan]);

		group.ColumnOf(orphan).Should().BeNull("Kibana is not featured here, so the tag-on stays a separate card");
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

	private static ApiCatalogItem Item(string key, string? parentKey = null) =>
		new(key, key, $"/api/doc/{key}/", "<svg></svg>", null, [], parentKey);

	private static ApiCatalogEntry Entry(string key, string title, params string[] categories) =>
		new(key, title, $"/api/doc/{key}/") { CatalogCategories = categories };
}
