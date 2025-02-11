// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
using Elastic.Markdown.Myst.Directives;
using FluentAssertions;

namespace Elastic.Markdown.Tests.Directives;

public abstract class AdmonitionBaseTests(ITestOutputHelper output, string directive) : DirectiveTest<AdmonitionBlock>(output,
$$"""
:::{{{directive}}}
This is an attention block
:::
A regular paragraph.
"""
)
{
	[Fact]
	public void ParsesAdmonitionBlock() => Block.Should().NotBeNull();

	[Fact]
	public void SetsCorrectAdmonitionType() => Block!.Admonition.Should().Be(directive);
}

public class WarningTests(ITestOutputHelper output) : AdmonitionBaseTests(output, "warning")
{
	[Fact]
	public void SetsTitle() => Block!.Title.Should().Be("Warning");
}

public class NoteTests(ITestOutputHelper output) : AdmonitionBaseTests(output, "note")
{
	[Fact]
	public void SetsTitle() => Block!.Title.Should().Be("Note");
}

public class TipTests(ITestOutputHelper output) : AdmonitionBaseTests(output, "tip")
{
	[Fact]
	public void SetsTitle() => Block!.Title.Should().Be("Tip");
}

public class ImportantTests(ITestOutputHelper output) : AdmonitionBaseTests(output, "important")
{
	[Fact]
	public void SetsTitle() => Block!.Title.Should().Be("Important");
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
```{admonition} This is my custom title
This is an attention block
```
A regular paragraph.
"""
)
{
	[Fact]
	public void SetsCorrectAdmonitionType() => Block!.Admonition.Should().Be("admonition");

	[Fact]
	public void SetsCustomTitle() => Block!.Title.Should().Be("This is my custom title");
}

public class DropdownTitleTests(ITestOutputHelper output) : DirectiveTest<AdmonitionBlock>(output,
"""
:::{dropdown} This is my custom dropdown
:open:
This is an attention block
:::
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


public class NestedDirectiveWithListTests(ITestOutputHelper output) : DirectiveTest<AdmonitionBlock>(output,
	"""
	# heading

	:::::{note}

	- List Item 1
	  ::::{note}
	  Hello, World!
	  ::::

	## What

	:::::
	"""
)
{
	[Fact]
	public void Render() => Html.Should().Contain("""
	                                              <li>List Item 1
	                                              <div class="admonition note">
	                                              	<p class="admonition-title">Note</p>
	                                              	Hello, World!
	                                              </div>
	                                              </li>
	                                              """);
}


public class NestedDirectiveWithListTests2(ITestOutputHelper output) : DirectiveTest<AdmonitionBlock>(output,
	"""
	# heading

	:::{note}

	- List Item 1
	  :::{note}
	  Hello, World!
	  :::

	## What

	:::
	"""
)
{
	[Fact]
	public void Render() => Html.Should().Contain("""
	                                              <li>List Item 1
	                                              <div class="admonition note">
	                                              	<p class="admonition-title">Note</p>
	                                              	Hello, World!
	                                              </div>
	                                              </li>
	                                              """);
}

public class NestedDirectiveWithListTests3(ITestOutputHelper output) : DirectiveTest<AdmonitionBlock>(output,
	"""
	# heading

	:::{note}

	- List Item 1
	  :::::{note}
	  Hello, World!
	  :::::

	## What

	:::
	"""
)
{
	[Fact]
	public void Render() => Html.Should().Contain("""
	                                              <li>List Item 1
	                                              <div class="admonition note">
	                                              	<p class="admonition-title">Note</p>
	                                              	Hello, World!
	                                              </div>
	                                              </li>
	                                              """);
}


public class DirectiveInList(ITestOutputHelper output) : DirectiveTest<AdmonitionBlock>(output,
	"""
	# heading

	- List Item 1
	  :::::{note}
	  Hello, World!
	  :::::
	"""
)
{
	[Fact]
	public void Type() => Block!.Admonition.Should().Be("note");

	[Fact]
	public void Render() => Html.Should().Contain("""
	                                              <li>List Item 1
	                                              <div class="admonition note">
	                                              	<p class="admonition-title">Note</p>
	                                              	Hello, World!
	                                              </div>
	                                              </li>
	                                              """);
}
