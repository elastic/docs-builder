// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Markdown.Myst.CodeBlocks;
using Elastic.Markdown.Tests.Inline;

namespace Elastic.Markdown.Tests.CodeBlocks;

[InheritsTests]
public abstract class CodeBlockTests(string directive, string? language = null) : BlockTest<EnhancedCodeBlock>(
	$$"""
```{{directive}} {{language}}
var x = 1;
```
A regular paragraph.
"""
)
{
	[Test]
	public void ParsesAdmonitionBlock() => Block.Should().NotBeNull();
}

[InheritsTests]
public class CodeBlockDirectiveTests() : CodeBlockTests("{code-block}", "csharp")
{
	[Test]
	public void SetsLanguage() => Block!.Language.Should().Be("csharp");
}

[InheritsTests]
public class CodeTests() : CodeBlockTests("{code}", "python")
{
	[Test]
	public void SetsLanguage() => Block!.Language.Should().Be("python");
}

[InheritsTests]
public class SourceCodeTests() : CodeBlockTests("{sourcecode}", "java")
{
	[Test]
	public void SetsLanguage() => Block!.Language.Should().Be("java");
}

[InheritsTests]
public class RawMarkdownCodeBlockTests() : CodeBlockTests("javascript")
{
	[Test]
	public void SetsLanguage() => Block!.Language.Should().Be("javascript");
}
