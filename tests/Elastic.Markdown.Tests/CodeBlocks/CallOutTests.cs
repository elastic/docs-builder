// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.Myst.CodeBlocks;
using Elastic.Markdown.Tests.Inline;
using FluentAssertions;
using JetBrains.Annotations;
using Xunit.Abstractions;

namespace Elastic.Markdown.Tests.CodeBlocks;

public abstract class CodeBlockCallOutTests(
	ITestOutputHelper output,
	string language,
	[LanguageInjection("csharp")] string code,
	[LanguageInjection("markdown")] string? markdown = null
)
	: BlockTest<EnhancedCodeBlock>(output,
$$"""
```{{language}}
{{code}}
```
{{markdown}}
"""
)
{
	[Fact]
	public void ParsesAdmonitionBlock() => Block.Should().NotBeNull();

	[Fact]
	public void SetsLanguage() => Block!.Language.Should().Be("csharp");

}

public class MagicCalOuts(ITestOutputHelper output) : CodeBlockCallOutTests(output, "csharp",
"""
var x = 1; // this is a callout
//this is not a callout
var y = x - 2;
var z = y - 2; // another callout
"""
	)
{
	[Fact]
	public void ParsesMagicCallOuts() => Block!.CallOuts
		.Should().NotBeNullOrEmpty()
		.And.HaveCount(2)
		.And.NotContain(c => c.Text.Contains("not a callout"));

	[Fact]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}

public class ClassicCallOutsRequiresContent(ITestOutputHelper output) : CodeBlockCallOutTests(output, "csharp",
"""
var x = 1; <1>
var y = x - 2;
var z = y - 2; <2>
"""
	)
{
	[Fact]
	public void ParsesMagicCallOuts() => Block!.CallOuts
		.Should().NotBeNullOrEmpty()
		.And.HaveCount(2)
		.And.OnlyContain(c => c.Text.StartsWith("<"));

	[Fact]
	public void RequiresContentToFollow() => Collector.Diagnostics.Should().HaveCount(1)
		.And.OnlyContain(c => c.Message.StartsWith("Code block with annotations is not followed by any content"));
}

public class ClassicCallOutsNotFollowedByList(ITestOutputHelper output) : CodeBlockCallOutTests(output, "csharp",
"""
var x = 1; <1>
var y = x - 2;
var z = y - 2; <2>
""",
"""
## hello world
"""

	)
{
	[Fact]
	public void ParsesMagicCallOuts() => Block!.CallOuts
		.Should().NotBeNullOrEmpty()
		.And.HaveCount(2)
		.And.OnlyContain(c => c.Text.StartsWith("<"));

	[Fact]
	public void RequiresContentToFollow() => Collector.Diagnostics.Should().HaveCount(1)
		.And.OnlyContain(c => c.Message.StartsWith("Code block with annotations is not followed by a list"));
}

public class ClassicCallOutsFollowedByListWithWrongCoung(ITestOutputHelper output) : CodeBlockCallOutTests(output, "csharp",
"""
var x = 1; <1>
var y = x - 2;
var z = y - 2; <2>
""",
"""
1. Only marking the first callout
"""

	)
{
	[Fact]
	public void ParsesMagicCallOuts() => Block!.CallOuts
		.Should().NotBeNullOrEmpty()
		.And.HaveCount(2)
		.And.OnlyContain(c => c.Text.StartsWith("<"));

	[Fact]
	public void RequiresContentToFollow() => Collector.Diagnostics.Should().HaveCount(1)
		.And.OnlyContain(c => c.Message.StartsWith("Code block has 2 callouts but the following list only has 1"));
}

public class ClassicCallOutWithTheRightListItems(ITestOutputHelper output) : CodeBlockCallOutTests(output, "csharp",
"""
var x = 1; <1>
var y = x - 2;
var z = y - 2; <2>
""",
"""
1. First callout
2. Second callout
"""

	)
{
	[Fact]
	public void ParsesMagicCallOuts() => Block!.CallOuts
		.Should().NotBeNullOrEmpty()
		.And.HaveCount(2)
		.And.OnlyContain(c => c.Text.StartsWith("<"));

	[Fact]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}
