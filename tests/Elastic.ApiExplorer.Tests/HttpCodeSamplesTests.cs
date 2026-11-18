// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Nodes;
using AwesomeAssertions;
using Elastic.ApiExplorer.Model;
using Elastic.ApiExplorer.Operations;
using Microsoft.OpenApi;
using Nullean.Snapshots;

namespace Elastic.ApiExplorer.Tests;

public class HttpCodeSamplesTests
{
	[Test]
	public void CreateHttp_AliasesRequest_ContainsOnlyTheRequest()
	{
		var sample = SyntheticCodeSamples.CreateHttp(
			new SyntheticSampleInput(HttpMethod.Get, "/_cat/aliases/{name}", new OpenApiOperation())
			{
				HostVariable = "${ELASTICSEARCH_URL}",
				Security =
				[
					new OpenApiSecurityScheme { Type = SecuritySchemeType.ApiKey, In = ParameterLocation.Header, Name = "Authorization" }
				]
			},
			null
		);

		sample.Source.Should().Be("###\nGET {{ELASTICSEARCH_URL}}/_cat/aliases/{{PATH_NAME}}\nAuthorization: ApiKey {{API_KEY}}\n");
	}

	[Test]
	public void CreateHttp_UnknownParametersAndServerValues_UseEnvironmentReferences()
	{
		var operation = new OpenApiOperation
		{
			Parameters =
			[
				new OpenApiParameter { Name = "id", In = ParameterLocation.Path, Required = true },
				new OpenApiParameter { Name = "size", In = ParameterLocation.Query, Required = true },
				new OpenApiParameter { Name = "X-Trace", In = ParameterLocation.Header, Required = true }
			]
		};
		var sample = SyntheticCodeSamples.CreateHttp(
			new SyntheticSampleInput(HttpMethod.Get, "/things/{id}", operation)
			{
				Servers = [new OpenApiServer { Url = "https://{region}.example.test/v1" }]
			},
			null
		);

		sample
			.Source
			.Should()
			.Be("###\nGET https://{{SERVER_REGION}}.example.test/v1/things/{{PATH_ID}}?size={{QUERY_SIZE}}\nX-Trace: {{HEADER_X_TRACE}}\n");
	}

	[Test]
	public void CreateHttp_RequiredContentTypeParameter_UsesTheBodyMediaType()
	{
		var operation = new OpenApiOperation
		{
			Parameters = [new OpenApiParameter { Name = "Content-Type", In = ParameterLocation.Header, Required = true }]
		};
		var sample = SyntheticCodeSamples.CreateHttp(
			new SyntheticSampleInput(HttpMethod.Post, "/things", operation)
			{
				HostVariable = "${ELASTICSEARCH_URL}",
				Parameters = [.. operation.Parameters!],
				Body = "{}"
			},
			"application/json"
		);

		sample.Source.Should().Be("###\nPOST {{ELASTICSEARCH_URL}}/things\nContent-Type: application/json\n\n{}\n");
	}

	[Test]
	public void CreateHttp_PartialServerTemplate_InlinesTheStatedDefault()
	{
		var sample = SyntheticCodeSamples.CreateHttp(
			new SyntheticSampleInput(HttpMethod.Get, "/things", new OpenApiOperation())
			{
				Servers =
				[
					new OpenApiServer
					{
						Url = "https://{region}.example.test/v1",
						Variables = new Dictionary<string, OpenApiServerVariable> { ["region"] = new() { Default = "us-east-1" } }
					}
				]
			},
			null
		);

		sample.Source.Should().Be("###\nGET https://us-east-1.example.test/v1/things\n");
	}

	[Test]
	[Arguments(null, "API_URL")]
	[Arguments("elasticsearch", "ELASTICSEARCH_URL")]
	[Arguments("serverless-elasticsearch", "ELASTICSEARCH_URL")]
	[Arguments("kibana", "KIBANA_URL")]
	[Arguments("logstash", "LOGSTASH_URL")]
	[Arguments("cloud-enterprise", "API_URL")]
	public void CreateHttp_WithoutServer_UsesTheProductsEnvironmentVariable(string? productId, string expected)
	{
		var sample = SyntheticCodeSamples.CreateHttp(
			new SyntheticSampleInput(HttpMethod.Get, "/things", new OpenApiOperation())
			{
				HostVariable = GeneratedCodeSamples.SampleTargetFor(productId).HostVariable
			},
			null
		);

		sample.Source.Should().Be("###\nGET {{" + expected + "}}/things\n");
	}

