// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Markdown.Myst.Directives.Admonition;

namespace Elastic.Markdown.Tests.Directives;

public abstract class AdmonitionBaseTests(string directive) : DirectiveTest<AdmonitionBlock>(
	$$"""
:::{{{directive}}}
This is an attention block
:::
A regular paragraph.
"""
)
{
	[Test]
	public void ParsesAdmonitionBlock() => Block.Should().NotBeNull();

	[Test]
	public void SetsCorrectAdmonitionType() => Block!.Admonition.Should().Be(directive);
}

public class WarningTests() : AdmonitionBaseTests("warning")
{
	[Test]
	public void SetsTitle() => Block!.Title.Should().Be("Warning");
}

public class NoteTests() : AdmonitionBaseTests("note")
{
	[Test]
	public void SetsTitle() => Block!.Title.Should().Be("Note");
}

public class TipTests() : AdmonitionBaseTests("tip")
{
	[Test]
	public void SetsTitle() => Block!.Title.Should().Be("Tip");
}

public class ImportantTests() : AdmonitionBaseTests("important")
{
	[Test]
	public void SetsTitle() => Block!.Title.Should().Be("Important");
}

public class NoteTitleTests() : DirectiveTest<AdmonitionBlock>(
	"""
```{note} This is my custom note
This is an attention block
```
A regular paragraph.
"""
)
{
	[Test]
	public void SetsCorrectAdmonitionType() => Block!.Admonition.Should().Be("note");

	[Test]
	public void SetsCustomTitle() => Block!.Title.Should().Be("Note This is my custom note");
}

public class AdmonitionTitleTests() : DirectiveTest<AdmonitionBlock>(
	"""
```{admonition} This is my custom title
This is an attention block
```
A regular paragraph.
"""
)
{
	[Test]
	public void SetsCorrectAdmonitionType() => Block!.Admonition.Should().Be("admonition");

	[Test]
	public void SetsCustomTitle() => Block!.Title.Should().Be("This is my custom title");
}

public class DropdownTitleTests() : DirectiveTest<AdmonitionBlock>(
	"""
:::{dropdown} This is my custom dropdown
:open:
This is an attention block
:::
A regular paragraph.
"""
)
{
	[Test]
	public void SetsCorrectAdmonitionType() => Block!.Admonition.Should().Be("dropdown");

	[Test]
	public void SetsCustomTitle() => Block!.Title.Should().Be("This is my custom dropdown");

	[Test]
	public void SetsDropdownOpen() => Block!.DropdownOpen.Should().BeTrue();
}

public class DropdownPlainTextTitleTests() : DirectiveTest<AdmonitionBlock>(
	"""
:::{dropdown} Deprecate `elastic.apm` settings
Dropdown body content.
:::
"""
)
{
	[Test]
	public void StripsBackticksFromTitle() => Block!.Title.Should().Be("Deprecate elastic.apm settings");

	[Test]
	public void RendersPlainTextTitleInHtml()
	{
		Html.Should().Contain("Deprecate elastic.apm settings");
		Html.Should().NotContain("`elastic.apm`");
	}
}

public class DropdownPlainTextBoldTitleTests() : DirectiveTest<AdmonitionBlock>(
	"""
:::{dropdown} Disable **Save** button
Dropdown body content.
:::
"""
)
{
	[Test]
	public void StripsBoldMarkersFromTitle() => Block!.Title.Should().Be("Disable Save button");

	[Test]
	public void RendersBoldTitleAsPlainTextInHtml()
	{
		Html.Should().Contain("Disable Save button");
		Html.Should().NotContain("**Save**");
	}
}

public class DropdownPlainTextItalicTitleTests() : DirectiveTest<AdmonitionBlock>(
	"""
:::{dropdown} Use _italic_ emphasis
Dropdown body content.
:::
"""
)
{
	[Test]
	public void StripsItalicMarkersFromTitle() => Block!.Title.Should().Be("Use italic emphasis");

	[Test]
	public void RendersItalicTitleAsPlainTextInHtml()
	{
		Html.Should().Contain("Use italic emphasis");
		Html.Should().NotContain("_italic_");
	}
}

