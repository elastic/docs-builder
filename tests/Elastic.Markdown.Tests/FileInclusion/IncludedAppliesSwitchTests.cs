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
/// Tests that when the same snippet containing applies-switch is included multiple times,
/// each include generates unique IDs to avoid HTML ID collisions.
/// </summary>
[InheritsTests]
public class IncludedAppliesSwitchTests() : DirectiveTest<IncludeBlock>(
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

	[Test]
	public void ParsesBlock() => Block.Should().NotBeNull();

	[Test]
	// First include at line 2: (2 * 1000) + 0 = 2000
	// Second include at line 7: (7 * 1000) + 0 = 7000
	public void EachIncludeHasUniqueIds() => Html.Should().MatchHtmlSnapshot(DocsHtml.Body());
}

/// <summary>
/// Tests that a snippet with multiple applies-switches generates unique IDs for each one.
/// </summary>
[InheritsTests]
public class IncludedMultipleAppliesSwitchTests() : DirectiveTest<IncludeBlock>("""
:::{include} _snippets/multi-applies-switch.md
:::
""")
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

	[Test]
	public void ParsesBlock() => Block.Should().NotBeNull();

	[Test]
	// Include at line 2, first applies-switch at line 0: (2 * 1000) + 0 = 2000
	// Include at line 2, second applies-switch at line 8: (2 * 1000) + 8 = 2008
	public void EachAppliesSwitchHasUniqueIds() =>
		Html.Should().MatchInlineHtmlSnapshot(
			"""
		<div class="applies-switch">
			<input class="applies-switch-input" checked="checked" id="applies-switch-item-2000-0" name="applies-switch-set-2000" type="radio" tabindex="0">
			<label class="applies-switch-label" data-sync-id="applies-B70B4596" data-sync-group="applies-switches" for="applies-switch-item-2000-0">
				<span class="applies applies-item">
					<applies-to-popover badge-key="Stack" lifecycle-class="ga" lifecycle-name="GA" show-lifecycle-name="false" show-version="false" has-multiple-lifecycles="false" popover-data="" show-popover="false" is-inline="true"></applies-to-popover>
				</span>
			</label>
			<div class="applies-switch-content" tabindex="0">
				<p>First switch - Stack</p>
			</div>
		</div>
		<p>Some content between.</p>
		<div class="applies-switch">
			<input class="applies-switch-input" checked="checked" id="applies-switch-item-2008-0" name="applies-switch-set-2008" type="radio" tabindex="0">
			<label class="applies-switch-label" data-sync-id="applies-E100A717" data-sync-group="applies-switches" for="applies-switch-item-2008-0">
				<span class="applies applies-item">
					<span class="applies-switch-fallback">serverless:</span>
				</span>
			</label>
			<div class="applies-switch-content" tabindex="0">
				<p>Second switch - Serverless</p>
			</div>
		</div>
		""",
			DocsHtml.Body()
		);
}