	[Test]
	[Arguments(null, "")]
	[Arguments("", "")]
	[Arguments("  ", "")]
	[Arguments("/", "")]
	[Arguments("/api/v1", "/api/v1")]
	[Arguments("api/v1/", "/api/v1")]
	public void CreateHttp_WithoutAnAbsoluteServer_UsesTheHostVariableAndKeepsTheBasePath(string? server, string basePath)
	{
		var sample = SyntheticCodeSamples.CreateHttp(
			new SyntheticSampleInput(HttpMethod.Get, "/things", new OpenApiOperation())
			{
				Servers = server is null ? null : [new OpenApiServer { Url = server }],
				HostVariable = "${ELASTICSEARCH_URL}"
			},
			null
		);

		sample.Source.Should().Be("###\nGET {{ELASTICSEARCH_URL}}" + basePath + "/things\n");
	}

	[Test]
	[Arguments("https://api.example.test/v1/", "https://api.example.test/v1")]
	[Arguments("http://api.example.test", "http://api.example.test")]
	[Arguments("//example.test/api", "https://example.test/api")]
	public void CreateHttp_WithAnAbsoluteServer_InlinesTheUrl(string server, string expected)
	{
		var sample = SyntheticCodeSamples.CreateHttp(
			new SyntheticSampleInput(HttpMethod.Get, "/things", new OpenApiOperation()) { Servers = [new OpenApiServer { Url = server }] },
			null
		);

		sample.Source.Should().Be($"###\nGET {expected}/things\n");
	}

	[Test]
	public void CreateHttp_WholeHostTemplate_UsesTheNamedEnvironmentVariableInsteadOfItsDefault()
	{
		var sample = SyntheticCodeSamples.CreateHttp(
			new SyntheticSampleInput(HttpMethod.Post, "/api/alerting/v2/action_policies/_bulk_disable", new OpenApiOperation())
			{
				Servers =
				[
					new OpenApiServer
					{
						Url = "https://{kibana_url}",
						Variables = new Dictionary<string, OpenApiServerVariable> { ["kibana_url"] = new() { Default = "localhost:5601" } }
					}
				]
			},
			null
		);

		sample.Source.Should().Be("###\nPOST {{KIBANA_URL}}/api/alerting/v2/action_policies/_bulk_disable\n");
	}

	[Test]
	public void CreateHttp_JsonRequest_PreservesBodyAndCredentialPlaceholders()
	{
		var input = new SyntheticSampleInput(HttpMethod.Post, "/{index}/_search", new OpenApiOperation())
		{
			Body = "{\n  \"query\": { \"match_all\": {} }\n}",
			Security =
			[
				new OpenApiSecurityScheme { Type = SecuritySchemeType.ApiKey, In = ParameterLocation.Header, Name = "Authorization" }
			]
		};

		var sample = SyntheticCodeSamples.CreateHttp(input, "application/json");

		sample.Source.Should().MatchSnapshot();
		sample.Generated.Should().BeTrue();
		sample.HighlightClass.Should().Be("language-http");
	}

	[Test]
	public void CreateHttp_BodylessRequest_UsesServerAndParameterExamples()
	{
		var operation = new OpenApiOperation
		{
			Parameters =
			[
				new OpenApiParameter { Name = "id", In = ParameterLocation.Path, Required = true, Example = JsonValue.Create("a/b") },
				new OpenApiParameter { Name = "pretty", In = ParameterLocation.Query, Required = true },
				new OpenApiParameter { Name = "size", In = ParameterLocation.Query, Required = true },
				new OpenApiParameter { Name = "kbn-xsrf", In = ParameterLocation.Header, Required = true },
				new OpenApiParameter { Name = "X-Trace", In = ParameterLocation.Header, Required = true }
			]
		};
		var sample = SyntheticCodeSamples.CreateHttp(
			new SyntheticSampleInput(HttpMethod.Get, "/things/{id}?pretty=true", operation)
			{
				Servers =
				[
					new OpenApiServer
					{
						Url = "https://{host}/api",
						Variables = new Dictionary<string, OpenApiServerVariable> { ["host"] = new() { Default = "example.test" } }
					}
				]
			},
			null
		);

		sample.Source.Should().MatchSnapshot();
	}

