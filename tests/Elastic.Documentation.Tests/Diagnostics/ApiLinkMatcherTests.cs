// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Diagnostics;

namespace Elastic.Documentation.Tests.Diagnostics;

public class ApiLinkMatcherTests
{
	[Test]
	[Arguments("https://www.elastic.co/docs/api/doc/kibana/v8/operation/operation-x", "/docs/api/doc/kibana/v8/operation/operation-x")]
	[Arguments("https://elastic.co/docs/api/doc/kibana/", "/docs/api/doc/kibana")]
	[Arguments("http://www.elastic.co/docs/api/doc/Kibana/Group/Endpoint-Foo?x=1#frag", "/docs/api/doc/kibana/group/endpoint-foo")]
	[Arguments("https://www.elastic.co/docs/api/doc/elasticsearch//operation/operation-bulk", "/docs/api/doc/elasticsearch/operation/operation-bulk")]
	public void TryGetApiPath_ApiUrl_ReturnsNormalizedPath(string url, string expected)
	{
		ApiLinkMatcher.TryGetApiPath(url, out var path).Should().BeTrue();
		path.Should().Be(expected);
	}

	[Test]
	[Arguments("https://www.elastic.co/docs/solutions/search")]
	[Arguments("https://example.com/docs/api/doc/kibana")]
	[Arguments("https://www.elastic.co/docs/api/")]
	[Arguments("/docs/api/doc/kibana")]
	[Arguments("mailto:a@elastic.co")]
	[Arguments("")]
	[Arguments(null)]
	public void TryGetApiPath_NonApiUrl_ReturnsFalse(string? url) => ApiLinkMatcher.TryGetApiPath(url, out _).Should().BeFalse();
}
