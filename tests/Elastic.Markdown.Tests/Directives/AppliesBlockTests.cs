// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.Diagnostics;
using Elastic.Markdown.Myst.Directives;
using FluentAssertions;

namespace Elastic.Markdown.Tests.Directives;

public class AppliesBlockTests(ITestOutputHelper output) : DirectiveTest<AppliesBlock>(output,
"""
# heading
:::{applies}
:eck: unavailable
:::
"""
)
{
	[Fact]
	public void ParsesBlock() => Block.Should().NotBeNull();

	[Fact]
	public void IncludesProductAvailability() =>
		Html.Should().Contain("Unavailable</span>")
			.And.Contain("Elastic Cloud Kubernetes")
			.And.Contain("Applies To:");


	[Fact]
	public void NoErrors() => Collector.Diagnostics.Should().BeEmpty();
}

public class EmptyAppliesBlock(ITestOutputHelper output) : DirectiveTest<AppliesBlock>(output,
"""

A paragraph that's not a heading

```{applies}
```
"""
)
{
	[Fact]
	public void ParsesBlock() => Block.Should().NotBeNull();

	[Fact]
	public void DoesNotRender() =>
		Html.Should().Be("<p>A paragraph that's not a heading</p>");

	[Fact]
	public void EmitErrorOnEmptyBlock()
	{
		Collector.Diagnostics.Should().NotBeNullOrEmpty().And.HaveCount(2);
		Collector.Diagnostics.Should().OnlyContain(d => d.Severity == Severity.Error);
		Collector.Diagnostics.Should()
			.Contain(d => d.Message.Contains("{applies} block with no product availability specified"));

		Collector.Diagnostics.Should()
			.Contain(d => d.Message.Contains("{applies} should follow a heading"));
	}
}

// ensures we allow for empty lines between heading and applies block
public class AppliesHeadingTests(ITestOutputHelper output) : DirectiveTest<AppliesBlock>(output,
"""
# heading



```{applies}
:eck: unavailable
```
"""
)
{
	[Fact]
	public void NoErrors() => Collector.Diagnostics.Should().BeEmpty();
}

