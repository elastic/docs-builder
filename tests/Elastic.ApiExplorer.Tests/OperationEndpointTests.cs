// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Nodes;
using AwesomeAssertions;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Model;
using Elastic.ApiExplorer.Operations;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Tests;

public class OperationEndpointTests
{
	private static ApiOperation Op(HttpMethod method, string route, string? operationId = null, params string[] queryParameters) =>
		new(
			method,
			new OpenApiOperation
			{
				OperationId = operationId,
				Parameters =
				[
					.. queryParameters.Select(
						static name => (IOpenApiParameter)new OpenApiParameter { Name = name, In = ParameterLocation.Query }
					)
				],
				Responses = new OpenApiResponses { ["200"] = new OpenApiResponse { Description = "ok" } }
			},
			route,
			new OpenApiPathItem(),
			"search"
		);

	private static string Bump(params (string Method, string Route)[] variants) =>
		"**All methods and paths for this operation:**\n\n"
			+ string.Concat(
				variants.Select(
					static v =>
						$"<div>\n  <span class=\"operation-verb {v.Method.ToLowerInvariant()}\">{v.Method}</span>\n  <span class=\"operation-path\">{v.Route}</span>\n  </div>\n"
				)
			)
			+ "\nRuns a search.";

	private static string Label(EndpointRow row) =>
		$"{row.Method}{(row.AlsoMethods.Count > 0 ? $"+{string.Join(",", row.AlsoMethods)}" : "")} {row.Route}";

	[Test]
	public void Build_SearchSiblings_MergeIntoTwoRowsWithPostFirst()
	{
		var get = Op(HttpMethod.Get, "/_search", "search", "q");
		var siblings = new[]
		{
			Op(HttpMethod.Post, "/_search", "search-1", "q"),
			Op(HttpMethod.Get, "/{index}/_search", "search-2", "q"),
			Op(HttpMethod.Post, "/{index}/_search", "search-3", "q")
		};

		var endpoint = OperationEndpoint.Build(get, null, siblings);

		endpoint.Rows.Select(Label).Should().Equal("post+get /{index}/_search", "post+get /_search");
		endpoint.Rows[0].Segments.Select(static s => (s.Text, s.Optional)).Should().Equal(("{index}", true), ("_search", false));
		endpoint.OptionalPathParameters.Should().BeEquivalentTo(["index"]);
		endpoint.ShortestRoute.Should().Be("/_search");
	}

	[Test]
	public void Build_BumpListing_PutItemStillLeadsWithPost_AndIsStrippedFromTheDescription()
	{
		var put = Op(HttpMethod.Put, "/{index}/_bulk", "bulk");
		var description = Bump(("POST", "/_bulk"), ("PUT", "/_bulk"), ("POST", "/{index}/_bulk"), ("PUT", "/{index}/_bulk"));

		var endpoint = OperationEndpoint.Build(put, description, []);

		endpoint.Rows.Select(Label).Should().Equal("post+put /{index}/_bulk", "post+put /_bulk");
		endpoint.Description.Should().Be("Runs a search.");
	}

	[Test]
	public void Build_SameLengthRoutes_TheSpecItemWinsTheMainRow()
	{
		var item = Op(HttpMethod.Post, "/{index}/_aliases/{name}", "indices-put-alias");
		var description = Bump(
			("PUT", "/{index}/_alias/{name}"),
			("POST", "/{index}/_alias/{name}"),
			("PUT", "/{index}/_aliases/{name}"),
			("POST", "/{index}/_aliases/{name}")
		);

		var endpoint = OperationEndpoint.Build(item, description, []);

		endpoint.Rows.Select(Label).Should().Equal("post+put /{index}/_aliases/{name}", "post+put /{index}/_alias/{name}");
		endpoint.OptionalPathParameters.Should().BeEmpty();
	}

