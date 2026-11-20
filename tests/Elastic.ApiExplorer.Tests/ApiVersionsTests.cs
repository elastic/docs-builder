// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer.Infrastructure;

namespace Elastic.ApiExplorer.Tests;

public class ApiVersionsTests
{
	[Test]
	public void DefaultTree_MainAmongMajors_ReturnsTheHighestMajor() => ApiVersions.DefaultTree(["main", "8", "9"]).Should().Be("9");

	[Test]
	public void DefaultTree_MainOnly_ReturnsMain() => ApiVersions.DefaultTree(["main"]).Should().Be("main");

	[Test]
	public void DefaultTree_NoNumericMajor_ReturnsMainBeforeTheFirstTree() => ApiVersions.DefaultTree(["beta", "main"]).Should().Be("main");

	[Test]
	public void DefaultTree_NoTrees_ReturnsNull() => ApiVersions.DefaultTree([]).Should().BeNull();
}
