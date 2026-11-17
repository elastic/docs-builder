// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Mcp.Remote.Gateways;
using Elastic.Documentation.Search.Contract;

namespace Mcp.Remote.Tests;

public class ApiRouteMatchTests
{
	private static ApiRouteEntry Route(string path, params string[] methods) => new() { Path = path, Methods = methods };

	[Test]
	public void Parse_MethodAndTemplate_LowercasesAndWritesParametersAsWildcards()
	{
		var typed = ApiRouteMatch.Parse("get /{index}/_Search");

		typed.Method.Should().Be("GET");
		typed.Segments.Should().Equal("*", "_search");
	}

	[Test]
	public void Parse_PathWithoutMethod_HasNoMethod()
	{
		var typed = ApiRouteMatch.Parse("/_cluster/health/");

		typed.Method.Should().BeNull();
		typed.Segments.Should().Equal("_cluster", "health");
	}

	[Test]
	[Arguments("GET /_cluster/health", "/_cluster/health", 2)]
	[Arguments("GET /my-index/_search", "/{index}/_search", 1)]
	[Arguments("GET /cluster/health", "/_cluster/health", 2)]
	[Arguments("GET /", "/", 0)]
	[Arguments("GET /_cluster/health", "/_cluster/health/{index}", ApiRouteMatch.NoMatch)]
	[Arguments("GET /{index}/_search", "/_search/{index}", ApiRouteMatch.NoMatch)]
	[Arguments("DELETE /_cluster/health", "/_cluster/health", ApiRouteMatch.NoMatch)]
	public void Score_CountsLiteralSegmentsTheTypedEndpointSpellsOut(string endpoint, string route, int expected) =>
		ApiRouteMatch.Score(ApiRouteMatch.Parse(endpoint), Route(route, "GET")).Should().Be(expected);

	[Test]
	public void Score_PathWithoutMethod_MatchesAnyMethod() =>
		ApiRouteMatch.Score(ApiRouteMatch.Parse("/_bulk"), Route("/_bulk", "POST", "PUT")).Should().Be(1);
}