	[Test]
	public void CreateHttp_ArrayAndObjectParameters_KeepSerializationAndEncoding()
	{
		var operation = new OpenApiOperation
		{
			Parameters =
			[
				new OpenApiParameter
				{
					Name = "ids",
					In = ParameterLocation.Query,
					Required = true,
					Style = ParameterStyle.Form,
					Explode = true,
					Example = new JsonArray("a/b", "c d")
				},
				new OpenApiParameter
				{
					Name = "filter",
					In = ParameterLocation.Query,
					Required = true,
					Style = ParameterStyle.DeepObject,
					Explode = true,
					Example = new JsonObject { ["tag"] = "a&b" }
				},
				new OpenApiParameter { Name = "a-b", In = ParameterLocation.Path, Required = true },
				new OpenApiParameter { Name = "a_b", In = ParameterLocation.Path, Required = true }
			]
		};

		SyntheticCodeSamples
			.CreateHttp(new SyntheticSampleInput(HttpMethod.Get, "/{a-b}/{a_b}", operation), null)
			.Source
			.Should()
			.MatchSnapshot();
	}

	[Test]
	[Arguments("api-key-header", "GET {{API_URL}}/things\nAuthorization: ApiKey {{API_KEY}}")]
	[Arguments("api-key-query", "GET {{API_URL}}/things?key={{API_KEY}}")]
	[Arguments("basic", "GET {{API_URL}}/things\nAuthorization: Basic {{BASIC_CREDENTIALS}}")]
	[Arguments("bearer", "GET {{API_URL}}/things\nAuthorization: Bearer {{TOKEN}}")]
	[Arguments("oauth", "GET {{API_URL}}/things\nAuthorization: Bearer {{TOKEN}}")]
	[Arguments("openid", "GET {{API_URL}}/things\nAuthorization: Bearer {{TOKEN}}")]
	public void CreateHttp_Authentication_UsesClientEnvironmentWithoutInlineDeclarations(string kind, string request)
	{
		var scheme = kind switch
		{
			"api-key-header" =>
				new OpenApiSecurityScheme { Type = SecuritySchemeType.ApiKey, In = ParameterLocation.Header, Name = "Authorization" },
			"api-key-query" => new OpenApiSecurityScheme { Type = SecuritySchemeType.ApiKey, In = ParameterLocation.Query, Name = "key" },
			"basic" => new OpenApiSecurityScheme { Type = SecuritySchemeType.Http, Scheme = "basic" },
			"bearer" => new OpenApiSecurityScheme { Type = SecuritySchemeType.Http, Scheme = "bearer" },
			"oauth" => new OpenApiSecurityScheme { Type = SecuritySchemeType.OAuth2 },
			_ => new OpenApiSecurityScheme { Type = SecuritySchemeType.OpenIdConnect }
		};
		var sample = SyntheticCodeSamples.CreateHttp(
			new SyntheticSampleInput(HttpMethod.Get, "/things", new OpenApiOperation()) { Security = [scheme] },
			null
		);

		sample.Source.Should().Be($"###\n{request}\n");
	}

	[Test]
	[Arguments("basic")]
	[Arguments("bearer")]
	public void CreateHttp_HttpAuthentication_UsesEditableCredential(string scheme)
	{
		var sample = SyntheticCodeSamples.CreateHttp(
			new SyntheticSampleInput(HttpMethod.Get, "/", new OpenApiOperation())
			{
				Security = [new OpenApiSecurityScheme { Type = SecuritySchemeType.Http, Scheme = scheme }]
			},
			null
		);

		sample.Source.Should().MatchSnapshot(scheme, new SnapshotOptions { NameReplacesArguments = true });
	}

	[Test]
	public void CreateHttp_QueryAuthentication_ReplacesExistingValueWithoutDuplicates()
	{
		var sample = SyntheticCodeSamples.CreateHttp(
			new SyntheticSampleInput(HttpMethod.Get, "/?key=example&size=2", new OpenApiOperation())
			{
				Security = [new OpenApiSecurityScheme { Type = SecuritySchemeType.ApiKey, In = ParameterLocation.Query, Name = "key" }]
			},
			null
		);

		sample.Source.Should().MatchSnapshot();
	}

	[Test]
	public void CreateHttp_AuthenticationParameters_UseEnvironmentReferences()
	{
		var operation = new OpenApiOperation
		{
			Parameters =
			[
				new OpenApiParameter { Name = "Authorization", In = ParameterLocation.Header, Required = true },
				new OpenApiParameter { Name = "key", In = ParameterLocation.Query, Required = true },
				new OpenApiParameter { Name = "id", In = ParameterLocation.Path, Required = true },
				new OpenApiParameter { Name = "size", In = ParameterLocation.Query, Required = true },
				new OpenApiParameter { Name = "X-Trace", In = ParameterLocation.Header, Required = true }
			]
		};
		var sample = SyntheticCodeSamples.CreateHttp(
			new SyntheticSampleInput(HttpMethod.Get, "/things/{id}?key={key}", operation)
			{
				Security =
				[
					new OpenApiSecurityScheme { Type = SecuritySchemeType.Http, Scheme = "bearer" },
					new OpenApiSecurityScheme { Type = SecuritySchemeType.ApiKey, In = ParameterLocation.Query, Name = "key" }
				]
			},
			null
		);

		sample.Source.Should().MatchSnapshot();
	}

