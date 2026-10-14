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
		samples[0].Source.Should().Be("DELETE /api/dashboards/{id}");
		samples[1].Language.Should().Be("curl");
		samples[1].Source.Should().Contain("DELETE");
		samples[1].Source.Should().Contain("/api/dashboards/{id}");
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

		samples[0].Source.Should().Be("DELETE /api/cases?ids={ids}");
		samples[1].Source.Should().Contain("https://{kibana_url}/api/cases?ids={ids}");
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

		samples[0].Source.Should().Be("GET /a/own/b/{name}");
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

		samples[0].Source.Should().Be("GET /items?ids={ids}");
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
				"curl -X POST \"https://{kibana_url}/api/apm/fleet/apm_server_schema\" \\\n" + "  -H \"kbn-xsrf: true\" \\\n" +
					"  -H \"Content-Type: application/json\" \\\n" + "  -d '" + body + "'"
			);
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
	public void Create_WithADeclaredRequestTypeButNoBody_StillStatesTheContentType()
	{
		var samples = SyntheticCodeSamples.Create(new SyntheticSampleInput(HttpMethod.Post, "/api/foo", JsonPost()));

		samples[1].Source.Should().Contain("-H \"Content-Type: application/json\"").And.NotContain("-d ");
	}

	[Test]
	public void Create_WithApiKeyHeaderScheme_SendsTheKeyInThatHeader()
	{
		var scheme = new OpenApiSecurityScheme { Type = SecuritySchemeType.ApiKey, Name = "Authorization", In = ParameterLocation.Header };

		var samples = SyntheticCodeSamples.Create(new SyntheticSampleInput(HttpMethod.Get, "/api/x", new OpenApiOperation())
		{
			Security = [scheme]
		});

		samples[1].Source.Should().Contain("-H \"Authorization: $API_KEY\"");
		samples[0].Source.Should().Be("GET /api/x");
	}

	[Test]
	public void Create_WithApiKeyQueryScheme_PutsTheKeyInTheCurlUrlOnly()
	{
		var scheme = new OpenApiSecurityScheme { Type = SecuritySchemeType.ApiKey, Name = "key", In = ParameterLocation.Query };

		var samples = SyntheticCodeSamples.Create(new SyntheticSampleInput(HttpMethod.Get, "/api/x", new OpenApiOperation())
		{
			Security = [scheme]
		});

		samples[1].Source.Should().Contain("\"/api/x?key=$API_KEY\"");
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

		samples[1].Source.Should().Contain("-u \"$USERNAME:$PASSWORD\"");
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

		samples[1].Source.Should().Contain("-H \"Authorization: Bearer $TOKEN\"");
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

		samples[1].Source.Should().Contain("Authorization: ApiKey abc").And.NotContain("$API_KEY");
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

		samples[1].Source.Should().Contain("-H \"X-Api-Key: $API_KEY\"").And.NotContain("X-Api-Key: string");
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
