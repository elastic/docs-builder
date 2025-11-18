// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Extensions;
using Elastic.Documentation.Navigation;
using FluentAssertions;

namespace Elastic.Markdown.Tests.DocSet;

public class BreadCrumbTests(ITestOutputHelper output) : NavigationTestsBase(output)
{
	[Fact]
	public void ParsesATableOfContents()
	{
		var documentationSet = Generator.DocumentationSet;
		var allKeys = documentationSet.NavigationIndexedByCrossLink.Keys;
		allKeys.Should().Contain("docs-builder://testing/nested/index.md");
		allKeys.Should().Contain("docs-builder://testing/nest-under-index/index.md");

		var lookup = Path.Combine("testing", "nested", "index.md");
		var doc = Generator.DocumentationSet.MarkdownFiles
			.FirstOrDefault(f => f.SourceFile.FullName.EndsWith(lookup, StringComparison.OrdinalIgnoreCase));

		doc.Should().NotBeNull();

		var f = documentationSet.NavigationIndexedByCrossLink.FirstOrDefault(kv => kv.Key == "docs-builder://testing/deeply-nested/foo.md");
		f.Should().NotBeNull();

		documentationSet.NavigationIndexedByCrossLink.Should().ContainKey(doc.CrossLink);
		var nav = documentationSet.NavigationIndexedByCrossLink[doc.CrossLink];

		nav.Parent.Should().NotBeNull();

		INavigationTraversable navigationTraversable = documentationSet;
		var docNavigation = navigationTraversable.GetNavigationItem(doc);
		docNavigation.Should().NotBeNull();
		var parents = navigationTraversable.GetParentsOfMarkdownFile(doc);

		parents.Should().HaveCount(2);

	}
}