	[Test]
	public void CreateHttp_ExplodedObjectQuery_PreservesScenarioValues()
	{
		var operation = new OpenApiOperation
		{
			Parameters =
			[
				new OpenApiParameter
				{
					Name = "filter",
					In = ParameterLocation.Query,
					Required = true,
					Style = ParameterStyle.Form,
					Explode = true,
					Example = new JsonObject { ["a"] = "default", ["b"] = "other" }
				}
			]
		};

		SyntheticCodeSamples
			.CreateHttp(new SyntheticSampleInput(HttpMethod.Get, "/?a=chosen", operation), null)
			.Source
			.Should()
			.MatchSnapshot();
	}

	[Test]
	public void Fill_AuthoredOtherLanguages_GetsScenarioSpecificHttp()
	{
		var operation = JsonOperation();
		var scenarios = Fill(
			operation,
			new ExampleScenario
			{
				Title = "Search",
				TabId = "search",
				RequestJson = "{\"size\":1}",
				RequestLine = ("POST", "/other?size=2"),
				CodeSamples = [new CodeSample("Python", "client.search()", "language-python")]
			}
		);

		var scenario = scenarios.Single();
		var http = scenario.CodeSamples.Single(static s => s.IsHttp);
		http.Source.Should().MatchSnapshot();
		scenario.ShowRequest.Should().BeFalse();
		scenario.CodeSamples[0].Language.Should().Be("Python");
	}

	[Test]
	public void Fill_NoExplicitBody_UsesJsonSchema()
	{
		var scenario = Fill(JsonOperation(), new ExampleScenario { Title = "Search", TabId = "search" }).Single();

		scenario.CodeSamples.Single(static s => s.IsHttp).Source.Should().MatchSnapshot();
	}

	[Test]
	public void Fill_AuthoredHttp_PreservesSource()
	{
		var authored = new CodeSample("HTTP", "GET https://example.test/\n", "language-http");
		var scenario = Fill(
			new OpenApiOperation(),
			new ExampleScenario { Title = "Read", TabId = "read", CodeSamples = [authored] }
		).Single();

		var sample = scenario.CodeSamples.Should().ContainSingle().Which;
		sample.Source.Should().Be(authored.Source);
		sample.Generated.Should().BeFalse();
	}

	[Test]
	public void Fill_ExistingFirstLanguage_RemainsFirstWithoutConsoleOrCurl()
	{
		var scenario = Fill(
			new OpenApiOperation(),
			new ExampleScenario { Title = "Read", TabId = "read", CodeSamples = [new CodeSample("Java", "client.read()", "language-java")] }
		).Single();

		scenario.CodeSamples.Select(static s => s.Language).Should().Equal("Java", "HTTP");
	}

	[Test]
	[Arguments("text/plain")]
	[Arguments("application/x-ndjson")]
	[Arguments("multipart/form-data")]
	public void Fill_UnsupportedBody_DoesNotGenerateHttp(string mediaType)
	{
		var operation = new OpenApiOperation
		{
			RequestBody = new OpenApiRequestBody
			{
				Content = new Dictionary<string, IOpenApiMediaType> { [mediaType] = new OpenApiMediaType() }
			}
		};
		Fill(operation, new ExampleScenario { Title = "Write", TabId = "write", RequestJson = "{}" })
			.Single()
			.CodeSamples
			.Should()
			.BeEmpty();
	}

	[Test]
	public void Fill_ExternalUnmatchedAndUnknownBodies_DoesNotInventRequests()
	{
		var scenarios = new[]
		{
			new ExampleScenario { Title = "External", TabId = "external", RequestExternalValue = "https://example.test/body.json" },
			new ExampleScenario { Title = "Unmatched", TabId = "unmatched", FromUnmatchedSamples = true },
			new ExampleScenario { Title = "Unknown", TabId = "unknown" }
		};
		var operation = new OpenApiOperation
		{
			RequestBody = new OpenApiRequestBody
			{
				Content = new Dictionary<string, IOpenApiMediaType> { ["application/json"] = new OpenApiMediaType() }
			}
		};

		Fill(operation, scenarios).SelectMany(static s => s.CodeSamples).Should().BeEmpty();
	}

