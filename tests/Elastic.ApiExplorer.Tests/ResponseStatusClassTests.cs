// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer.Operations;

namespace Elastic.ApiExplorer.Tests;

public class ResponseStatusClassTests
{
	[Test]
	[Arguments("200", "success")]
	[Arguments("2XX", "success")]
	[Arguments("400", "error")]
	[Arguments("5XX", "error")]
	[Arguments("301", "info")]
	[Arguments("default", "info")]
	public void StatusClassFor_StatusCode_ReturnsChipModifier(string statusCode, string expected) =>
		OperationPageModel.StatusClassFor(statusCode).Should().Be(expected);
}
