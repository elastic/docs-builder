// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using FluentAssertions;
using JetBrains.Annotations;

namespace Elastic.Markdown.Tests.Inline;

public class FootnotesBasicTests(ITestOutputHelper output) : InlineTest(output,
	// language=markdown
	"""
	Here's a simple footnote[^1] and another[^2].

	[^1]: This is the first footnote.
	[^2]: This is the second footnote.
	""")
{
	[Fact]
	public void ContainsFootnoteReferences()
	{
		Html.Should().Contain("footnote-ref");
		Html.Should().Contain("href=\"#fn:1\"");
		Html.Should().Contain("href=\"#fn:2\"");
	}

	[Fact]
	public void ContainsFootnoteContainer()
	{
		Html.Should().Contain("class=\"footnotes\"");
	}

	[Fact]
	public void ContainsFootnoteDefinitions()
	{
		Html.Should().Contain("id=\"fn:1\"");
		Html.Should().Contain("id=\"fn:2\"");
		Html.Should().Contain("This is the first footnote.");
		Html.Should().Contain("This is the second footnote.");
	}

	[Fact]
	public void ContainsBackReferences()
	{
		Html.Should().Contain("footnote-back-ref");
		Html.Should().Contain("href=\"#fnref:1\"");
		Html.Should().Contain("href=\"#fnref:2\"");
	}
}

public partial class FootnotesMultipleReferencesTests(ITestOutputHelper output) : InlineTest(output,
	// language=markdown
	"""
	First reference[^1] and second reference[^1].

	[^1]: This footnote is referenced twice.
	""")
{
	[Fact]
	public void ContainsMultipleReferencesToSameFootnote()
	{
		Html.Should().Contain("href=\"#fn:1\"");
		// Should have at least 2 occurrences
		var count = MyRegex().Count(Html);
		count.Should().BeGreaterThanOrEqualTo(2);
	}

	[Fact]
	public void ContainsMultipleBackReferences()
	{
		// Should have references back to both instances
		Html.Should().Contain("href=\"#fnref:1\"");
		Html.Should().Contain("href=\"#fnref:2\"");
	}

	[System.Text.RegularExpressions.GeneratedRegex("href=\"#fn:1\"")]
	private static partial System.Text.RegularExpressions.Regex MyRegex();
}

public class FootnotesComplexContentTests(ITestOutputHelper output) : InlineTest(output,
	// language=markdown
	"""
	Here's a complex footnote[^complex].

	[^complex]: This footnote has multiple elements.

	    It has multiple paragraphs.

	    > And even a blockquote.

	    - List item 1
	    - List item 2
	""")
{
	[Fact]
	public void ContainsComplexFootnoteStructure()
	{
		Html.Should().Contain("href=\"#fn:1\"");
		Html.Should().Contain("This footnote has multiple elements.");
		Html.Should().Contain("It has multiple paragraphs.");
	}

	[Fact]
	public void ContainsBlockquoteInFootnote()
	{
		Html.Should().Contain("blockquote");
		Html.Should().Contain("And even a blockquote.");
	}

	[Fact]
	public void ContainsListInFootnote()
	{
		Html.Should().Contain("List item 1");
		Html.Should().Contain("List item 2");
	}
}

public class FootnotesWithCodeTests(ITestOutputHelper output) : InlineTest(output,
	// language=markdown
	"""
	See the code example[^code].

	[^code]: Example code:

	    ```python
	    def hello():
	        print("Hello, world!")
	    ```
	""")
{
	[Fact]
	public void ContainsCodeBlockInFootnote()
	{
		Html.Should().Contain("Example code:");
		Html.Should().Contain("hello");
	}
}

public class FootnotesConsecutiveDefinitionsTests(ITestOutputHelper output) : InlineTest(output,
	// language=markdown
	"""
	First[^1], second[^2], third[^3].

	[^1]: First footnote.
	[^2]: Second footnote.
	[^3]: Third footnote.
	""")
{
	[Fact]
	public void HandlesConsecutiveFootnoteDefinitions()
	{
		Html.Should().Contain("First footnote.");
		Html.Should().Contain("Second footnote.");
		Html.Should().Contain("Third footnote.");
	}

	[Fact]
	public void AllFootnoteReferencesAreLinked()
	{
		Html.Should().Contain("href=\"#fn:1\"");
		Html.Should().Contain("href=\"#fn:2\"");
		Html.Should().Contain("href=\"#fn:3\"");
	}
}

public class FootnotesInListTests(ITestOutputHelper output) : InlineTest(output,
	// language=markdown
	"""
	- Item one
	- Item two with footnote[^list]

	[^list]: Footnote from list item.
	""")
{
	[Fact]
	public void FootnoteWorksInListItem()
	{
		Html.Should().Contain("href=\"#fn:1\"");
		Html.Should().Contain("Footnote from list item.");
	}
}

public class FootnotesWithNamedReferencesTests(ITestOutputHelper output) : InlineTest(output,
	// language=markdown
	"""
	Named reference[^my-footnote].

	[^my-footnote]: This uses a named identifier.
	""")
{
	[Fact]
	public void HandlesNamedFootnoteIdentifiers()
	{
		Html.Should().Contain("footnote-ref");
		Html.Should().Contain("This uses a named identifier.");
	}
}

