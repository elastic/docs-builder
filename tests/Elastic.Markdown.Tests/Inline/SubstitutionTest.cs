// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.Myst.CodeBlocks;
using Elastic.Markdown.Myst.Substitution;
using FluentAssertions;

namespace Elastic.Markdown.Tests.Inline;

public class SubstitutionTest(ITestOutputHelper output) : LeafTest<SubstitutionLeaf>(output,
"""
---
sub:
  hello-world: "Hello World!"
---
The following should be subbed: {{hello-world}}
not a comment
"""
)
{

	[Fact]
	public void ReplacesSubsFromFrontMatter() =>
		Html.Should().Contain(
				"""Hello World!"""
			).And.Contain(
				"""not a comment"""
			)
			.And.NotContain(
				"""{{hello-world}}"""
			);
}

public class NeedsDoubleBrackets(ITestOutputHelper output) : InlineTest(output,
"""
---
sub:
  hello-world: "Hello World!"
---

# Testing substitutions

The following should be subbed: {{hello-world}}
not a comment
not a {{valid-key}}
not a {substitution}
"""
)
{

	[Fact]
	public void PreservesSingleBracket() =>
		Html.Should().Contain(
				"""Hello World!"""
			).And.Contain(
				"""not a comment"""
			)
			.And.NotContain(
				"""{{hello-world}}"""
			)
			.And.Contain( // treated as attributes to the block
				"""{substitution}"""
			)
			.And.Contain(
				"""{{valid-key}}"""
			);
}

public class SubstitutionInCodeBlockTest(ITestOutputHelper output) : BlockTest<EnhancedCodeBlock>(output,
"""
---
sub:
  version: "7.17.0"
---

# Testing substitutions

```{code} sh
wget https://artifacts.elastic.co/downloads/elasticsearch/elasticsearch-{{version}}-linux-x86_64.tar.gz
wget https://artifacts.elastic.co/downloads/elasticsearch/elasticsearch-{{version}}-linux-x86_64.tar.gz.sha512
shasum -a 512 -c elasticsearch-{{version}}-linux-x86_64.tar.gz.sha512 <1>
tar -xzf elasticsearch-{{version}}-linux-x86_64.tar.gz
cd elasticsearch-{{version}}/ <2>
```
"""
)
{

	[Fact]
	public void ReplacesSubsInCode() =>
		Html.Should().Contain("7.17.0");
}


public class SupportsSubstitutionsFromDocSet(ITestOutputHelper output) : InlineTest(output,
"""
---
sub:
  hello-world: "Hello World!"
---
The following should be subbed: {{hello-world}}
The following should be subbed as well: {{global-var}}
"""
, new() { { "global-var", "A variable from docset.yml" } }
)
{

	[Fact]
	public void EmitsGlobalVariable() =>
		Html.Should().Contain("Hello World!")
			.And.NotContain("{{hello-world}}")
			.And.Contain("A variable from docset.yml")
			.And.NotContain("{{global-var}}");
}


public class CanNotShadeGlobalVariables(ITestOutputHelper output) : InlineTest(output,
"""
---
sub:
  hello-world: "Hello World!"
---

# Testing CanNotShadeGlobalVariables

The following should be subbed: {{hello-world}}
The following should be subbed as well: {{hello-world}}
"""
, new() { { "hello-world", "A variable from docset.yml" } }
)
{

	[Fact]
	public void OnlySeesGlobalVariable() =>
		Html.Should().NotContain("Hello World!<br />")
			.And.NotContain("{{hello-world}}")
			.And.Contain("A variable from docset.yml");

	[Fact]
	public void HasError() => Collector.Diagnostics.Should().HaveCount(1)
		.And.Contain(d => d.Message.Contains("{hello-world} can not be redeclared in front matter as its a global substitution"));
}

public class ReplaceInHeader(ITestOutputHelper output) : InlineTest(output,
"""
---
sub:
  hello-world: "Hello World!"
---

# Testing ReplaceInHeader

## {{hello-world}} [#custom-anchor]

"""
)
{

	[Fact]
	public void OnlySeesGlobalVariable() =>
		Html.Should().Contain("<h2>Hello World!");

	[Fact]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);

}
