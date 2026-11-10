// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Diagnostics;

namespace Elastic.Markdown.Tests.Inline;

public class ApiDescriptionLinkTests() : InlineTest("""
Placeholder.
""")
{
	[Test]
	public void RenderApiDescription_LinksToMissingApiPages_HintAtTheDescription()
	{
		var writer = new HtmlWriter(Set, FileSystem, new DescriptionGenerator());
		var source = FileSystem.FileInfo.New("docs/api/doc/kibana/operation/operation-x/description.md");

		var markdown =
			"""
			See [gone](/docs/api/doc/kibana/operation/operation-gone), [here](/docs/api/doc/kibana/operation/operation-exists),
			[groups](https://www.elastic.co/docs/api/doc/kibana/group/endpoint-gone) and [elsewhere](https://example.com).
			""";
		_ = writer.RenderApiDescription(markdown, source);
		// The same description renders again on another page, which must not warn a second time.
		_ = writer.RenderApiDescription(markdown, FileSystem.FileInfo.New("docs/api/doc/kibana/operation/operation-y/description.md"));

		var warnings = Collector.Diagnostics.Where(d => d.Severity == Severity.Hint && d.Message.Contains("API link")).ToArray();
		warnings.Should().HaveCount(2);
		warnings.Should().OnlyContain(d => d.File.EndsWith("description.md"));
		Collector.HasApiHint("https://www.elastic.co/docs/api/doc/kibana/operation/operation-gone");
		Collector.HasApiHint("endpoint-gone");
		Collector.HasNoApiHintFor("operation-exists");
	}
}
