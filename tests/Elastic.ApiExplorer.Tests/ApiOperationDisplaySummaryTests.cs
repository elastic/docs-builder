// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer.Operations;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Tests;

public class ApiOperationDisplaySummaryTests
{
	private static ApiOperation OperationWithSummary(string? summary) =>
		new(HttpMethod.Get, new OpenApiOperation { Summary = summary }, "/x", new OpenApiPathItem(), "api");

	[Test]
	[Arguments("Run an async search\n", "Run an async search")]
	[Arguments("DEPRECATED: gone soon.\n\nGet instance types", "DEPRECATED: gone soon. Get instance types")]
	[Arguments("  Get started\t\f now ", "Get started now")]
	[Arguments("Get started now", "Get started now")]
	[Arguments("Get aliases", "Get aliases")]
	[Arguments(null, null)]
	public void DisplaySummary_Summary_CollapsesOnlyAsciiWhitespace(string? summary, string? expected) =>
		OperationWithSummary(summary).DisplaySummary.Should().Be(expected);

	[Test]
	public void DisplaySummary_SummaryWithNewline_LeavesTheSpecValueUntouched()
	{
		var operation = OperationWithSummary("Run an async search\n");

		_ = operation.DisplaySummary;

		operation.Operation.Summary.Should().Be("Run an async search\n");
	}
}
