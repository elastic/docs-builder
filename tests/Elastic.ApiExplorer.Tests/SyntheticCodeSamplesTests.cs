// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Nodes;
using AwesomeAssertions;
using Elastic.ApiExplorer.Model;
using Elastic.ApiExplorer.Operations;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Tests;

public class SyntheticCodeSamplesTests
{
	[Test]
	public void Create_BuildsConsoleAndCurlFromMethodAndPath()
	{
		var operation = new OpenApiOperation();
		var samples = SyntheticCodeSamples.Create(new SyntheticSampleInput(HttpMethod.Delete, "/api/dashboards/{id}", operation));

		samples.Should().HaveCount(2);
		samples[0].Language.Should().Be("Console");
		samples[0].Source.Should().Be("DELETE /api/dashboards/ID");
		samples[1].Language.Should().Be("curl");
		samples[1].Source.Should().Contain("DELETE");
		samples[1].Source.Should().Contain("/api/dashboards/${ID}");
	}

	[Test]
	public void Create_MarksEverySampleGenerated() =>
		SyntheticCodeSamples
			.Create(new SyntheticSampleInput(HttpMethod.Get, "/api/x", new OpenApiOperation()))
			.Should()
			.AllSatisfy(static s => s.Generated.Should().BeTrue());

	[Test]
	public void Create_IncludesRequiredQueryAndHeaders()
	{
		var operation = new OpenApiOperation
		{
			Parameters =
			[
				new OpenApiParameter { Name = "ids", In = ParameterLocation.Query, Required = true },
				new OpenApiParameter { Name = "optional", In = ParameterLocation.Query, Required = false },
				new OpenApiParameter
				{
					Name = "kbn-xsrf",
					In = ParameterLocation.Header,
					Required = true,
					Schema = new OpenApiSchema { Examples = [JsonValue.Create("true")] }
				}
			]
		};

		var samples = SyntheticCodeSamples.Create(new SyntheticSampleInput(HttpMethod.Delete, "/api/cases", operation)
		{
			Servers = [new OpenApiServer { Url = "https://{kibana_url}" }]
		});

		samples[0].Source.Should().Be("DELETE /api/cases?ids=IDS");
		samples[1].Source.Should().Contain("${KIBANA_URL}/api/cases?ids=${IDS}");
		samples[1].Source.Should().Contain("kbn-xsrf: true");
		samples[1].Source.Should().NotContain("optional");
	}

	[Test]
	public void Create_WithPathParameterExample_FillsItIn()
	{
		var operation = new OpenApiOperation
		{
			Parameters =
			[
				new OpenApiParameter
				{
					Name = "organization_id",
					In = ParameterLocation.Path,
					Required = true,
					Schema = new OpenApiSchema { Examples = [JsonValue.Create("198583657190")] }
				}
			]
		};

		var samples = SyntheticCodeSamples.Create(
			new SyntheticSampleInput(HttpMethod.Get, "/organizations/{organization_id}/subscription", operation)
		);

		samples[0].Source.Should().Be("GET /organizations/198583657190/subscription");
		samples[1].Source.Should().Contain("/organizations/198583657190/subscription");
	}

	[Test]
	public void Create_WithAHeaderThatOnlyHasADefault_SendsTheDefault()
	{
		var operation = new OpenApiOperation
		{
			Parameters =
			[
				new OpenApiParameter
				{
					Name = "elastic-api-version",
					In = ParameterLocation.Header,
					Required = true,
					Schema = new OpenApiSchema { Default = JsonValue.Create("2023-10-31"), Enum = [JsonValue.Create("2023-10-31")] }
				}
			]
		};

		var samples = SyntheticCodeSamples.Create(new SyntheticSampleInput(HttpMethod.Post, "/api/apm/agent_keys", operation));

		samples[1].Source.Should().Contain("-H \"elastic-api-version: 2023-10-31\"");
	}

