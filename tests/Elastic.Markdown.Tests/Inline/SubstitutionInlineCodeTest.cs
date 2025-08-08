// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using FluentAssertions;

namespace Elastic.Markdown.Tests.Inline;

public class SubstitutionInlineCodeTest(ITestOutputHelper output) : InlineTest(output,
"""
---
sub:
  version: "8.15.0"
  env-var: "MY_VAR"
---

# Testing inline code substitutions

Regular code: `wget elasticsearch-{{version}}.tar.gz`

Code with substitutions: {subs}`wget elasticsearch-{{version}}.tar.gz`

Multiple substitutions: {subs}`export {{env-var}}={{version}}`

With mutations: {subs}`version {{version | M.M}}`
"""
)
{
	[Fact]
	public void TestSubstitutionInlineCode()
	{
		// Check that regular code blocks are not processed
		Html.Should().Contain("<code>wget elasticsearch-{{version}}.tar.gz</code>");

		// Check that {subs} inline code blocks have substitutions applied
		Html.Should().Contain("<code>wget elasticsearch-8.15.0.tar.gz</code>");
		Html.Should().Contain("<code>export MY_VAR=8.15.0</code>");
		Html.Should().Contain("<code>version 8.15</code>");
	}

	[Fact]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}
