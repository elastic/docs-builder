// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

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
		var samples = SyntheticCodeSamples.Create(HttpMethod.Delete, "/api/dashboards/{id}", operation, null);

		samples.Should().HaveCount(2);
		samples[0].Language.Should().Be("Console");
		samples[0].Source.Should().Be("DELETE /api/dashboards/{id}");
		samples[1].Language.Should().Be("curl");
		samples[1].Source.Should().Contain("DELETE");
		samples[1].Source.Should().Contain("/api/dashboards/{id}");
	}

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
					Schema = new OpenApiSchema { Examples = [System.Text.Json.Nodes.JsonValue.Create("true")] }
				}
			]
		};

		var samples = SyntheticCodeSamples.Create(
			HttpMethod.Delete,
			"/api/cases",
			operation,
			[new OpenApiServer { Url = "https://{kibana_url}" }]
		);

		samples[0].Source.Should().Be("DELETE /api/cases?ids={ids}");
		samples[1].Source.Should().Contain("https://{kibana_url}/api/cases?ids={ids}");
		samples[1].Source.Should().Contain("kbn-xsrf: true");
		samples[1].Source.Should().NotContain("optional");
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
		var samples = SyntheticCodeSamples.Create(
			HttpMethod.Post,
			"/api/apm/fleet/apm_server_schema",
			JsonPost(),
			[new OpenApiServer { Url = "https://{kibana_url}" }],
			body
		);

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
		var samples = SyntheticCodeSamples.Create(
			HttpMethod.Post,
			"/api/foo",
			JsonPost(),
			null, /*lang=json,strict*/
			"""{"name":"it's"}"""
		);

		samples[1].Source.Should().Contain("it'\\''s");
	}

	[Test]
	public void Create_WithoutABody_LeavesTheSamplesBodyless()
	{
		var samples = SyntheticCodeSamples.Create(HttpMethod.Post, "/api/foo", JsonPost(), null, "  ");

		samples[0].Source.Should().Be("POST /api/foo");
		samples[1].Source.Should().NotContain("-d ");
	}

	[Test]
	public void BuildExampleScenarios_SyntheticSamplesWithTheExampleBody_CarryTheRequest()
	{
		const string body = /*lang=json,strict*/  """{"schema":{"foo":"bar"}}""";
		var samples = SyntheticCodeSamples.Create(HttpMethod.Post, "/api/foo", JsonPost(), null, body);
		var request = new ExampleDisplay("Example", null, body, null);

		var scenarios = OperationPageModel.BuildExampleScenarios([request], [], samples);

		scenarios.Should().ContainSingle();
		scenarios[0].CodeSamples.Should().BeEquivalentTo(samples);
		scenarios[0].CodeSamplesIncludeRequest.Should().BeTrue();
		scenarios[0].ShowRequest.Should().BeFalse();
	}
}
