// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Markdown.Myst.CodeBlocks;
using Elastic.Markdown.Myst.InlineParsers.Substitution;

namespace Elastic.Markdown.Tests.Inline;

public class SubstitutionTest() : LeafTest<SubstitutionLeaf>(
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
	[Test]
	public void ReplacesSubsFromFrontMatter() =>
		Html.Should().Contain("""Hello World!""").And.Contain("""not a comment""").And.NotContain("""{{hello-world}}""");
}

public class NeedsDoubleBrackets() : InlineTest(
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
	[Test]
	public void PreservesSingleBracket() =>
		Html
			.Should()
			.Contain("""Hello World!""")
			.And
			.Contain("""not a comment""")
			.And
			.NotContain("""{{hello-world}}""")
			.And
			.Contain( // treated as attributes to the block
			"""{substitution}""")
			.And
			.Contain("""{{valid-key}}""");
}

public class SubstitutionInCodeBlockTest() : BlockTest<EnhancedCodeBlock>(
	"""
---
sub:
  version: "7.17.0"
---

# Testing substitutions

```{code} sh subs=true
wget https://artifacts.elastic.co/downloads/elasticsearch/elasticsearch-{{version}}-linux-x86_64.tar.gz
wget https://artifacts.elastic.co/downloads/elasticsearch/elasticsearch-{{version}}-linux-x86_64.tar.gz.sha512
shasum -a 512 -c elasticsearch-{{version}}-linux-x86_64.tar.gz.sha512 <1>
tar -xzf elasticsearch-{{version}}-linux-x86_64.tar.gz
cd elasticsearch-{{version}}/ <2>
```
"""
)
{
	[Test]
	public void ReplacesSubsInCode() => Html.Should().Contain("7.17.0");
}

public class SupportsSubstitutionsFromDocSet() : InlineTest(
	"""
---
sub:
  hello-world: "Hello World!"
---
The following should be subbed: {{hello-world}}
The following should be subbed as well: {{global-var}}
""",
	new() { { "global-var", "A variable from docset.yml" } }
)
{
	[Test]
	public void EmitsGlobalVariable() =>
		Html
			.Should()
			.Contain("Hello World!")
			.And
			.NotContain("{{hello-world}}")
			.And
			.Contain("A variable from docset.yml")
			.And
			.NotContain("{{global-var}}");
}

public class CanNotShadeGlobalVariables() : InlineTest(
	"""
---
sub:
  hello-world: "Hello World!"
---

# Testing CanNotShadeGlobalVariables

The following should be subbed: {{hello-world}}
The following should be subbed as well: {{hello-world}}
""",
	new() { { "hello-world", "A variable from docset.yml" } }
)
{
	[Test]
	public void OnlySeesGlobalVariable() =>
		Html.Should().NotContain("Hello World!<br />").And.NotContain("{{hello-world}}").And.Contain("A variable from docset.yml");

	[Test]
	public void HasError() =>
		Collector
			.Diagnostics
			.Should()
			.HaveCount(1)
			.And
			.Contain(d => d.Message.Contains("{hello-world} can not be redeclared in front matter as its a global substitution"));
}

public class ReplaceInHeader() : InlineTest(
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
	[Test]
	public void OnlySeesGlobalVariable() =>
		Html.ShouldContainHtml("""<h2><a class="headerlink" href="#custom-anchor">Hello World!</a></h2>""");

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}

public class ReplaceInImageAlt() : InlineTest(
	"""
---
sub:
  hello-world: Hello World
---

# Testing ReplaceInImageAlt

![{{hello-world}}](_static/img/observability.png)
"""
)
{
	[Test]
	public void OnlySeesGlobalVariable() => Html.Should().NotContain("alt=\"{{hello-world}}\"").And.Contain("alt=\"Hello World\"");
}

public class ReplaceInImageTitle() : InlineTest(
	"""
---
sub:
  hello-world: Hello World
---

# Testing ReplaceInImageTitle

![Observability](_static/img/observability.png "{{hello-world}}")
"""
)
{
	[Test]
	public void OnlySeesGlobalVariable() => Html.Should().NotContain("title=\"{{hello-world}}\"").And.Contain("title=\"Observability\"");
}

