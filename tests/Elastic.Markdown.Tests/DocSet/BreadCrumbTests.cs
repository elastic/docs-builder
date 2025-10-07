// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.IO;
using FluentAssertions;

namespace Elastic.Markdown.Tests.DocSet;

public class BreadCrumbTests(ITestOutputHelper output) : NavigationTestsBase(output)
{
	[Fact]
	public void ParsesATableOfContents()
	{
		var doc = Generator.DocumentationSet.MarkdownFiles.FirstOrDefault(f => f.RelativePath == Path.Combine("testing", "nested", "index.md"));

		doc.Should().NotBeNull();

		IPositionalNavigation positionalNavigation = Generator.DocumentationSet;

		var allKeys = positionalNavigation.NavigationIndexedByCrossLink.Keys;
		allKeys.Should().Contain("docs-builder://testing/nested/index.md");

		var f = positionalNavigation.NavigationIndexedByCrossLink.FirstOrDefault(kv => kv.Key == "docs-builder://testing/deeply-nested/foo.md");
		f.Should().NotBeNull();

		positionalNavigation.NavigationIndexedByCrossLink.Should().ContainKey(doc.CrossLink);
		var nav = positionalNavigation.NavigationIndexedByCrossLink[doc.CrossLink];

		nav.Parent.Should().NotBeNull();

		var parents = positionalNavigation.GetParentsOfMarkdownFile(doc);

		parents.Should().HaveCount(2);

	}
}
