// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
using Elastic.Markdown.Myst.Directives;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Elastic.Markdown.Tests.Directives;

public class MermaidBlockTests(ITestOutputHelper output) : DirectiveTest<MermaidBlock>(output,
"""
```{mermaid} /_static/img/observability.png
flowchart LR
  A[Jupyter Notebook] --> C
  B[MyST Markdown] --> C
  C(mystmd) --> D{AST}
  D <--> E[LaTeX]
  E --> F[PDF]
  D --> G[Word]
  D --> H[React]
  D --> I[HTML]
  D <--> J[JATS]
```
"""
)
{
	[Fact]
	public void ParsesBlock() => Block.Should().NotBeNull();

	[Fact]
	public void IncludesRawFlowChart() =>
		Html.Should().Contain("D --&gt; I[HTML]");
}
