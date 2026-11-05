// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;

namespace Elastic.Markdown.Tests.Inline;

[InheritsTests]
public class ApiDescriptionLinkTests() : InlineTest("""
Placeholder.
""")
{
	[Test]
	public void RenderApiDescription_LinksBetweenApiPages_AreRecordedWithTheirCanonicalUrl()
	{
		var writer = new HtmlWriter(Set, FileSystem, new DescriptionGenerator());
		var source = FileSystem.FileInfo.New("docs/api/doc/kibana/operation/operation-x/description.md");

		_ = writer.RenderApiDescription(
			"""
			See [the rule](/docs/api/doc/kibana/operation/operation-find-rules), [groups](https://www.elastic.co/docs/api/doc/kibana/group/endpoint-a)
			and [the guide](https://www.elastic.co/docs/solutions/search) or [elsewhere](https://example.com).
			""",
			source
		);

		Collector
			.ApiLinks
			.Select(l => l.Url)
			.Should()
			.BeEquivalentTo([
				"https://www.elastic.co/docs/api/doc/kibana/operation/operation-find-rules",
				"https://www.elastic.co/docs/api/doc/kibana/group/endpoint-a"
			]);
		Collector.ApiLinks.Should().OnlyContain(l => l.File.EndsWith("description.md"));
	}
}
