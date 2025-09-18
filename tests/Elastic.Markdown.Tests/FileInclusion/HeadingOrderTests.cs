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
public class IncludeWithStepperOrderTests(ITestOutputHelper output) : DirectiveTest<IncludeBlock>(output,
"""
## Getting Started
### Prerequisites 

:::::{stepper}

::::{step} Install dependencies
First step in the process.
::::

::::{step} Configure settings
Second step in the process.
::::

:::::

## Main Process

:::{include} _snippets/process-steps.md
:::

## Final Steps
### Cleanup
### Verification
"""
)
{
	protected override void AddToFileSystem(MockFileSystem fileSystem)
	{
		// language=markdown
		var inclusion = """
:::::{stepper}

::::{step} Execute process
Main execution step from snippet.
::::

::::{step} Monitor progress
Monitoring step from snippet.
::::

:::::

### Additional notes
### Troubleshooting
""";
		fileSystem.AddFile(@"docs/_snippets/process-steps.md", inclusion);
	}

	[Fact]
	public void ParsesBlock() => Block.Should().NotBeNull();

	[Fact]
	public void TableOfContentsRespectsOrderWithStepperAndInclude()
	{
		// Get the table of contents from the file - use values to get them in order
		var toc = File.PageTableOfContent.Values.ToList();

		// The headings should appear in document order:
		// 1. Getting Started
		// 2. Prerequisites
		// 3. Install dependencies (stepper step)
		// 4. Configure settings (stepper step)
		// 5. Main Process
		// 6. Execute process (stepper step from included snippet)
		// 7. Monitor progress (stepper step from included snippet)
		// 8. Additional notes (from included snippet)
		// 9. Troubleshooting (from included snippet)
		// 10. Final Steps
		// 11. Cleanup
		// 12. Verification

		toc.Should().HaveCount(12);

		// Check the order is correct
		var expectedOrder = new[]
		{
			"Getting Started",
			"Prerequisites",
			"Install dependencies",
			"Configure settings",
			"Main Process",
			"Execute process",
			"Monitor progress",
			"Additional notes",
			"Troubleshooting",
			"Final Steps",
			"Cleanup",
			"Verification"
		};

		var actualOrder = toc.Select(t => t.Heading).ToArray();
		actualOrder.Should().Equal(expectedOrder);
	}
}

public class StepperBeforeIncludeOrderTests(ITestOutputHelper output) : DirectiveTest<IncludeBlock>(output,
"""
:::::{stepper}

::::{step} Initial setup
Starting with stepper at the beginning.
::::

::::{step} Configuration
Configuration step.
::::

:::::

## Middle Section

:::{include} _snippets/middle-content.md
:::

## Final Section
### Conclusion
"""
)
{
	protected override void AddToFileSystem(MockFileSystem fileSystem)
	{
		// language=markdown
		var inclusion = """
### Included heading one
### Included heading two

:::::{stepper}

::::{step} Included step one
Step from included content.
::::

::::{step} Included step two
Another step from included content.
::::

:::::
""";
		fileSystem.AddFile(@"docs/_snippets/middle-content.md", inclusion);
	}

	[Fact]
	public void ParsesBlock() => Block.Should().NotBeNull();

	[Fact]
	public void TableOfContentsRespectsOrderWithStepperBeforeInclude()
	{
		// Get the table of contents from the file - use values to get them in order
		var toc = File.PageTableOfContent.Values.ToList();

		// The headings should appear in document order:
		// 1. Initial setup (stepper step)
		// 2. Configuration (stepper step)
		// 3. Middle Section
		// 4. Included heading one (from included snippet)
		// 5. Included heading two (from included snippet)
		// 6. Included step one (stepper step from included snippet)
		// 7. Included step two (stepper step from included snippet)
		// 8. Final Section
		// 9. Conclusion

		toc.Should().HaveCount(9);

		// Check the order is correct
		var expectedOrder = new[]
		{
			"Initial setup",
			"Configuration",
			"Middle Section",
			"Included heading one",
			"Included heading two",
			"Included step one",
			"Included step two",
			"Final Section",
			"Conclusion"
		};

		var actualOrder = toc.Select(t => t.Heading).ToArray();
		actualOrder.Should().Equal(expectedOrder);
	}
}
