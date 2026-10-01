// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer.Model;
using Elastic.ApiExplorer.Operations;
using Microsoft.AspNetCore.Html;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Tests;

public class OperationPathsTests
{
	private const string ClusterHealthDescription =
		"""
		**All methods and paths for this operation:**

		<div>
		                      <span class="operation-verb get">GET</span>
		                      <span class="operation-path">/_cluster/health</span>
		                      </div>
		                    <div>
		                      <span class="operation-verb get">GET</span>
		                      <span class="operation-path">/_cluster/health/{index}</span>
		                      </div>
		                    

		You can also use the API to get the health status of only specified data streams and indices.
		""";

	private static readonly CodeSample[] ClusterHealthSamples = [new("Console", "GET _cluster/health", "language-console")];

	[Test]
	[Arguments("/_cluster/health", false)]
	[Arguments("/_cluster/health/{index}", true)]
	public void ClusterHealth_Index_RequiredFollowsSelectedPath(string selectedPath, bool indexRequired)
	{
		var paths = OperationPaths.Resolve(ClusterHealthDescription, "get", "/_cluster/health/{index}", ClusterHealthSamples);
		var selected = paths.Paths.Single(path => path.Route == selectedPath);
		var parameter = new ApiPathParameter
		{
			Parameter = new OpenApiParameter { Name = "index", In = ParameterLocation.Path, Required = true },
			Type = null,
			EnumValues = [],
			UnionOptions = [],
			DescriptionHtml = HtmlString.Empty,
			DescriptionMarkdown = null,
			Required = true
		};

		var applied = OperationPaths.ApplyRequirement(parameter, paths with { Selected = selected });

		applied.Required.Should().Be(indexRequired);
	}

	[Test]
	public void ClusterHealth_VisibleExample_UsesTheSelectedPath()
	{
		var paths = OperationPaths.Resolve(ClusterHealthDescription, "get", "/_cluster/health/{index}", ClusterHealthSamples);
		var examples = OperationPaths.PerPathExamples(basis: null, paths, ClusterHealthSamples);

		paths.Selected.Should().Be(new OperationPathChoice("get", "/_cluster/health"));
		examples.Single(example => example.Route == "/_cluster/health").CodeSamples[0].Source.Should().Be("GET _cluster/health");
		examples
			.Single(example => example.Route == "/_cluster/health/{index}")
			.CodeSamples[0]
			.Source
			.Should()
			.Be("GET /_cluster/health/{index}");
		paths.Description.Should().StartWith("You can also use the API");
	}

	[Test]
	public void SpacesPath_KeepsTheDeleteVerbAndTheCanonicalRoute()
	{
		const string description =
			"""
			**Spaces method and path for this operation:**

			<div><span class="operation-verb delete">delete</span>&nbsp;<span class="operation-path">/s/{space_id}/api/agent_builder/agents/{id}</span></div>

			Delete an agent by ID.
			""";

		var paths = OperationPaths.Resolve(description, "delete", "/api/agent_builder/agents/{id}", []);

		paths
			.Paths
			.Select(path => path.Key)
			.Should()
			.Equal("delete /api/agent_builder/agents/{id}", "delete /s/{space_id}/api/agent_builder/agents/{id}");
		paths.Description.Should().Be("Delete an agent by ID.");
	}
}
