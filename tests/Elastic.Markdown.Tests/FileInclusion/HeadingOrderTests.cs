// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using Elastic.Markdown.IO;
using Elastic.Markdown.Myst.Directives.Include;
using Elastic.Markdown.Myst.Directives.Stepper;
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

	[Fact]
	public void HeadingLevelsArePreservedFromSnippet()
	{
		// Verify that real h2/h3 headings from snippets keep their original levels
		// and are NOT adjusted based on parent document context
		var toc = File.PageTableOfContent.Values.ToList();

		// Five is ## in the snippet, should stay level 2
		toc[4].Heading.Should().Be("Five");
		toc[4].Level.Should().Be(2, "h2 from snippet should remain level 2, not be adjusted");

		// Six is ### in the snippet, should stay level 3
		toc[5].Heading.Should().Be("Six");
		toc[5].Level.Should().Be(3, "h3 from snippet should remain level 3");
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

/// <summary>
/// Tests that stepper steps in included snippets inherit the correct heading level
/// from the parent document's context. This is the key test for the DocumentTraversal fix.
/// </summary>
public class StepperInIncludeHeadingLevelTests(ITestOutputHelper output) : DirectiveTest<IncludeBlock>(output,
"""
## Main Heading

Some intro content.

:::{include} _snippets/stepper-snippet.md
:::

## Another Section
"""
)
{
	protected override void AddToFileSystem(MockFileSystem fileSystem)
	{
		// The stepper in this snippet should get heading level 3 (one deeper than the ## heading before the include)
		var inclusion = """
:::::{stepper}

::::{step} Step One
First step content.
::::

::::{step} Step Two
Second step content.
::::

:::::
""";
		fileSystem.AddFile(@"docs/_snippets/stepper-snippet.md", inclusion);
	}

	[Fact]
	public void ParsesBlock() => Block.Should().NotBeNull();

	[Fact]
	public void StepperStepsInSnippetInheritCorrectHeadingLevel()
	{
		// Get the table of contents
		var toc = File.PageTableOfContent.Values.ToList();

		// Should have: Main Heading (2), Step One (3), Step Two (3), Another Section (2)
		toc.Should().HaveCount(4);

		// Verify headings
		toc[0].Heading.Should().Be("Main Heading");
		toc[0].Level.Should().Be(2);

		// Key assertion: stepper steps from included snippet should be level 3
		// (one level deeper than the ## Main Heading that precedes the include)
		toc[1].Heading.Should().Be("Step One");
		toc[1].Level.Should().Be(3, "stepper step in snippet should inherit heading level from parent document");

		toc[2].Heading.Should().Be("Step Two");
		toc[2].Level.Should().Be(3, "stepper step in snippet should inherit heading level from parent document");

		toc[3].Heading.Should().Be("Another Section");
		toc[3].Level.Should().Be(2);
	}
}

/// <summary>
/// Tests stepper heading levels with a deeper heading context (### before include).
/// </summary>
public class StepperInIncludeWithH3ContextTests(ITestOutputHelper output) : DirectiveTest<IncludeBlock>(output,
"""
## Main Heading

### Sub Heading

Some content under sub heading.

:::{include} _snippets/stepper-snippet.md
:::

## Another Section
"""
)
{
	protected override void AddToFileSystem(MockFileSystem fileSystem)
	{
		// The stepper in this snippet should get heading level 4 (one deeper than the ### heading before the include)
		var inclusion = """
:::::{stepper}

::::{step} Deep Step
Step content.
::::

:::::
""";
		fileSystem.AddFile(@"docs/_snippets/stepper-snippet.md", inclusion);
	}

	[Fact]
	public void ParsesBlock() => Block.Should().NotBeNull();

	[Fact]
	public void StepperStepsInSnippetInheritDeeperHeadingLevel()
	{
		var toc = File.PageTableOfContent.Values.ToList();

		// Should have: Main Heading (2), Sub Heading (3), Deep Step (4), Another Section (2)
		toc.Should().HaveCount(4);

		toc[0].Heading.Should().Be("Main Heading");
		toc[0].Level.Should().Be(2);

		toc[1].Heading.Should().Be("Sub Heading");
		toc[1].Level.Should().Be(3);

		// Key assertion: step should be level 4 (one deeper than ### Sub Heading)
		toc[2].Heading.Should().Be("Deep Step");
		toc[2].Level.Should().Be(4, "stepper step should be one level deeper than the ### heading before the include");

		toc[3].Heading.Should().Be("Another Section");
		toc[3].Level.Should().Be(2);
	}
}

/// <summary>
/// Tests stepper in snippet when there's no preceding heading (should default to h2).
/// </summary>
public class StepperInIncludeWithNoHeadingContextTests(ITestOutputHelper output) : DirectiveTest<IncludeBlock>(output,
"""
:::{include} _snippets/stepper-snippet.md
:::

## After Include
"""
)
{
	protected override void AddToFileSystem(MockFileSystem fileSystem)
	{
		var inclusion = """
:::::{stepper}

::::{step} First Step
No heading before this include.
::::

:::::
""";
		fileSystem.AddFile(@"docs/_snippets/stepper-snippet.md", inclusion);
	}

	[Fact]
	public void ParsesBlock() => Block.Should().NotBeNull();

	[Fact]
	public void StepperStepsDefaultToH2WhenNoHeadingContext()
	{
		var toc = File.PageTableOfContent.Values.ToList();

		// Should have: First Step (2), After Include (2)
		toc.Should().HaveCount(2);

		// With no preceding heading, stepper should default to level 2
		toc[0].Heading.Should().Be("First Step");
		toc[0].Level.Should().Be(2, "stepper step should default to h2 when no preceding heading");

		toc[1].Heading.Should().Be("After Include");
		toc[1].Level.Should().Be(2);
	}
}

/// <summary>
/// Tests that stepper steps in snippets respect their own snippet's heading structure
/// and are NOT adjusted when the snippet has its own preceding heading.
/// </summary>
public class StepperInSnippetWithOwnHeadingTests(ITestOutputHelper output) : DirectiveTest<IncludeBlock>(output,
"""
## Parent Heading

:::{include} _snippets/stepper-snippet.md
:::

## Another Parent Heading
"""
)
{
	protected override void AddToFileSystem(MockFileSystem fileSystem)
	{
		// The snippet has its own h2 heading before the stepper
		// The stepper step should be level 3 (based on snippet's own h2), NOT adjusted by parent
		var inclusion = """
## Snippet Heading

:::::{stepper}

::::{step} Step In Snippet
Step content.
::::

:::::
""";
		fileSystem.AddFile(@"docs/_snippets/stepper-snippet.md", inclusion);
	}

	[Fact]
	public void ParsesBlock() => Block.Should().NotBeNull();

	[Fact]
	public void StepperStepRespectsSnippetOwnHeadingStructure()
	{
		var toc = File.PageTableOfContent.Values.ToList();

		// Should have: Parent Heading (2), Snippet Heading (2), Step In Snippet (3), Another Parent Heading (2)
		toc.Should().HaveCount(4);

		toc[0].Heading.Should().Be("Parent Heading");
		toc[0].Level.Should().Be(2);

		// The snippet's own heading should be preserved
		toc[1].Heading.Should().Be("Snippet Heading");
		toc[1].Level.Should().Be(2, "snippet's own heading should remain level 2");

		// The stepper step should be level 3 based on snippet's own h2, NOT adjusted by parent
		// This verifies that when a snippet has its own heading structure, we don't override it
		toc[2].Heading.Should().Be("Step In Snippet");
		toc[2].Level.Should().Be(3, "stepper step should be level 3 based on snippet's own h2, not adjusted by parent");
		toc[2].IsStepperStep.Should().BeTrue();

		toc[3].Heading.Should().Be("Another Parent Heading");
		toc[3].Level.Should().Be(2);
	}
}

/// <summary>
/// Tests that stepper steps are capped at h6 even when preceding heading would push them deeper.
/// </summary>
public class StepperInSnippetWithH6CappingTests(ITestOutputHelper output) : DirectiveTest<IncludeBlock>(output,
"""
## H2
### H3
#### H4
##### H5
###### H6

:::{include} _snippets/stepper-snippet.md
:::
"""
)
{
	protected override void AddToFileSystem(MockFileSystem fileSystem)
	{
		var inclusion = """
:::::{stepper}

::::{step} Step After H6
Step content.
::::

:::::
""";
		fileSystem.AddFile(@"docs/_snippets/stepper-snippet.md", inclusion);
	}

	[Fact]
	public void ParsesBlock() => Block.Should().NotBeNull();

	[Fact]
	public void StepperStepIsCappedAtH6()
	{
		var toc = File.PageTableOfContent.Values.ToList();

		// Should have: H2 (2), H3 (3), H4 (4), H5 (5), H6 (6), Step After H6 (6)
		toc.Should().HaveCount(6);

		// Verify the step is capped at level 6, not level 7
		toc[5].Heading.Should().Be("Step After H6");
		toc[5].Level.Should().Be(6, "stepper step should be capped at h6 even when preceding heading is h6");
		toc[5].IsStepperStep.Should().BeTrue();
	}
}

/// <summary>
/// Tests multiple includes with different heading contexts to ensure each is adjusted independently.
/// </summary>
public class MultipleIncludesWithDifferentContextsTests(ITestOutputHelper output) : DirectiveTest<IncludeBlock>(output,
"""
## First Section

:::{include} _snippets/first.md
:::

### Subsection

:::{include} _snippets/second.md
:::

## Final Section
"""
)
{
	protected override void AddToFileSystem(MockFileSystem fileSystem)
	{
		var firstInclusion = """
:::::{stepper}

::::{step} First Step
First step content.
::::

:::::
""";
		fileSystem.AddFile(@"docs/_snippets/first.md", firstInclusion);

		var secondInclusion = """
:::::{stepper}

::::{step} Second Step
Second step content.
::::

:::::
""";
		fileSystem.AddFile(@"docs/_snippets/second.md", secondInclusion);
	}

	[Fact]
	public void ParsesBlock() => Block.Should().NotBeNull();

	[Fact]
	public void EachIncludeAdjustsBasedOnItsOwnContext()
	{
		var toc = File.PageTableOfContent.Values.ToList();

		// Should have: First Section (2), First Step (3), Subsection (3), Second Step (4), Final Section (2)
		toc.Should().HaveCount(5);

		toc[0].Heading.Should().Be("First Section");
		toc[0].Level.Should().Be(2);

		// First step should be level 3 (after h2)
		toc[1].Heading.Should().Be("First Step");
		toc[1].Level.Should().Be(3, "first step should be level 3 after h2");
		toc[1].IsStepperStep.Should().BeTrue();

		toc[2].Heading.Should().Be("Subsection");
		toc[2].Level.Should().Be(3);

		// Second step should be level 4 (after h3)
		toc[3].Heading.Should().Be("Second Step");
		toc[3].Level.Should().Be(4, "second step should be level 4 after h3");
		toc[3].IsStepperStep.Should().BeTrue();

		toc[4].Heading.Should().Be("Final Section");
		toc[4].Level.Should().Be(2);
	}
}

/// <summary>
/// Tests that stepper steps in the main document (not in snippets) correctly calculate
/// their heading levels based on preceding headings. This ensures our changes didn't break
/// the existing behavior for steppers in the main document.
/// </summary>
public class StepperInMainDocumentTests(ITestOutputHelper output) : DirectiveTest<StepperBlock>(output,
"""
## Main Heading

### Sub Heading

:::::{stepper}

::::{step} Step After H3
First step after h3 heading.
::::

::::{step} Another Step
Second step, should also be h4.
::::

:::::

## Another Main Heading

:::::{stepper}

::::{step} Step After H2
Step after h2 heading.
::::

:::::
"""
)
{
	[Fact]
	public void ParsesBlock() => Block.Should().NotBeNull();

	[Fact]
	public void StepperStepsInMainDocumentCalculateCorrectHeadingLevels()
	{
		var toc = File.PageTableOfContent.Values.ToList();

		// Should have: Main Heading (2), Sub Heading (3), Step After H3 (4), Another Step (4),
		//              Another Main Heading (2), Step After H2 (3)
		toc.Should().HaveCount(6);

		toc[0].Heading.Should().Be("Main Heading");
		toc[0].Level.Should().Be(2);

		toc[1].Heading.Should().Be("Sub Heading");
		toc[1].Level.Should().Be(3);

		// Stepper steps after h3 should be h4
		toc[2].Heading.Should().Be("Step After H3");
		toc[2].Level.Should().Be(4, "stepper step after h3 should be h4");
		toc[2].IsStepperStep.Should().BeTrue();

		toc[3].Heading.Should().Be("Another Step");
		toc[3].Level.Should().Be(4, "stepper step after h3 should be h4");
		toc[3].IsStepperStep.Should().BeTrue();

		toc[4].Heading.Should().Be("Another Main Heading");
		toc[4].Level.Should().Be(2);

		// Stepper step after h2 should be h3
		toc[5].Heading.Should().Be("Step After H2");
		toc[5].Level.Should().Be(3, "stepper step after h2 should be h3");
		toc[5].IsStepperStep.Should().BeTrue();
	}
}

/// <summary>
/// Tests stepper steps at the beginning of a document (no preceding heading).
/// </summary>
public class StepperAtDocumentStartTests(ITestOutputHelper output) : DirectiveTest<StepperBlock>(output,
"""
:::::{stepper}

::::{step} First Step
Step at the very beginning of the document.
::::

::::{step} Second Step
Another step at the beginning.
::::

:::::

## First Heading
"""
)
{
	[Fact]
	public void ParsesBlock() => Block.Should().NotBeNull();

	[Fact]
	public void StepperStepsAtDocumentStartDefaultToH2()
	{
		var toc = File.PageTableOfContent.Values.ToList();

		// Should have: First Step (2), Second Step (2), First Heading (2)
		toc.Should().HaveCount(3);

		// With no preceding heading, stepper steps should default to h2
		toc[0].Heading.Should().Be("First Step");
		toc[0].Level.Should().Be(2, "stepper step at document start should default to h2");
		toc[0].IsStepperStep.Should().BeTrue();

		toc[1].Heading.Should().Be("Second Step");
		toc[1].Level.Should().Be(2, "stepper step at document start should default to h2");
		toc[1].IsStepperStep.Should().BeTrue();

		toc[2].Heading.Should().Be("First Heading");
		toc[2].Level.Should().Be(2);
	}
}