	[Test]
	public void Create_WithAParameterWhoseExampleIsNull_KeepsThePlaceholder()
	{
		// The reader stores a literal `example: null` as a sentinel string, which must not reach the URL.
		var operation = new OpenApiOperation
		{
			Parameters =
			[
				new OpenApiParameter { Name = "id", In = ParameterLocation.Path, Required = true, Example = JsonNullSentinel.JsonNull }
			]
		};

		var samples = SyntheticCodeSamples.Create(new SyntheticSampleInput(HttpMethod.Get, "/things/{id}", operation));

		samples[0].Source.Should().Be("GET /things/ID");
	}

	[Test]
	public void Create_WithoutAServer_UsesTheHostVariable()
	{
		var samples = SyntheticCodeSamples.Create(new SyntheticSampleInput(HttpMethod.Get, "/_search", new OpenApiOperation())
		{
			HostVariable = "${ELASTICSEARCH_URL}"
		});

		samples[1].Source.Should().StartWith("curl -X GET \"${ELASTICSEARCH_URL}/_search\"");
	}

	[Test]
	public void Create_WithAQueryAlreadyInTheRoute_AppendsWithAnAmpersand()
	{
		var operation = new OpenApiOperation
		{
			Parameters = [new OpenApiParameter { Name = "size", In = ParameterLocation.Query, Required = true }]
		};

		var samples = SyntheticCodeSamples.Create(new SyntheticSampleInput(HttpMethod.Get, "/_search?pretty", operation));

		samples[0].Source.Should().Be("GET /_search?pretty&size=SIZE");
	}

	[Test]
	[Arguments("elasticsearch", "${ELASTICSEARCH_URL}")]
	[Arguments("serverless-elasticsearch", "${ELASTICSEARCH_URL}")]
	[Arguments("kibana", "${KIBANA_URL}")]
	[Arguments("logstash", "${LOGSTASH_URL}")]
	[Arguments("cloud-enterprise", "${API_URL}")]
	[Arguments(null, "${API_URL}")]
	public void HostVariableFor_NamesTheProductsHost(string? productId, string expected) =>
		GeneratedCodeSamples.HostVariableFor(productId).Should().Be(expected);

	[Test]
	public void Create_WithTheExampleOfAnOpenApi30Schema_FillsItIn()
	{
		// The reader keeps the `example` of an OpenAPI 3.0 schema in Example, not Examples.
#pragma warning disable CS0618
		var schema = new OpenApiSchema { Example = JsonValue.Create("198583657190") };
#pragma warning restore CS0618
		var operation = new OpenApiOperation
		{
			Parameters = [new OpenApiParameter { Name = "organization_id", In = ParameterLocation.Path, Required = true, Schema = schema }]
		};

		var samples = SyntheticCodeSamples.Create(new SyntheticSampleInput(HttpMethod.Get, "/organizations/{organization_id}", operation));

		samples[0].Source.Should().Be("GET /organizations/198583657190");
	}

	[Test]
	public void Create_WithParameterExampleOnTheParameter_PrefersItToTheSchemasAndKeepsOtherPlaceholders()
	{
		var operation = new OpenApiOperation
		{
			Parameters =
			[
				new OpenApiParameter
				{
					Name = "id",
					In = ParameterLocation.Path,
					Required = true,
					Example = JsonValue.Create("own"),
					Schema = new OpenApiSchema { Examples = [JsonValue.Create("schema")] }
				},
				new OpenApiParameter { Name = "name", In = ParameterLocation.Path, Required = true }
			]
		};

		var samples = SyntheticCodeSamples.Create(new SyntheticSampleInput(HttpMethod.Get, "/a/{id}/b/{name}", operation));

		samples[0].Source.Should().Be("GET /a/own/b/NAME");
	}

	[Test]
	public void Create_WithRequiredQueryExample_FillsItIn()
	{
		var operation = new OpenApiOperation
		{
			Parameters =
			[
				new OpenApiParameter
				{
					Name = "q",
					In = ParameterLocation.Query,
					Required = true,
					Schema = new OpenApiSchema { Examples = [JsonValue.Create("a b")] }
				}
			]
		};

		var samples = SyntheticCodeSamples.Create(new SyntheticSampleInput(HttpMethod.Get, "/search", operation));

		samples[0].Source.Should().Be("GET /search?q=a%20b");
	}

