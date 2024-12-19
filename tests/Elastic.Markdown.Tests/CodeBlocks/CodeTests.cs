// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.Myst.CodeBlocks;
using Elastic.Markdown.Tests.Inline;
using FluentAssertions;
using Xunit.Abstractions;

namespace Elastic.Markdown.Tests.CodeBlocks;

public abstract class CodeBlockTests(ITestOutputHelper output, string directive, string? language = null)
	: BlockTest<EnhancedCodeBlock>(output,
$$"""
```{{directive}} {{language}}
var x = 1;
```
A regular paragraph.
"""
)
{
	[Fact]
	public void ParsesAdmonitionBlock() => Block.Should().NotBeNull();
}

public class CodeBlockDirectiveTests(ITestOutputHelper output) : CodeBlockTests(output, "{code-block}", "csharp")
{
	[Fact]
	public void SetsLanguage() => Block!.Language.Should().Be("csharp");
}

public class CodeTests(ITestOutputHelper output) : CodeBlockTests(output, "{code}", "python")
{
	[Fact]
	public void SetsLanguage() => Block!.Language.Should().Be("python");
}

public class SourceCodeTests(ITestOutputHelper output) : CodeBlockTests(output, "{sourcecode}", "java")
{
	[Fact]
	public void SetsLanguage() => Block!.Language.Should().Be("java");
}

public class RawMarkdownCodeBlockTests(ITestOutputHelper output) : CodeBlockTests(output, "javascript")
{
	[Fact]
	public void SetsLanguage() => Block!.Language.Should().Be("javascript");
}

