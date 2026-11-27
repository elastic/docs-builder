// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.Documentation.Links;

namespace Elastic.ApiExplorer.Tests;

public class ApiUrlBuilderTests
{
	[Test]
	[Arguments("", "elasticsearch", "/api/elasticsearch")]
	[Arguments("", "elasticsearch/v9", "/api/elasticsearch/v9")]
	public void ProductRoot_UsesVersionAwareSuffix(string urlPathPrefix, string apiUrlSuffix, string expected) =>
		ApiUrlBuilder.ProductRoot(urlPathPrefix, apiUrlSuffix).Should().Be(expected);

	[Test]
	public void NextSegment_IsTheSegmentTheLegacyHostMapsBackToTheApiRoot() =>
		ApiLegacyRedirects
			.ToLegacy($"api/elasticsearch/{ApiUrlBuilder.NextSegment}/operation/x")
			.Should()
			.Be("api/doc/elasticsearch/operation/x");

	[Test]
	[Arguments("", "cloud-connect", "/api/cloud-connect/authentication")]
	[Arguments("", "elasticsearch/v8", "/api/elasticsearch/v8/authentication")]
	public void AuthenticationUrl_UsesProductSuffix(string urlPathPrefix, string apiUrlSuffix, string expected) =>
		ApiUrlBuilder.AuthenticationUrl(urlPathPrefix, apiUrlSuffix).Should().Be(expected);

	[Test]
	[Arguments("", "kibana", "/api/kibana/spaces")]
	[Arguments("/docs", "kibana/v8", "/docs/api/kibana/v8/spaces")]
	public void ChildUrl_HasNoTrailingSlash(string urlPathPrefix, string apiUrlSuffix, string expected) =>
		ApiUrlBuilder.ChildUrl(urlPathPrefix, apiUrlSuffix, "spaces").Should().Be(expected);

	[Test]
	[Arguments("", "cloud-connect", "/api/cloud-connect/servers")]
	[Arguments("", "elasticsearch/v9", "/api/elasticsearch/v9/servers")]
	public void ServersUrl_UsesProductSuffix(string urlPathPrefix, string apiUrlSuffix, string expected) =>
		ApiUrlBuilder.ServersUrl(urlPathPrefix, apiUrlSuffix).Should().Be(expected);
}
