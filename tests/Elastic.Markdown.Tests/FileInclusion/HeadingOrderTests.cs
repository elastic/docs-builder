// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using Elastic.Markdown.IO;
using Elastic.Markdown.Myst.Directives.Include;
using Elastic.Markdown.Tests.Directives;
using FluentAssertions;

namespace Elastic.Markdown.Tests.FileInclusion;

public class IncludeHeadingOrderTests(ITestOutputHelper output) : DirectiveTest<IncludeBlock>(output,
"""
## Check status, stop, and restart SLM
### Get SLM status 
### Stop SLM 
### Start SLM
## Check status, stop, and restart ILM

:::{include} _snippets/ilm-status.md
:::
"""
)
{
	protected override void AddToFileSystem(MockFileSystem fileSystem)
	{
		// language=markdown
		var inclusion = """
### Get ILM status
### Stop ILM 
### Start ILM
""";
		fileSystem.AddFile(@"docs/_snippets/ilm-status.md", inclusion);
	}

	[Fact]
	public void ParsesBlock() => Block.Should().NotBeNull();

	[Fact]
	public void IncludesSnippetAfterMainContent() =>
		Html.Should().Contain("Get SLM status").And.Contain("Get ILM status");

	[Fact]
	public void TableOfContentsRespectsOrder()
	{
		// Get the table of contents from the file - use values to get them in order
		var toc = File.PageTableOfContent.Values.ToList();

		// The headings should appear in document order:
		// 1. Check status, stop, and restart SLM
		// 2. Get SLM status
		// 3. Stop SLM
		// 4. Start SLM
		// 5. Check status, stop, and restart ILM
		// 6. Get ILM status (from included snippet)
		// 7. Stop ILM (from included snippet)
		// 8. Start ILM (from included snippet)

		toc.Should().HaveCount(8);

		// Check the order is correct
		var expectedOrder = new[]
		{
			"Check status, stop, and restart SLM",
			"Get SLM status",
			"Stop SLM",
			"Start SLM",
			"Check status, stop, and restart ILM",
			"Get ILM status",
			"Stop ILM",
			"Start ILM"
		};

		var actualOrder = toc.Select(t => t.Heading).ToArray();
		actualOrder.Should().Equal(expectedOrder);
	}
}

public class IncludeBeforeHeadingsOrderTests(ITestOutputHelper output) : DirectiveTest<IncludeBlock>(output,
"""
:::{include} _snippets/intro-status.md
:::

## Check status, stop, and restart SLM
### Get SLM status 
### Stop SLM 
### Start SLM

## Check status, stop, and restart ILM
### Get ILM status
### Stop ILM 
### Start ILM
"""
)
{
	protected override void AddToFileSystem(MockFileSystem fileSystem)
	{
		// language=markdown
		var inclusion = """
## Introduction
### Overview of lifecycle management
### Prerequisites
""";
		fileSystem.AddFile(@"docs/_snippets/intro-status.md", inclusion);
	}

	[Fact]
	public void ParsesBlock() => Block.Should().NotBeNull();

	[Fact]
	public void TableOfContentsRespectsOrderWithIncludeFirst()
	{
		// Get the table of contents from the file - use values to get them in order
		var toc = File.PageTableOfContent.Values.ToList();

		// The headings should appear in document order:
		// 1. Introduction (from included snippet at top)
		// 2. Overview of lifecycle management (from included snippet)
		// 3. Prerequisites (from included snippet)
		// 4. Check status, stop, and restart SLM
		// 5. Get SLM status
		// 6. Stop SLM
		// 7. Start SLM
		// 8. Check status, stop, and restart ILM
		// 9. Get ILM status
		// 10. Stop ILM
		// 11. Start ILM

		toc.Should().HaveCount(11);

		// Check the order is correct
		var expectedOrder = new[]
		{
			"Introduction",
			"Overview of lifecycle management",
			"Prerequisites",
			"Check status, stop, and restart SLM",
			"Get SLM status",
			"Stop SLM",
			"Start SLM",
			"Check status, stop, and restart ILM",
			"Get ILM status",
			"Stop ILM",
			"Start ILM"
		};

		var actualOrder = toc.Select(t => t.Heading).ToArray();
		actualOrder.Should().Equal(expectedOrder);
	}
}

public class IncludeInMiddleOfHeadingsOrderTests(ITestOutputHelper output) : DirectiveTest<IncludeBlock>(output,
"""
## Check status, stop, and restart SLM
### Get SLM status 
### Stop SLM 
### Start SLM

:::{include} _snippets/troubleshooting.md
:::

## Check status, stop, and restart ILM
### Get ILM status
### Stop ILM 
### Start ILM
"""
)
{
	protected override void AddToFileSystem(MockFileSystem fileSystem)
	{
		// language=markdown
		var inclusion = """
## Common troubleshooting steps
### Check service health
### Review logs
""";
		fileSystem.AddFile(@"docs/_snippets/troubleshooting.md", inclusion);
	}

	[Fact]
	public void ParsesBlock() => Block.Should().NotBeNull();

	[Fact]
	public void TableOfContentsRespectsOrderWithIncludeInMiddle()
	{
		// Get the table of contents from the file - use values to get them in order
		var toc = File.PageTableOfContent.Values.ToList();

		// The headings should appear in document order:
		// 1. Check status, stop, and restart SLM
		// 2. Get SLM status
		// 3. Stop SLM
		// 4. Start SLM
		// 5. Common troubleshooting steps (from included snippet in middle)
		// 6. Check service health (from included snippet)
		// 7. Review logs (from included snippet)
		// 8. Check status, stop, and restart ILM
		// 9. Get ILM status
		// 10. Stop ILM
		// 11. Start ILM

		toc.Should().HaveCount(11);

		// Check the order is correct
		var expectedOrder = new[]
		{
			"Check status, stop, and restart SLM",
			"Get SLM status",
			"Stop SLM",
			"Start SLM",
			"Common troubleshooting steps",
			"Check service health",
			"Review logs",
			"Check status, stop, and restart ILM",
			"Get ILM status",
			"Stop ILM",
			"Start ILM"
		};

		var actualOrder = toc.Select(t => t.Heading).ToArray();
		actualOrder.Should().Equal(expectedOrder);
	}
}
