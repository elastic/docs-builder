// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Navigation;
using FluentAssertions;

namespace Elastic.Markdown.Tests.DocSet;

public class BreadCrumbTests(ITestOutputHelper output) : NavigationTestsBase(output)
{
	[Fact]
	public void ParsesATableOfContents()
	{
		var documentationSet = Generator.DocumentationSet;
		INavigationTraversable navigationTraversable = documentationSet;
		var crossLinks = Generator.DocumentationSet.MarkdownFiles.ToDictionary(f => $"docs-builder://{f.RelativePath}");
		var allKeys = crossLinks.Keys.ToList();
		allKeys.Should().Contain("docs-builder://testing/nested/index.md");
		allKeys.Should().Contain("docs-builder://testing/nest-under-index/index.md");

		var lookup = Path.Combine("testing", "nested", "index.md");
		var doc = Generator.DocumentationSet.MarkdownFiles
			.FirstOrDefault(f => f.SourceFile.FullName.EndsWith(lookup, StringComparison.OrdinalIgnoreCase));

		doc.Should().NotBeNull();

		var f = crossLinks.FirstOrDefault(kv => kv.Key == "docs-builder://testing/deeply-nested/foo.md");
		f.Should().NotBeNull();

		crossLinks.Should().ContainKey(doc.CrossLink);
		var nav = navigationTraversable.GetCurrent(crossLinks[doc.CrossLink]);

		nav.Parent.Should().NotBeNull();

		var docNavigation = navigationTraversable.GetNavigationItem(doc);
		docNavigation.Should().NotBeNull();
		var parents = navigationTraversable.GetParentsOfMarkdownFile(doc);

		parents.Should().HaveCount(2);

	}
}
