// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Links;

namespace Elastic.Documentation.Tests.Diagnostics;

public class ApiLinkMatcherTests
{
	[Test]
	[Arguments("https://www.elastic.co/docs/api/kibana/v8/operation/operation-x", "/docs/api/kibana/v8/operation/operation-x")]
	[Arguments("https://elastic.co/docs/api/kibana/", "/docs/api/kibana")]
	[Arguments("http://www.elastic.co/docs/api/Kibana/Group/Endpoint-Foo?x=1#frag", "/docs/api/kibana/group/endpoint-foo")]
	[Arguments("https://www.elastic.co/docs/api/elasticsearch//operation/operation-bulk", "/docs/api/elasticsearch/operation/operation-bulk")]
	[Arguments("https://www.elastic.co/docs/api/doc/kibana/v8/operation/operation-x", "/docs/api/doc/kibana/v8/operation/operation-x")]
	public void TryGetApiPath_ApiUrl_ReturnsNormalizedPath(string url, string expected)
	{
		ApiLinkMatcher.TryGetApiPath(url, out var path).Should().BeTrue();
		path.Should().Be(expected);
	}

	[Test]
	[Arguments("https://www.elastic.co/docs/solutions/search")]
	[Arguments("https://example.com/docs/api/kibana")]
	[Arguments("https://www.elastic.co/docs/api/")]
	[Arguments("https://www.elastic.co/docs/api/doc/")]
	[Arguments("/docs/api/kibana")]
	[Arguments("mailto:a@elastic.co")]
	[Arguments("")]
	[Arguments(null)]
	public void TryGetApiPath_NonApiUrl_ReturnsFalse(string? url) => ApiLinkMatcher.TryGetApiPath(url, out _).Should().BeFalse();

	[Test]
	[Arguments("/docs/api/kibana/operation/operation-x", "/docs", "https://www.elastic.co/docs/api/kibana/operation/operation-x")]
	[Arguments("/elastic/docs-builder/docs/123/api/kibana/group/endpoint-a", "/elastic/docs-builder/docs/123", "https://www.elastic.co/docs/api/kibana/group/endpoint-a")]
	[Arguments("https://www.elastic.co/docs/api/kibana", "/docs", "https://www.elastic.co/docs/api/kibana")]
	[Arguments("https://www.elastic.co/docs/api/doc/kibana", "/docs", "https://www.elastic.co/docs/api/doc/kibana")]
	public void TryCanonicalize_ApiPageLink_ReturnsAbsoluteProductionUrl(string url, string prefix, string expected)
	{
		ApiLinkMatcher.TryCanonicalize(url, prefix, out var canonical).Should().BeTrue();
		canonical.Should().Be(expected);
	}

	[Test]
	[Arguments("/docs/solutions/search", "/docs")]
	[Arguments("/docs/api/", "/docs")]
	[Arguments("../operation/x", "/docs")]
	[Arguments("https://example.com/docs/api/kibana", "/docs")]
	[Arguments(null, "/docs")]
	public void TryCanonicalize_OtherLink_ReturnsFalse(string? url, string prefix) =>
		ApiLinkMatcher.TryCanonicalize(url, prefix, out _).Should().BeFalse();

	[Test]
	[Arguments("api/doc/kibana/v9", "api/kibana/v9", "api/doc/kibana/v9")]
	[Arguments("api/kibana", "api/kibana", "api/doc/kibana")]
	[Arguments("/docs/api/doc/kibana/operation/x", "/docs/api/kibana/operation/x", "/docs/api/doc/kibana/operation/x")]
	[Arguments("/api/kibana", "/api/kibana", "/api/doc/kibana")]
	[Arguments("api/", "api/", "api/")]
	[Arguments("guide/api/kibana", "guide/api/kibana", "guide/api/kibana")]
	public void ToCanonicalAndToLegacy_SitePath_SwapOnlyTheApiPrefix(string sitePath, string canonical, string legacy)
	{
		ApiLegacyRedirects.ToCanonical(sitePath).Should().Be(canonical);
		ApiLegacyRedirects.ToLegacy(sitePath).Should().Be(legacy);
	}
}
