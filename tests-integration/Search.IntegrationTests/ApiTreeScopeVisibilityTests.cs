// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Nodes;
using AwesomeAssertions;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Search;
using Elastic.Documentation.Search;
using Elastic.Documentation.Search.Common;
using Elastic.Documentation.Search.Contract;
using Elastic.Documentation.Search.Contract.Mapping;
using Elastic.Mapping;
using Elastic.Transport;
using Microsoft.Extensions.Logging.Abstractions;
using SearchRequest = Elastic.Documentation.Search.Contract.SearchRequest;

namespace Search.IntegrationTests;

/// <summary>
/// Runs the queries the search service builds against a throwaway cluster that holds five hand-built documents, and
/// checks which of them each version-tree scope lets through. It creates and deletes its own index, so point
/// <c>API_TREE_SCOPE_ES_URL</c> at a disposable cluster with security disabled, never at a shared one.
/// </summary>
[NotInParallel(nameof(ApiTreeScopeVisibilityTests))]
public class ApiTreeScopeVisibilityTests
{
	private const string Markdown = "/docs/explore-analyze/search-guide";
	private const string LegacyApi = "/docs/api/doc/legacy/operation/operation-search-legacy";
	private const string EsDefault = "/docs/api/doc/elasticsearch/operation/operation-search";
	private const string EsDefaultInV9 = "/docs/api/doc/elasticsearch/v9/operation/operation-search";
	private const string EsV8Variant = "/docs/api/doc/elasticsearch/v8/operation/operation-search";
	private const string CloudDefault = "/docs/api/doc/cloud/operation/operation-search-deployments";

	private static readonly string? EndpointUrl = Environment.GetEnvironmentVariable("API_TREE_SCOPE_ES_URL");

	private static DocumentationDocument Api(
		string path,
		string title,
		string key,
		bool hidden,
		params (string Tree, string Root)[] trees
	) =>
		new()
		{
			Path = path,
			Title = title,
			SearchTitle = title,
			ContentType = "api",
			Hidden = hidden,
			Api = new ApiOperationMetadata
			{
				Endpoints = ["_search"],
				Method = "POST",
				Path = "/_search",
				OperationId = "search",
				IdWords = 1,
				Key = key,
				Trees = trees.Select(t => new ApiTreeEntry { Tree = t.Tree, Root = t.Root }).ToArray()
			}
		};

	private static IEnumerable<DocumentationDocument> Documents()
	{
		yield return new DocumentationDocument { Path = Markdown, Title = "Search guide", SearchTitle = "Search guide" };
		yield return Api(LegacyApi, "Search legacy", key: "", hidden: false);
		yield return Api(
			EsDefault,
			"Search",
			"elasticsearch",
			hidden: false,
			("main", "/docs/api/doc/elasticsearch"),
			("9", "/docs/api/doc/elasticsearch/v9")
		);
		yield return Api(EsV8Variant, "Search", "elasticsearch", hidden: true, ("8", "/docs/api/doc/elasticsearch/v8"));
		yield return Api(CloudDefault, "Search deployments", "cloud", hidden: false, ("main", "/docs/api/doc/cloud"));
	}

	/// <summary>Each scope with the hit URLs a reader gets, which name the document that matched.</summary>
	public static IEnumerable<Func<(ApiTreeScope?, string[])>> Scopes()
	{
		yield return () => (null, [Markdown, LegacyApi, EsDefault, CloudDefault]);
		yield return () => (new ApiTreeScope("elasticsearch", "8"), [Markdown, LegacyApi, EsV8Variant, CloudDefault]);
		yield return () => (new ApiTreeScope("elasticsearch", "9"), [Markdown, LegacyApi, EsDefaultInV9, CloudDefault]);
		yield return () => (new ApiTreeScope("cloud", "4"), [Markdown, LegacyApi, EsDefault]);
	}

	[Test]
	[MethodDataSource(nameof(Scopes))]
	public async Task NavigationSearch_Scope_ShowsTheExpectedDocuments(ApiTreeScope? scope, string[] hitUrls)
	{
		Skip.Unless(EndpointUrl is not null, "API_TREE_SCOPE_ES_URL is not set");
		var ct = TestContext.Current!.Execution.CancellationToken;
		using var cluster = await DisposableIndex.CreateAsync(new Uri(EndpointUrl), Documents(), ct);
		using var navigation = new NavigationSearchService(cluster.Search, cluster.Accessor, NullLogger<NavigationSearchService>.Instance);

		var docsWide = await navigation.NavigationSearchAsync(new NavigationSearchRequest { Query = "search", ApiTree = scope }, ct);
		var apiOnly = await navigation.NavigationSearchAsync(
			new NavigationSearchRequest { Query = "search", TypeFilter = "api", ApiTree = scope },
			ct
		);

		docsWide.Results.Select(r => r.Url).Order().Should().Equal(hitUrls.Order());
		apiOnly.Results.Select(r => r.Url).Order().Should().Equal(hitUrls.Where(u => u != Markdown).Order());
	}