	private static OpenApiOperation WithQueryExample(JsonNode example, ParameterStyle? style = null, bool? explode = null) =>
		new()
		{
			Parameters =
			[
				new OpenApiParameter
				{
					Name = "ids",
					In = ParameterLocation.Query,
					Required = true,
					Style = style,
					Explode = explode ?? false,
					Example = example
				}
			]
		};

	[Test]
	public void Create_WithAnArrayExampleOnAFormParameter_RepeatsTheName()
	{
		var operation = WithQueryExample(new JsonArray("a", "b"), ParameterStyle.Form, explode: true);

		var samples = SyntheticCodeSamples.Create(new SyntheticSampleInput(HttpMethod.Get, "/items", operation));

		samples[0].Source.Should().Be("GET /items?ids=a&ids=b");
	}

	[Test]
	public void Create_WithAnArrayExampleAndNoStyleOrExplode_FollowsTheQueryDefault()
	{
		var operation = new OpenApiOperation
		{
			Parameters =
			[
				new OpenApiParameter { Name = "ids", In = ParameterLocation.Query, Required = true, Example = new JsonArray("a", "b") }
			]
		};

		var samples = SyntheticCodeSamples.Create(new SyntheticSampleInput(HttpMethod.Get, "/items", operation));

		samples[0].Source.Should().Be("GET /items?ids=a&ids=b");
	}

	[Test]
	public void Create_WithAnArrayExampleAndNoExplode_SendsOneCommaSeparatedValue()
	{
		var operation = WithQueryExample(new JsonArray("a", "b"), ParameterStyle.Form, explode: false);

		var samples = SyntheticCodeSamples.Create(new SyntheticSampleInput(HttpMethod.Get, "/items", operation));

		samples[0].Source.Should().Be("GET /items?ids=a,b");
	}

	[Test]
	[Arguments(ParameterStyle.Form, true, "GET /items?a=1&b=x")]
	[Arguments(ParameterStyle.Form, false, "GET /items?ids=a,1,b,x")]
	[Arguments(ParameterStyle.DeepObject, true, "GET /items?ids%5Ba%5D=1&ids%5Bb%5D=x")]
	public void Create_WithAnObjectExample_FollowsTheStyleAndExplode(ParameterStyle style, bool explode, string expected)
	{
		var operation = WithQueryExample(new JsonObject { ["a"] = 1, ["b"] = "x" }, style, explode);

		var samples = SyntheticCodeSamples.Create(new SyntheticSampleInput(HttpMethod.Get, "/items", operation));

		samples[0].Source.Should().Be(expected);
	}

	[Test]
	public void Create_WithAnObjectExampleOnAPathParameter_UsesTheSimpleStyle()
	{
		var operation = new OpenApiOperation
		{
			Parameters =
			[
				new OpenApiParameter
				{
					Name = "id",
					In = ParameterLocation.Path,
					Required = true,
					Explode = true,
					Example = new JsonObject { ["a"] = 1, ["b"] = 2 }
				}
			]
		};

		var samples = SyntheticCodeSamples.Create(new SyntheticSampleInput(HttpMethod.Get, "/items/{id}", operation));

		samples[0].Source.Should().Be("GET /items/a=1,b=2");
	}

	[Test]
	public void Create_WithAnObjectExampleHoldingAnObject_KeepsThePlaceholder()
	{
		var operation = WithQueryExample(new JsonObject { ["a"] = new JsonObject { ["b"] = 1 } });

		var samples = SyntheticCodeSamples.Create(new SyntheticSampleInput(HttpMethod.Get, "/items", operation));

		samples[0].Source.Should().Be("GET /items?ids=IDS");
	}

	private static OpenApiOperation JsonPost() =>
		new()
		{
			Parameters = [new OpenApiParameter { Name = "kbn-xsrf", In = ParameterLocation.Header, Required = true }],
			RequestBody = new OpenApiRequestBody
			{
				Content = new Dictionary<string, IOpenApiMediaType> { ["application/json"] = new OpenApiMediaType() }
			}
		};

