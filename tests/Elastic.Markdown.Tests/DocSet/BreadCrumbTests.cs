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
		var doc = Generator.DocumentationSet.Files.FirstOrDefault(f => f.RelativePath == Path.Combine("testing", "nested", "index.md")) as MarkdownFile;

		doc.Should().NotBeNull();

		IPositionalNavigation positionalNavigation = Generator.DocumentationSet;

		var allKeys = positionalNavigation.MarkdownNavigationLookup.Keys.ToList();
		allKeys.Should().Contain("docs-builder://testing/nested/index.md");

		var f = positionalNavigation.MarkdownNavigationLookup.FirstOrDefault(kv => kv.Key == "docs-builder://testing/nested/index.md");
		f.Should().NotBeNull();

		positionalNavigation.MarkdownNavigationLookup.Should().ContainKey(doc!.CrossLink);
		var nav = positionalNavigation.MarkdownNavigationLookup[doc.CrossLink];

		nav.Parent.Should().NotBeNull();

		var parents = positionalNavigation.GetParentMarkdownFiles(doc);

		parents.Should().HaveCount(2);

	}
}
