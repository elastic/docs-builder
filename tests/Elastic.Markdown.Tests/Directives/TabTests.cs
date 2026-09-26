// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Markdown.Myst.Directives.Tabs;

namespace Elastic.Markdown.Tests.Directives;

[InheritsTests]
public class TabTests() : DirectiveTest<TabSetBlock>(
	"""
:::::{tab-set}

::::{tab-item} Admonition
:::{tip}
Tabs are easy. You can even embed other directives like the admonition you see here.
:::
::::

::::{tab-item} Text

# Markdown

And of course you can use regular markdown
::::

::::{tab-item} Code
# Getting started with SQL

```sql
sql> SELECT * FROM library WHERE release_date < '2000-01-01';
    author     |     name      |  page_count   | release_date
---------------+---------------+---------------+------------------------
Dan Simmons    |Hyperion       |482            |1989-05-26T00:00:00.000Z
Frank Herbert  |Dune           |604            |1965-06-01T00:00:00.000Z
```
::::
:::::
"""
)
{
	[Test]
	public void ParsesBlock() => Block.Should().NotBeNull();

	[Test]
	public void ParsesTabItems()
	{
		var items = Block!.OfType<TabItemBlock>().ToArray();
		items.Should().NotBeNull().And.HaveCount(3);
		for (var i = 0; i < items.Length; i++)
			items[i].Index.Should().Be(i);
	}
}

[InheritsTests]
public class MultipleTabTests() : DirectiveTest<TabSetBlock>(
	"""
:::::{tab-set}
::::{tab-item} Admonition
:::{tip}
Tabs are easy. You can even embed other directives like the admonition you see here.
:::
::::
:::::

Paragraph

:::::{tab-set}
::::{tab-item} Admonition
:::{tip}
Tabs are easy. You can even embed other directives like the admonition you see here.
:::
::::
:::::
"""
)
{
	[Test]
	public void ParsesMultipleTabSets()
	{
		var sets = Document.OfType<TabSetBlock>().ToArray();
		sets.Length.Should().Be(2);
		for (var s = 0; s < sets.Length; s++)
		{
			var items = sets[s].OfType<TabItemBlock>().ToArray();
			items.Should().NotBeNull().And.HaveCount(1);
			for (var i = 0; i < items.Length; i++)
			{
				items[i].Index.Should().Be(i);
				items[i].TabSetIndex.Should().Be(sets[s].Line);
			}
		}
	}
}

[InheritsTests]
public class GroupTabTests() : DirectiveTest<TabSetBlock>(
	"""
::::{tab-set}
:group: languages
:::{tab-item} Java
:sync: java
Content for Java tab
:::

:::{tab-item} Golang
:sync: golang
Content for Golang tab
:::

:::{tab-item} C#
:sync: csharp
Content for C# tab
:::

::::

::::{tab-set}
:group: languages
:::{tab-item} Java
:sync: java
Content for Java tab
:::

:::{tab-item} Golang
:sync: golang
Content for Golang tab
:::

:::{tab-item} C#
:sync: csharp
Content for C# tab
:::

::::
"""
)
{
	[Test]
	public void ParsesMultipleTabSets()
	{
		var sets = Document.OfType<TabSetBlock>().ToArray();
		sets.Length.Should().Be(2);
		for (var s = 0; s < sets.Length; s++)
		{
			var items = sets[s].OfType<TabItemBlock>().ToArray();
			items.Should().NotBeNull().And.HaveCount(3);
			for (var i = 0; i < items.Length; i++)
			{
				items[i].Index.Should().Be(i);
				items[i].TabSetIndex.Should().Be(sets[s].Line);
			}
		}
	}

	[Test]
	public void ParsesGroup()
	{
		var sets = Document.OfType<TabSetBlock>().ToArray();
		sets.Length.Should().Be(2);

		foreach (var t in sets)
			t.GetGroupKey().Should().Be("languages");
	}

	[Test]
	public void ParsesSyncKey()
	{
		var set = Document.OfType<TabSetBlock>().First();
		var items = set.OfType<TabItemBlock>().ToArray();
		items.Should().HaveCount(3);
		items[0].SyncKey.Should().Be("java");
		items[1].SyncKey.Should().Be("golang");
		items[2].SyncKey.Should().Be("csharp");
	}
}

[InheritsTests]
public class LanguagesGroupRendersAsDropdownTests() : DirectiveTest<TabSetBlock>(
	"""
::::{tab-set}
:group: languages
:::{tab-item} Java
:sync: java
Content for Java tab
:::

:::{tab-item} Golang
:sync: golang
Content for Golang tab
:::
::::
"""
)
{
	[Test]
	public void DefaultsToDropdownForLanguages() => Block!.RenderAsDropdown().Should().BeTrue();
}

[InheritsTests]
public class OtherGroupsRenderAsTabsTests() : DirectiveTest<TabSetBlock>(
	"""
::::{tab-set}
:group: operating-systems
:::{tab-item} macOS
:sync: macos
Content for macOS tab
:::

:::{tab-item} Windows
:sync: windows
Content for Windows tab
:::
::::
"""
)
{
	[Test]
	public void DefaultsToTabsForOtherGroups() => Block!.RenderAsDropdown().Should().BeFalse();
}

[InheritsTests]
public class ExplicitDropdownOptInTests() : DirectiveTest<TabSetBlock>(
	"""
::::{tab-set}
:dropdown: true
:::{tab-item} One
Content for tab one
:::

:::{tab-item} Two
Content for tab two
:::
::::
"""
)
{
	[Test]
	public void OptsInWithoutAGroup() => Block!.RenderAsDropdown().Should().BeTrue();
}

[InheritsTests]
public class ExplicitDropdownOptOutTests() : DirectiveTest<TabSetBlock>(
	"""
::::{tab-set}
:group: languages
:dropdown: false
:::{tab-item} Java
:sync: java
Content for Java tab
:::

:::{tab-item} Golang
:sync: golang
Content for Golang tab
:::
::::
"""
)
{
	[Test]
	public void OptsOutOfTheLanguagesDefault() => Block!.RenderAsDropdown().Should().BeFalse();
}
