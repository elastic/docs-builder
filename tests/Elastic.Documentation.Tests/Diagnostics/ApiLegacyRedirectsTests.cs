// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Links;

namespace Elastic.Documentation.Tests.Diagnostics;

public class ApiLegacyRedirectsTests
{
	[Test]
	// A page the edge redirects to another page.
	[Arguments("/docs/api/doc/elasticsearch/servers", "/docs/api/elasticsearch")]
	// A renamed root keeps the rest of the path.
	[Arguments("/docs/api/doc/serverless/operation/operation-x", "/docs/api/kibana-serverless/operation/operation-x")]
	// Any other legacy path is the same page without /doc.
	[Arguments("/docs/api/doc/elasticsearch/v8/operation/operation-search", "/docs/api/elasticsearch/v8/operation/operation-search")]
	[Arguments("/docs/api/doc/kibana", "/docs/api/kibana")]
	public void Resolve_LegacyPath_ReturnsTheCurrentPage(string legacy, string expected) =>
		ApiLegacyRedirects.Resolve(legacy).Should().Be(expected);

	[Test]
	public void IsGone_RootWithoutPages_IsTrue() =>
		ApiLegacyRedirects.IsGone("/docs/api/doc/observability-serverless/operation/operation-x").Should().BeTrue();
}
