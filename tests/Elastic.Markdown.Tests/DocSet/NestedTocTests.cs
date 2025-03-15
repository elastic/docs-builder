// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.IO;
using FluentAssertions;

namespace Elastic.Markdown.Tests.DocSet;

public class NestedTocTests(ITestOutputHelper output) : NavigationTestsBase(output)
{
	[Fact]
	public void InjectsNestedTocsIntoDocumentationSet()
	{
		var doc = Generator.DocumentationSet.Files.FirstOrDefault(f => f.RelativePath == Path.Combine("development", "index.md")) as MarkdownFile;

		doc.Should().NotBeNull();

		// ensure we link back up to main toc in docset yaml
		doc!.Parent.Should().NotBeNull();

		// its parent should be null
		doc.Parent!.Parent.Should().BeNull();

		// its parent should point to an index
		doc.Parent.Index.Should().NotBeNull();
		doc.Parent.Index!.RelativePath.Should().Be("index.md");

	}
}
