// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
using Elastic.Markdown.Myst.Directives;
using FluentAssertions;
using Xunit.Abstractions;

namespace Elastic.Markdown.Tests.Directives;

public abstract class AdmonitionTests(ITestOutputHelper output, string directive) : DirectiveTest<AdmonitionBlock>(output,
$$"""
```{{{directive}}}
This is an attention block
```
A regular paragraph.
"""
)
{
	[Fact]
	public void ParsesAdmonitionBlock() => Block.Should().NotBeNull();

	[Fact]
	public void SetsCorrectAdmonitionType() => Block!.Admonition.Should().Be(directive);
}

public class AttentionTests(ITestOutputHelper output) : AdmonitionTests(output, "attention")
{
	[Fact]
	public void SetsTitle() => Block!.Title.Should().Be("Attention");
}
public class CautionTests(ITestOutputHelper output) : AdmonitionTests(output, "caution")
{
	[Fact]
	public void SetsTitle() => Block!.Title.Should().Be("Caution");
}
public class DangerTests(ITestOutputHelper output) : AdmonitionTests(output, "danger")
{
	[Fact]
	public void SetsTitle() => Block!.Title.Should().Be("Danger");
}
public class ErrorTests(ITestOutputHelper output) : AdmonitionTests(output, "error")
{
	[Fact]
	public void SetsTitle() => Block!.Title.Should().Be("Error");
}
public class HintTests(ITestOutputHelper output) : AdmonitionTests(output, "hint")
{
	[Fact]
	public void SetsTitle() => Block!.Title.Should().Be("Hint");
}
public class ImportantTests(ITestOutputHelper output) : AdmonitionTests(output, "important")
{
	[Fact]
	public void SetsTitle() => Block!.Title.Should().Be("Important");
}
public class NoteTests(ITestOutputHelper output) : AdmonitionTests(output, "note")
{
	[Fact]
	public void SetsTitle() => Block!.Title.Should().Be("Note");
}
public class SeeAlsoTests(ITestOutputHelper output) : AdmonitionTests(output, "seealso")
{
	[Fact]
	public void SetsTitle() => Block!.Title.Should().Be("See Also");
}
public class TipTests(ITestOutputHelper output) : AdmonitionTests(output, "tip")
{
	[Fact]
	public void SetsTitle() => Block!.Title.Should().Be("Tip");
}

public class NoteTitleTests(ITestOutputHelper output) : DirectiveTest<AdmonitionBlock>(output,
"""
```{note} This is my custom note
This is an attention block
```
A regular paragraph.
"""
)
{
	[Fact]
	public void SetsCorrectAdmonitionType() => Block!.Admonition.Should().Be("note");

	[Fact]
	public void SetsCustomTitle() => Block!.Title.Should().Be("Note This is my custom note");
}

public class AdmonitionTitleTests(ITestOutputHelper output) : DirectiveTest<AdmonitionBlock>(output,
"""
```{admonition} This is my custom note
This is an attention block
```
A regular paragraph.
"""
)
{
	[Fact]
	public void SetsCorrectAdmonitionType() => Block!.Admonition.Should().Be("note");

	[Fact]
	public void SetsCustomTitle() => Block!.Title.Should().Be("This is my custom note");
}


public class DropdownTitleTests(ITestOutputHelper output) : DirectiveTest<AdmonitionBlock>(output,
"""
```{dropdown} This is my custom dropdown
:open:
This is an attention block
```
A regular paragraph.
"""
)
{
	[Fact]
	public void SetsCorrectAdmonitionType() => Block!.Admonition.Should().Be("dropdown");

	[Fact]
	public void SetsCustomTitle() => Block!.Title.Should().Be("This is my custom dropdown");

	[Fact]
	public void SetsDropdownOpen() => Block!.DropdownOpen.Should().BeTrue();
}

