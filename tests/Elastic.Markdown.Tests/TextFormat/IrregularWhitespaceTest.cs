// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Linq;
using Elastic.Documentation.Diagnostics;
using Elastic.Markdown.Tests.Inline;
using FluentAssertions;

namespace Elastic.Markdown.Tests.TextFormat;

public class IrregularWhitespaceTest(ITestOutputHelper output) : InlineTest(output,
"""
# Heading with no-break space\u00A0character

This is a paragraph with some\u2002en space and\u200Bzero width space.

## Subheading with\u3000ideographic space

* List item with\u00A0no-break space
* Another item

```csharp
// Code with\u00A0no-break space
var x = 1;
```

> Blockquote with\u2003em space
"""
)
{
	[Fact]
	public void DetectsIrregularWhitespaceInHeading()
	{
		var diagnostics = Collector.Diagnostics
			.Where(d => d.Line == 1)
			.Where(d => d.Message.Contains("U+00A0"))
			.ToList();

		diagnostics.Should().HaveCount(1);
		diagnostics[0].Severity.Should().Be(Severity.Warning);
	}

	[Fact]
	public void DetectsIrregularWhitespaceInParagraph()
	{
		var diagnostics = Collector.Diagnostics
			.Where(d => d.Line == 3)
			.Where(d => d.Message.Contains("irregular whitespace"))
			.ToList();

		diagnostics.Should().HaveCountGreaterThanOrEqualTo(2);

		// Verify en space detection
		diagnostics.Should().Contain(d => d.Message.Contains("U+2002"));

		// Verify zero width space detection
		diagnostics.Should().Contain(d => d.Message.Contains("U+200B"));
	}

	[Fact]
	public void DetectsIrregularWhitespaceInSubheading()
	{
		var diagnostics = Collector.Diagnostics
			.Where(d => d.Line == 5)
			.Where(d => d.Message.Contains("U+3000"))
			.ToList();

		diagnostics.Should().HaveCount(1);
		diagnostics[0].Severity.Should().Be(Severity.Warning);
	}

	[Fact]
	public void DetectsIrregularWhitespaceInListItem()
	{
		var diagnostics = Collector.Diagnostics
			.Where(d => d.Line == 7)
			.Where(d => d.Message.Contains("U+00A0"))
			.ToList();

		diagnostics.Should().HaveCount(1);
		diagnostics[0].Severity.Should().Be(Severity.Warning);
	}

	[Fact]
	public void DetectsIrregularWhitespaceInCodeBlock()
	{
		var diagnostics = Collector.Diagnostics
			.Where(d => d.Line == 11)
			.Where(d => d.Message.Contains("U+00A0"))
			.ToList();

		diagnostics.Should().HaveCount(1);
		diagnostics[0].Severity.Should().Be(Severity.Warning);
	}

	[Fact]
	public void DetectsIrregularWhitespaceInBlockquote()
	{
		var diagnostics = Collector.Diagnostics
			.Where(d => d.Line == 15)
			.Where(d => d.Message.Contains("U+2003"))
			.ToList();

		diagnostics.Should().HaveCount(1);
		diagnostics[0].Severity.Should().Be(Severity.Warning);
	}

	[Fact]
	public void GeneratesProperWarningMessage()
	{
		var noBreakSpaceWarning = Collector.Diagnostics
			.FirstOrDefault(d => d.Message.Contains("U+00A0"));

		noBreakSpaceWarning.Should().NotBeNull();
		noBreakSpaceWarning!.Message.Should()
			.Contain("Irregular whitespace character detected: U+00A0 (No-Break Space (NBSP))")
			.And.Contain("may impair Markdown rendering");
	}
}