public class MutationOperatorTest() : InlineTest(
	"""
---
sub:
  version: "9.0.4"
---

# Testing Mutation Operators

Version: {{version|M.M}}
Version with space: {{version | M.M}}
Major only: {{version|M}}
Major only with space: {{version | M}}
Major.x: {{version|M.x}}
Major.x with space: {{version | M.x}}
Increase major: {{version|M+1}}
Increase major with space: {{version | M+1}}
Increase minor: {{version|M.M+1}}
Increase minor with space: {{version | M.M+1}}
"""
)
{
	[Test]
	public void MutationOperatorsWorkWithAndWithoutSpaces()
	{
		// Both versions with and without spaces should render the same way
		Html
			.Should()
			.Contain("Version: 9.0")
			.And
			.Contain("Version with space: 9.0")
			.And
			.Contain("Major only: 9")
			.And
			.Contain("Major only with space: 9")
			.And
			.Contain("Major.x: 9.x")
			.And
			.Contain("Major.x with space: 9.x")
			.And
			.Contain("Increase major: 10.0.0")
			.And
			.Contain("Increase major with space: 10.0.0")
			.And
			.Contain("Increase minor: 9.1.0")
			.And
			.Contain("Increase minor with space: 9.1.0");
	}

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}

public class MultipleMutationOperatorsTest() : InlineTest(
	"""
---
sub:
  version: "9.0.4"
  product: "Elasticsearch"
---

# Testing Multiple Mutation Operators

Version: {{version|M.M|lc}}
Version with spaces: {{version | M.M | lc}}
Product: {{product|uc}}
Product with spaces: {{product | uc}}
"""
)
{
	[Test]
	public void MultipleMutationOperatorsWorkWithAndWithoutSpaces()
	{
		// Both versions with and without spaces should render the same way
		Html
			.Should()
			.Contain("Version: 9.0")
			.And
			.Contain("Version with spaces: 9.0")
			.And
			.Contain("Product: ELASTICSEARCH")
			.And
			.Contain("Product with spaces: ELASTICSEARCH");
	}

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}

public class MutationOperatorsInLinksTest() : InlineTest(
	"""
---
sub:
  version: "9.0.4"
  product: "Elasticsearch"
---

# Testing Mutation Operators in Links

[Link with mutation operator](https://www.elastic.co/guide/en/elasticsearch/reference/{{version|M.M}}/index.html)
[Link with mutation operator and space](https://www.elastic.co/guide/en/elasticsearch/reference/{{version | M.M}}/index.html)
[Link text with mutation]({{product|uc}} {{version|M.M}})
[Link text with mutation and space]({{product | uc}} {{version | M.M}})

"""
)
{
	[Test]
	public void MutationOperatorsWorkInLinks()
	{
		// Check URL mutations
		Html
			.Should()
			.Contain("href=\"https://www.elastic.co/guide/en/elasticsearch/reference/9.0/index.html\"")
			.And
			.NotContain("{{version|M.M}}")
			.And
			.NotContain("{{version | M.M}}");

		// Check link text mutations
		Html.Should().Contain("ELASTICSEARCH 9.0").And.NotContain("{{product|uc}}").And.NotContain("{{version|M.M}}");

		// Check link text mutations with spaces
		Html.Should().Contain("ELASTICSEARCH 9.0").And.NotContain("{{product | uc}}").And.NotContain("{{version | M.M}}");
	}

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}

public class MutationOperatorsInCodeBlocksTest() : BlockTest<EnhancedCodeBlock>(
	"""
---
sub:
  version: "9.0.4"
  product: "Elasticsearch"
---

# Testing Mutation Operators in Code Blocks

```{code} sh subs=true
# Install Elasticsearch {{version|M.M}}
wget https://artifacts.elastic.co/downloads/elasticsearch/elasticsearch-{{version|M.M}}-linux-x86_64.tar.gz

# With space in mutation
wget https://artifacts.elastic.co/downloads/elasticsearch/elasticsearch-{{version | M.M}}-linux-x86_64.tar.gz
```
"""
)
{
	[Test]
	public void MutationOperatorsWorkInCodeBlocks() =>
		Html
			.Should()
			.Contain("# Install Elasticsearch 9.0")
			.And
			.Contain("elasticsearch-9.0-linux-x86_64.tar.gz")
			.And
			.NotContain("{{version|M.M}}")
			.And
			.NotContain("{{version | M.M}}");

	[Test]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}