	[Test]
	public async Task SearchAsync_NoScope_ShowsDefaultRenderingsOnly()
	{
		Skip.Unless(EndpointUrl is not null, "API_TREE_SCOPE_ES_URL is not set");
		var ct = TestContext.Current!.Execution.CancellationToken;
		using var cluster = await DisposableIndex.CreateAsync(new Uri(EndpointUrl), Documents(), ct);

		var response = await cluster.Search.SearchAsync(new SearchRequest { Query = "search" }, ct);

		response
			.Results
			.Select(r => r.Document.Path)
			.Order()
			.Should()
			.Equal(new[] { Markdown, LegacyApi, EsDefault, CloudDefault }.Order());
	}

	private sealed class DisposableIndex : IDisposable
	{
		private readonly string _name;

		private DisposableIndex(ElasticsearchClientAccessor accessor, string name)
		{
			Accessor = accessor;
			_name = name;
			Search = new DefaultSearchService<DocumentationDocument>(
				accessor.Client,
				name,
				new SearchQueryConfiguration { SemanticEnabled = false },
				NullLogger<DefaultSearchService<DocumentationDocument>>.Instance
			);
		}

		public ElasticsearchClientAccessor Accessor { get; }
		public DefaultSearchService<DocumentationDocument> Search { get; }

		public static async Task<DisposableIndex> CreateAsync(Uri uri, IEnumerable<DocumentationDocument> documents, CancellationToken ct)
		{
			var accessor = new ElasticsearchClientAccessor(
				new DocumentationEndpoints
				{
					Elasticsearch = new ElasticsearchEndpoint { Uri = uri, Username = "elastic", Password = "unused" }
				},
				new SearchConfiguration { Synonyms = [], Rules = [], DiminishTerms = [] }
			);
			var index = new DisposableIndex(accessor, $"api-tree-scope-check-{Guid.NewGuid():N}");
			try
			{
				await index.FillAsync(documents, ct);
				return index;
			}
			catch
			{
				index.Dispose();
				throw;
			}
		}

		private async Task FillAsync(IEnumerable<DocumentationDocument> documents, CancellationToken ct)
		{
			var synonyms = await Accessor
				.Client
				.Transport
				.PutAsync<StringResponse>(
					$"_synonyms/{_name}",
					PostData.String("""{"synonyms_set":[{"synonyms":"docs, documentation"}]}"""),
					ct
				);
			synonyms.ApiCallDetails.HasSuccessfulStatusCode.Should().BeTrue(synonyms.Body);
			var created = await Accessor.Client.Transport.PutAsync<StringResponse>(_name, PostData.String(LexicalIndexBody(_name)), ct);
			created.ApiCallDetails.HasSuccessfulStatusCode.Should().BeTrue(created.Body);
			foreach (var document in documents)
			{
				var indexed = await Accessor.Client.IndexAsync(document, i => i.Index(_name).Id(document.Path), ct);
				indexed.IsValidResponse.Should().BeTrue(indexed.DebugInformation);
			}
			_ = await Accessor.Client.Indices.RefreshAsync(_name, cancellationToken: ct);
		}

		/// <summary>The lexical docs index with the analysis the exporter gives it, synonym sets included.</summary>
		private static string LexicalIndexBody(string synonymSetName)
		{
			var context = DocumentationMappingContext.DocumentationDocument.CreateContext(type: "assembler", env: "test");
			var settings = ElasticsearchTypeContext.BuildFullSettingsJson(
				context.GetSettingsJson(),
				a => SharedAnalysisFactory.BuildAnalysis(a, synonymSetName, ["docs, documentation"]),
				indexSettings: null
			);
			return new JsonObject
			{
				["settings"] = JsonNode.Parse(settings)!["settings"]!.DeepClone(),
				["mappings"] = JsonNode.Parse(context.GetMappingsJson())
			}.ToJsonString();
		}

		public void Dispose()
		{
			_ = Accessor.Client.Transport.Delete<StringResponse>(_name);
			_ = Accessor.Client.Transport.Delete<StringResponse>($"_synonyms/{_name}");
			Accessor.Dispose();
		}
	}
}
