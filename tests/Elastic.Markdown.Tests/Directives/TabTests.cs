// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
using Elastic.Markdown.Myst.Directives;
using FluentAssertions;

namespace Elastic.Markdown.Tests.Directives;

public class TabTests(ITestOutputHelper output) : DirectiveTest<TabSetBlock>(output,
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
	[Fact]
	public void ParsesBlock() => Block.Should().NotBeNull();

	[Fact]
	public void ParsesTabItems()
	{
		var items = Block!.OfType<TabItemBlock>().ToArray();
		items.Should().NotBeNull().And.HaveCount(3);
		for (var i = 0; i < items.Length; i++)
		{
			items[i].Index.Should().Be(i);
			items[i].TabSetIndex.Should().Be(0);
		}
	}
}

public class MultipleTabTests(ITestOutputHelper output) : DirectiveTest<TabSetBlock>(output,
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
	[Fact]
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
				items[i].TabSetIndex.Should().Be(s);
			}
		}
	}
}

public class GroupTabTests(ITestOutputHelper output) : DirectiveTest<TabSetBlock>(output,
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
	[Fact]
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
				items[i].TabSetIndex.Should().Be(s);
			}
		}
	}

	[Fact]
	public void ParsesGroup()
	{
		var sets = Document.OfType<TabSetBlock>().ToArray();
		sets.Length.Should().Be(2);

		foreach (var t in sets)
			t.GetGroupKey().Should().Be("languages");
	}

	[Fact]
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
