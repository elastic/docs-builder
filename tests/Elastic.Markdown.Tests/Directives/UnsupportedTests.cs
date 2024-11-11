// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.Diagnostics;
using Elastic.Markdown.Myst.Directives;
using FluentAssertions;
using Xunit.Abstractions;

namespace Elastic.Markdown.Tests.Directives;

public abstract class UnsupportedDirectiveTests(ITestOutputHelper output, string directive)
	: DirectiveTest<UnsupportedDirectiveBlock>(output,
$$"""
Content before bad directive

```{{{directive}}}
Version brief summary
```
A regular paragraph.
"""
)
{
	[Fact]
	public void ParsesAdmonitionBlock() => Block.Should().NotBeNull();

	[Fact]
	public void SetsCorrectDirectiveType() => Block!.Directive.Should().Be(directive);

	[Fact]
	public void TracksASingleWarning() => Collector.Warnings.Should().Be(1);

	[Fact]
	public void EmitsUnsupportedWarnings()
	{
		Collector.Diagnostics.Should().NotBeNullOrEmpty()
			.And.HaveCount(1);
		Collector.Diagnostics.Should().OnlyContain(d => d.Severity == Severity.Warning);
		Collector.Diagnostics.Should()
			.OnlyContain(d => d.Message.StartsWith($"Directive block '{directive}' is unsupported."));
	}
}

public class BibliographyDirectiveTests(ITestOutputHelper output) : UnsupportedDirectiveTests(output, "bibliography");
public class BlockQuoteDirectiveTests(ITestOutputHelper output) : UnsupportedDirectiveTests(output, "blockquote");
public class FrameDirectiveTests(ITestOutputHelper output) : UnsupportedDirectiveTests(output, "iframe");
public class CsvTableDirectiveTests(ITestOutputHelper output) : UnsupportedDirectiveTests(output, "csv-table");
public class MystDirectiveDirectiveTests(ITestOutputHelper output) : UnsupportedDirectiveTests(output, "myst");
public class TopicDirectiveTests(ITestOutputHelper output) : UnsupportedDirectiveTests(output, "topic");
public class ExerciseDirectiveTest(ITestOutputHelper output) : UnsupportedDirectiveTests(output, "exercise");
public class SolutionDirectiveTests(ITestOutputHelper output) : UnsupportedDirectiveTests(output, "solution");
public class TocTreeDirectiveTests(ITestOutputHelper output) : UnsupportedDirectiveTests(output, "solution");
