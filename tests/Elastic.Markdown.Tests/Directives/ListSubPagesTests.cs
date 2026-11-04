// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Markdown.Myst.Directives.SubPages;

namespace Elastic.Markdown.Tests.Directives;

[InheritsTests]
public class ListSubPagesTests() : DirectiveTest<ListSubPagesBlock>("""
:::{list-sub-pages}
:::
""")
{
	protected override void AddToFileSystem(MockFileSystem fileSystem)
	{
		fileSystem.AddFile("docs/page1.md", new MockFileData("# Page One\n\nContent."));
		fileSystem.AddFile("docs/page2.md", new MockFileData("# Page Two\n\nContent."));
	}

	[Test]
	public void ParsesListSubPagesBlock() => Block.Should().NotBeNull();

	[Test]
	public void SetsCorrectDirectiveType() => Block!.Directive.Should().Be("list-sub-pages");

	[Test]
	public void ResolvesSubPagesFromNavigation()
	{
		Block!.SubPages.Should().NotBeNull();
		Block.SubPages.Should().HaveCount(2);
	}

	[Test]
	public void SubPagesContainTitlesAndUrls() =>
		Block!.SubPages.Should().OnlyContain(p => !string.IsNullOrEmpty(p.Title) && !string.IsNullOrEmpty(p.Url));

	[Test]
	public void RendersListWithLinks()
	{
		Html.Should().Contain("list-sub-pages");
		Html.Should().Contain("<a ");
		Html.Should().Contain("</a>");
	}
}

[InheritsTests]
public class ListSubPagesWithDescriptionsTests() : DirectiveTest<ListSubPagesBlock>("""
:::{list-sub-pages}
:::
""")
{
	protected override void AddToFileSystem(MockFileSystem fileSystem)
	{
		fileSystem.AddFile("docs/page1.md", new MockFileData("""
---
description: First page description
---
# Page One

Content.
"""));
		fileSystem.AddFile("docs/page2.md", new MockFileData("# Page Two\n\nContent."));
	}

	[Test]
	public void IncludesDescriptionWhenPresent()
	{
		var pageWithDescription = Block!.SubPages.FirstOrDefault(p => p.Description is not null);
		pageWithDescription.Should().NotBeNull();
		pageWithDescription!.Description.Should().Be("First page description");
	}

	[Test]
	public void RendersDescriptionInOutput() => Html.Should().Contain("First page description");
}

[InheritsTests]
public class ListSubPagesWithFolderSiblingTests() : DirectiveTest<ListSubPagesBlock>("""
:::{list-sub-pages}
:::
""")
{
	protected override void AddToFileSystem(MockFileSystem fileSystem)
	{
		fileSystem.AddFile("docs/getting-started/index.md", new MockFileData("# Getting Started\n\nContent."));
		fileSystem.AddFile("docs/getting-started/install.md", new MockFileData("# Install\n\nContent."));
		fileSystem.AddFile("docs/page1.md", new MockFileData("# Page One\n\nContent."));
	}
}
