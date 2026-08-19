// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;

namespace Elastic.Documentation.OpenApiIndex.Tests;

public class OpenApiInvalidationPathsTests
{
	[Fact]
	public void Build_AlwaysIncludesIndexJson()
	{
		var paths = OpenApiInvalidationPaths.Build([]);

		paths.Should().ContainSingle().Which.Should().Be("/index.json");
	}

	[Fact]
	public void Build_AddsLeadingSlashForEachObjectKey()
	{
		var paths = OpenApiInvalidationPaths.Build(["elastic/elasticsearch/8.16/openapi.json"]);

		paths.Should().BeEquivalentTo(
		[
			"/index.json",
			"/elastic/elasticsearch/8.16/openapi.json"
		]);
	}

	[Fact]
	public void Build_DeduplicatesRepeatedKeys()
	{
		var paths = OpenApiInvalidationPaths.Build(
		[
			"elastic/elasticsearch/8.16/openapi.json",
			"elastic/elasticsearch/8.16/openapi.json"
		]);

		paths.Should().HaveCount(2);
	}

	[Fact]
	public void Build_TrimsLeadingSlashFromObjectKeys()
	{
		var paths = OpenApiInvalidationPaths.Build(["/elastic/kibana/9.5/openapi.yaml"]);

		paths.Should().Contain("/elastic/kibana/9.5/openapi.yaml");
	}
}
