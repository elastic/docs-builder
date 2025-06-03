// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.Myst.CodeBlocks;
using Elastic.Markdown.Myst.Directives;
using Elastic.Markdown.Tests.Inline;
using FluentAssertions;

namespace Elastic.Markdown.Tests.CodeBlocks;

public class MermaidBlockTests(ITestOutputHelper output) : BlockTest<EnhancedCodeBlock>(output,
"""
```mermaid
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

	// should still attempt to render contents as Markdown
	[Fact]
	public void IncludesRawFlowChart() =>
		Html.Should().Contain("D --&gt; I[HTML]");
}
