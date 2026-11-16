// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer.Infrastructure;

namespace Elastic.ApiExplorer.Tests;

public class ApiVersionsTests
{
	[Test]
	public void DefaultTree_MainAmongMajors_ReturnsMain() => ApiVersions.DefaultTree(["9", "main", "8"]).Should().Be("main");

	[Test]
	public void DefaultTree_NoMain_ReturnsFirstResolvedTree() => ApiVersions.DefaultTree(["9", "8"]).Should().Be("9");

	[Test]
	public void DefaultTree_NoTrees_ReturnsNull() => ApiVersions.DefaultTree([]).Should().BeNull();
}
