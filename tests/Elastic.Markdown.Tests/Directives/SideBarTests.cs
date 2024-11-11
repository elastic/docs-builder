// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
using Elastic.Markdown.Myst.Directives;
using FluentAssertions;
using Xunit.Abstractions;

namespace Elastic.Markdown.Tests.Directives;

public class SideBarTests(ITestOutputHelper output) : DirectiveTest<SideBarBlock>(output,
"""
```{sidebar}
This code is very helpful.

It does lots of things.

But it does not sing.
```
"""
)
{
	[Fact]
	public void ParsesBlock () => Block.Should().NotBeNull();

}
