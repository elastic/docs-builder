// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using Elastic.Markdown.Myst.Directives.Include;
using Elastic.Markdown.Tests.Directives;
using FluentAssertions;

namespace Elastic.Markdown.Tests.FileInclusion;

/// <summary>
/// Tests that when the same snippet containing tab-set is included multiple times,
/// each include generates unique IDs to avoid HTML ID collisions.
/// </summary>
public class IncludedTabSetTests(ITestOutputHelper output) : DirectiveTest<IncludeBlock>(output,
"""
:::{include} _snippets/tab-set.md
:::

Some content between includes.

:::{include} _snippets/tab-set.md
:::
"""
)
{
	protected override void AddToFileSystem(MockFileSystem fileSystem)
	{
		// language=markdown
		var snippet =
"""
::::{tab-set}
:::{tab-item} First
Content for first tab
:::
:::{tab-item} Second
Content for second tab
:::
::::
""";
		fileSystem.AddFile(@"docs/_snippets/tab-set.md", snippet);
	}

	[Fact]
	public void ParsesBlock() => Block.Should().NotBeNull();

	[Fact]
	public void EachIncludeHasUniqueIds()
	{
		// First include at line 2: (2 * 1000) + 0 = 2000
		// Second include at line 7: (7 * 1000) + 0 = 7000
		Html.Should().Contain("tabs-item-2000-0");
		Html.Should().Contain("tabs-item-2000-1");
		Html.Should().Contain("tabs-set-2000");

		Html.Should().Contain("tabs-item-7000-0");
		Html.Should().Contain("tabs-item-7000-1");
		Html.Should().Contain("tabs-set-7000");
	}
}

/// <summary>
/// Tests that a snippet with multiple tab-sets generates unique IDs for each one.
/// </summary>
public class IncludedMultipleTabSetTests(ITestOutputHelper output) : DirectiveTest<IncludeBlock>(output,
"""
:::{include} _snippets/multi-tab-set.md
:::
"""
)
{
	protected override void AddToFileSystem(MockFileSystem fileSystem)
	{
		// language=markdown
		var snippet =
"""
::::{tab-set}
:::{tab-item} First
First tab set
:::
::::

Some content between.

::::{tab-set}
:::{tab-item} Second
Second tab set
:::
::::
""";
		fileSystem.AddFile(@"docs/_snippets/multi-tab-set.md", snippet);
	}

	[Fact]
	public void ParsesBlock() => Block.Should().NotBeNull();

	[Fact]
	public void EachTabSetHasUniqueIds()
	{
		// Include at line 2, first tab-set at line 0: (2 * 1000) + 0 = 2000
		// Include at line 2, second tab-set at line 8: (2 * 1000) + 8 = 2008
		Html.Should().Contain("tabs-set-2000");
		Html.Should().Contain("tabs-set-2008");
	}
}
