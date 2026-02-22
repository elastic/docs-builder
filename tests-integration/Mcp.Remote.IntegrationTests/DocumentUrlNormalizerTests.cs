// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Mcp.Remote.Gateways;
using FluentAssertions;

namespace Mcp.Remote.IntegrationTests;

/// <summary>
/// Unit tests for <see cref="DocumentGateway.NormalizeUrl"/>.
/// These tests do not require an Elasticsearch connection.
/// </summary>
public class DocumentUrlNormalizerTests
{
	[Theory]
	[InlineData("/docs/deploy-manage/api-keys", "/docs/deploy-manage/api-keys")]
	[InlineData("deploy-manage/api-keys", "/deploy-manage/api-keys")]
	[InlineData("https://www.elastic.co/docs/deploy-manage/api-keys", "/docs/deploy-manage/api-keys")]
	[InlineData("https://docs-v3-preview.elastic.dev/elastic/docs-content/tree/main/deploy-manage/api-keys", "/elastic/docs-content/tree/main/deploy-manage/api-keys")]
	[InlineData("/docs/deploy-manage/api-keys/", "/docs/deploy-manage/api-keys")]
	[InlineData("https://www.elastic.co/docs/deploy-manage/api-keys?ref=nav", "/docs/deploy-manage/api-keys")]
	[InlineData("https://www.elastic.co/docs/deploy-manage/api-keys#section", "/docs/deploy-manage/api-keys")]
	[InlineData("  /docs/deploy-manage/api-keys  ", "/docs/deploy-manage/api-keys")]
	public void NormalizeUrl_ReturnsExpectedPath(string input, string expected) =>
		DocumentGateway.NormalizeUrl(input).Should().Be(expected);
}