	[Test]
	public void Build_KibanaSpacesListing_LeadsWithThePathOutsideTheSpaceAndListsTheSpaceVariantAfterIt()
	{
		var item = Op(HttpMethod.Post, "/api/agent_builder/tools/_execute");
		const string description =
			"**Spaces method and path for this operation:**\n\n<div><span class=\"operation-verb post\">post</span>&nbsp;<span class=\"operation-path\">/s/{space_id}/api/agent_builder/tools/_execute</span></div>\n\nRefer to Spaces for more information.";

		var endpoint = OperationEndpoint.Build(item, description, []);

		endpoint
			.Rows
			.Select(static r => r.Route)
			.Should()
			.Equal("/api/agent_builder/tools/_execute", "/s/{space_id}/api/agent_builder/tools/_execute");
		endpoint.Rows.SelectMany(static r => r.Segments).Should().OnlyContain(static s => !s.Optional);
		endpoint.OptionalPathParameters.Should().BeEmpty();
		endpoint.ShortestRoute.Should().Be("/api/agent_builder/tools/_execute");
		endpoint.Description.Should().Be("Refer to Spaces for more information.");
	}

	[Test]
	public void FromVariants_SpacePrefixOnAnUnrelatedRoute_IsNotASpaceVariant()
	{
		// /s/{id}/other is not /other in a space; the longest-first rule applies as usual.
		var endpoint = OperationEndpoint.FromVariants([new("get", "/s/{id}/other"), new("get", "/api/other")], "/api/other");

		endpoint.Rows.Select(static r => r.Route).Should().Equal("/s/{id}/other", "/api/other");
	}

	[Test]
	public void Build_ClusterHealth_GetOnly_IndexIsOptional()
	{
		var item = Op(HttpMethod.Get, "/_cluster/health/{index}", "cluster-health-1");

		var endpoint = OperationEndpoint.Build(item, Bump(("GET", "/_cluster/health"), ("GET", "/_cluster/health/{index}")), []);

		endpoint.Rows.Select(Label).Should().Equal("get /_cluster/health/{index}", "get /_cluster/health");
		endpoint.OptionalPathParameters.Should().BeEquivalentTo(["index"]);
	}

	[Test]
	public void Build_SinglePath_HasOneRowAndKeepsTheDescription()
	{
		var endpoint = OperationEndpoint.Build(Op(HttpMethod.Delete, "/_async_search/{id}"), "Deletes an async search.", []);

		endpoint.Rows.Select(Label).Should().Equal("delete /_async_search/{id}");
		endpoint.OptionalPathParameters.Should().BeEmpty();
		endpoint.Description.Should().Be("Deletes an async search.");
	}

	[Test]
	public void SelectPrimary_PicksTheDominantMethodOnTheLongestRoute()
	{
		var operations = new[]
		{
			Op(HttpMethod.Get, "/_search", "search"),
			Op(HttpMethod.Post, "/_search", "search-1"),
			Op(HttpMethod.Get, "/{index}/_search", "search-2"),
			Op(HttpMethod.Post, "/{index}/_search", "search-3")
		};

		var primary = OperationEndpoint.SelectPrimary(operations);

		primary.Operation.OperationId.Should().Be("search-3");
		ApiUrlBuilder.CanonicalOperationMoniker(operations, primary).Should().Be("operation-search");
	}

	private static OperationEndpoint SearchEndpoint() =>
		OperationEndpoint.FromVariants(
			[
				new EndpointVariant("post", "/{index}/_search"),
				new EndpointVariant("get", "/{index}/_search"),
				new EndpointVariant("post", "/_search"),
				new EndpointVariant("get", "/_search")
			],
			"/{index}/_search"
		);

	[Test]
	[Arguments("/my-index-000001/_search?from=40&size=20", "/{index}/_search")]
	[Arguments("/_search", "/_search")]
	[Arguments("_search", "/_search")]
	[Arguments("my-index/_search", "/{index}/_search")]
	[Arguments("$ELASTICSEARCH_URL/my-index/_search?size=1", "/{index}/_search")]
	[Arguments("https://localhost:9200/_search", "/_search")]
	[Arguments("localhost:9200/_search", "/_search")]
	public void TryFindRow_RequestTargets_ResolveToTheirRow(string target, string route)
	{
		SearchEndpoint().TryFindRow(target, out var row).Should().BeTrue();
		row!.Route.Should().Be(route);
	}

