// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Api.Core.Search;

namespace Elastic.Documentation.Api.Infrastructure.Adapters.Search;

public class MockSearchGateway : ISearchGateway
{
	private static readonly List<SearchResultItem> Results =
	[
		new SearchResultItem
		{
			Url = "https://www.elastic.co/kibana",
			Title = "Kibana: Explore, Visualize, Discover Data",
			Description =
				"Run data analytics at speed and scale for observability, security, and search with Kibana. Powerful analysis on any data from any source.",
			Parents = []
		},
		new SearchResultItem
		{
			Url = "https://www.elastic.co/docs/explore-analyze",
			Title = "Explore and analyze | Elastic Docs",
			Description = "Kibana provides a comprehensive suite of tools to help you search, interact with, explore, and analyze your data effectively.",
			Parents = []
		},
		new SearchResultItem
		{
			Url = "https://www.elastic.co/docs/deploy-manage/deploy/self-managed/install-kibana",
			Title = "Install Kibana | Elastic Docs",
			Description =
				"Information on how to set up Kibana and get it running, including downloading, enrollment with Elasticsearch cluster, and configuration.",
			Parents = []
		},
		new SearchResultItem
		{
			Url = "https://www.elastic.co/kibana/kibana-lens",
			Title = "Kibana Lens – Data visualization. Simply.",
			Description =
				"Kibana Lens simplifies the process of data visualization through a drag‑and‑drop experience, ideal for exploring logs, trends, and metrics.",
			Parents = []
		},
		new SearchResultItem
		{
			Url = "https://www.elastic.co/docs",
			Title = "Elastic Docs – Elastic products, guides & reference",
			Description =
				"Official Elastic documentation. Explore guides for Elastic Cloud (hosted & on‑prem), product documentation, how‑to guides and API reference.",
			Parents = []
		},
		new SearchResultItem
		{
			Url = "https://www.elastic.co/docs/get-started/introduction",
			Title = "Get started | Elastic Docs",
			Description =
				"Use Elasticsearch to search, index, store, and analyze data of all shapes and sizes in near real time. Kibana is the graphical user interface for Elasticsearch.",
			Parents = []
		},
		new SearchResultItem
		{
			Url = "https://www.elastic.co/docs/solutions/search/elasticsearch-basics-quickstart",
			Title = "Elasticsearch basics quickstart",
			Description = "Hands‑on introduction to fundamental Elasticsearch concepts: indices, documents, mappings, and search via Console syntax.",
			Parents = []
		},
		new SearchResultItem
		{
			Url = "https://www.elastic.co/docs/api/doc/elasticsearch/group/endpoint-document",
			Title = "Elasticsearch API documentation",
			Description =
				"Elastic provides REST APIs that are used by the UI components and can be called directly to configure and access Elasticsearch features.",
			Parents = []
		}
	];

	public async Task<(int TotalHits, List<SearchResultItem> Results)> SearchAsync(string query, int pageNumber, int pageSize, CancellationToken ctx = default)
	{
		var filteredResults = Results
			.Where(item =>
				item.Title.Equals(query, StringComparison.OrdinalIgnoreCase) ||
				item.Description?.Equals(query, StringComparison.OrdinalIgnoreCase) == true)
			.ToList();

		var pagedResults = filteredResults
			.Skip((pageNumber - 1) * pageSize)
			.Take(pageSize)
			.ToList();

		Console.WriteLine($"MockSearchGateway: Paged results count: {pagedResults.Count}");

		return await Task.Delay(1000, ctx)
			.ContinueWith(_ => (TotalHits: filteredResults.Count, Results: pagedResults), ctx);
	}
}
