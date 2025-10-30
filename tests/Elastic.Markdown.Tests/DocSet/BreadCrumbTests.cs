// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Extensions;
using Elastic.Markdown.IO;
using FluentAssertions;

namespace Elastic.Markdown.Tests.DocSet;

public class BreadCrumbTests(ITestOutputHelper output) : NavigationTestsBase(output)
{
	[Fact]
	public void ParsesATableOfContents()
	{
		IPositionalNavigation positionalNavigation = Generator.DocumentationSet;
		var allKeys = positionalNavigation.NavigationIndexedByCrossLink.Keys;
		allKeys.Should().Contain("docs-builder://testing/nested/index.md");
		allKeys.Should().Contain("docs-builder://testing/nest-under-index/index.md");

		var lookup = Path.Combine("testing", "nested", "index.md");
		var folder = Path.Combine(Generator.Context.DocumentationSourceDirectory.FullName, "testing");
		var testingFiles = Generator.DocumentationSet.MarkdownFiles
			.Where(f => f.SourceFile.IsSubPathOf(f.SourceFile.FileSystem.DirectoryInfo.New(folder)));
		var doc = Generator.DocumentationSet.MarkdownFiles
			.FirstOrDefault(f => f.SourceFile.FullName.EndsWith(lookup, StringComparison.OrdinalIgnoreCase));

		doc.Should().NotBeNull();


		var f = positionalNavigation.NavigationIndexedByCrossLink.FirstOrDefault(kv => kv.Key == "docs-builder://testing/deeply-nested/foo.md");
		f.Should().NotBeNull();

		positionalNavigation.NavigationIndexedByCrossLink.Should().ContainKey(doc.CrossLink);
		var nav = positionalNavigation.NavigationIndexedByCrossLink[doc.CrossLink];

		nav.Parent.Should().NotBeNull();

		_ = positionalNavigation.MarkdownNavigationLookup.TryGetValue(doc, out var docNavigation);
		docNavigation.Should().NotBeNull();
		var parents = positionalNavigation.GetParentsOfMarkdownFile(doc);

		parents.Should().HaveCount(3);

	}
}