	[Test]
	public void Create_WithARequestBody_PutsItInConsoleAndCurl()
	{
		const string body = "{\n  \"schema\": {\n    \"foo\": \"bar\"\n  }\n}";
		var samples = SyntheticCodeSamples.Create(new SyntheticSampleInput(HttpMethod.Post, "/api/apm/fleet/apm_server_schema", JsonPost())
		{
			Servers = [new OpenApiServer { Url = "https://{kibana_url}" }],
			Body = body
		});

		samples[0].Source.Should().Be("POST /api/apm/fleet/apm_server_schema\n" + body);
		samples[1]
			.Source
			.Should()
			.Be(
				"curl -X POST \"${KIBANA_URL}/api/apm/fleet/apm_server_schema\" \\\n" + "  -H \"kbn-xsrf: true\" \\\n" +
					"  -H \"Content-Type: application/json\" \\\n" + "  -d '" + body.Replace("\n", "\n  ") + "'"
			);
	}

	[Test]
	public void Create_WithShellCharactersInAHeaderExample_EscapesThemForTheShell()
	{
		var operation = new OpenApiOperation
		{
			Parameters =
			[
				new OpenApiParameter
				{
					Name = "X-Note",
					In = ParameterLocation.Header,
					Required = true,
					Example = JsonValue.Create("say \"hi\" to $HOME, `whoami` and C:\\dir\nnext")
				}
			]
		};

		var samples = SyntheticCodeSamples.Create(new SyntheticSampleInput(HttpMethod.Get, "/api/x", operation));

		samples[1].Source.Should().Contain("-H \"X-Note: say \\\"hi\\\" to \\$HOME, \\`whoami\\` and C:\\\\dir next\"");
	}

	[Test]
	public void Create_WithASingleQuoteInTheBody_EscapesItForTheShell()
	{
		var samples = SyntheticCodeSamples.Create(new SyntheticSampleInput(HttpMethod.Post, "/api/foo", JsonPost())
		{
			Body = /*lang=json,strict*/  """{"name":"it's"}"""
		});

		samples[1].Source.Should().Contain("it'\\''s");
	}

	[Test]
	public void Create_WithoutABody_LeavesTheSamplesBodyless()
	{
		var samples = SyntheticCodeSamples.Create(new SyntheticSampleInput(HttpMethod.Post, "/api/foo", JsonPost()) { Body = "  " });

		samples[0].Source.Should().Be("POST /api/foo");
		samples[1].Source.Should().NotContain("-d ");
	}

	[Test]
	public void Create_WithoutABody_SendsNoContentType()
	{
		var samples = SyntheticCodeSamples.Create(new SyntheticSampleInput(HttpMethod.Post, "/api/foo", JsonPost()));

		samples[1].Source.Should().NotContain("Content-Type").And.NotContain("-d ");
	}

	[Test]
	public void Create_WithApiKeyHeaderScheme_SendsTheKeyInThatHeader()
	{
		var scheme = new OpenApiSecurityScheme { Type = SecuritySchemeType.ApiKey, Name = "Authorization", In = ParameterLocation.Header };

		var samples = SyntheticCodeSamples.Create(new SyntheticSampleInput(HttpMethod.Get, "/api/x", new OpenApiOperation())
		{
			Security = [scheme]
		});

		samples[1].Source.Should().Contain("-H \"Authorization: ApiKey ${API_KEY}\"");
		samples[0].Source.Should().Be("GET /api/x");
	}

	[Test]
	public void Create_WithAnApiKeySchemeInAnotherHeader_SendsTheKeyAlone()
	{
		var scheme = new OpenApiSecurityScheme { Type = SecuritySchemeType.ApiKey, Name = "X-Api-Key", In = ParameterLocation.Header };

		var samples = SyntheticCodeSamples.Create(new SyntheticSampleInput(HttpMethod.Get, "/api/x", new OpenApiOperation())
		{
			Security = [scheme]
		});

		samples[1].Source.Should().Contain("-H \"X-Api-Key: ${API_KEY}\"").And.NotContain("ApiKey ${API_KEY}");
	}

