// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Documentation.Testing;
using Elastic.Markdown.Myst.Directives.Include;
using Elastic.Markdown.Tests.Directives;
using Nullean.Snapshots.Html;

namespace Elastic.Markdown.Tests.FileInclusion;

/// <summary>
/// Tests that when the same snippet containing tab-set is included multiple times,
/// each include generates unique IDs to avoid HTML ID collisions.
/// </summary>
[InheritsTests]
public class IncludedTabSetTests() : DirectiveTest<IncludeBlock>(
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

	[Test]
	public void ParsesBlock() => Block.Should().NotBeNull();

	[Test]
	// First include at line 2: (2 * 1000) + 0 = 2000
	// Second include at line 7: (7 * 1000) + 0 = 7000
	public void EachIncludeHasUniqueIds() => Html.Should().MatchHtmlSnapshot(DocsHtml.Body());
}

/// <summary>
/// Tests that a snippet with multiple tab-sets generates unique IDs for each one.
/// </summary>
[InheritsTests]
public class IncludedMultipleTabSetTests() : DirectiveTest<IncludeBlock>("""
:::{include} _snippets/multi-tab-set.md
:::
""")
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

	[Test]
	public void ParsesBlock() => Block.Should().NotBeNull();

	[Test]
	// Include at line 2, first tab-set at line 0: (2 * 1000) + 0 = 2000
	// Include at line 2, second tab-set at line 8: (2 * 1000) + 8 = 2008
	public void EachTabSetHasUniqueIds() =>
		Html.Should().MatchInlineHtmlSnapshot(
			"""
		<div class="tabs">
			<input class="tabs-input" checked="checked" id="tabs-item-2000-0" name="tabs-set-2000" type="radio" tabindex="0">
			<label class="tabs-label" data-sync-id="" data-sync-group="" for="tabs-item-2000-0">First</label>
			<div class="tabs-content" tabindex="0">
				<p>First tab set</p>
			</div>
		</div>
		<p>Some content between.</p>
		<div class="tabs">
			<input class="tabs-input" checked="checked" id="tabs-item-2008-0" name="tabs-set-2008" type="radio" tabindex="0">
			<label class="tabs-label" data-sync-id="" data-sync-group="" for="tabs-item-2008-0">Second</label>
			<div class="tabs-content" tabindex="0">
				<p>Second tab set</p>
			</div>
		</div>
		""",
			DocsHtml.Body()
		);
}
