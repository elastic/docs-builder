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
## One
### Two
### Three
### Four
## Five

:::{include} _snippets/test.md
:::
"""
)
{
	protected override void AddToFileSystem(MockFileSystem fileSystem)
	{
		// language=markdown
		var inclusion = """
### Six
### Seven
### Eight
""";
		fileSystem.AddFile(@"docs/_snippets/test.md", inclusion);
	}

	[Fact]
	public void ParsesBlock() => Block.Should().NotBeNull();

	[Fact]
	public void IncludesSnippetAfterMainContent() =>
		Html.Should().Contain("Two").And.Contain("Six");

	[Fact]
	public void TableOfContentsRespectsOrder()
	{
		// Get the table of contents from the file - use values to get them in order
		var toc = File.PageTableOfContent.Values.ToList();

		// The headings should appear in document order:
		// 1. One
		// 2. Two
		// 3. Three
		// 4. Four
		// 5. Five
		// 6. Six (from included snippet)
		// 7. Seven (from included snippet)
		// 8. Eight (from included snippet)

		toc.Should().HaveCount(8);

		// Check the order is correct
		var expectedOrder = new[]
		{
			"One",
			"Two",
			"Three",
			"Four",
			"Five",
			"Six",
			"Seven",
			"Eight"
		};

		var actualOrder = toc.Select(t => t.Heading).ToArray();
		actualOrder.Should().Equal(expectedOrder);
	}
}

public class IncludeBeforeHeadingsOrderTests(ITestOutputHelper output) : DirectiveTest<IncludeBlock>(output,
"""
:::{include} _snippets/test.md
:::

## Four
### Five
### Six
### Seven

## Eight
### Nine
### Ten
### Eleven
"""
)
{
	protected override void AddToFileSystem(MockFileSystem fileSystem)
	{
		// language=markdown
		var inclusion = """
## One
### Two
### Three
""";
		fileSystem.AddFile(@"docs/_snippets/test.md", inclusion);
	}

	[Fact]
	public void ParsesBlock() => Block.Should().NotBeNull();

	[Fact]
	public void TableOfContentsRespectsOrderWithIncludeFirst()
	{
		// Get the table of contents from the file - use values to get them in order
		var toc = File.PageTableOfContent.Values.ToList();

		// The headings should appear in document order:
		// 1. One (from included snippet at top)
		// 2. Two (from included snippet)
		// 3. Three (from included snippet)
		// 4. Four
		// 5. Five
		// 6. Six
		// 7. Seven
		// 8. Eight
		// 9. Nine
		// 10. Ten
		// 11. Eleven

		toc.Should().HaveCount(11);

		// Check the order is correct
		var expectedOrder = new[]
		{
			"One",
			"Two",
			"Three",
			"Four",
			"Five",
			"Six",
			"Seven",
			"Eight",
			"Nine",
			"Ten",
			"Eleven"
		};

		var actualOrder = toc.Select(t => t.Heading).ToArray();
		actualOrder.Should().Equal(expectedOrder);
	}
}

public class IncludeInMiddleOfHeadingsOrderTests(ITestOutputHelper output) : DirectiveTest<IncludeBlock>(output,
"""
## One
### Two
### Three
### Four

:::{include} _snippets/test.md
:::

## Seven
### Eight
### Nine
### Ten
"""
)
{
	protected override void AddToFileSystem(MockFileSystem fileSystem)
	{
		// language=markdown
		var inclusion = """
## Five
### Six
""";
		fileSystem.AddFile(@"docs/_snippets/test.md", inclusion);
	}

	[Fact]
	public void ParsesBlock() => Block.Should().NotBeNull();

	[Fact]
	public void TableOfContentsRespectsOrderWithIncludeInMiddle()
	{
		// Get the table of contents from the file - use values to get them in order
		var toc = File.PageTableOfContent.Values.ToList();

		// The headings should appear in document order:
		// 1. One
		// 2. Two
		// 3. Three
		// 4. Four
		// 5. Five (from included snippet in middle)
		// 6. Six (from included snippet)
		// 7. Seven
		// 8. Eight
		// 9. Nine
		// 10. Ten

		toc.Should().HaveCount(10);

		// Check the order is correct
		var expectedOrder = new[]
		{
			"One",
			"Two",
			"Three",
			"Four",
			"Five",
			"Six",
			"Seven",
			"Eight",
			"Nine",
			"Ten"
		};

		var actualOrder = toc.Select(t => t.Heading).ToArray();
		actualOrder.Should().Equal(expectedOrder);
	}
}
public class IncludeWithStepperOrderTests(ITestOutputHelper output) : DirectiveTest<IncludeBlock>(output,
"""
## One
### Two