	[Test]
	[Arguments("/my-index/_count")]
	[Arguments("/a/b/_search")]
	[Arguments("")]
	[Arguments("$ELASTICSEARCH_URL")]
	public void TryFindRow_UnknownTarget_IsFalse(string target) => SearchEndpoint().TryFindRow(target, out _).Should().BeFalse();

	[Test]
	public void TryFindRow_LiteralSegmentsWinOverPlaceholders()
	{
		var endpoint = OperationEndpoint.FromVariants(
			[new EndpointVariant("get", "/_cat/{target}"), new EndpointVariant("get", "/_cat/indices")],
			"/_cat/indices"
		);

		endpoint.TryFindRow("/_cat/indices", out var row).Should().BeTrue();
		row!.Route.Should().Be("/_cat/indices");
	}

	[Test]
	[Arguments("kbn:/api/agent_builder/tools/_execute", "/api/agent_builder/tools/_execute")]
	[Arguments("kbn://api/agent_builder/tools/_execute", "/api/agent_builder/tools/_execute")]
	[Arguments("http://${KIBANA_URL}/api/agent_builder/tools/_execute", "/api/agent_builder/tools/_execute")]
	[Arguments("kbn:/s/my-space/api/agent_builder/tools/_execute", "/s/{space_id}/api/agent_builder/tools/_execute")]
	public void TryFindRow_KibanaTargets_ResolveToTheirRow(string target, string route)
	{
		var endpoint = OperationEndpoint.FromVariants(
			[
				new EndpointVariant("post", "/api/agent_builder/tools/_execute"),
				new EndpointVariant("post", "/s/{space_id}/api/agent_builder/tools/_execute")
			],
			"/api/agent_builder/tools/_execute"
		);

		endpoint.TryFindRow(target, out var row).Should().BeTrue();
		row!.Route.Should().Be(route);
	}

	[Test]
	public void CanonicalOperationMoniker_UnrelatedIds_UseThePrimaryOperation()
	{
		var operations = new[] { Op(HttpMethod.Get, "/a", "op-a"), Op(HttpMethod.Post, "/b", "op-b") };

		ApiUrlBuilder.CanonicalOperationMoniker(operations, operations[1]).Should().Be("operation-op-b");
	}

	[Test]
	public void CanonicalOperationMoniker_GroupWithAnotherApi_StillUsesThePrimaryBase()
	{
		var operations = new[]
		{
			Op(HttpMethod.Put, "/{index}/_alias/{name}", "indices-put-alias"),
			Op(HttpMethod.Post, "/{index}/_alias/{name}", "indices-put-alias-1"),
			Op(HttpMethod.Post, "/_aliases", "indices-update-aliases")
		};

		ApiUrlBuilder.CanonicalOperationMoniker(operations, operations[1]).Should().Be("operation-indices-put-alias");
	}

	[Test]
	public void CanonicalOperationMoniker_BaseTakenByAnOperationOutsideTheGroup_KeepsThePrimaryId()
	{
		var operations = new[] { Op(HttpMethod.Get, "/_search", "search-1"), Op(HttpMethod.Post, "/_search", "search-2") };
		var taken = new HashSet<string>(StringComparer.Ordinal) { "operation-search", "operation-search-1", "operation-search-2" };

		ApiUrlBuilder.CanonicalOperationMoniker(operations, operations[1], taken).Should().Be("operation-search-2");
	}

	[Test]
	public void CanonicalOperationMoniker_BaseTakenByAnEarlierGroup_KeepsThePrimaryId()
	{
		// Two groups share a base and no standalone "search" exists: the first takes the base, the second keeps its id.
		var first = new[] { Op(HttpMethod.Get, "/_search", "search-1"), Op(HttpMethod.Post, "/_search", "search-2") };
		var second = new[] { Op(HttpMethod.Get, "/_search/scroll", "search-3"), Op(HttpMethod.Post, "/_search/scroll", "search-4") };
		var taken = new HashSet<string>(StringComparer.Ordinal)
		{
			"operation-search-1",
			"operation-search-2",
			"operation-search-3",
			"operation-search-4"
		};

		var firstPage = ApiUrlBuilder.CanonicalOperationMoniker(first, first[1], taken);
		_ = taken.Add(firstPage);
		var secondPage = ApiUrlBuilder.CanonicalOperationMoniker(second, second[1], taken);

		firstPage.Should().Be("operation-search");
		secondPage.Should().Be("operation-search-4");
	}

