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
		var items = ApiHubSwitcher.Build([Entry("elasticsearch", "Elasticsearch")], currentApiKey: null, "/api/");

		items.Should().BeEmpty();
	}

	[Test]
	public void Build_EmptyEntries_ReturnsEmpty()
	{
		var items = ApiHubSwitcher.Build([], currentApiKey: "elasticsearch", "/api/");

		items.Should().BeEmpty();
	}

	[Test]
	public void Build_HubOptionFirst_NeverSelected()
	{
		var items = ApiHubSwitcher.Build([Entry("elasticsearch", "Elasticsearch")], currentApiKey: "elasticsearch", "/api/");

		items[0].Label.Should().Be("Back to hub");
		items[0].Url.Should().Be("/api/");
		items[0].Selected.Should().BeFalse();
	}

	[Test]
	public void Build_OrdersEntriesAlphabeticallyByTitle()
	{
		var items = ApiHubSwitcher.Build(
			[Entry("kibana", "Kibana"), Entry("elasticsearch", "Elasticsearch")],
			currentApiKey: "kibana",
			"/api/"
		);

		items.Select(i => i.Label).Should().Equal("Back to hub", "Elasticsearch", "Kibana");
	}

	[Test]
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

	private static ApiCatalogEntry Entry(string key, string title) => new(key, title, $"/api/doc/{key}/");
}
