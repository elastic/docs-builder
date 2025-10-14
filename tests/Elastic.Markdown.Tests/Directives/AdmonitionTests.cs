// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.Myst.Directives.Admonition;
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

public class DropdownAppliesToTests(ITestOutputHelper output) : DirectiveTest<AdmonitionBlock>(output,
"""
:::{dropdown} This is my custom dropdown
:applies_to: stack: ga 9.0
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
	public void SetsAppliesToDefinition() => Block!.AppliesToDefinition.Should().Be("stack: ga 9.0");

	[Fact]
	public void ParsesAppliesTo() => Block!.AppliesTo.Should().NotBeNull();
}

public class DropdownPropertyParsingTests(ITestOutputHelper output) : DirectiveTest<AdmonitionBlock>(output,
"""
:::{dropdown} Test Dropdown
:open:
:name: test-dropdown
This is test content
:::
A regular paragraph.
"""
)
{
	[Fact]
	public void SetsCorrectAdmonitionType() => Block!.Admonition.Should().Be("dropdown");

	[Fact]
	public void SetsCustomTitle() => Block!.Title.Should().Be("Test Dropdown");

	[Fact]
	public void SetsDropdownOpen() => Block!.DropdownOpen.Should().BeTrue();

	[Fact]
	public void SetsCrossReferenceName() => Block!.CrossReferenceName.Should().Be("test-dropdown");
}

public class DropdownNestedContentTests(ITestOutputHelper output) : DirectiveTest<AdmonitionBlock>(output,
"""
::::{dropdown} Nested Content Test
:open:
This dropdown contains nested content with colons:

- Time: 10:30 AM
- URL: https://example.com:8080/path
- Configuration: key:value pairs
- Code: `function test() { return "hello:world"; }`

And even nested directives:

:::{note} Nested Note
This is a nested note with colons: 10:30 AM
:::

More content after nested directive.
::::
A regular paragraph.
"""
)
{
	private readonly ITestOutputHelper _output = output;

	[Fact]
	public void SetsCorrectAdmonitionType() => Block!.Admonition.Should().Be("dropdown");

	[Fact]
	public void SetsCustomTitle() => Block!.Title.Should().Be("Nested Content Test");

	[Fact]
	public void SetsDropdownOpen() => Block!.DropdownOpen.Should().BeTrue();

	[Fact]
	public void ContainsContentWithColons()
	{
		var html = Html;
		html.Should().Contain("Time: 10:30 AM");
		html.Should().Contain("URL: https://example.com:8080/path");
		html.Should().Contain("Configuration: key:value pairs");
		html.Should().Contain("function test() { return &quot;hello:world&quot;; }");
	}

	[Fact]
	public void ContainsNestedDirective()
	{
		var html = Html;
		// Output the full HTML for inspection
		_output.WriteLine("Generated HTML:");
		_output.WriteLine(html);

		html.Should().Contain("Nested Note");
		html.Should().Contain("This is a nested note with colons: 10:30 AM");
		// Verify the nested note was actually parsed as a directive, not just plain text
		html.Should().Contain("class=\"admonition note\"");
		html.Should().Contain("admonition-title");
		html.Should().Contain("admonition-content");
	}

	[Fact]
	public void ContainsContentAfterNestedDirective()
	{
		var html = Html;
		html.Should().Contain("More content after nested directive");
	}
}

public class DropdownComplexPropertyTests(ITestOutputHelper output) : DirectiveTest<AdmonitionBlock>(output,
"""
:::{dropdown} Complex Properties Test
:applies_to: stack: ga 9.0
This is content with applies_to property
:::
A regular paragraph.
"""
)
{
	[Fact]
	public void SetsCorrectAdmonitionType() => Block!.Admonition.Should().Be("dropdown");

	[Fact]
	public void SetsCustomTitle() => Block!.Title.Should().Be("Complex Properties Test");

	[Fact]
	public void ParsesAppliesToWithComplexValue()
	{
		Block!.AppliesToDefinition.Should().Be("stack: ga 9.0");
		Block!.AppliesTo.Should().NotBeNull();
	}
}

public class NoteAppliesToTests(ITestOutputHelper output) : DirectiveTest<AdmonitionBlock>(output,
"""
:::{note}
:applies_to: stack: ga
This is a note with applies_to information
:::
A regular paragraph.
"""
)
{
	[Fact]
	public void SetsCorrectAdmonitionType() => Block!.Admonition.Should().Be("note");

	[Fact]
	public void SetsTitle() => Block!.Title.Should().Be("Note");

	[Fact]
	public void SetsAppliesToDefinition() => Block!.AppliesToDefinition.Should().Be("stack: ga");

	[Fact]
	public void ParsesAppliesTo() => Block!.AppliesTo.Should().NotBeNull();

	[Fact]
	public void RendersAppliesToInHtml()
	{
		var html = Html;
		html.Should().Contain("applies applies-admonition");
		html.Should().Contain("admonition-title__separator");
		html.Should().Contain("applicable-info");
	}
}

public class WarningAppliesToTests(ITestOutputHelper output) : DirectiveTest<AdmonitionBlock>(output,
"""
:::{warning}
:applies_to: stack: ga
This is a warning with applies_to information
:::
A regular paragraph.
"""
)
{
	[Fact]
	public void SetsCorrectAdmonitionType() => Block!.Admonition.Should().Be("warning");

	[Fact]
	public void SetsTitle() => Block!.Title.Should().Be("Warning");

	[Fact]
	public void SetsAppliesToDefinition() => Block!.AppliesToDefinition.Should().Be("stack: ga");

	[Fact]
	public void ParsesAppliesTo() => Block!.AppliesTo.Should().NotBeNull();

	[Fact]
	public void RendersAppliesToInHtml()
	{
		var html = Html;
		html.Should().Contain("applies applies-admonition");
		html.Should().Contain("admonition-title__separator");
		html.Should().Contain("applicable-info");
	}
}

public class TipAppliesToTests(ITestOutputHelper output) : DirectiveTest<AdmonitionBlock>(output,
"""
:::{tip}
:applies_to: stack: ga
This is a tip with applies_to information
:::
A regular paragraph.
"""
)
{
	[Fact]
	public void SetsCorrectAdmonitionType() => Block!.Admonition.Should().Be("tip");

	[Fact]
	public void SetsTitle() => Block!.Title.Should().Be("Tip");

	[Fact]
	public void SetsAppliesToDefinition() => Block!.AppliesToDefinition.Should().Be("stack: ga");

	[Fact]
	public void ParsesAppliesTo() => Block!.AppliesTo.Should().NotBeNull();

	[Fact]
	public void RendersAppliesToInHtml()
	{
		var html = Html;
		html.Should().Contain("applies applies-admonition");
		html.Should().Contain("admonition-title__separator");
		html.Should().Contain("applicable-info");
	}
}

public class ImportantAppliesToTests(ITestOutputHelper output) : DirectiveTest<AdmonitionBlock>(output,
"""
:::{important}
:applies_to: stack: ga
This is an important notice with applies_to information
:::
A regular paragraph.
"""
)
{
	[Fact]
	public void SetsCorrectAdmonitionType() => Block!.Admonition.Should().Be("important");

	[Fact]
	public void SetsTitle() => Block!.Title.Should().Be("Important");

	[Fact]
	public void SetsAppliesToDefinition() => Block!.AppliesToDefinition.Should().Be("stack: ga");

	[Fact]
	public void ParsesAppliesTo() => Block!.AppliesTo.Should().NotBeNull();

	[Fact]
	public void RendersAppliesToInHtml()
	{
		var html = Html;
		html.Should().Contain("applies applies-admonition");
		html.Should().Contain("admonition-title__separator");
		html.Should().Contain("applicable-info");
	}
}

public class AdmonitionAppliesToTests(ITestOutputHelper output) : DirectiveTest<AdmonitionBlock>(output,
"""
:::{admonition} Custom Admonition
:applies_to: stack: ga
This is a custom admonition with applies_to information
:::
A regular paragraph.
"""
)
{
	[Fact]
	public void SetsCorrectAdmonitionType() => Block!.Admonition.Should().Be("admonition");

	[Fact]
	public void SetsCustomTitle() => Block!.Title.Should().Be("Custom Admonition");

	[Fact]
	public void SetsAppliesToDefinition() => Block!.AppliesToDefinition.Should().Be("stack: ga");

	[Fact]
	public void ParsesAppliesTo() => Block!.AppliesTo.Should().NotBeNull();

	[Fact]
	public void RendersAppliesToInHtml()
	{
		var html = Html;
		html.Should().Contain("applies applies-admonition");
		html.Should().Contain("admonition-title__separator");
		html.Should().Contain("applicable-info");
	}
}
