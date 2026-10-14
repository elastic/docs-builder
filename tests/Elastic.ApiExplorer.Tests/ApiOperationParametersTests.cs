// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Nodes;
using AwesomeAssertions;
using Elastic.ApiExplorer.Model;
using Elastic.ApiExplorer.Operations;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Tests;

public class ApiOperationParametersTests
{
	private static OpenApiParameter Param(string name, ParameterLocation location, string? example = null) =>
		new() { Name = name, In = location, Required = true, Example = example is null ? null : JsonValue.Create(example) };

	[Test]
	public void Parameters_IncludeThePathItemsAfterTheOperationsOwn()
	{
		var operation = new OpenApiOperation { Parameters = [Param("pretty", ParameterLocation.Query)] };
		var path = new OpenApiPathItem { Parameters = [Param("organization_id", ParameterLocation.Path)] };

		var merged = new ApiOperation(HttpMethod.Get, operation, "/organizations/{organization_id}", path, "api").Parameters;

		merged.Select(static p => p.Name).Should().Equal("pretty", "organization_id");
	}

	[Test]
	public void Parameters_LetTheOperationOverrideAPathItemParameterOfTheSameNameAndLocation()
	{
		var operation = new OpenApiOperation { Parameters = [Param("id", ParameterLocation.Path, "own")] };
		var path = new OpenApiPathItem
		{
			Parameters = [Param("id", ParameterLocation.Path, "inherited"), Param("id", ParameterLocation.Query)]
		};

		var merged = new ApiOperation(HttpMethod.Get, operation, "/things/{id}", path, "api").Parameters;

		merged.Should().HaveCount(2);
		merged[0].Example!.ToString().Should().Be("own");
		merged[1].In.Should().Be(ParameterLocation.Query);
	}

	[Test]
	public void Parameters_WithoutAnyOnEitherSide_AreEmpty() =>
		new ApiOperation(HttpMethod.Get, new OpenApiOperation(), "/x", new OpenApiPathItem(), "api").Parameters.Should().BeEmpty();

	[Test]
	public void Create_FillsAPathItemParameterExampleIntoTheSamples()
	{
		var operation = new OpenApiOperation();
		var path = new OpenApiPathItem { Parameters = [Param("organization_id", ParameterLocation.Path, "198583657190")] };
		var apiOperation = new ApiOperation(HttpMethod.Get, operation, "/organizations/{organization_id}/subscription", path, "api");

		var samples = SyntheticCodeSamples.Create(new SyntheticSampleInput(apiOperation.OperationType, apiOperation.Route, operation)
		{
			Parameters = apiOperation.Parameters
		});

		samples[0].Source.Should().Be("GET /organizations/198583657190/subscription");
	}
}
