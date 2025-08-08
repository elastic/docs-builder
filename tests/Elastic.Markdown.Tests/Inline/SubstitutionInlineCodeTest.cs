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

Code with substitutions: {subs=true}`wget elasticsearch-{{version}}.tar.gz`

Multiple substitutions: {subs=true}`export {{env-var}}={{version}}`

With mutations: {subs=true}`version {{version | M.M}}`
"""
)
{
	[Fact]
	public void ProcessesSubstitutionsInInlineCode()
	{
		Html.Should()
			.Contain("<code>wget elasticsearch-{{version}}.tar.gz</code>") // Regular code should not process subs
			.And.Contain("<code>wget elasticsearch-8.15.0.tar.gz</code>") // Should process subs
			.And.Contain("<code>export MY_VAR=8.15.0</code>") // Multiple subs
			.And.Contain("<code>version 8.15</code>"); // Mutations
	}

	[Fact]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}