	[Test]
	[Arguments("/api/fleet/agent_policies/{agentPolicyId}/status", "AGENT_POLICY_ID", "${AGENT_POLICY_ID}")]
	[Arguments("/organizations/{organization_id}/subscription", "ORGANIZATION_ID", "${ORGANIZATION_ID}")]
	[Arguments("/deployments/{deployment-id}/{ref_id}", "DEPLOYMENT_ID/REF_ID", "${DEPLOYMENT_ID}/${REF_ID}")]
	public void Create_WithoutAnExample_NamesThePlaceholderAfterTheParameter(string route, string console, string shell)
	{
		var samples = SyntheticCodeSamples.Create(new SyntheticSampleInput(HttpMethod.Get, route, new OpenApiOperation()));

		// Console shows the bare name; the curl makes it a shell variable like the host and credential beside it.
		samples[0].Source.Should().Contain(console).And.NotContain("{");
		samples[1].Source.Should().Contain(shell).And.NotContain("--globoff");
	}

	[Test]
	public void Create_WithAServerUrlThatIsOneVariable_UsesItAsTheHostVariable()
	{
		var samples = SyntheticCodeSamples.Create(new SyntheticSampleInput(HttpMethod.Get, "/api/status", new OpenApiOperation())
		{
			Servers = [new OpenApiServer { Url = "https://{kibana_url}" }]
		});

		samples[1].Source.Should().StartWith("curl -X GET \"${KIBANA_URL}/api/status\"");
	}

	[Test]
	public void Create_WithAServerVariableInsideTheUrl_NamesItInPlace()
	{
		var samples = SyntheticCodeSamples.Create(new SyntheticSampleInput(HttpMethod.Get, "/x", new OpenApiOperation())
		{
			Servers = [new OpenApiServer { Url = "https://{region}.api.example.test/v1" }]
		});

		samples[1].Source.Should().StartWith("curl -X GET \"https://${REGION}.api.example.test/v1/x\"");
	}

	[Test]
	public void Create_WithEveryPlaceholderFilled_ShowsTheValues()
	{
		var operation = new OpenApiOperation
		{
			Parameters =
			[
				new OpenApiParameter { Name = "id", In = ParameterLocation.Path, Required = true, Example = JsonValue.Create("d1") }
			]
		};

		var samples = SyntheticCodeSamples.Create(new SyntheticSampleInput(HttpMethod.Delete, "/api/dashboards/{id}", operation));

		samples[1].Source.Should().StartWith("curl -X DELETE \"${API_URL}/api/dashboards/d1\"");
	}

	[Test]
	public void Create_WithApiKeyQueryScheme_PutsTheKeyInTheCurlUrlOnly()
	{
		var scheme = new OpenApiSecurityScheme { Type = SecuritySchemeType.ApiKey, Name = "key", In = ParameterLocation.Query };

		var samples = SyntheticCodeSamples.Create(new SyntheticSampleInput(HttpMethod.Get, "/api/x", new OpenApiOperation())
		{
			Security = [scheme]
		});

		samples[1].Source.Should().Contain("\"${API_URL}/api/x?key=${API_KEY}\"");
		samples[0].Source.Should().Be("GET /api/x");
	}

	[Test]
	public void Create_WithBasicScheme_SendsUserAndPassword()
	{
		var scheme = new OpenApiSecurityScheme { Type = SecuritySchemeType.Http, Scheme = "basic" };

		var samples = SyntheticCodeSamples.Create(new SyntheticSampleInput(HttpMethod.Get, "/api/x", new OpenApiOperation())
		{
			Security = [scheme]
		});

		samples[1].Source.Should().Contain("-u \"${USERNAME}:${PASSWORD}\"");
	}

	[Test]
	[Arguments(SecuritySchemeType.Http, "bearer")]
	[Arguments(SecuritySchemeType.OAuth2, null)]
	[Arguments(SecuritySchemeType.OpenIdConnect, null)]
	public void Create_WithTokenSchemes_SendsABearerToken(SecuritySchemeType type, string? httpScheme)
	{
		var scheme = new OpenApiSecurityScheme { Type = type, Scheme = httpScheme };

		var samples = SyntheticCodeSamples.Create(new SyntheticSampleInput(HttpMethod.Get, "/api/x", new OpenApiOperation())
		{
			Security = [scheme]
		});

		samples[1].Source.Should().Contain("-H \"Authorization: Bearer ${TOKEN}\"");
	}

