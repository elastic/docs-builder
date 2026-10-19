// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer.Model;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Tests;

public class OperationSummaryTests
{
	[Test]
	[Arguments(null, null)]
	[Arguments("", null)]
	[Arguments("   ", null)]
	[Arguments(" Get connectors\n", "Get connectors")]
	public void GetSummary_BlankOrPadded_ReturnsNullOrTrimmed(string? summary, string? expected)
	{
		var operation = new OpenApiOperation { Summary = summary };

		OpenApiExtensionReader.GetSummary(operation).Should().Be(expected);
	}

	[Test]
	[Arguments(null, null)]
	[Arguments(" ", null)]
	[Arguments("get-actions-connector-oauth-callback-script", "Get actions connector oauth callback script")]
	[Arguments("async_search_delete", "Async search delete")]
	public void HumanizeOperationId_KebabOrSnakeCase_ReturnsReadableTitle(string? operationId, string? expected) =>
		OpenApiExtensionReader.HumanizeOperationId(operationId).Should().Be(expected);
}