	[Test]
	public void Fill_UnsupportedRequiredAuthentication_DoesNotGenerateHttp()
	{
		var scenario = HttpCodeSamples.Fill(
			[new ExampleScenario { Title = "Read", TabId = "read" }],
			new SyntheticSampleInput(HttpMethod.Get, "/", new OpenApiOperation()),
			new SchemaAnalyzer(new OpenApiDocument()),
			new HttpSecurityUnsupported()
		).Single();

		scenario.CodeSamples.Should().BeEmpty();
	}

	[Test]
	public void HttpSchemes_UnsupportedFirstAlternative_ChoosesSupportedAlternative()
	{
		var document = SecurityDocument();
		var operation = new OpenApiOperation { Security = [Requirement("cookie"), Requirement("token")] };

		var security = OpenApiAuthSchemeResolver.HttpSchemes(operation, document).Value.Should().BeOfType<HttpSecuritySupported>().Subject;

		security.Schemes.Should().ContainSingle().Which.Scheme.Should().Be("bearer");
	}

	[Test]
	public void HttpSchemes_OperationOverridesAndAnonymousAccess_DoNotInheritCredentials()
	{
		var document = SecurityDocument();
		document.Security = [Requirement("token")];
		var anonymous = new OpenApiOperation { Security = [] };
		var optional = new OpenApiOperation { Security = [Requirement("cookie"), new OpenApiSecurityRequirement()] };

		OpenApiAuthSchemeResolver
			.HttpSchemes(anonymous, document)
			.Value
			.Should()
			.BeOfType<HttpSecuritySupported>()
			.Subject
			.Schemes
			.Should()
			.BeEmpty();
		OpenApiAuthSchemeResolver
			.HttpSchemes(optional, document)
			.Value
			.Should()
			.BeOfType<HttpSecuritySupported>()
			.Subject
			.Schemes
			.Should()
			.BeEmpty();
	}

	[Test]
	public void HttpSchemes_RequiredUnsupportedScheme_IsNotAnonymous()
	{
		var document = SecurityDocument();
		var operation = new OpenApiOperation { Security = [Requirement("token", "cookie")] };

		OpenApiAuthSchemeResolver.HttpSchemes(operation, document).Value.Should().BeOfType<HttpSecurityUnsupported>();
	}

	[Test]
	public void HttpSchemes_ConflictingAuthorizationSchemes_DoesNotDropARequiredCredential()
	{
		var document = SecurityDocument();
		document.Components!.SecuritySchemes!["basic"] = new OpenApiSecurityScheme { Type = SecuritySchemeType.Http, Scheme = "basic" };
		var operation = new OpenApiOperation { Security = [Requirement("token", "basic")] };

		OpenApiAuthSchemeResolver.HttpSchemes(operation, document).Value.Should().BeOfType<HttpSecurityUnsupported>();
	}

	private static OpenApiDocument SecurityDocument() =>
		new()
		{
			Components = new OpenApiComponents
			{
				SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>
				{
					["cookie"] = new OpenApiSecurityScheme
					{
						Type = SecuritySchemeType.ApiKey,
						In = ParameterLocation.Cookie,
						Name = "session"
					},
					["token"] = new OpenApiSecurityScheme { Type = SecuritySchemeType.Http, Scheme = "bearer" }
				}
			}
		};

	private static OpenApiSecurityRequirement Requirement(params string[] schemes)
	{
		var requirement = new OpenApiSecurityRequirement();
		foreach (var scheme in schemes)
			requirement[new OpenApiSecuritySchemeReference(scheme)] = [];
		return requirement;
	}

	private static OpenApiOperation JsonOperation() =>
		new()
		{
			OperationId = "search",
			RequestBody = new OpenApiRequestBody
			{
				Content = new Dictionary<string, IOpenApiMediaType>
				{
					["application/json"] = new OpenApiMediaType
					{
						Schema = new OpenApiSchema
						{
							Type = JsonSchemaType.Object,
							Required = new HashSet<string> { "size" },
							Properties = new Dictionary<string, IOpenApiSchema>
							{
								["size"] = new OpenApiSchema { Type = JsonSchemaType.Integer }
							}
						}
					}
				}
			}
		};

	private static IReadOnlyList<ExampleScenario> Fill(OpenApiOperation operation, params ExampleScenario[] scenarios) =>
		HttpCodeSamples.Fill(
			scenarios,
			new SyntheticSampleInput(HttpMethod.Get, "/_search", operation),
			new SchemaAnalyzer(new OpenApiDocument()),
			new HttpSecuritySupported([])
		);
}
