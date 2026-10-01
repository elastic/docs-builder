// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Diagnostics;
using Elastic.Markdown.Myst.Directives;

namespace Elastic.Markdown.Tests.Directives;

[InheritsTests]
public abstract class UnsupportedDirectiveTests(string directive) : DirectiveTest<UnsupportedDirectiveBlock>(
	$$"""
Content before bad directive

```{{{directive}}}
Version brief summary
```
A regular paragraph.
"""
)
{
	[Test]
	public void ParsesAdmonitionBlock() => Block.Should().NotBeNull();

	[Test]
	public void SetsCorrectDirectiveType() => Block!.Directive.Should().Be(directive);

	[Test]
	public void TracksASingleWarning() => Collector.Warnings.Should().Be(1);

	[Test]
	public void EmitsUnsupportedWarnings()
	{
		Collector.Diagnostics.Should().NotBeNullOrEmpty().And.HaveCount(1);
		Collector.Diagnostics.Should().OnlyContain(d => d.Severity == Severity.Warning);
		Collector.Diagnostics.Should().OnlyContain(d => d.Message.StartsWith($"Directive block '{directive}' is unsupported."));
	}
}

[InheritsTests]
public class BibliographyDirectiveTests() : UnsupportedDirectiveTests("bibliography");

[InheritsTests]
public class BlockQuoteDirectiveTests() : UnsupportedDirectiveTests("blockquote");

[InheritsTests]
public class FrameDirectiveTests() : UnsupportedDirectiveTests("iframe");

[InheritsTests]
public class CsvTableDirectiveTests() : UnsupportedDirectiveTests("csv-table");

[InheritsTests]
public class MystDirectiveDirectiveTests() : UnsupportedDirectiveTests("myst");

[InheritsTests]
public class TopicDirectiveTests() : UnsupportedDirectiveTests("topic");

[InheritsTests]
public class ExerciseDirectiveTest() : UnsupportedDirectiveTests("exercise");

[InheritsTests]
public class SolutionDirectiveTests() : UnsupportedDirectiveTests("solution");

[InheritsTests]
public class TocTreeDirectiveTests() : UnsupportedDirectiveTests("solution");