	[Test]
	public void Create_WhenTheOperationRequiresTheSchemesHeader_KeepsItsOwnExample()
	{
		var scheme = new OpenApiSecurityScheme { Type = SecuritySchemeType.ApiKey, Name = "Authorization", In = ParameterLocation.Header };
		var operation = new OpenApiOperation
		{
			Parameters =
			[
				new OpenApiParameter
				{
					Name = "Authorization",
					In = ParameterLocation.Header,
					Required = true,
					Schema = new OpenApiSchema { Examples = [JsonValue.Create("ApiKey abc")] }
				}
			]
		};

		var samples = SyntheticCodeSamples.Create(new SyntheticSampleInput(HttpMethod.Get, "/api/x", operation) { Security = [scheme] });

		samples[1].Source.Should().Contain("Authorization: ApiKey abc").And.NotContain("${API_KEY}");
	}

	[Test]
	public void Create_WhenTheRequiredHeaderHasNoExample_TheCredentialReplacesIt()
	{
		var scheme = new OpenApiSecurityScheme { Type = SecuritySchemeType.ApiKey, Name = "X-Api-Key", In = ParameterLocation.Header };
		var operation = new OpenApiOperation
		{
			Parameters = [new OpenApiParameter { Name = "X-Api-Key", In = ParameterLocation.Header, Required = true }]
		};

		var samples = SyntheticCodeSamples.Create(new SyntheticSampleInput(HttpMethod.Get, "/api/x", operation) { Security = [scheme] });

		samples[1].Source.Should().Contain("-H \"X-Api-Key: ${API_KEY}\"").And.NotContain("X-Api-Key: string");
	}

	[Test]
	public void Create_WithoutAConsolePrefix_BuildsOnlyCurl()
	{
		var samples = SyntheticCodeSamples.Create(new SyntheticSampleInput(HttpMethod.Get, "/api/x", new OpenApiOperation())
		{
			ConsolePrefix = null
		});

		samples.Should().ContainSingle().Which.IsCurl.Should().BeTrue();
	}

	[Test]
	public void Create_WithKibanaPrefix_AddressesKibanaInConsole()
	{
		var samples = SyntheticCodeSamples.Create(new SyntheticSampleInput(HttpMethod.Get, "/api/status", new OpenApiOperation())
		{
			ConsolePrefix = "kbn:"
		});

		samples[0].Source.Should().Be("GET kbn:/api/status");
	}

	[Test]
	public void BuildExampleScenarios_SyntheticSamplesWithTheExampleBody_CarryTheRequest()
	{
		const string body = /*lang=json,strict*/  """{"schema":{"foo":"bar"}}""";
		var samples = SyntheticCodeSamples.Create(new SyntheticSampleInput(HttpMethod.Post, "/api/foo", JsonPost()) { Body = body });
		var request = new ExampleDisplay("Example", null, body, null);

		var scenarios = OperationPageModel.BuildExampleScenarios([request], [], samples);

		scenarios.Should().ContainSingle();
		scenarios[0].CodeSamples.Should().BeEquivalentTo(samples);
		scenarios[0].CodeSamplesIncludeRequest.Should().BeTrue();
		scenarios[0].ShowRequest.Should().BeFalse();
	}

	[Test]
	[Arguments(null, "")]
	[Arguments("elasticsearch", "")]
	[Arguments("serverless-elasticsearch", "")]
	[Arguments("kibana", "kbn:")]
	[Arguments("ess", null)]
	[Arguments("cloud-hosted", null)]
	[Arguments("logstash", null)]
	public void ConsolePrefixFor_OnlyElasticsearchAndKibanaCanRunConsole(string? productId, string? expected) =>
		GeneratedCodeSamples.ConsolePrefixFor(productId).Should().Be(expected);
}