public class DropdownAppliesToTests() : DirectiveTest<AdmonitionBlock>(
	"""
:::{dropdown} This is my custom dropdown
:applies_to: stack: ga 9.0
This is an attention block
:::
A regular paragraph.
"""
)
{
	[Test]
	public void SetsCorrectAdmonitionType() => Block!.Admonition.Should().Be("dropdown");

	[Test]
	public void SetsCustomTitle() => Block!.Title.Should().Be("This is my custom dropdown");

	[Test]
	public void SetsAppliesToDefinition() => Block!.AppliesToDefinition.Should().Be("stack: ga 9.0");

	[Test]
	public void ParsesAppliesTo() => Block!.AppliesTo.Should().NotBeNull();
}

public class DropdownPropertyParsingTests() : DirectiveTest<AdmonitionBlock>(
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
	[Test]
	public void SetsCorrectAdmonitionType() => Block!.Admonition.Should().Be("dropdown");

	[Test]
	public void SetsCustomTitle() => Block!.Title.Should().Be("Test Dropdown");

	[Test]
	public void SetsDropdownOpen() => Block!.DropdownOpen.Should().BeTrue();

	[Test]
	public void SetsCrossReferenceName() => Block!.CrossReferenceName.Should().Be("test-dropdown");
}

public class DropdownNestedContentTests() : DirectiveTest<AdmonitionBlock>(
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
	[Test]
	public void SetsCorrectAdmonitionType() => Block!.Admonition.Should().Be("dropdown");

	[Test]
	public void SetsCustomTitle() => Block!.Title.Should().Be("Nested Content Test");

	[Test]
	public void SetsDropdownOpen() => Block!.DropdownOpen.Should().BeTrue();

	[Test]
	public void ContainsContentWithColons()
	{
		var html = Html;
		html.Should().Contain("Time: 10:30 AM");
		// URL with port is NOT autolinked (excluded by autolink rules)
		html.Should().Contain("URL: https://example.com:8080/path");
		html.Should().NotContain("""<a href="https://example.com:8080/path""");
		html.Should().Contain("Configuration: key:value pairs");
		html.Should().Contain("function test() { return &quot;hello:world&quot;; }");
	}

	[Test]
	public void ContainsNestedDirective()
	{
		var html = Html;
		html.Should().Contain("Nested Note");
		html.Should().Contain("This is a nested note with colons: 10:30 AM");
		// Verify the nested note was actually parsed as a directive, not just plain text
		html.Should().Contain("class=\"admonition note\"");
		html.Should().Contain("admonition-title");
		html.Should().Contain("admonition-content");
	}

	[Test]
	public void ContainsContentAfterNestedDirective()
	{
		var html = Html;
		html.Should().Contain("More content after nested directive");
	}
}

public class DropdownComplexPropertyTests() : DirectiveTest<AdmonitionBlock>(
	"""
:::{dropdown} Complex Properties Test
:applies_to: stack: ga 9.0
This is content with applies_to property
:::
A regular paragraph.
"""
)
{
	[Test]
	public void SetsCorrectAdmonitionType() => Block!.Admonition.Should().Be("dropdown");

	[Test]
	public void SetsCustomTitle() => Block!.Title.Should().Be("Complex Properties Test");

	[Test]
	public void ParsesAppliesToWithComplexValue()
	{
		Block!.AppliesToDefinition.Should().Be("stack: ga 9.0");
		Block!.AppliesTo.Should().NotBeNull();
	}
}

public class NoteAppliesToTests() : DirectiveTest<AdmonitionBlock>(
	"""
:::{note}
:applies_to: stack: ga
This is a note with applies_to information
:::
A regular paragraph.
"""
)
{
	[Test]
	public void SetsCorrectAdmonitionType() => Block!.Admonition.Should().Be("note");

	[Test]
	public void SetsTitle() => Block!.Title.Should().Be("Note");

	[Test]
	public void SetsAppliesToDefinition() => Block!.AppliesToDefinition.Should().Be("stack: ga");

	[Test]
	public void ParsesAppliesTo() => Block!.AppliesTo.Should().NotBeNull();

	[Test]
	public void RendersAppliesToInHtml()
	{
		var html = Html;
		html.Should().Contain("applies applies-admonition");
		html.Should().Contain("admonition-title__separator");
		html.Should().Contain("applies-to-popover");
	}
}

