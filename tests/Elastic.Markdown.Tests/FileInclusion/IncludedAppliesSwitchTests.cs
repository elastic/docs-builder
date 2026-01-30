// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using Elastic.Markdown.Myst.Directives.Include;
using Elastic.Markdown.Tests.Directives;
using FluentAssertions;

namespace Elastic.Markdown.Tests.FileInclusion;

/// <summary>
/// Tests that when the same snippet containing applies-switch is included multiple times,
/// each include generates unique IDs to avoid HTML ID collisions.
/// </summary>
public class IncludedAppliesSwitchTests(ITestOutputHelper output) : DirectiveTest<IncludeBlock>(output,
"""
:::{include} _snippets/applies-switch.md
:::

Some content between includes.

:::{include} _snippets/applies-switch.md
:::
"""
)
{
	protected override void AddToFileSystem(MockFileSystem fileSystem)
	{
		// language=markdown
		var snippet =
"""
::::{applies-switch}
:::{applies-item} stack:
Content for Stack
:::
:::{applies-item} serverless:
Content for Serverless
:::
::::
""";
		fileSystem.AddFile(@"docs/_snippets/applies-switch.md", snippet);
	}

	[Fact]
	public void ParsesBlock() => Block.Should().NotBeNull();

	[Fact]
	public void EachIncludeHasUniqueIds()
	{
		// First include at line 2: (2 * 1000) + 0 = 2000
		// Second include at line 7: (7 * 1000) + 0 = 7000
		Html.Should().Contain("applies-switch-item-2000-0");
		Html.Should().Contain("applies-switch-item-2000-1");
		Html.Should().Contain("applies-switch-set-2000");

		Html.Should().Contain("applies-switch-item-7000-0");
		Html.Should().Contain("applies-switch-item-7000-1");
		Html.Should().Contain("applies-switch-set-7000");
	}
}

/// <summary>
/// Tests that a snippet with multiple applies-switches generates unique IDs for each one.
/// </summary>
public class IncludedMultipleAppliesSwitchTests(ITestOutputHelper output) : DirectiveTest<IncludeBlock>(output,
"""
:::{include} _snippets/multi-applies-switch.md
:::
"""
)
{
	protected override void AddToFileSystem(MockFileSystem fileSystem)
	{
		// language=markdown
		var snippet =
"""
::::{applies-switch}
:::{applies-item} stack:
First switch - Stack
:::
::::

Some content between.

::::{applies-switch}
:::{applies-item} serverless:
Second switch - Serverless
:::
::::
""";
		fileSystem.AddFile(@"docs/_snippets/multi-applies-switch.md", snippet);
	}

	[Fact]
	public void ParsesBlock() => Block.Should().NotBeNull();

	[Fact]
	public void EachAppliesSwitchHasUniqueIds()
	{
		// Include at line 2, first applies-switch at line 0: (2 * 1000) + 0 = 2000
		// Include at line 2, second applies-switch at line 8: (2 * 1000) + 8 = 2008
		Html.Should().Contain("applies-switch-set-2000");
		Html.Should().Contain("applies-switch-set-2008");
	}
}