	[Test]
	public void CanonicalOperationMoniker_BaseOwnedByTheGroup_UsesTheBase()
	{
		var operations = new[] { Op(HttpMethod.Get, "/_search", "search"), Op(HttpMethod.Post, "/_search", "search-1") };
		var taken = new HashSet<string>(StringComparer.Ordinal) { "operation-search", "operation-search-1" };

		ApiUrlBuilder.CanonicalOperationMoniker(operations, operations[1], taken).Should().Be("operation-search");
	}

	[Test]
	public void RedirectPage_ForwardsToTheTargetAndKeepsTheQueryAndFragment()
	{
		var html = ApiRedirectPage.Html("/docs/api/doc/elasticsearch/operation/operation-search");

		html.Should().Contain("<link rel=\"canonical\" href=\"/docs/api/doc/elasticsearch/operation/operation-search\">");
		html.Should().Contain(
			"window.location.replace(\"/docs/api/doc/elasticsearch/operation/operation-search\" + window.location.search + window.location.hash)"
		);
		html.Should().Contain("noindex");
	}

	private static ApiOperation Contract(HttpMethod method, bool requiredQuery, string responseProperty) =>
		new(
			method,
			new OpenApiOperation
			{
				Parameters =
				[
					new OpenApiParameter
					{
						Name = "q",
						In = ParameterLocation.Query,
						Required = requiredQuery,
						Schema = new OpenApiSchema { Type = JsonSchemaType.String }
					}
				],
				Responses = new OpenApiResponses
				{
					["200"] = new OpenApiResponse
					{
						Description = "ok",
						Content = new Dictionary<string, IOpenApiMediaType>
						{
							["application/json"] = new OpenApiMediaType
							{
								Schema = new OpenApiSchema
								{
									Type = JsonSchemaType.Object,
									Properties = new Dictionary<string, IOpenApiSchema>
									{
										[responseProperty] = new OpenApiSchema { Type = JsonSchemaType.Integer }
									}
								}
							}
						}
					}
				}
			},
			"/_search",
			new OpenApiPathItem(),
			"search"
		);

	[Test]
	public void AreInterchangeable_SameContract_IsTrue() =>
		OperationEndpoint
			.AreInterchangeable([Contract(HttpMethod.Get, true, "took"), Contract(HttpMethod.Post, true, "took")])
			.Should()
			.BeTrue();

	[Test]
	public void AreInterchangeable_SameParameterNamesButDifferentRequiredness_IsFalse() =>
		OperationEndpoint
			.AreInterchangeable([Contract(HttpMethod.Get, true, "took"), Contract(HttpMethod.Post, false, "took")])
			.Should()
			.BeFalse();

	[Test]
	public void AreInterchangeable_SameStatusCodesButDifferentInlineResponseSchema_IsFalse() =>
		OperationEndpoint
			.AreInterchangeable([Contract(HttpMethod.Get, true, "took"), Contract(HttpMethod.Post, true, "hits")])
			.Should()
			.BeFalse();

	private static ApiOperation WithBody(HttpMethod method, bool bodyRequired) =>
		new(
			method,
			new OpenApiOperation
			{
				RequestBody = new OpenApiRequestBody
				{
					Required = bodyRequired,
					Content = new Dictionary<string, IOpenApiMediaType>
					{
						["application/json"] = new OpenApiMediaType { Schema = new OpenApiSchema { Type = JsonSchemaType.Object } }
					}
				},
				Responses = new OpenApiResponses { ["200"] = new OpenApiResponse { Description = "ok" } }
			},
			"/foo",
			new OpenApiPathItem(),
			"foo"
		);

	[Test]
	public void AreInterchangeable_SameBodyButDifferentRequiredness_IsFalse() =>
		OperationEndpoint.AreInterchangeable([WithBody(HttpMethod.Post, true), WithBody(HttpMethod.Put, false)]).Should().BeFalse();

	[Test]
	public void AreInterchangeable_SameBodyAndRequiredness_IsTrue() =>
		OperationEndpoint.AreInterchangeable([WithBody(HttpMethod.Post, true), WithBody(HttpMethod.Put, true)]).Should().BeTrue();

