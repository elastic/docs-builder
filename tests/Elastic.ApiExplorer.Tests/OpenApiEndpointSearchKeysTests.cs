// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer.Export;
using Elastic.Documentation.Configuration.Versions;
using Elastic.Documentation.Search.Contract;
using Elastic.Documentation.Versions;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Tests;

public class OpenApiEndpointSearchKeysTests
{
	private const string SearchDescription =
		"""
		**All methods and paths for this operation:**

		<div>
		                      <span class="operation-verb get">GET</span>
		                      <span class="operation-path">/_search</span>
		                      </div>
		                    <div>
		                      <span class="operation-verb post">POST</span>
		                      <span class="operation-path">/_search</span>
		                      </div>
		                    <div>
		                      <span class="operation-verb get">GET</span>
		                      <span class="operation-path">/{index}/_search</span>
		                      </div>

		Get search hits that match the query.
		""";

	private static readonly VersionsConfiguration VersionsConfiguration = new()
	{
		VersioningSystems = new Dictionary<VersioningSystemId, VersioningSystem>
		{
			{
				VersioningSystemId.Stack,
				new VersioningSystem { Id = VersioningSystemId.Stack, Base = new SemVersion(8, 0, 0), Current = new SemVersion(9, 2, 0) }
			}
		}
	};

	private static OpenApiDocument CreateSpec() =>
		new()
		{
			Paths = new OpenApiPaths
			{
				["/{index}/_search"] = new OpenApiPathItem
				{
					Operations = new Dictionary<HttpMethod, OpenApiOperation>
					{
						[HttpMethod.Post] = new OpenApiOperation
						{
							OperationId = "search",
							Summary = "Run a search",
							Description = SearchDescription
						}
					}
				},
				["/_search/scroll/{scroll_id}"] = new OpenApiPathItem
				{
					Operations = new Dictionary<HttpMethod, OpenApiOperation>
					{
						[HttpMethod.Post] = new OpenApiOperation { OperationId = "scroll", Summary = "Run a scrolling search" }
					}
				},
				["/_cat/indices/{index}"] = new OpenApiPathItem
				{
					Operations = new Dictionary<HttpMethod, OpenApiOperation>
					{
						[HttpMethod.Get] = new OpenApiOperation { OperationId = "cat-indices", Summary = "Get index information" }
					}
				}
			}
		};

	private static Dictionary<string, DocumentationDocument> ConvertByOperationId() =>
		new OpenApiDocumentExporter(VersionsConfiguration)
			.ConvertToDocuments(CreateSpec(), "elasticsearch")
			.ToDictionary(d => d.Api!.OperationId);

	[Test]
	public void ConvertToDocuments_SearchWithAlternatePaths_IndexesOwnPathThenAlternatesDeduped()
	{
		var search = ConvertByOperationId()["search"];

		search.Api!.Endpoints.Should().Equal("{index}/_search", "_search");
		search.Api.Method.Should().Be("POST");
		search.Api.Path.Should().Be("/{index}/_search");
		search.Api.OperationId.Should().Be("search");
	}

	[Test]
	public void ConvertToDocuments_ScrollWithTrailingParameter_IndexesPathWithAndWithoutParameter()
	{
		var scroll = ConvertByOperationId()["scroll"];

		scroll.Api!.Endpoints.Should().Equal("_search/scroll/{scroll_id}", "_search/scroll");
	}

	[Test]
	public void ConvertToDocuments_CatIndices_IndexesPathWithAndWithoutTrailingParameter()
	{
		var catIndices = ConvertByOperationId()["cat-indices"];

		catIndices.Api!.Endpoints.Should().Equal("_cat/indices/{index}", "_cat/indices");
		catIndices.Api.Method.Should().Be("GET");
	}

	[Test]
	public void AlternatePaths_OperationListHtml_ReturnsRawPathsInOrder() =>
		OpenApiDocumentExporter.AlternatePaths(SearchDescription).Should().Equal("/_search", "/_search", "/{index}/_search");

	[Test]
	public void AlternatePaths_NoOperationList_ReturnsEmpty() =>
		OpenApiDocumentExporter.AlternatePaths("Get search hits that match the query.").Should().BeEmpty();
}
