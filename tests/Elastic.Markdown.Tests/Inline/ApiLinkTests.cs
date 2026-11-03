// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using JetBrains.Annotations;
using Markdig.Syntax.Inlines;

namespace Elastic.Markdown.Tests.Inline;

[InheritsTests]
public class ApiLinkRecordedTests() : LinkTestBase(
	"""
[Search](https://www.elastic.co/docs/api/doc/elasticsearch/operation/operation-search#query-params)
"""
)
{
	[Test]
	public void RecordsApiLinkWithLocation()
	{
		var link = Collector.ApiLinks.Should().ContainSingle().Subject;
		link.Url.Should().Be("https://www.elastic.co/docs/api/doc/elasticsearch/operation/operation-search#query-params");
		link.Line.Should().BePositive();
		link.File.Should().EndWith(".md");
	}

	[Test]
	public void HasNoErrors() => Collector.Errors.Should().Be(0);
}

[InheritsTests]
public class NonApiElasticLinkTests() : LinkTestBase("""
[Docs](https://www.elastic.co/docs/solutions/search)
""")
{
	[Test]
	public void RecordsNoApiLink() => Collector.ApiLinks.Should().BeEmpty();
}

[InheritsTests]
public class ApiAutoLinkTests() : InlineTest<LinkInline>(
	"""
See https://www.elastic.co/docs/api/doc/kibana/operation/operation-missing for details.
"""
)
{
	[Test]
	public void RecordsBareUrlAsApiLink() =>
		Collector
			.ApiLinks
			.Should()
			.ContainSingle()
			.Which
			.Url
			.Should()
			.Be("https://www.elastic.co/docs/api/doc/kibana/operation/operation-missing");
}