	/// <summary>An object nested <paramref name="depth"/> levels deep whose innermost property is <paramref name="leaf"/>.</summary>
	private static OpenApiSchema Nested(int depth, string leaf) =>
		depth == 0
			? new OpenApiSchema
			{
				Type = JsonSchemaType.Object,
				Properties = new Dictionary<string, IOpenApiSchema> { [leaf] = new OpenApiSchema { Type = JsonSchemaType.String } }
			}
			: new OpenApiSchema
			{
				Type = JsonSchemaType.Object,
				Properties = new Dictionary<string, IOpenApiSchema> { ["child"] = Nested(depth - 1, leaf) }
			};

	private static ApiOperation WithResponseSchema(HttpMethod method, OpenApiSchema schema) =>
		new(
			method,
			new OpenApiOperation
			{
				Responses = new OpenApiResponses
				{
					["200"] = new OpenApiResponse
					{
						Description = "ok",
						Content = new Dictionary<string, IOpenApiMediaType>
						{
							["application/json"] = new OpenApiMediaType { Schema = schema }
						}
					}
				}
			},
			"/foo",
			new OpenApiPathItem(),
			"foo"
		);

	[Test]
	public void AreInterchangeable_SchemasThatDifferOnlyDeepDown_IsFalse() =>
		OperationEndpoint
			.AreInterchangeable([
				WithResponseSchema(HttpMethod.Get, Nested(8, "took")),
				WithResponseSchema(HttpMethod.Post, Nested(8, "hits"))
			])
			.Should()
			.BeFalse();

	[Test]
	public void AreInterchangeable_DeepSchemasThatMatch_IsTrue() =>
		OperationEndpoint
			.AreInterchangeable([
				WithResponseSchema(HttpMethod.Get, Nested(8, "took")),
				WithResponseSchema(HttpMethod.Post, Nested(8, "took"))
			])
			.Should()
			.BeTrue();

	private static ApiOperation WithLifecycle(HttpMethod method, bool deprecated, bool beta) =>
		new(
			method,
			new OpenApiOperation
			{
				Deprecated = deprecated,
				Extensions = beta
					? new Dictionary<string, IOpenApiExtension> { ["x-beta"] = new JsonNodeExtension(JsonValue.Create(true)) }
					: null,
				Responses = new OpenApiResponses { ["200"] = new OpenApiResponse { Description = "ok" } }
			},
			"/foo",
			new OpenApiPathItem(),
			"foo"
		);

	[Test]
	public void AreInterchangeable_OneMethodDeprecated_IsFalse() =>
		OperationEndpoint
			.AreInterchangeable([
				WithLifecycle(HttpMethod.Get, deprecated: true, beta: false),
				WithLifecycle(HttpMethod.Post, deprecated: false, beta: false)
			])
			.Should()
			.BeFalse();

	[Test]
	public void AreInterchangeable_OneMethodBeta_IsFalse() =>
		OperationEndpoint
			.AreInterchangeable([
				WithLifecycle(HttpMethod.Get, deprecated: false, beta: true),
				WithLifecycle(HttpMethod.Post, deprecated: false, beta: false)
			])
			.Should()
			.BeFalse();

	private static ApiOperation WithServers(HttpMethod method, params string[] urls) =>
		new(
			method,
			new OpenApiOperation
			{
				Servers = urls.Length == 0 ? null : [.. urls.Select(static u => new OpenApiServer { Url = u })],
				Responses = new OpenApiResponses { ["200"] = new OpenApiResponse { Description = "ok" } }
			},
			"/foo",
			new OpenApiPathItem(),
			"foo"
		);

	private static ApiOperation WithServerVariable(HttpMethod method, string defaultRegion, params string[] regions) =>
		new(
			method,
			new OpenApiOperation
			{
				Servers =
				[
					new OpenApiServer
					{
						Url = "https://{region}.api.example.com",
						Variables = new Dictionary<string, OpenApiServerVariable>
						{
							["region"] = new OpenApiServerVariable
							{
								Default = defaultRegion,
								Enum = regions.Length == 0 ? null : [.. regions]
							}
						}
					}
				],
				Responses = new OpenApiResponses { ["200"] = new OpenApiResponse { Description = "ok" } }
			},
			"/foo",
			new OpenApiPathItem(),
			"foo"
		);

