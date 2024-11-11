// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
using Elastic.Markdown.Myst.Substitution;
using FluentAssertions;
using Xunit.Abstractions;

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
	public void GeneratesAttributesInHtml() =>
		Html.Should().Contain(
				"""Hello World!<br />"""
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
The following should be subbed: {{hello-world}}
not a comment
not a {{valid-key}}
not a {substitution}
"""
)
{

	[Fact]
	public void GeneratesAttributesInHtml() =>
		Html.Should().Contain(
				"""Hello World!<br />"""
			).And.Contain(
				"""not a comment"""
			)
			.And.NotContain(
				"""{{hello-world}}"""
			)
			.And.NotContain( // treated as attributes to the block
				"""{substitution}"""
			)
			.And.Contain(
				"""{{valid-key}}"""
			);
}
