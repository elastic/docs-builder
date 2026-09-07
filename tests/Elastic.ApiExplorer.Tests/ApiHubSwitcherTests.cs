// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Landing;
using Elastic.Documentation.Configuration.Products;
using Elastic.Documentation.Configuration.Toc;

namespace Elastic.ApiExplorer.Tests;

public class ApiHubSwitcherTests
{
	[Fact]
	public void CollectDeclaredEntries_NullConfig_ReturnsEmpty()
	{
		var entries = ApiHubSwitcher.CollectDeclaredEntries("", null);

		entries.Should().BeEmpty();
	}

	[Fact]
	public void CollectDeclaredEntries_EmptyConfig_ReturnsEmpty()
	{
		var entries = ApiHubSwitcher.CollectDeclaredEntries("", new Dictionary<string, ResolvedApiConfiguration>());

		entries.Should().BeEmpty();
	}

	[Fact]
	public void CollectDeclaredEntries_TwoConfigs_UsesDisplayNamesAndProductRoots()
	{
		var configs = new Dictionary<string, ResolvedApiConfiguration>
		{
			["elasticsearch"] = Config("elasticsearch", "Elasticsearch"),
			["kibana"] = Config("kibana", "Kibana")
		};

		var entries = ApiHubSwitcher.CollectDeclaredEntries("", configs);

		entries.Should().HaveCount(2);
		entries
			.Should()
			.ContainSingle(e => e.Key == "elasticsearch")
			.Which
			.Should()
			.BeEquivalentTo(new ApiCatalogEntry("elasticsearch", "Elasticsearch", "/api/doc/elasticsearch/"));
		entries
			.Should()
			.ContainSingle(e => e.Key == "kibana")
			.Which
			.Should()
			.BeEquivalentTo(new ApiCatalogEntry("kibana", "Kibana", "/api/doc/kibana/"));
	}

	[Fact]
	public void Build_NullCurrentKey_ReturnsEmpty()
	{
		var items = ApiHubSwitcher.Build([Entry("elasticsearch", "Elasticsearch")], currentApiKey: null, "/api/");

		items.Should().BeEmpty();
	}

	[Fact]
	public void Build_EmptyEntries_ReturnsEmpty()
	{
		var items = ApiHubSwitcher.Build([], currentApiKey: "elasticsearch", "/api/");

		items.Should().BeEmpty();
	}

	[Fact]
	public void Build_HubOptionFirst_NeverSelected()
	{
		var items = ApiHubSwitcher.Build([Entry("elasticsearch", "Elasticsearch")], currentApiKey: "elasticsearch", "/api/");

		items[0].Label.Should().Be("Back to hub");
		items[0].Url.Should().Be("/api/");
		items[0].Selected.Should().BeFalse();
	}

	[Fact]
	public void Build_OrdersEntriesAlphabeticallyByTitle()
	{
		var items = ApiHubSwitcher.Build(
			[Entry("kibana", "Kibana"), Entry("elasticsearch", "Elasticsearch")],
			currentApiKey: "kibana",
			"/api/"
		);

		items.Select(i => i.Label).Should().Equal("Back to hub", "Elasticsearch", "Kibana");
	}

	[Fact]
	public void Build_SelectedMatchesCurrentKey()
	{
		var items = ApiHubSwitcher.Build(
			[Entry("elasticsearch", "Elasticsearch"), Entry("kibana", "Kibana")],
			currentApiKey: "elasticsearch",
			"/api/"
		);

		items.Count(i => i.Selected).Should().Be(1);
		items.Single(i => i.Selected).Label.Should().Be("Elasticsearch");
	}

	private static ResolvedApiConfiguration Config(string key, string displayName) =>
		new() { ProductKey = key, Product = new Product { Id = key, DisplayName = displayName }, SpecFileName = $"{key}.json" };

	private static ApiCatalogEntry Entry(string key, string title) => new(key, title, $"/api/doc/{key}/");
}