	[Test]
	public void AreInterchangeable_SameServerUrlButDifferentVariableDefault_IsFalse() =>
		OperationEndpoint
			.AreInterchangeable([WithServerVariable(HttpMethod.Get, "us-east-1"), WithServerVariable(HttpMethod.Post, "eu-west-1")])
			.Should()
			.BeFalse();

	[Test]
	public void AreInterchangeable_SameServerUrlButDifferentVariableEnum_IsFalse() =>
		OperationEndpoint
			.AreInterchangeable([
				WithServerVariable(HttpMethod.Get, "us-east-1", "us-east-1", "eu-west-1"),
				WithServerVariable(HttpMethod.Post, "us-east-1", "us-east-1")
			])
			.Should()
			.BeFalse();

	[Test]
	public void AreInterchangeable_SameServerVariablesInAnyOrder_IsTrue() =>
		OperationEndpoint
			.AreInterchangeable([
				WithServerVariable(HttpMethod.Get, "us-east-1", "eu-west-1", "us-east-1"),
				WithServerVariable(HttpMethod.Post, "us-east-1", "us-east-1", "eu-west-1")
			])
			.Should()
			.BeTrue();

	[Test]
	public void AreInterchangeable_DifferentOperationServers_IsFalse() =>
		OperationEndpoint
			.AreInterchangeable([WithServers(HttpMethod.Get, "https://a.example"), WithServers(HttpMethod.Post, "https://b.example")])
			.Should()
			.BeFalse();

	[Test]
	public void AreInterchangeable_OwnServersVersusInherited_IsFalse() =>
		OperationEndpoint
			.AreInterchangeable([WithServers(HttpMethod.Get, "https://a.example"), WithServers(HttpMethod.Post)])
			.Should()
			.BeFalse();

	private static ApiOperation WithResponseHeader(HttpMethod method, string? header, bool required = false) =>
		new(
			method,
			new OpenApiOperation
			{
				Responses = new OpenApiResponses
				{
					["200"] = new OpenApiResponse
					{
						Description = "ok",
						Headers = header is null
							? null
							: new Dictionary<string, IOpenApiHeader>
							{
								[header] = new OpenApiHeader
								{
									Required = required,
									Schema = new OpenApiSchema { Type = JsonSchemaType.String }
								}
							}
					}
				}
			},
			"/foo",
			new OpenApiPathItem(),
			"foo"
		);

	[Test]
	public void AreInterchangeable_OneResponseDeclaresAHeader_IsFalse() =>
		OperationEndpoint
			.AreInterchangeable([WithResponseHeader(HttpMethod.Get, "X-Elastic-Product"), WithResponseHeader(HttpMethod.Post, null)])
			.Should()
			.BeFalse();

	[Test]
	public void AreInterchangeable_SameHeaderButDifferentRequiredness_IsFalse() =>
		OperationEndpoint
			.AreInterchangeable([
				WithResponseHeader(HttpMethod.Get, "X-Elastic-Product", required: true),
				WithResponseHeader(HttpMethod.Post, "X-Elastic-Product")
			])
			.Should()
			.BeFalse();

	[Test]
	public void AreInterchangeable_SameResponseHeaders_IsTrue() =>
		OperationEndpoint
			.AreInterchangeable([
				WithResponseHeader(HttpMethod.Get, "X-Elastic-Product"),
				WithResponseHeader(HttpMethod.Post, "X-Elastic-Product")
			])
			.Should()
			.BeTrue();

	private static ApiOperation WithQuerySerialization(HttpMethod method, ParameterStyle? style, bool? explode, bool allowReserved = false)
	{
		var ids = new OpenApiParameter
		{
			Name = "ids",
			In = ParameterLocation.Query,
			Style = style,
			AllowReserved = allowReserved,
			Schema = new OpenApiSchema { Type = JsonSchemaType.Array, Items = new OpenApiSchema { Type = JsonSchemaType.String } }
		};
		// Left unset, Explode follows the style (true for form), which the defaults test relies on.
		if (explode is { } value)
			ids.Explode = value;
		return new ApiOperation(
			method,
			new OpenApiOperation
			{
				Parameters = [ids],
				Responses = new OpenApiResponses { ["200"] = new OpenApiResponse { Description = "ok" } }
			},
			"/foo",
			new OpenApiPathItem(),
			"foo"
		);
	}