:::::{stepper}

::::{step} Three
Content for step three.
::::

::::{step} Four
Content for step four.
::::

:::::

## Five

:::{include} _snippets/test.md
:::

## Ten
### Eleven
### Twelve
"""
)
{
	protected override void AddToFileSystem(MockFileSystem fileSystem)
	{
		// language=markdown
		var inclusion = """
:::::{stepper}

::::{step} Six
Content for step six.
::::

::::{step} Seven
Content for step seven.
::::

:::::

### Eight
### Nine
""";
		fileSystem.AddFile(@"docs/_snippets/test.md", inclusion);
	}

	[Fact]
	public void ParsesBlock() => Block.Should().NotBeNull();

	[Fact]
	public void TableOfContentsRespectsOrderWithStepperAndInclude()
	{
		// Get the table of contents from the file - use values to get them in order
		var toc = File.PageTableOfContent.Values.ToList();

		// The headings should appear in document order:
		// 1. One
		// 2. Two
		// 3. Three (stepper step)
		// 4. Four (stepper step)
		// 5. Five
		// 6. Six (stepper step from included snippet)
		// 7. Seven (stepper step from included snippet)
		// 8. Eight (from included snippet)
		// 9. Nine (from included snippet)
		// 10. Ten
		// 11. Eleven
		// 12. Twelve

		toc.Should().HaveCount(12);

		// Check the order is correct
		var expectedOrder = new[]
		{
			"One",
			"Two",
			"Three",
			"Four",
			"Five",
			"Six",
			"Seven",
			"Eight",
			"Nine",
			"Ten",
			"Eleven",
			"Twelve"
		};

		var actualOrder = toc.Select(t => t.Heading).ToArray();
		actualOrder.Should().Equal(expectedOrder);
	}
}

public class StepperBeforeIncludeOrderTests(ITestOutputHelper output) : DirectiveTest<IncludeBlock>(output,
"""
:::::{stepper}

::::{step} One
Starting with stepper at the beginning.
::::

::::{step} Two
Configuration step.
::::

:::::

## Three

:::{include} _snippets/test.md
:::

## Eight
### Nine
"""
)
{
	protected override void AddToFileSystem(MockFileSystem fileSystem)
	{
		// language=markdown
		var inclusion = """
### Four
### Five

:::::{stepper}

::::{step} Six
Step from included content.
::::

::::{step} Seven
Another step from included content.
::::

:::::
""";
		fileSystem.AddFile(@"docs/_snippets/test.md", inclusion);
	}

	[Fact]
	public void ParsesBlock() => Block.Should().NotBeNull();

	[Fact]
	public void TableOfContentsRespectsOrderWithStepperBeforeInclude()
	{
		// Get the table of contents from the file - use values to get them in order
		var toc = File.PageTableOfContent.Values.ToList();

		// The headings should appear in document order:
		// 1. One (stepper step)
		// 2. Two (stepper step)
		// 3. Three
		// 4. Four (from included snippet)
		// 5. Five (from included snippet)
		// 6. Six (stepper step from included snippet)
		// 7. Seven (stepper step from included snippet)
		// 8. Eight
		// 9. Nine

		toc.Should().HaveCount(9);

		// Check the order is correct
		var expectedOrder = new[]
		{
			"One",
			"Two",
			"Three",
			"Four",
			"Five",
			"Six",
			"Seven",
			"Eight",
			"Nine"
		};

		var actualOrder = toc.Select(t => t.Heading).ToArray();
		actualOrder.Should().Equal(expectedOrder);
	}
}
