// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer.Model;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Tests;

public class SyntheticCodeSamplesTests
{
	[Fact]
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

	[Fact]
	public void Create_IncludesRequiredQueryAndHeaders()
	{
		var operation = new OpenApiOperation
		{
			Parameters =
			[
				new OpenApiParameter
				{
					Name = "ids",
					In = ParameterLocation.Query,
					Required = true
				},
				new OpenApiParameter
				{
					Name = "optional",
					In = ParameterLocation.Query,
					Required = false
				},
				new OpenApiParameter
				{
					Name = "kbn-xsrf",
					In = ParameterLocation.Header,
					Required = true,
					Schema = new OpenApiSchema { Example = "true" }
				}
			]
		};

		var samples = SyntheticCodeSamples.Create(
			HttpMethod.Delete,
			"/api/cases",
			operation,
			[new OpenApiServer { Url = "https://{kibana_url}" }]);

		samples[0].Source.Should().Be("DELETE /api/cases?ids={ids}");
		samples[1].Source.Should().Contain("https://{kibana_url}/api/cases?ids={ids}");
		samples[1].Source.Should().Contain("kbn-xsrf: true");
		samples[1].Source.Should().NotContain("optional");
	}
}
