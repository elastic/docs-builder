// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Assembler.ApiCrawl;

namespace Elastic.Documentation.Build.Tests.ApiCrawl;

public class ApiCrawlOptionsTests
{
	[Test]
	[Arguments("docs", "/docs")]
	[Arguments("/docs/", "/docs")]
	[Arguments("", "")]
	[Arguments("/", "")]
	public void PathPrefix_ConfiguredPrefix_NeverStartsWithTwoSlashes(string sitePrefix, string expected)
	{
		var options = new ApiCrawlOptions(new Uri("https://site.test"), sitePrefix, null, AllowIndexing: false);

		options.PathPrefix.Should().Be(expected);
		new Uri(options.Site, $"{options.PathPrefix}/sitemap.xml").AbsoluteUri.Should().Be($"https://site.test{expected}/sitemap.xml");
	}
}
