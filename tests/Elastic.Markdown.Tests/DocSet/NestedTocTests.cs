// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Extensions;
using Elastic.Documentation.Navigation;
using Elastic.Documentation.Navigation.Isolated;
using Elastic.Documentation.Navigation.Isolated.Leaf;
using Elastic.Documentation.Navigation.Isolated.Node;
using Elastic.Markdown.IO;
using FluentAssertions;

namespace Elastic.Markdown.Tests.DocSet;

public class NestedTocTests(ITestOutputHelper output) : NavigationTestsBase(output)
{
	[Fact]
	public void InjectsNestedTocsIntoDocumentationSet()
	{
		var doc = Generator.DocumentationSet.MarkdownFiles.FirstOrDefault(f => f.RelativePath == Path.Combine("development", "index.md"));

		doc.Should().NotBeNull();
		IPositionalNavigation positionalNavigation = Generator.DocumentationSet;
		positionalNavigation.MarkdownNavigationLookup.Should().ContainKey(doc);
		if (!positionalNavigation.MarkdownNavigationLookup.TryGetValue(doc, out var nav))
			throw new Exception($"Could not find nav item for {doc.CrossLink}");

		nav.Should().BeOfType<TableOfContentsNavigation<MarkdownFile>>();
		var parent = nav.Parent;

		// ensure we link back up to the main toc in docset yaml
		parent.Should().NotBeNull();
		parent.Should().BeOfType<DocumentationSetNavigation<MarkdownFile>>();

		// its parent should point to an index
		var index = (parent as DocumentationSetNavigation<MarkdownFile>)?.Index;
		index.Should().NotBeNull();
		var fileNav = index as FileNavigationLeaf<MarkdownFile>;
		fileNav.Should().NotBeNull();
		fileNav.Model.RelativePath.OptionalWindowsReplace().Should().Be("index.md");

	}
}
