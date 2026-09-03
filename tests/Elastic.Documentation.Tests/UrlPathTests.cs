// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Extensions;

namespace Elastic.Documentation.Tests;

public class UrlPathTests
{
	[Theory]
	[InlineData("/docs/api/doc/elasticsearch", "https://www.elastic.co/docs/api/doc/elasticsearch")]
	[InlineData("docs/api/doc/elasticsearch", "https://www.elastic.co/docs/api/doc/elasticsearch")]
	public void MakeAbsolute_RelativeUrl_UsesCanonicalBase(string url, string expected) =>
		UrlPath.MakeAbsolute(new Uri("https://www.elastic.co"), url).Should().Be(expected);

	[Fact]
	public void MakeAbsolute_AbsoluteUrl_ReturnsUnchanged()
	{
		const string url = "https://example.com/reference";

		UrlPath.MakeAbsolute(new Uri("https://www.elastic.co"), url).Should().Be(url);
	}
}
