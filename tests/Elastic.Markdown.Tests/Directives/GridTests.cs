// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
using Elastic.Markdown.Myst.Directives;
using FluentAssertions;
using Xunit.Abstractions;

namespace Elastic.Markdown.Tests.Directives;

public class GridTests(ITestOutputHelper output) : DirectiveTest<GridBlock>(output,
"""
````{grid} 2 2 3 4
```{grid-item-card} Admonitions
:link: admonitions.html
  Click this card to learn about admonitions.
```
```{grid-item-card} Code Blocks
:link: code.html
  Click this card to learn about code blocks.
```
```{grid-item-card} Tabs and Dropdowns
:link: tabs_dropdowns.html
  Click this card to learn about Tabs and Dropdowns.
```
"""
)
{
	[Fact]
	public void ParsesBlock() => Block.Should().NotBeNull();

	[Fact]
	public void ParsesBreakPoint()
	{
		Block!.BreakPoint.Should().NotBeNull();
		Block!.BreakPoint.Xs.Should().Be(2);
		Block!.BreakPoint.Sm.Should().Be(2);
		Block!.BreakPoint.Md.Should().Be(3);
		Block!.BreakPoint.Lg.Should().Be(4);
	}
}