	[Test]
	public void AreInterchangeable_SameSchemaButDifferentExplode_IsFalse() =>
		OperationEndpoint
			.AreInterchangeable([
				WithQuerySerialization(HttpMethod.Get, ParameterStyle.Form, explode: true),
				WithQuerySerialization(HttpMethod.Post, ParameterStyle.Form, explode: false)
			])
			.Should()
			.BeFalse();

	[Test]
	public void AreInterchangeable_SameSchemaButDifferentStyle_IsFalse() =>
		OperationEndpoint
			.AreInterchangeable([
				WithQuerySerialization(HttpMethod.Get, ParameterStyle.Form, explode: false),
				WithQuerySerialization(HttpMethod.Post, ParameterStyle.SpaceDelimited, explode: false)
			])
			.Should()
			.BeFalse();

	[Test]
	public void AreInterchangeable_SameSchemaButDifferentAllowReserved_IsFalse() =>
		OperationEndpoint
			.AreInterchangeable([
				WithQuerySerialization(HttpMethod.Get, null, null, allowReserved: true),
				WithQuerySerialization(HttpMethod.Post, null, null)
			])
			.Should()
			.BeFalse();

	[Test]
	public void AreInterchangeable_UnsetSerializationEqualsTheQueryDefaults_IsTrue() =>
		OperationEndpoint
			.AreInterchangeable([
				WithQuerySerialization(HttpMethod.Get, null, null),
				WithQuerySerialization(HttpMethod.Post, ParameterStyle.Form, explode: true)
			])
			.Should()
			.BeTrue();

	private static ApiOperation WithSecurity(HttpMethod method, IList<OpenApiSecurityRequirement>? security) =>
		new(
			method,
			new OpenApiOperation
			{
				Security = security,
				Responses = new OpenApiResponses { ["200"] = new OpenApiResponse { Description = "ok" } }
			},
			"/foo",
			new OpenApiPathItem(),
			"foo"
		);

	private static OpenApiSecurityRequirement Requires(string scheme) => new() { [new OpenApiSecuritySchemeReference(scheme)] = [] };

	[Test]
	public void Differences_NamesEveryFacetThatDiffers()
	{
		var a = WithSecurity(HttpMethod.Get, [Requires("apiKey")]) with
		{
			Operation = new OpenApiOperation { Security = [Requires("apiKey")], Deprecated = true }
		};
		var b = WithSecurity(HttpMethod.Post, [Requires("basic")]);

		OperationEndpoint.Differences([a, b]).Should().Equal("responses", "security", "lifecycle");
	}

	[Test]
	public void Differences_SameCall_IsEmpty() =>
		OperationEndpoint.Differences([Contract(HttpMethod.Get, true, "took"), Contract(HttpMethod.Post, true, "took")]).Should().BeEmpty();

	[Test]
	public void AreInterchangeable_SameContractButDifferentSecurityScheme_IsFalse() =>
		OperationEndpoint
			.AreInterchangeable([WithSecurity(HttpMethod.Get, [Requires("apiKey")]), WithSecurity(HttpMethod.Post, [Requires("basic")])])
			.Should()
			.BeFalse();

	[Test]
	public void AreInterchangeable_InheritedSecurityVersusNone_IsFalse() =>
		OperationEndpoint.AreInterchangeable([WithSecurity(HttpMethod.Get, null), WithSecurity(HttpMethod.Post, [])]).Should().BeFalse();

	[Test]
	public void AreInterchangeable_SameSecurityInAnyOrder_IsTrue() =>
		OperationEndpoint
			.AreInterchangeable([
				WithSecurity(HttpMethod.Get, [Requires("apiKey"), Requires("basic")]),
				WithSecurity(HttpMethod.Post, [Requires("basic"), Requires("apiKey")])
			])
			.Should()
			.BeTrue();
}