public class WarningAppliesToTests() : DirectiveTest<AdmonitionBlock>(
	"""
:::{warning}
:applies_to: stack: ga
This is a warning with applies_to information
:::
A regular paragraph.
"""
)
{
	[Test]
	public void SetsCorrectAdmonitionType() => Block!.Admonition.Should().Be("warning");

	[Test]
	public void SetsTitle() => Block!.Title.Should().Be("Warning");

	[Test]
	public void SetsAppliesToDefinition() => Block!.AppliesToDefinition.Should().Be("stack: ga");

	[Test]
	public void ParsesAppliesTo() => Block!.AppliesTo.Should().NotBeNull();

	[Test]
	public void RendersAppliesToInHtml()
	{
		var html = Html;
		html.Should().Contain("applies applies-admonition");
		html.Should().Contain("admonition-title__separator");
		html.Should().Contain("applies-to-popover");
	}
}

public class TipAppliesToTests() : DirectiveTest<AdmonitionBlock>(
	"""
:::{tip}
:applies_to: stack: ga
This is a tip with applies_to information
:::
A regular paragraph.
"""
)
{
	[Test]
	public void SetsCorrectAdmonitionType() => Block!.Admonition.Should().Be("tip");

	[Test]
	public void SetsTitle() => Block!.Title.Should().Be("Tip");

	[Test]
	public void SetsAppliesToDefinition() => Block!.AppliesToDefinition.Should().Be("stack: ga");

	[Test]
	public void ParsesAppliesTo() => Block!.AppliesTo.Should().NotBeNull();

	[Test]
	public void RendersAppliesToInHtml()
	{
		var html = Html;
		html.Should().Contain("applies applies-admonition");
		html.Should().Contain("admonition-title__separator");
		html.Should().Contain("applies-to-popover");
	}
}

public class ImportantAppliesToTests() : DirectiveTest<AdmonitionBlock>(
	"""
:::{important}
:applies_to: stack: ga
This is an important notice with applies_to information
:::
A regular paragraph.
"""
)
{
	[Test]
	public void SetsCorrectAdmonitionType() => Block!.Admonition.Should().Be("important");

	[Test]
	public void SetsTitle() => Block!.Title.Should().Be("Important");

	[Test]
	public void SetsAppliesToDefinition() => Block!.AppliesToDefinition.Should().Be("stack: ga");

	[Test]
	public void ParsesAppliesTo() => Block!.AppliesTo.Should().NotBeNull();

	[Test]
	public void RendersAppliesToInHtml()
	{
		var html = Html;
		html.Should().Contain("applies applies-admonition");
		html.Should().Contain("admonition-title__separator");
		html.Should().Contain("applies-to-popover");
	}
}

public class AdmonitionAppliesToTests() : DirectiveTest<AdmonitionBlock>(
	"""
:::{admonition} Custom Admonition
:applies_to: stack: ga
This is a custom admonition with applies_to information
:::
A regular paragraph.
"""
)
{
	[Test]
	public void SetsCorrectAdmonitionType() => Block!.Admonition.Should().Be("admonition");

	[Test]
	public void SetsCustomTitle() => Block!.Title.Should().Be("Custom Admonition");

	[Test]
	public void SetsAppliesToDefinition() => Block!.AppliesToDefinition.Should().Be("stack: ga");

	[Test]
	public void ParsesAppliesTo() => Block!.AppliesTo.Should().NotBeNull();

	[Test]
	public void RendersAppliesToInHtml()
	{
		var html = Html;
		html.Should().Contain("applies applies-admonition");
		html.Should().Contain("admonition-title__separator");
		html.Should().Contain("applies-to-popover");
	}
}
